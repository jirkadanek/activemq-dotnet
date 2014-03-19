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
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace Apache.NMS.ZMQ
{
	/// <summary>
	/// An object capable of receiving messages from some destination
	/// </summary>
	public class MessageConsumer : IMessageConsumer
	{
		protected static readonly TimeSpan zeroTimeout = new TimeSpan(0);

		private readonly Session session;
		private readonly AcknowledgementMode acknowledgementMode;
		private Destination destination;
		private event MessageListener listener;
		private int listenerCount = 0;
		private Thread asyncDeliveryThread = null;
		private object asyncDeliveryLock = new object();
		private bool asyncDelivery = false;
		private bool asyncInit = false;
		private byte[] rawDestinationName;

		private ConsumerTransformerDelegate consumerTransformer;
		public ConsumerTransformerDelegate ConsumerTransformer
		{
			get { return this.consumerTransformer; }
			set { this.consumerTransformer = value; }
		}

		public MessageConsumer(Session sess, AcknowledgementMode ackMode, IDestination dest, string selector)
		{
			// UNUSED_PARAM(selector);		// Selectors are not currently supported

			if(null == sess.Connection.Context)
			{
				throw new NMSConnectionException();
			}

			this.session = sess;
			this.destination = (Destination) dest;
			this.rawDestinationName = Destination.encoding.GetBytes(this.destination.Name);
			this.acknowledgementMode = ackMode;
		}

		private object listenerLock = new object();
		public event MessageListener Listener
		{
			add
			{
				lock(listenerLock)
				{
					this.listener += value;
					if(0 == this.listenerCount)
					{
						StartAsyncDelivery();
					}

					this.listenerCount++;
				}
			}

			remove
			{
				lock(listenerLock)
				{
					this.listener -= value;
					if(this.listenerCount > 0)
					{
						this.listenerCount--;
						if(0 == this.listenerCount)
						{
							StopAsyncDelivery();
						}
					}
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
			return Receive(TimeSpan.MaxValue);
		}

		/// <summary>
		/// Receive message from subscriber
		/// </summary>
		/// <returns>
		/// message interface
		/// </returns>
		public IMessage Receive(TimeSpan timeout)
		{
			int size;
			byte[] receivedMsg = this.destination.ReceiveBytes(timeout, out size);

			if(size > 0)
			{
				// Strip off the subscribed destination name.
				// TODO: Support decoding of all message types + all meta data (e.g., headers and properties)
				int msgStart = this.rawDestinationName.Length;
				int msgLength = receivedMsg.Length - msgStart;
				string msgContent = Encoding.UTF8.GetString(receivedMsg, msgStart, msgLength);
				return ToNmsMessage(msgContent);
			}

			return null;
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
			this.destination = null;
		}

		protected virtual void StopAsyncDelivery()
		{
			lock(this.asyncDeliveryLock)
			{
				this.asyncDelivery = false;
				if(null != this.asyncDeliveryThread)
				{
					Tracer.Info("Stopping async delivery thread.");
					this.asyncDeliveryThread.Interrupt();
					if(!this.asyncDeliveryThread.Join(10000))
					{
						Tracer.Info("Aborting async delivery thread.");
						this.asyncDeliveryThread.Abort();
					}

					this.asyncDeliveryThread = null;
					Tracer.Info("Async delivery thread stopped.");
				}
			}
		}

		protected virtual void StartAsyncDelivery()
		{
			Debug.Assert(null == this.asyncDeliveryThread);
			lock(this.asyncDeliveryLock)
			{
				this.asyncInit = false;
				this.asyncDelivery = true;
				this.asyncDeliveryThread = new Thread(new ThreadStart(MsgDispatchLoop));
				this.asyncDeliveryThread.Name = string.Format("MsgConsumerAsync: {0}", this.destination.Name);
				this.asyncDeliveryThread.IsBackground = true;
				this.asyncDeliveryThread.Start();
				while(!asyncInit)
				{
					Thread.Sleep(1);
				}
			}
		}

		protected virtual void MsgDispatchLoop()
		{
			Tracer.InfoFormat("Starting dispatcher thread consumer: {0}", this.asyncDeliveryThread.Name);
			TimeSpan receiveWait = TimeSpan.FromSeconds(2);

			// Signal that this thread has started.
			asyncInit = true;

			while(asyncDelivery)
			{
				try
				{
					IMessage message = Receive(receiveWait);

					if(asyncDelivery)
					{
						if(null != message)
						{
							try
							{
								listener(message);
							}
							catch(Exception ex)
							{
								HandleAsyncException(ex);
							}
						}
						else
						{
							Thread.Sleep(0);
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
			Tracer.InfoFormat("Stopped dispatcher thread consumer: {0}", this.asyncDeliveryThread.Name);
		}

		protected virtual void HandleAsyncException(Exception e)
		{
			this.session.Connection.HandleException(e);
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
			// Strip off the destination name prefix.
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
