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
using System.Text;
using System.Threading;
using Apache.NMS.Util;
using ZSendRecvOpt = ZMQ.SendRecvOpt;
using ZSocket = ZMQ.Socket;
using ZSocketType = ZMQ.SocketType;

namespace Apache.NMS.ZMQ
{
	/// <summary>
	/// An object capable of receiving messages from some destination
	/// </summary>
	public class MessageConsumer : IMessageConsumer
	{
		protected TimeSpan zeroTimeout = new TimeSpan(0);

		private readonly Session session;
		private readonly AcknowledgementMode acknowledgementMode;
		/// <summary>
		/// Socket object
		/// </summary>
		private ZSocket messageSubscriber = null;
		/// <summary>
		/// Context binding string
		/// </summary>
		private string contextBinding;
		private Queue destination;
		private event MessageListener listener;
		private int listenerCount = 0;
		private Thread asyncDeliveryThread = null;
		private AutoResetEvent pause = new AutoResetEvent(false);
		private Atomic<bool> asyncDelivery = new Atomic<bool>(false);

		private ConsumerTransformerDelegate consumerTransformer;
		public ConsumerTransformerDelegate ConsumerTransformer
		{
			get { return this.consumerTransformer; }
			set { this.consumerTransformer = value; }
		}

		public MessageConsumer(Session session, AcknowledgementMode acknowledgementMode, IDestination destination, string selector)
		{
			if(null == Connection.Context)
			{
				throw new NMSConnectionException();
			}

			this.session = session;
			this.acknowledgementMode = acknowledgementMode;
			this.messageSubscriber = Connection.Context.Socket(ZSocketType.SUB);
			if(null == this.messageSubscriber)
			{
				throw new ResourceAllocationException();
			}

			string clientId = session.Connection.ClientId;

			this.contextBinding = session.Connection.BrokerUri.LocalPath;
			this.destination = new Queue(this.contextBinding);
			if(!string.IsNullOrEmpty(clientId))
			{
				this.messageSubscriber.StringToIdentity(clientId, Encoding.Unicode);
			}

			this.messageSubscriber.Connect(contextBinding);
			this.messageSubscriber.Subscribe(selector ?? string.Empty, Encoding.ASCII);
		}

		public event MessageListener Listener
		{
			add
			{
				listener += value;
				listenerCount++;
				StartAsyncDelivery();
			}

			remove
			{
				if(listenerCount > 0)
				{
					listener -= value;
					listenerCount--;
				}

				if(0 == listenerCount)
				{
					StopAsyncDelivery();
				}
			}
		}

		/// <summary>
		/// Receive message from subscriber
		/// </summary>
		/// <returns>
		/// message interface
		/// </returns>
		public IMessage Receive()
		{
			// TODO: Support decoding of all message types + all meta data (e.g., headers and properties)
			return ToNmsMessage(messageSubscriber.Recv(Encoding.ASCII, ZSendRecvOpt.NONE));
		}

		/// <summary>
		/// Receive message from subscriber
		/// </summary>
		/// <returns>
		/// message interface
		/// </returns>
		public IMessage Receive(TimeSpan timeout)
		{
			// TODO: Support decoding of all message types + all meta data (e.g., headers and properties)
			return ToNmsMessage(messageSubscriber.Recv(Encoding.ASCII, timeout.Milliseconds));
		}

		/// <summary>
		/// Receive message from subscriber
		/// </summary>
		/// <returns>
		/// message interface
		/// </returns>
		public IMessage ReceiveNoWait()
		{
			return Receive(zeroTimeout);
		}

		/// <summary>
		/// Clean up
		/// </summary>
		public void Dispose()
		{
			Close();
		}

		/// <summary>
		/// Clean up
		/// </summary>
		public void Close()
		{
			StopAsyncDelivery();
			if(null != messageSubscriber)
			{
				messageSubscriber.Dispose();
				messageSubscriber = null;
			}
		}

		protected virtual void StopAsyncDelivery()
		{
			if(asyncDelivery.CompareAndSet(true, false))
			{
				if(null != asyncDeliveryThread)
				{
					Tracer.Info("Stopping async delivery thread.");
					pause.Set();
					if(!asyncDeliveryThread.Join(10000))
					{
						Tracer.Info("Aborting async delivery thread.");
						asyncDeliveryThread.Abort();
					}

					asyncDeliveryThread = null;
					Tracer.Info("Async delivery thread stopped.");
				}
			}
		}

		protected virtual void StartAsyncDelivery()
		{
			if(asyncDelivery.CompareAndSet(false, true))
			{
				asyncDeliveryThread = new Thread(new ThreadStart(DispatchLoop));
				asyncDeliveryThread.Name = "Message Consumer Dispatch: " + contextBinding;
				asyncDeliveryThread.IsBackground = true;
				asyncDeliveryThread.Start();
			}
		}

		protected virtual void DispatchLoop()
		{
			Tracer.Info("Starting dispatcher thread consumer: " + this);

			while(asyncDelivery.Value)
			{
				try
				{
					IMessage message = Receive();
					if(asyncDelivery.Value && message != null)
					{
						try
						{
							listener(message);
						}
						catch(Exception e)
						{
							HandleAsyncException(e);
						}
					}
				}
				catch(ThreadAbortException ex)
				{
					Tracer.InfoFormat("Thread abort received in thread: {0} : {1}", this, ex.Message);
					break;
				}
				catch(Exception ex)
				{
					Tracer.ErrorFormat("Exception while receiving message in thread: {0} : {1}", this, ex.Message);
				}
			}
			Tracer.Info("Stopping dispatcher thread consumer: " + this);
		}

		protected virtual void HandleAsyncException(Exception e)
		{
			session.Connection.HandleException(e);
		}

		/// <summary>
		/// Create nms message object
		/// </summary>
		/// <param name="message">
		/// zmq message object
		/// </param>
		/// <returns>
		/// nms message object
		/// </returns>
		protected virtual IMessage ToNmsMessage(string messageText)
		{
			IMessage nmsMessage = new TextMessage(messageText);

			try
			{
				nmsMessage.NMSMessageId = "";
				nmsMessage.NMSDestination = this.destination;
				nmsMessage.NMSDeliveryMode = MsgDeliveryMode.NonPersistent;
				nmsMessage.NMSPriority = MsgPriority.Normal;
				nmsMessage.NMSTimestamp = DateTime.Now;
				nmsMessage.NMSTimeToLive = new TimeSpan(0);
				nmsMessage.NMSType = "";
			}
			catch(InvalidOperationException)
			{
				// Log error
			}

			if(null != this.ConsumerTransformer)
			{
				IMessage transformedMessage = ConsumerTransformer(this.session, this, nmsMessage);

				if(null != transformedMessage)
				{
					nmsMessage = transformedMessage;
				}
			}

			return nmsMessage;
		}
	}
}
