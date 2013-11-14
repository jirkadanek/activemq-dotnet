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
using Apache.NMS.MQTT.Transport;
using Apache.NMS.MQTT.Commands;

namespace Apache.NMS.MQTT
{
	public class Connection : IConnection
	{
		private readonly CONNECT info = null;
		private ITransport transport;
		private readonly Uri brokerUri;
        private readonly IList sessions = ArrayList.Synchronized(new ArrayList());
        private readonly IDictionary dispatchers = Hashtable.Synchronized(new Hashtable());
        private readonly object myLock = new object();
        private readonly Atomic<bool> connected = new Atomic<bool>(false);
        private readonly Atomic<bool> closed = new Atomic<bool>(false);
        private readonly Atomic<bool> closing = new Atomic<bool>(false);
        private readonly Atomic<bool> transportFailed = new Atomic<bool>(false);
        private Exception firstFailureError = null;
        private int sessionCounter = 0;
        private readonly Atomic<bool> started = new Atomic<bool>(false);
        private ConnectionMetaData metaData = null;
        private bool disposed = false;
        private readonly MessageTransformation messageTransformation;
        private readonly ThreadPoolExecutor executor = new ThreadPoolExecutor();

		public Connection(Uri connectionUri, ITransport transport)
		{
			this.brokerUri = connectionUri;
			this.clientIdGenerator = clientIdGenerator;

			SetTransport(transport);

			this.info = new CONNECT();
		}

		~Connection()
		{
			Dispose(false);
		}

		#region Properties

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

		public String UserName
		{
			get { return this.info.UserName; }
			set { this.info.UserName = value; }
		}

		public String Password
		{
			get { return this.info.Password; }
			set { this.info.Password = value; }
		}

		/// <summary>
		/// This property sets the acknowledgment mode for the connection.
		/// The URI parameter connection.ackmode can be set to a string value
		/// that maps to the enumeration value.
		/// </summary>
		public string AckMode
		{
			set { this.acknowledgementMode = NMSConvert.ToAcknowledgementMode(value); }
		}

		public IConnectionMetaData MetaData
		{
			get { return this.metaData ?? (this.metaData = new ConnectionMetaData()); }
		}

		public Uri BrokerUri
		{
			get { return brokerUri; }
		}

		public ITransport ITransport
		{
			get { return transport; }
			set { this.transport = value; }
		}

		public bool TransportFailed
		{
			get { return this.transportFailed.Value; }
		}

		public Exception FirstFailureError
		{
			get { return this.firstFailureError; }
		}

		public TimeSpan RequestTimeout
		{
			get { return this.requestTimeout; }
			set { this.requestTimeout = value; }
		}

		public AcknowledgementMode AcknowledgementMode
		{
			get { return acknowledgementMode; }
			set { this.acknowledgementMode = value; }
		}

		internal MessageTransformation MessageTransformation
		{
			get { return this.messageTransformation; }
		}

		#endregion

		private void SetTransport(ITransport newTransport)
		{
			this.transport = newTransport;
			this.transport.Command = new CommandHandler(OnCommand);
			this.transport.Exception = new ExceptionHandler(OnTransportException);
			this.transport.Interrupted = new InterruptedHandler(OnTransportInterrupted);
			this.transport.Resumed = new ResumedHandler(OnTransportResumed);
		}

		/// <summary>
		/// Starts asynchronous message delivery of incoming messages for this connection.
		/// Synchronous delivery is unaffected.
		/// </summary>
		public void Start()
		{
			CheckConnected();
			if(started.CompareAndSet(false, true))
			{
				lock(sessions.SyncRoot)
				{
					foreach(Session session in sessions)
					{
						session.Start();
					}
				}
			}
		}

		/// <summary>
		/// This property determines if the asynchronous message delivery of incoming
		/// messages has been started for this connection.
		/// </summary>
		public bool IsStarted
		{
			get { return started.Value; }
		}

		/// <summary>
		/// Temporarily stop asynchronous delivery of inbound messages for this connection.
		/// The sending of outbound messages is unaffected.
		/// </summary>
		public void Stop()
		{
			if(started.CompareAndSet(true, false))
			{
				lock(sessions.SyncRoot)
				{
					foreach(Session session in sessions)
					{
						session.Stop();
					}
				}
			}
		}

		/// <summary>
		/// Creates a new session to work on this connection
		/// </summary>
		public ISession CreateSession()
		{
			return CreateMQTTSession(acknowledgementMode);
		}

		/// <summary>
		/// Creates a new session to work on this connection
		/// </summary>
		public ISession CreateSession(AcknowledgementMode sessionAcknowledgementMode)
		{
			return CreateMQTTSession(sessionAcknowledgementMode);
		}

		protected virtual Session CreateMQTTSession(AcknowledgementMode ackMode)
		{
			CheckConnected();
			return new Session(this, NextSessionId, ackMode);
		}

		internal void AddSession(Session session)
		{
			if(!this.closing.Value)
			{
				sessions.Add(session);
			}
		}

		internal void RemoveSession(Session session)
		{
			if(!this.closing.Value)
			{
				sessions.Remove(session);
				RemoveDispatcher(session);
			}
		}

		internal void AddDispatcher(ConsumerId id, IDispatcher dispatcher)
		{
			if(!this.closing.Value)
			{
				this.dispatchers.Add(id, dispatcher);
			}
		}

		internal void RemoveDispatcher(ConsumerId id)
		{
			if(!this.closing.Value)
			{
				this.dispatchers.Remove(id);
			}
		}

		internal void AddProducer(ProducerId id, MessageProducer producer)
		{
			if(!this.closing.Value)
			{
				this.producers.Add(id, producer);
			}
		}

		internal void RemoveProducer(ProducerId id)
		{
			if(!this.closing.Value)
			{
				this.producers.Remove(id);
			}
		}

	    internal void RemoveDispatcher(IDispatcher dispatcher) 
		{
	        this.connectionAudit.RemoveDispatcher(dispatcher);
	    }

		public void Close()
		{
			// TODO
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

		protected void CheckClosedOrFailed()
		{
			CheckClosed();
			if(transportFailed.Value)
			{
				throw new ConnectionFailedException(firstFailureError.Message);
			}
		}

		protected void CheckClosed()
		{
			if(closed.Value)
			{
				throw new ConnectionClosedException();
			}
		}

	}
}

