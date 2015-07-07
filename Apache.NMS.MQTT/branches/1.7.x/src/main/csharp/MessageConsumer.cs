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
using System.Threading;
using System.Collections.Generic;
using System.Collections.Specialized;
using Apache.NMS.MQTT.Messages;
using Apache.NMS.MQTT.Util;
using Apache.NMS.MQTT.Threads;
using Apache.NMS.Util;

namespace Apache.NMS.MQTT
{
	public class MessageConsumer : IMessageConsumer, IDispatcher
	{
        private readonly Session session;
        private readonly MessageTransformation messageTransformation;
        private readonly MessageDispatchChannel unconsumedMessages;
        private readonly LinkedList<MessageDispatch> dispatchedMessages = new LinkedList<MessageDispatch>();

		private readonly Atomic<bool> started = new Atomic<bool>();

        private Exception failureError;
		private ThreadPoolExecutor executor;
		private int consumerId;
		protected bool disposed = false;
		private Topic destination = null;

		private event MessageListener listener;

		public MessageConsumer(Session session, Topic destination, int consumerId)
		{
			if(destination == null)
			{
				throw new InvalidDestinationException("Consumer cannot receive on Null Destinations.");
            }
            else if(destination.TopicName == null)
            {
                throw new InvalidDestinationException("The destination object was not given a physical name.");
            }

			this.session = session;
			this.consumerId = consumerId;
			this.destination = destination;
			this.messageTransformation = this.session.Connection.MessageTransformation;
			this.unconsumedMessages = new FifoMessageDispatchChannel();

			// If the destination contained a URI query, then use it to set public properties
			// on the ConsumerInfo
			if(destination.Options != null)
			{
				// Get options prefixed with "consumer.*"
				StringDictionary options = URISupport.GetProperties(destination.Options, "consumer.");
				// Extract out custom extension options "consumer.nms.*"
				StringDictionary customConsumerOptions = URISupport.ExtractProperties(options, "nms.");

				URISupport.SetProperties(this, options);
				URISupport.SetProperties(this, customConsumerOptions, "nms.");
			}
		}

		#region Property Accessors

        public Exception FailureError
        {
            get { return this.failureError; }
            set { this.failureError = value; }
        }

		private ConsumerTransformerDelegate consumerTransformer;
		/// <summary>
		/// A Delegate that is called each time a Message is dispatched to allow the client to do
		/// any necessary transformations on the received message before it is delivered.
		/// </summary>
		public ConsumerTransformerDelegate ConsumerTransformer
		{
			get { return this.consumerTransformer; }
			set { this.consumerTransformer = value; }
		}


		public event MessageListener Listener
		{
			add
			{
				CheckClosed();

				bool wasStarted = this.session.Started;

				if(wasStarted)
				{
					this.session.Stop();
				}

				listener += value;
				this.session.Redispatch(this.unconsumedMessages);

				if(wasStarted)
				{
					this.session.Start();
				}
			}
			remove { listener -= value; }
		}

		public int ConsumerId
		{
			get { return this.consumerId; }
		}

		public Topic Destination
		{
			get { return this.destination; }
		}

		#endregion

		public void Start()
		{
			if(this.unconsumedMessages.Closed)
			{
				return;
			}

			this.started.Value = true;
			this.unconsumedMessages.Start();
			this.session.Executor.Wakeup();
		}

		public void Stop()
		{
			this.started.Value = false;
			this.unconsumedMessages.Stop();
		}

		public virtual void Close()
		{
			if(!this.unconsumedMessages.Closed)
			{
                Tracer.DebugFormat("Consumer {0} closing normally.", this.ConsumerId);
                this.DoClose();
			}
		}

		internal void DoClose()
		{
	        Shutdown();
	        //this.session.Connection.Oneway(removeCommand);
		}

		/// <summary>
		/// Called from the parent Session of this Consumer to indicate that its
		/// parent session is closing and this Consumer should close down but not
		/// send any message to the Broker as the parent close will take care of
		/// removing its child resources at the broker.
		/// </summary>
		internal void Shutdown()
		{
			if(!this.unconsumedMessages.Closed)
			{
				if(Tracer.IsDebugEnabled)
				{
					Tracer.DebugFormat("Shutdown of Consumer[{0}] started.", ConsumerId);
				}

				// Do we have any acks we need to send out before closing?
				// Ack any delivered messages now.
				if(!this.session.IsTransacted)
				{
				}

	            if (this.executor != null) 
				{
					this.executor.Shutdown();
					this.executor.AwaitTermination(TimeSpan.FromMinutes(1));
					this.executor = null;
	            }

	            if (this.session.IsClientAcknowledge)
				{
	            }

				if(!this.session.IsTransacted)
				{
					lock(this.dispatchedMessages)
					{
						dispatchedMessages.Clear();
					}
				}

				this.session.RemoveConsumer(this);
				this.unconsumedMessages.Close();

				if(Tracer.IsDebugEnabled)
				{
					Tracer.DebugFormat("Shutdown of Consumer[{0}] completed.", ConsumerId);
				}
			}			
		}

		public bool Iterate()
		{
			if(this.listener != null)
			{
				MessageDispatch dispatch = this.unconsumedMessages.DequeueNoWait();
				if(dispatch != null)
				{
					this.Dispatch(dispatch);
					return true;
				}
			}

			return false;
		}

		public IMessage Receive()
		{
			CheckClosed();
			CheckMessageListener();

			MessageDispatch dispatch = this.Dequeue(TimeSpan.FromMilliseconds(-1));

			if(dispatch == null)
			{
				return null;
			}

			BeforeMessageIsConsumed(dispatch);
			AfterMessageIsConsumed(dispatch, false);

			return CreateMQTTMessage(dispatch);
		}

		public IMessage Receive(TimeSpan timeout)
		{
			CheckClosed();
			CheckMessageListener();

			MessageDispatch dispatch = null;
			dispatch = this.Dequeue(timeout);

			if(dispatch == null)
			{
				return null;
			}

			BeforeMessageIsConsumed(dispatch);
			AfterMessageIsConsumed(dispatch, false);

			return CreateMQTTMessage(dispatch);
		}

		public IMessage ReceiveNoWait()
		{
			CheckClosed();
			CheckMessageListener();

			MessageDispatch dispatch = null;
			dispatch = this.Dequeue(TimeSpan.Zero);

			if(dispatch == null)
			{
				return null;
			}

			BeforeMessageIsConsumed(dispatch);
			AfterMessageIsConsumed(dispatch, false);

			return CreateMQTTMessage(dispatch);
		}

		public virtual void Dispatch(MessageDispatch dispatch)
		{
		}

		/// <summary>
		/// Used to get an enqueued message from the unconsumedMessages list. The
		/// amount of time this method blocks is based on the timeout value.  if
		/// timeout == Timeout.Infinite then it blocks until a message is received.
		/// if timeout == 0 then it it tries to not block at all, it returns a
		/// message if it is available if timeout > 0 then it blocks up to timeout
		/// amount of time.  Expired messages will consumed by this method.
		/// </summary>
		/// <param name="timeout">
		/// A <see cref="System.TimeSpan"/>
		/// </param>
		/// <returns>
		/// A <see cref="MessageDispatch"/>
		/// </returns>
		private MessageDispatch Dequeue(TimeSpan timeout)
		{
			DateTime deadline = DateTime.Now;

			if(timeout > TimeSpan.Zero)
			{
				deadline += timeout;
			}

			while(true)
			{
				MessageDispatch dispatch = this.unconsumedMessages.Dequeue(timeout);

				// Grab a single date/time for calculations to avoid timing errors.
				DateTime dispatchTime = DateTime.Now;

				if(dispatch == null)
				{
					if(timeout > TimeSpan.Zero && !this.unconsumedMessages.Closed)
					{
						if(dispatchTime > deadline)
						{
							// Out of time.
							timeout = TimeSpan.Zero;
						}
						else
						{
							// Adjust the timeout to the remaining time.
							timeout = deadline - dispatchTime;
						}
					}
					else
					{
                        // Informs the caller of an error in the event that an async exception
                        // took down the parent connection.
                        if(this.failureError != null)
                        {
                            throw NMSExceptionSupport.Create(this.failureError);
                        }

						return null;
					}
				}
				else if(dispatch.Message == null)
				{
					return null;
				}
				else
				{
					return dispatch;
				}
			}
		}

		public virtual void BeforeMessageIsConsumed(MessageDispatch dispatch)
		{
		}

		public virtual void AfterMessageIsConsumed(MessageDispatch dispatch, bool expired)
		{
			if(this.unconsumedMessages.Closed)
			{
				return;
			}
		}

		private void CheckClosed()
		{
			if(this.unconsumedMessages.Closed)
			{
				throw new NMSException("The Consumer has been Closed");
			}
		}

		private void CheckMessageListener()
		{
			if(this.listener != null)
			{
				throw new NMSException("Cannot perform a Sync receive on a MessageConsumer that has an async listener");
			}
		}

	    protected bool IsAutoAcknowledge
		{
			get { return this.session.IsAutoAcknowledge; }
		}

        protected bool IsIndividualAcknowledge
		{
			get { return this.session.IsIndividualAcknowledge; }
		}

        protected bool IsClientAcknowledge
		{
			get { return this.session.IsClientAcknowledge; }
		}

		private MQTTMessage CreateMQTTMessage(MessageDispatch dispatch)
		{
			MQTTMessage message = dispatch.Message.Clone() as MQTTMessage;

			if(this.ConsumerTransformer != null)
			{
				IMessage newMessage = ConsumerTransformer(this.session, this, message);
				if(newMessage != null)
				{
					message = this.messageTransformation.TransformMessage<MQTTMessage>(newMessage);
				}
			}

			message.Connection = this.session.Connection;

			if(IsClientAcknowledge)
			{
				message.Acknowledger += new AcknowledgeHandler(DoClientAcknowledge);
			}
			else if(IsIndividualAcknowledge)
			{
				message.Acknowledger += new AcknowledgeHandler(DoIndividualAcknowledge);
			}
			else
			{
				message.Acknowledger += new AcknowledgeHandler(DoNothingAcknowledge);
			}

			return message;
		}

		protected void DoIndividualAcknowledge(MQTTMessage message)
		{
			MessageDispatch dispatch = null;

			lock(this.dispatchedMessages)
			{
				foreach(MessageDispatch originalDispatch in this.dispatchedMessages)
				{
					if(originalDispatch.Message.MessageId.Equals(message.MessageId))
					{
						dispatch = originalDispatch;
						this.dispatchedMessages.Remove(originalDispatch);
						break;
					}
				}
			}

			if(dispatch == null)
			{
				Tracer.DebugFormat("Attempt to Ack MessageId[{0}] failed because the original dispatch is not in the Dispatch List", message.MessageId);
				return;
			}

//			MessageAck ack = new MessageAck(dispatch, (byte) AckType.IndividualAck, 1);
//			Tracer.Debug("Sending Individual Ack for MessageId: " + ack.LastMessageId.ToString());
//			this.session.SendAck(ack);
		}

		protected void DoNothingAcknowledge(MQTTMessage message)
		{
		}

		protected void DoClientAcknowledge(MQTTMessage message)
		{
			this.CheckClosed();
			Tracer.Debug("Sending Client Ack:");
//			this.session.Acknowledge();
		}

	    internal bool Closed
	    {
            get { return this.unconsumedMessages.Closed; }
	    }

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected void Dispose(bool disposing)
		{
			if(disposed)
			{
				return;
			}

			try
			{
				Close();
			}
			catch
			{
				// Ignore network errors.
			}

			disposed = true;
		}
	}
}

