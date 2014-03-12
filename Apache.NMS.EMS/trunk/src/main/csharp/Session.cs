/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;

namespace Apache.NMS.EMS
{
    /// <summary>
    /// Represents a NMS session to TIBCO.
    /// </summary>
    public class Session : Apache.NMS.ISession
    {
        public readonly TIBCO.EMS.Session tibcoSession;
        private bool closed = false;
        private bool disposed = false;

        public Session(TIBCO.EMS.Session session)
        {
            this.tibcoSession = session;
        }

        ~Session()
        {
            Dispose(false);
        }

        #region ISession Members

        public Apache.NMS.IMessageProducer CreateProducer()
        {
            return CreateProducer(null);
        }

        public Apache.NMS.IMessageProducer CreateProducer(Apache.NMS.IDestination destination)
        {
            Apache.NMS.EMS.Destination destinationObj = (Apache.NMS.EMS.Destination) destination;

            try
            {
                Apache.NMS.IMessageProducer producer = EMSConvert.ToNMSMessageProducer(this, this.tibcoSession.CreateProducer(destinationObj.tibcoDestination));
                ConfigureProducer(producer);
                return producer;
            }
            catch(Exception ex)
            {
                ExceptionUtil.WrapAndThrowNMSException(ex);
                return null;
            }
        }

        public Apache.NMS.IMessageConsumer CreateConsumer(Apache.NMS.IDestination destination)
        {
            Apache.NMS.EMS.Destination destinationObj = (Apache.NMS.EMS.Destination) destination;

            try
            {
                Apache.NMS.IMessageConsumer consumer = EMSConvert.ToNMSMessageConsumer(this, this.tibcoSession.CreateConsumer(destinationObj.tibcoDestination));
                ConfigureConsumer(consumer);
                return consumer;
            }
            catch(Exception ex)
            {
                ExceptionUtil.WrapAndThrowNMSException(ex);
                return null;
            }
        }

        public Apache.NMS.IMessageConsumer CreateConsumer(Apache.NMS.IDestination destination, string selector)
        {
            Apache.NMS.EMS.Destination destinationObj = (Apache.NMS.EMS.Destination) destination;

            try
            {
                Apache.NMS.IMessageConsumer consumer = EMSConvert.ToNMSMessageConsumer(this, this.tibcoSession.CreateConsumer(destinationObj.tibcoDestination, selector));
                ConfigureConsumer(consumer);
                return consumer;
            }
            catch(Exception ex)
            {
                ExceptionUtil.WrapAndThrowNMSException(ex);
                return null;
            }
        }

        public Apache.NMS.IMessageConsumer CreateConsumer(Apache.NMS.IDestination destination, string selector, bool noLocal)
        {
            Apache.NMS.EMS.Destination destinationObj = (Apache.NMS.EMS.Destination) destination;

            try
            {
                Apache.NMS.IMessageConsumer consumer = EMSConvert.ToNMSMessageConsumer(this, this.tibcoSession.CreateConsumer(destinationObj.tibcoDestination, selector, noLocal));
                ConfigureConsumer(consumer);
                return consumer;
            }
            catch(Exception ex)
            {
                ExceptionUtil.WrapAndThrowNMSException(ex);
                return null;
            }
        }

        public Apache.NMS.IMessageConsumer CreateDurableConsumer(Apache.NMS.ITopic destination, string name, string selector, bool noLocal)
        {
            Apache.NMS.EMS.Topic topicObj = (Apache.NMS.EMS.Topic) destination;

            try
            {
                Apache.NMS.IMessageConsumer consumer = EMSConvert.ToNMSMessageConsumer(this, this.tibcoSession.CreateDurableSubscriber(topicObj.tibcoTopic, name, selector, noLocal));
                ConfigureConsumer(consumer);
                return consumer;
            }
            catch(Exception ex)
            {
                ExceptionUtil.WrapAndThrowNMSException(ex);
                return null;
            }
        }

        private void ConfigureProducer(Apache.NMS.IMessageProducer producer)
        {
            producer.ProducerTransformer = this.ProducerTransformer;
        }

        private void ConfigureConsumer(Apache.NMS.IMessageConsumer consumer)
        {
            consumer.ConsumerTransformer = this.ConsumerTransformer;
        }

        public void DeleteDurableConsumer(string name)
        {
            try
            {
                this.tibcoSession.Unsubscribe(name);
            }
            catch(Exception ex)
            {
                ExceptionUtil.WrapAndThrowNMSException(ex);
            }
        }

        public IQueueBrowser CreateBrowser(IQueue queue)
        {
            Apache.NMS.EMS.Queue queueObj = (Apache.NMS.EMS.Queue) queue;

            try
            {
                return EMSConvert.ToNMSQueueBrowser(this.tibcoSession.CreateBrowser(queueObj.tibcoQueue));
            }
            catch(Exception ex)
            {
                ExceptionUtil.WrapAndThrowNMSException(ex);
                return null;
            }
        }

        public IQueueBrowser CreateBrowser(IQueue queue, string selector)
        {
            Apache.NMS.EMS.Queue queueObj = (Apache.NMS.EMS.Queue) queue;

            try
            {
                return EMSConvert.ToNMSQueueBrowser(this.tibcoSession.CreateBrowser(queueObj.tibcoQueue, selector));
            }
            catch(Exception ex)
            {
                ExceptionUtil.WrapAndThrowNMSException(ex);
                return null;
            }
        }

        public Apache.NMS.IQueue GetQueue(string name)
        {
            try
            {
                return EMSConvert.ToNMSQueue(this.tibcoSession.CreateQueue(name));
            }
            catch(Exception ex)
            {
                ExceptionUtil.WrapAndThrowNMSException(ex);
                return null;
            }
        }

        public Apache.NMS.ITopic GetTopic(string name)
        {
            try
            {
                return EMSConvert.ToNMSTopic(this.tibcoSession.CreateTopic(name));
            }
            catch(Exception ex)
            {
                ExceptionUtil.WrapAndThrowNMSException(ex);
                return null;
            }
        }

        public Apache.NMS.ITemporaryQueue CreateTemporaryQueue()
        {
            try
            {
                return EMSConvert.ToNMSTemporaryQueue(this.tibcoSession.CreateTemporaryQueue());
            }
            catch(Exception ex)
            {
                ExceptionUtil.WrapAndThrowNMSException(ex);
                return null;
            }
        }

        public Apache.NMS.ITemporaryTopic CreateTemporaryTopic()
        {
            try
            {
                return EMSConvert.ToNMSTemporaryTopic(this.tibcoSession.CreateTemporaryTopic());
            }
            catch(Exception ex)
            {
                ExceptionUtil.WrapAndThrowNMSException(ex);
                return null;
            }
        }

        /// <summary>
        /// Delete a destination (Queue, Topic, Temp Queue, Temp Topic).
        /// </summary>
        public void DeleteDestination(IDestination destination)
        {
            // TODO: Implement if possible.
        }

        public Apache.NMS.IMessage CreateMessage()
        {
            try
            {
                return EMSConvert.ToNMSMessage(this.tibcoSession.CreateMessage());
            }
            catch(Exception ex)
            {
                ExceptionUtil.WrapAndThrowNMSException(ex);
                return null;
            }
        }

        public Apache.NMS.ITextMessage CreateTextMessage()
        {
            try
            {
                return EMSConvert.ToNMSTextMessage(this.tibcoSession.CreateTextMessage());
            }
            catch(Exception ex)
            {
                ExceptionUtil.WrapAndThrowNMSException(ex);
                return null;
            }
        }

        public Apache.NMS.ITextMessage CreateTextMessage(string text)
        {
            try
            {
                return EMSConvert.ToNMSTextMessage(this.tibcoSession.CreateTextMessage(text));
            }
            catch(Exception ex)
            {
                ExceptionUtil.WrapAndThrowNMSException(ex);
                return null;
            }
        }

        public Apache.NMS.IMapMessage CreateMapMessage()
        {
            try
            {
                return EMSConvert.ToNMSMapMessage(this.tibcoSession.CreateMapMessage());
            }
            catch(Exception ex)
            {
                ExceptionUtil.WrapAndThrowNMSException(ex);
                return null;
            }
        }

        public Apache.NMS.IBytesMessage CreateBytesMessage()
        {
            try
            {
                return EMSConvert.ToNMSBytesMessage(this.tibcoSession.CreateBytesMessage());
            }
            catch(Exception ex)
            {
                ExceptionUtil.WrapAndThrowNMSException(ex);
                return null;
            }
        }

        public Apache.NMS.IBytesMessage CreateBytesMessage(byte[] body)
        {
            try
            {
                Apache.NMS.IBytesMessage bytesMessage = CreateBytesMessage();

                if(null != bytesMessage)
                {
                    bytesMessage.Content = body;
                }

                return bytesMessage;
            }
            catch(Exception ex)
            {
                ExceptionUtil.WrapAndThrowNMSException(ex);
                return null;
            }
        }

        public Apache.NMS.IStreamMessage CreateStreamMessage()
        {
            try
            {
                return EMSConvert.ToNMSStreamMessage(this.tibcoSession.CreateStreamMessage());
            }
            catch(Exception ex)
            {
                ExceptionUtil.WrapAndThrowNMSException(ex);
                return null;
            }
        }

        public Apache.NMS.IObjectMessage CreateObjectMessage(Object body)
        {
            try
            {
                return EMSConvert.ToNMSObjectMessage(this.tibcoSession.CreateObjectMessage(body));
            }
            catch(Exception ex)
            {
                ExceptionUtil.WrapAndThrowNMSException(ex);
                return null;
            }
        }

        public void Commit()
        {
            try
            {
                this.tibcoSession.Commit();
            }
            catch(Exception ex)
            {
                ExceptionUtil.WrapAndThrowNMSException(ex);
            }
        }

        public void Rollback()
        {
            try
            {
                this.tibcoSession.Rollback();
            }
            catch(Exception ex)
            {
                ExceptionUtil.WrapAndThrowNMSException(ex);
            }
        }

        public void Recover()
        {
            throw new NotSupportedException();
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

        #region Transaction State Events

        #pragma warning disable 0067
        public event SessionTxEventDelegate TransactionStartedListener;
        public event SessionTxEventDelegate TransactionCommittedListener;
        public event SessionTxEventDelegate TransactionRolledBackListener;
        #pragma warning restore 0067

        #endregion

        // Properties

        /// <summary>
        /// The default timeout for network requests.
        /// </summary>
        private TimeSpan requestTimeout = Apache.NMS.NMSConstants.defaultRequestTimeout;
        public TimeSpan RequestTimeout
        {
            get { return this.requestTimeout; }
            set { this.requestTimeout = value; }
        }

        public bool Transacted
        {
            get { return this.tibcoSession.Transacted; }
        }

        public Apache.NMS.AcknowledgementMode AcknowledgementMode
        {
            get { return EMSConvert.ToAcknowledgementMode(this.tibcoSession.SessionAcknowledgeMode); }
        }

        public void Close()
        {
            lock(this)
            {
                if(closed)
                {
                    return;
                }

                try
                {
                    this.tibcoSession.Close();
                }
                catch(Exception ex)
                {
                    ExceptionUtil.WrapAndThrowNMSException(ex);
                }
                finally
                {
                    closed = true;
                }
            }
        }

        #endregion

        #region IDisposable Members

        ///<summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        ///</summary>
        ///<filterpriority>2</filterpriority>
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

            if(disposing)
            {
                // Dispose managed code here.
            }

            try
            {
                Close();
            }
            catch
            {
                // Ignore errors.
            }

            disposed = true;
        }

        #endregion
    }
}
