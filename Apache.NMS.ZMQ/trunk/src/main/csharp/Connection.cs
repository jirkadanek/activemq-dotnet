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
using ZeroMQ;
using System.Collections.Generic;
using System.Text;

namespace Apache.NMS.ZMQ
{
    /// <summary>
    /// Represents a NMS connection ZMQ.
    /// </summary>
    ///
    public class Connection : IConnection
    {
		private class ProducerRef
		{
			public ZmqSocket producer = null;
			public int refCount = 1;
		}

        private AcknowledgementMode acknowledgementMode = AcknowledgementMode.AutoAcknowledge;
        private IRedeliveryPolicy redeliveryPolicy;
        private ConnectionMetaData metaData = null;
        private bool closed = true;
        private string clientId;
        private Uri brokerUri;
		private string producerContextBinding;
		private string consumerContextBinding;

        /// <summary>
        /// ZMQ context
        /// </summary>
		private static ZmqContext _context;
		private static Dictionary<string, ProducerRef> producerCache;
		private static object producerCacheLock;

		static Connection()
		{
			Connection._context = ZmqContext.Create();
			Connection.producerCache = new Dictionary<string, ProducerRef>();
			Connection.producerCacheLock = new object();
		}

		public Connection(Uri connectionUri)
		{
			this.brokerUri = connectionUri;
			this.producerContextBinding = string.Format("{0}://*:{1}", this.brokerUri.Scheme, this.brokerUri.Port);
			this.consumerContextBinding = string.Format("{0}://{1}:{2}", brokerUri.Scheme, brokerUri.Host, this.brokerUri.Port);
		}

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

		internal ZmqSocket GetProducer()
		{
			ProducerRef producerRef;
			string contextBinding = GetProducerContextBinding();

			lock(producerCacheLock)
			{
				if(!producerCache.TryGetValue(contextBinding, out producerRef))
				{
					producerRef = new ProducerRef();
					producerRef.producer = this.Context.CreateSocket(SocketType.PUB);
					if(null == producerRef.producer)
					{
						throw new ResourceAllocationException();
					}
					producerRef.producer.Bind(contextBinding);
					producerCache.Add(contextBinding, producerRef);
				}
				else
				{
					producerRef.refCount++;
				}
			}

			return producerRef.producer;
		}

		internal void ReleaseProducer(ZmqSocket endpoint)
		{
			// UNREFERENCED_PARAM(endpoint);
			ProducerRef producerRef;
			string contextBinding = GetProducerContextBinding();

			lock(producerCacheLock)
			{
				if(producerCache.TryGetValue(contextBinding, out producerRef))
				{
					producerRef.refCount--;
					if(producerRef.refCount < 1)
					{
						producerCache.Remove(contextBinding);
						producerRef.producer.Unbind(contextBinding);
					}
				}
			}
		}

		internal ZmqSocket GetConsumer(Encoding encoding, string destinationName)
		{
			ZmqSocket endpoint = this.Context.CreateSocket(SocketType.SUB);

			if(null == endpoint)
			{
				throw new ResourceAllocationException();
			}
			endpoint.Subscribe(encoding.GetBytes(destinationName));
			endpoint.Connect(GetConsumerBindingPath());

			return endpoint;
		}

		internal void ReleaseConsumer(ZmqSocket endpoint)
		{
			endpoint.Disconnect(GetConsumerBindingPath());
		}

		internal string GetProducerContextBinding()
		{
			return this.producerContextBinding;
		}

		private string GetConsumerBindingPath()
		{
			return this.consumerContextBinding;
		}

		public void Dispose()
        {
            Close();
        }

        public void Close()
        {
            Stop();

			lock(producerCacheLock)
			{
				foreach(KeyValuePair<string, ProducerRef> cacheItem in producerCache)
				{
					cacheItem.Value.producer.Unbind(cacheItem.Key);
				}

				producerCache.Clear();
			}
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
        internal ZmqContext Context
        {
            get { return Connection._context; }
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
