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
using System.Collections;
using System.Collections.Generic;
using Apache.NMS.Util;
using Apache.NMS.MQTT.Transport;
using Apache.NMS.MQTT.Threads;
using Apache.NMS.MQTT.Commands;
using Apache.NMS.MQTT.Util;

namespace Apache.NMS.MQTT
{
	public class Connection : IConnection
	{
		private static readonly TimeSpan InfiniteTimeSpan = TimeSpan.FromMilliseconds(Timeout.Infinite);

		private AcknowledgementMode acknowledgementMode = AcknowledgementMode.AutoAcknowledge;
		private readonly CONNECT info = null;
		private ITransport transport;
		private readonly Uri brokerUri;
        private readonly IList sessions = ArrayList.Synchronized(new ArrayList());
        private readonly IDictionary dispatchers = Hashtable.Synchronized(new Hashtable());
		private readonly IDictionary producers = Hashtable.Synchronized(new Hashtable());
        private readonly Atomic<bool> connected = new Atomic<bool>(false);
        private readonly Atomic<bool> closed = new Atomic<bool>(false);
        private readonly Atomic<bool> closing = new Atomic<bool>(false);
        private readonly Atomic<bool> transportFailed = new Atomic<bool>(false);
		private readonly object connectedLock = new object();
        private Exception firstFailureError = null;
        private int sessionCounter = 0;
        private readonly Atomic<bool> started = new Atomic<bool>(false);
        private ConnectionMetaData metaData = null;
        private bool disposed = false;
		private TimeSpan requestTimeout = NMSConstants.defaultRequestTimeout; // from connection factory
        private readonly MessageTransformation messageTransformation;
        private readonly ThreadPoolExecutor executor = new ThreadPoolExecutor();
		private readonly IdGenerator clientIdGenerator;
		private IRedeliveryPolicy redeliveryPolicy;
		private Scheduler scheduler = null;
		private bool userSpecifiedClientID;

		public Connection(Uri connectionUri, ITransport transport, IdGenerator clientIdGenerator)
		{
			this.brokerUri = connectionUri;
			this.clientIdGenerator = clientIdGenerator;

			SetTransport(transport);

			this.messageTransformation = new MQTTMessageTransformation(this);
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

        public string ClientId
        {
            get { return info.ClientId; }
            set
            {
				if(this.connected.Value)
				{
					throw new NMSException("You cannot change the ClientId once the Connection is connected");
				}

				this.info.ClientId = value;
				this.userSpecifiedClientID = true;
				CheckConnected();
            }
        }

		/// <summary>
		/// The Default Client Id used if the ClientId property is not set explicity.
		/// </summary>
		internal string DefaultClientId
		{
			set
			{
				this.info.ClientId = value;
				this.userSpecifiedClientID = true;
			}
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

		/// <summary>
		/// Get/or set the redelivery policy for this connection.
		/// </summary>
		public IRedeliveryPolicy RedeliveryPolicy
		{
			get { return this.redeliveryPolicy; }
			set { this.redeliveryPolicy = value; }
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
			int sessionId = Interlocked.Increment(ref sessionCounter);
			return new Session(this, ackMode, sessionId);
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

		internal void AddDispatcher(int id, IDispatcher dispatcher)
		{
			if(!this.closing.Value)
			{
				this.dispatchers.Add(id, dispatcher);
			}
		}

		internal void RemoveDispatcher(int id)
		{
			if(!this.closing.Value)
			{
				this.dispatchers.Remove(id);
			}
		}

		internal void AddProducer(int id, MessageProducer producer)
		{
			if(!this.closing.Value)
			{
				this.producers.Add(id, producer);
			}
		}

		internal void RemoveProducer(int id)
		{
			if(!this.closing.Value)
			{
				this.producers.Remove(id);
			}
		}

	    internal void RemoveDispatcher(IDispatcher dispatcher) 
		{
	    }

		public void Close()
		{
			if(!this.closed.Value && !transportFailed.Value)
			{
				this.Stop();
			}

			lock(connectedLock)
			{
				if(this.closed.Value)
				{
					return;
				}

				try
				{
					Tracer.InfoFormat("Connection[{0}]: Closing Connection Now.", this.ClientId);
					this.closing.Value = true;

                    Scheduler scheduler = this.scheduler;
                    if (scheduler != null) 
					{
                        try 
						{
                            scheduler.Stop();
                        } 
						catch (Exception e) 
						{
                            throw NMSExceptionSupport.Create(e);
                        }
                    }

					lock(sessions.SyncRoot)
					{
						foreach(Session session in sessions)
						{
							session.Shutdown();
						}
					}
					sessions.Clear();

					// Connected is true only when we've successfully sent our CONNECT
					// to the broker, so if we haven't announced ourselves there's no need to
					// inform the broker of a remove, and if the transport is failed, why bother.
					if(connected.Value && !transportFailed.Value)
					{
						DISCONNECT disconnect = new DISCONNECT();
						transport.Oneway(disconnect);
					}

					executor.Shutdown();
					if (!executor.AwaitTermination(TimeSpan.FromMinutes(1)))
					{
						Tracer.DebugFormat("Connection[{0}]: Failed to properly shutdown its executor", this.ClientId);
					}

					Tracer.DebugFormat("Connection[{0}]: Disposing of the Transport.", this.ClientId);
					transport.Stop();
					transport.Dispose();
				}
				catch(Exception ex)
				{
					Tracer.ErrorFormat("Connection[{0}]: Error during connection close: {1}", ClientId, ex);
				}
				finally
				{
					if(executor != null)
					{
						executor.Shutdown();
					}

					this.transport = null;
					this.closed.Value = true;
					this.connected.Value = false;
					this.closing.Value = false;
				}
			}
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

		public void PurgeTempDestinations()
		{
			throw new NotSupportedException("MQTT does not support temp destinations.");
		}

		protected void OnCommand(ITransport commandTransport, Command command)
		{
		}

		internal void OnTransportException(ITransport source, Exception cause)
		{
		}

		protected void OnTransportInterrupted(ITransport sender)
		{
		}

		protected void OnTransportResumed(ITransport sender)
		{
		}

		internal void OnSessionException(Session sender, Exception exception)
		{
			if(ExceptionListener != null)
			{
				try
				{
					ExceptionListener(exception);
				}
				catch
				{
					sender.Close();
				}
			}
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

		/// <summary>
		/// Check and ensure that the connection object is connected.  If it is not
		/// connected or is closed or closing, a ConnectionClosedException is thrown.
		/// </summary>
		internal void CheckConnected()
		{
			if(closed.Value)
			{
				throw new ConnectionClosedException();
			}

			if(!connected.Value)
			{
				DateTime timeoutTime = DateTime.Now + this.RequestTimeout;
				int waitCount = 1;

				while(true)
				{
					if(Monitor.TryEnter(connectedLock))
					{
						try
						{
							if(closed.Value || closing.Value)
							{
								break;
							}
							else if(!connected.Value)
							{
								if(!this.userSpecifiedClientID)
								{
									this.info.ClientId = this.clientIdGenerator.GenerateId();
								}

								try
								{
									if(null != transport)
									{
										// Make sure the transport is started.
										if(!this.transport.IsStarted)
										{
											this.transport.Start();
										}

										// Send the connection and see if an ack/nak is returned.
										Response response = transport.Request(this.info, this.RequestTimeout);
										if(!response.IsErrorResponse)
										{
											connected.Value = true;
										}
										else
										{
											ErrorResponse error = response as ErrorResponse;
											NMSException exception = error.Error;
											// This is non-recoverable.
											// Shutdown the transport connection, and re-create it, but don't start it.
											// It will be started if the connection is re-attempted.
											this.transport.Stop();
											ITransport newTransport = TransportFactory.CreateTransport(this.brokerUri);
											SetTransport(newTransport);
											throw exception;
										}
									}
								}
								catch(NMSException)
								{
									throw;
								}
								catch(Exception)
								{
									// We Swallow the generic version and throw ConnectionClosedException
								}
							}
						}
						finally
						{
							Monitor.Exit(connectedLock);
						}
					}

					if(connected.Value || closed.Value || closing.Value
						|| (DateTime.Now > timeoutTime && this.RequestTimeout != InfiniteTimeSpan))
					{
						break;
					}

					// Back off from being overly aggressive.  Having too many threads
					// aggressively trying to connect to a down broker pegs the CPU.
					Thread.Sleep(5 * (waitCount++));
				}

				if(!connected.Value)
				{
					throw new ConnectionClosedException();
				}
			}
		}

	    internal Scheduler Scheduler
		{
			get
			{
		        Scheduler result = this.scheduler;
		        if (result == null) 
				{
		            lock (this) 
					{
		                result = scheduler;
		                if (result == null) 
						{
		                    CheckClosed();
		                    try 
							{
		                        result = scheduler = new Scheduler(
									"MQTTConnection["+this.info.ClientId+"] Scheduler");
		                        scheduler.Start();
		                    }
							catch(Exception e)
							{
		                        throw NMSExceptionSupport.Create(e);
		                    }
		                }
		            }
		        }
		        return result;
			}
	    }
	}
}

