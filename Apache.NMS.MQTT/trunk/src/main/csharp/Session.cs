//
// Licensed to the Apache Software Foundation (ASF) under one or more
// contributor license agreements.  See the NOTICE file distributed with
// this work for additional information regarding copyright ownership.
// The ASF licenses this file to You under the Apache License, Version 2.0
// (the "License"); you may not use this file except in compliance with
// the License.  You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
using System;
using System.Collections;
using System.Threading;
using Apache.NMS.MQTT.Messages;
using Apache.NMS.MQTT.Util;

namespace Apache.NMS.MQTT
{
	public class Session : ISession, IDispatcher
	{
        /// <summary>
        /// Private object used for synchronization, instead of public "this"
        /// </summary>
        private readonly object myLock = new object();

        private readonly IDictionary consumers = Hashtable.Synchronized(new Hashtable());
        private readonly IDictionary producers = Hashtable.Synchronized(new Hashtable());

        private readonly SessionExecutor executor;
        private readonly Connection connection;

        private int consumerCounter;
        private int producerCounter;

        protected bool disposed = false;
        protected bool closed = false;
        protected bool closing = false;
		private int sessionId;

        private readonly AcknowledgementMode acknowledgementMode;
        private TimeSpan disposeStopTimeout = TimeSpan.FromMilliseconds(30000);
        private TimeSpan closeStopTimeout = TimeSpan.FromMilliseconds(Timeout.Infinite);
        private TimeSpan requestTimeout;

		public Session(Connection connection, AcknowledgementMode acknowledgementMode, int sessionId)
		{
            this.connection = connection;
            this.acknowledgementMode = acknowledgementMode;

            this.ConsumerTransformer = connection.ConsumerTransformer;
            this.ProducerTransformer = connection.ProducerTransformer;

            this.executor = new SessionExecutor(this, this.consumers);
			this.sessionId = sessionId;

            if(connection.IsStarted)
            {
                this.Start();
            }

            connection.AddSession(this);
		}

        ~Session()
        {
            Dispose(false);
        }

        #region Session Transaction Events

        // We delegate the events to the TransactionContext since it knows
        // what the state is for both Local and DTC transactions.

        public event SessionTxEventDelegate TransactionStartedListener
        {
			add { throw new NotSupportedException("MQTT Does not implement transactions"); }
            remove { throw new NotSupportedException("MQTT Does not implement transactions"); }
        }

        public event SessionTxEventDelegate TransactionCommittedListener
        {
            add { throw new NotSupportedException("MQTT Does not implement transactions"); }
            remove { throw new NotSupportedException("MQTT Does not implement transactions"); }
        }

        public event SessionTxEventDelegate TransactionRolledBackListener
        {
            add { throw new NotSupportedException("MQTT Does not implement transactions"); }
            remove { throw new NotSupportedException("MQTT Does not implement transactions"); }
        }

        #endregion

        #region Property Accessors

        public virtual AcknowledgementMode AcknowledgementMode
        {
            get { return this.acknowledgementMode; }
        }

        public virtual bool IsClientAcknowledge
        {
            get { return this.acknowledgementMode == AcknowledgementMode.ClientAcknowledge; }
        }

        public virtual bool IsAutoAcknowledge
        {
            get { return this.acknowledgementMode == AcknowledgementMode.AutoAcknowledge; }
        }

        public virtual bool IsDupsOkAcknowledge
        {
            get { return this.acknowledgementMode == AcknowledgementMode.DupsOkAcknowledge; }
        }

        public virtual bool IsIndividualAcknowledge
        {
            get { return this.acknowledgementMode == AcknowledgementMode.IndividualAcknowledge; }
        }

        public virtual bool IsTransacted
        {
            get{ return this.acknowledgementMode == AcknowledgementMode.Transactional; }
        }

        public Connection Connection
        {
            get { return this.connection; }
        }

        public bool Transacted
        {
            get { return this.IsTransacted; }
        }

        public SessionExecutor Executor
        {
            get { return this.executor; }
        }

        public TimeSpan RequestTimeout
        {
            get { return this.requestTimeout; }
            set { this.requestTimeout = value; }
        }

		public int SessionId
		{
			get { return this.sessionId; }
		}

        private ConsumerTransformerDelegate consumerTransformer;
        /// <summary>
        /// A Delegate that is called each time a Message is dispatched to allow the client to do
        /// any necessary transformations on the received message before it is delivered.
        /// The Session instance sets the delegate on each Consumer it creates.
        /// </summary>
        public ConsumerTransformerDelegate ConsumerTransformer
        {
            get { return this.consumerTransformer; }
            set { this.consumerTransformer = value; }
        }

        private ProducerTransformerDelegate producerTransformer;
        /// <summary>
        /// A delegate that is called each time a Message is sent from this Producer which allows
        /// the application to perform any needed transformations on the Message before it is sent.
        /// The Session instance sets the delegate on each Producer it creates.
        /// </summary>
        public ProducerTransformerDelegate ProducerTransformer
        {
            get { return this.producerTransformer; }
            set { this.producerTransformer = value; }
        }

		#endregion

        #region ISession Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if(this.disposed)
            {
                return;
            }

            try
            {
                // Force a Stop when we are Disposing vs a Normal Close.
                this.executor.Stop(this.disposeStopTimeout);

                Close();
            }
            catch
            {
                // Ignore network errors.
            }

            this.disposed = true;
        }

        public virtual void Close()
        {
            if (!this.closed)
            {
                try
                {
                    Tracer.InfoFormat("Closing The Session with Id {0}", this.sessionId);
                    DoClose();
                    Tracer.InfoFormat("Closed The Session with Id {0}", this.sessionId);
                }
                catch (Exception ex)
                {
                    Tracer.ErrorFormat("Error during session close: {0}", ex);
                }
            }
        }

		internal void DoClose()
		{
			Shutdown();
		}

        internal void Shutdown()
        {
            Tracer.InfoFormat("Executing Shutdown on Session with Id {0}", this.SessionId);

            if(this.closed)
            {
                return;
            }

            lock(myLock)
            {
                if(this.closed || this.closing)
                {
                    return;
                }

                try
                {
                    this.closing = true;

                    // Stop all message deliveries from this Session
                    this.executor.Stop(this.closeStopTimeout);

                    lock(consumers.SyncRoot)
                    {
                        foreach(MessageConsumer consumer in consumers.Values)
                        {
                            consumer.FailureError = this.connection.FirstFailureError;
                            consumer.Shutdown();
                        }
                    }
                    consumers.Clear();

                    lock(producers.SyncRoot)
                    {
                        foreach(MessageProducer producer in producers.Values)
                        {
                            producer.Shutdown();
                        }
                    }
                    producers.Clear();

                    Connection.RemoveSession(this);
                }
                catch(Exception ex)
                {
                    Tracer.ErrorFormat("Error during session close: {0}", ex);
                }
                finally
                {
                    this.closed = true;
                    this.closing = false;
                }
            }
        }

        public IMessageProducer CreateProducer()
        {
            return CreateProducer(null);
        }

        public IMessageProducer CreateProducer(IDestination destination)
        {
            MessageProducer producer = null;
			// TODO
            return producer;
        }

        public IMessageConsumer CreateConsumer(IDestination destination)
        {
            return CreateConsumer(destination, null, false);
        }

        public IMessageConsumer CreateConsumer(IDestination destination, string selector)
        {
            return CreateConsumer(destination, selector, false);
        }

        public IMessageConsumer CreateConsumer(IDestination destination, string selector, bool noLocal)
        {
            if(destination == null)
            {
                throw new InvalidDestinationException("Cannot create a Consumer with a Null destination");
            }

            MessageConsumer consumer = null;
            return consumer;
        }

        public IMessageConsumer CreateDurableConsumer(ITopic destination, string name, string selector, bool noLocal)
        {
            if(destination == null)
            {
                throw new InvalidDestinationException("Cannot create a Consumer with a Null destination");
            }

            MessageConsumer consumer = null;
            return consumer;
        }

        public IQueueBrowser CreateBrowser(IQueue queue)
        {
            throw new NotSupportedException("Not supported with MQTT Protocol");
        }

        public IQueueBrowser CreateBrowser(IQueue queue, string selector)
        {
            throw new NotSupportedException("Not supported with MQTT Protocol");
        }

        public IQueue GetQueue(string name)
        {
            throw new NotSupportedException("Not supported with MQTT Protocol");
        }

        public ITopic GetTopic(string name)
        {
			return new Topic(name);
        }

        public ITemporaryQueue CreateTemporaryQueue()
        {
            throw new NotSupportedException("Not supported with MQTT Protocol");
        }

        public ITemporaryTopic CreateTemporaryTopic()
        {
            throw new NotSupportedException("Not supported with MQTT Protocol");
        }

        public void DeleteDurableConsumer(string name)
        {
            throw new NotSupportedException("MQTT Cannot delete Durable Consumers");
        }

        /// <summary>
        /// Delete a destination (Queue, Topic, Temp Queue, Temp Topic).
        /// </summary>
        public void DeleteDestination(IDestination destination)
        {
            throw new NotSupportedException("MQTT Cannot delete Destinations");
        }

        public IMessage CreateMessage()
        {
            throw new NotSupportedException("No empty Message in MQTT");
        }

        public ITextMessage CreateTextMessage()
        {
            throw new NotSupportedException("No Text Message in MQTT");
        }

        public ITextMessage CreateTextMessage(string text)
        {
            throw new NotSupportedException("No Text Message in MQTT");
        }

        public IMapMessage CreateMapMessage()
        {
            throw new NotSupportedException("No Map Message in MQTT");
        }

        public IBytesMessage CreateBytesMessage()
        {
            return ConfigureMessage(new BytesMessage()) as IBytesMessage;
        }

        public IBytesMessage CreateBytesMessage(byte[] body)
        {
            BytesMessage answer = new BytesMessage();
            answer.Content = body;
            return ConfigureMessage(answer) as IBytesMessage;
        }

        public IStreamMessage CreateStreamMessage()
        {
            throw new NotSupportedException("No Object Message in MQTT");
        }

        public IObjectMessage CreateObjectMessage(object body)
        {
            throw new NotSupportedException("No Object Message in MQTT");
        }

        public void Commit()
        {
            throw new NotSupportedException("No Transaction support in MQTT");
        }

        public void Rollback()
        {
            throw new NotSupportedException("No Transaction support in MQTT");
        }

        public void Recover()
        {
            CheckClosed();

            if (acknowledgementMode == AcknowledgementMode.Transactional)
            {
                throw new IllegalStateException("Cannot Recover a Transacted Session");
            }

            throw new NotSupportedException("No Recover support yet");
        }

        public void Stop()
        {
            if(this.executor != null)
            {
                this.executor.Stop();
            }
        }

        public void Start()
        {
            foreach(MessageConsumer consumer in this.consumers.Values)
            {
                consumer.Start();
            }

            if(this.executor != null)
            {
                this.executor.Start();
            }
        }

        public bool Started
        {
            get
            {
                return this.executor != null ? this.executor.Running : false;
            }
        }

		#endregion

        public void AddConsumer(MessageConsumer consumer)
        {
            if(!this.closing)
            {
                int id = consumer.ConsumerId;

                // Registered with Connection before we register at the broker.
                consumers[id] = consumer;
                //connection.AddDispatcher(id, this);
            }
        }

        public void RemoveConsumer(MessageConsumer consumer)
        {
            //connection.RemoveDispatcher(consumer.ConsumerId);
            if(!this.closing)
            {
                consumers.Remove(consumer.ConsumerId);
            }
			connection.RemoveDispatcher(consumer);
        }

        public void AddProducer(MessageProducer producer)
        {
            if(!this.closing)
            {
                int id = producer.ProducerId;

                this.producers[id] = producer;
                //this.connection.AddProducer(id, producer);
            }
        }

        public void RemoveProducer(int producerId)
        {
            connection.RemoveProducer(producerId);
            if(!this.closing)
            {
                producers.Remove(producerId);
            }
        }

        internal void Redispatch(MessageDispatchChannel channel)
        {
            MessageDispatch[] messages = channel.RemoveAll();
            System.Array.Reverse(messages);

            foreach(MessageDispatch message in messages)
            {
                this.executor.ExecuteFirst(message);
            }
        }

        public void Dispatch(MessageDispatch dispatch)
        {
            if(this.executor != null)
            {
                this.executor.Execute(dispatch);
            }
        }

        private MQTTMessage ConfigureMessage(MQTTMessage message)
        {
            message.Connection = this.connection;
            return message;
        }

        private void CheckClosed()
        {
            if (closed)
            {
                throw new IllegalStateException("Session is Closed");
            }
        }

        /// <summary>
        /// Prevents message from throwing an exception if a client calls Acknoweldge on
        /// a message that is part of a transaction either being produced or consumed.  The
        /// JMS Spec indicates that users should be able to call Acknowledge with no effect
        /// if the message is in a transaction.
        /// </summary>
        /// <param name="message">
        /// A <see cref="MQTTMessage"/>
        /// </param>
        private static void DoNothingAcknowledge(MQTTMessage message)
        {
        }

		private int NextConsumerId
		{
			get { return Interlocked.Increment(ref this.consumerCounter); }
		}

		private int NextProducerId
		{
			get { return Interlocked.Increment(ref this.producerCounter); }
		}

	}
}

