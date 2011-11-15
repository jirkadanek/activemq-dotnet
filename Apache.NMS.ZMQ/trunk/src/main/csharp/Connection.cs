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
using ZContext = ZMQ.Context;

namespace Apache.NMS.ZMQ
{
    /// <summary>
    /// Represents a NMS connection ZMQ.
    /// </summary>
    ///
    public class Connection : IConnection
    {
        private AcknowledgementMode acknowledgementMode = AcknowledgementMode.AutoAcknowledge;
        private IRedeliveryPolicy redeliveryPolicy;
        private ConnectionMetaData metaData = null;
        private bool closed = true;
        private string clientId;
        private Uri brokerUri;

        /// <summary>
        /// ZMQ context
        /// </summary>
        static private ZContext _context = new ZContext(1);

        /// <summary>
        /// Starts message delivery for this connection.
        /// </summary>
        public void Start()
        {
            closed = false;
        }

        /// <summary>
        /// This property determines if the asynchronous message delivery of incoming
        /// messages has been started for this connection.
        /// </summary>
        public bool IsStarted
        {
            get { return !closed; }
        }

        /// <summary>
        /// Stop message delivery for this connection.
        /// </summary>
        public void Stop()
        {
            closed = true;
        }

        /// <summary>
        /// Creates a new session to work on this connection
        /// </summary>
        public ISession CreateSession()
        {
            return CreateSession(acknowledgementMode);
        }

        /// <summary>
        /// Creates a new session to work on this connection
        /// </summary>
        public ISession CreateSession(AcknowledgementMode mode)
        {
            return new Session(this, mode);
        }

        public void Dispose()
        {
            Close();
        }

        public void Close()
        {
            Stop();
        }

        public void PurgeTempDestinations()
        {
        }

        /// <summary>
        /// The default timeout for network requests.
        /// </summary>
        public TimeSpan RequestTimeout
        {
            get { return NMSConstants.defaultRequestTimeout; }
            set { }
        }

        public AcknowledgementMode AcknowledgementMode
        {
            get { return acknowledgementMode; }
            set { acknowledgementMode = value; }
        }

        /// <summary>
        /// Get/or set the broker Uri.
        /// </summary>
        public Uri BrokerUri
        {
            get { return brokerUri; }
            set { brokerUri = value; }
        }

        /// <summary>
        /// Get/or set the client Id
        /// </summary>
        public string ClientId
        {
            get { return clientId; }
            set { clientId = value; }
        }

        /// <summary>
        /// Get/or set the redelivery policy for this connection.
        /// </summary>
        public IRedeliveryPolicy RedeliveryPolicy
        {
            get { return this.redeliveryPolicy; }
            set { this.redeliveryPolicy = value; }
        }

        private ConsumerTransformerDelegate consumerTransformer;
        public ConsumerTransformerDelegate ConsumerTransformer
        {
            get { return this.consumerTransformer; }
            set { this.consumerTransformer = value; }
        }

        private ProducerTransformerDelegate producerTransformer;
        public ProducerTransformerDelegate ProducerTransformer
        {
            get { return this.producerTransformer; }
            set { this.producerTransformer = value; }
        }

        /// <summary>
        /// Gets ZMQ connection context
        /// </summary>
        static internal ZContext Context
        {
            get { return _context; }
        }

        /// <summary>
        /// Gets the Meta Data for the NMS Connection instance.
        /// </summary>
        public IConnectionMetaData MetaData
        {
            get { return this.metaData ?? (this.metaData = new ConnectionMetaData()); }
        }

        /// <summary>
        /// A delegate that can receive transport level exceptions.
        /// </summary>
        public event ExceptionListener ExceptionListener;

        /// <summary>
        /// An asynchronous listener that is notified when a Fault tolerant connection
        /// has been interrupted.
        /// </summary>
        public event ConnectionInterruptedListener ConnectionInterruptedListener;

        /// <summary>
        /// An asynchronous listener that is notified when a Fault tolerant connection
        /// has been resumed.
        /// </summary>
        public event ConnectionResumedListener ConnectionResumedListener;

        public void HandleException(System.Exception e)
        {
            if(ExceptionListener != null && !this.closed)
            {
                ExceptionListener(e);
            }
            else
            {
                Tracer.Error(e);
            }
        }

        public void HandleTransportInterrupted()
        {
            Tracer.Debug("Transport has been Interrupted.");

            if(this.ConnectionInterruptedListener != null && !this.closed)
            {
                try
                {
                    this.ConnectionInterruptedListener();
                }
                catch
                {
                }
            }
        }

        public void HandleTransportResumed()
        {
            Tracer.Debug("Transport has resumed normal operation.");

            if(this.ConnectionResumedListener != null && !this.closed)
            {
                try
                {
                    this.ConnectionResumedListener();
                }
                catch
                {
                }
            }
        }
    }
}
