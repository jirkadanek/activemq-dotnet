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
using System.Net;
using Apache.NMS.Util;

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

			if(null == sess
				|| null == sess.Connection
				|| null == sess.Connection.Context)
			{
				throw new NMSConnectionException();
			}

			Destination theDest = dest as Destination;

			if(null == theDest)
			{
				throw new InvalidDestinationException("Consumer cannot receive on Null Destinations.");
			}
			else if(null == theDest.Name)
			{
				throw new InvalidDestinationException("The destination object was not given a physical name.");
			}
			else if(theDest.IsTemporary)
			{
				String physicalName = theDest.Name;

				if(String.IsNullOrEmpty(physicalName))
				{
					throw new InvalidDestinationException("Physical name of Destination should be valid: " + theDest);
				}
			}

			this.session = sess;
			this.destination = theDest;
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
				int receivedMsgIndex = this.rawDestinationName.Length;
				int msgLength = receivedMsg.Length - receivedMsgIndex;
				byte[] msgContent = new byte[msgLength];

				for(int index = 0; index < msgLength; index++, receivedMsgIndex++)
				{
					msgContent[index] = receivedMsg[receivedMsgIndex];
				}

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

			this.destination.InitReceiver();
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
							Thread.Sleep(1);
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
		protected virtual IMessage ToNmsMessage(byte[] msgData)
		{
			BaseMessage nmsMessage = null;
			int messageType = WireFormat.MT_UNKNOWN;
			int fieldType = WireFormat.MFT_NONE;
			DateTime messageTimestamp = DateTime.UtcNow;
			string messageNMSType = null;
			string messageCorrelationId = null;
			IDestination messageReplyTo = null;
			MsgDeliveryMode messageDeliveryMode = MsgDeliveryMode.NonPersistent;
			MsgPriority messagePriority = MsgPriority.Normal;
			TimeSpan messageTimeToLive = TimeSpan.FromTicks(0);
			byte[] messageProperties = null;
			int fieldLen;
			int index = 0;
			string messageID = string.Empty;
			byte[] messageBody = null;

			try
			{
				// Parse the commond message fields
				do
				{
					fieldType = ReadInt(msgData, ref index);
					switch(fieldType)
					{
					case WireFormat.MFT_NONE:
						break;

					case WireFormat.MFT_MESSAGEID:
						messageID = ReadString(msgData, ref index);
						break;

					case WireFormat.MFT_TIMESTAMP:
						fieldLen = ReadInt(msgData, ref index);
						Debug.Assert(sizeof(long) == fieldLen);
						messageTimestamp = DateTime.FromBinary(ReadLong(msgData, ref index));
						break;

					case WireFormat.MFT_NMSTYPE:
						messageNMSType = ReadString(msgData, ref index);
						break;

					case WireFormat.MFT_CORRELATIONID:
						messageCorrelationId = ReadString(msgData, ref index);
						break;

					case WireFormat.MFT_REPLYTO:
						string replyToDestName = ReadString(msgData, ref index);
						messageReplyTo = this.session.GetDestination(replyToDestName);
						break;

					case WireFormat.MFT_DELIVERYMODE:
						fieldLen = ReadInt(msgData, ref index);
						Debug.Assert(sizeof(int) == fieldLen);
						messageDeliveryMode = (MsgDeliveryMode) ReadInt(msgData, ref index);
						break;

					case WireFormat.MFT_PRIORITY:
						fieldLen = ReadInt(msgData, ref index);
						Debug.Assert(sizeof(int) == fieldLen);
						messagePriority = (MsgPriority) ReadInt(msgData, ref index);
						break;

					case WireFormat.MFT_TIMETOLIVE:
						fieldLen = ReadInt(msgData, ref index);
						Debug.Assert(sizeof(long) == fieldLen);
						messageTimeToLive = TimeSpan.FromTicks(ReadLong(msgData, ref index));
						break;

					case WireFormat.MFT_HEADERS:
						fieldLen = ReadInt(msgData, ref index);
						messageProperties = new byte[fieldLen];
						for(int propIndex = 0; propIndex < fieldLen; propIndex++, index++)
						{
							messageProperties[propIndex] = msgData[index];
						}
						break;

					case WireFormat.MFT_MSGTYPE:
						fieldLen = ReadInt(msgData, ref index);
						Debug.Assert(sizeof(int) == fieldLen);
						messageType = ReadInt(msgData, ref index);
						break;

					case WireFormat.MFT_BODY:
						messageBody = ReadBytes(msgData, ref index);
						break;

					default:
						// Skip past this field.
						Tracer.WarnFormat("Unknown message field type: {0}", fieldType);
						fieldLen = ReadInt(msgData, ref index);
						index += fieldLen;
						break;
					}
				} while(WireFormat.MFT_NONE != fieldType && index < msgData.Length);
			}
			catch(Exception ex)
			{
				Tracer.ErrorFormat("Exception parsing message: {0}", ex.Message);
			}

			// Instantiate the message type
			switch(messageType)
			{
			case WireFormat.MT_MESSAGE:
				nmsMessage = new BaseMessage();
				break;

			case WireFormat.MT_TEXTMESSAGE:
				nmsMessage = new TextMessage();
				if(null != messageBody)
				{
					((TextMessage) nmsMessage).Text = Encoding.UTF8.GetString(messageBody);
				}
				break;

			case WireFormat.MT_UNKNOWN:
			default:
				break;
			}

			// Set the common headers.
			if(null != nmsMessage)
			{
				try
				{
					nmsMessage.NMSMessageId = messageID;
					nmsMessage.NMSCorrelationID = messageCorrelationId;
					nmsMessage.NMSDestination = this.destination;
					nmsMessage.NMSReplyTo = messageReplyTo;
					nmsMessage.NMSDeliveryMode = messageDeliveryMode;
					nmsMessage.NMSPriority = messagePriority;
					nmsMessage.NMSTimestamp = messageTimestamp;
					nmsMessage.NMSTimeToLive = messageTimeToLive;
					nmsMessage.NMSType = messageNMSType;
					if(null != messageProperties)
					{
						nmsMessage.PropertiesMap = PrimitiveMap.Unmarshal(messageProperties);
					}
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
						nmsMessage = transformedMessage as BaseMessage;
					}
				}
			}

			return nmsMessage;
		}

		private long ReadLong(byte[] msgData, ref int index)
		{
			long val = BitConverter.ToInt64(msgData, index);
			index += sizeof(long);
			return IPAddress.NetworkToHostOrder(val);
		}

		private int ReadInt(byte[] msgData, ref int index)
		{
			int val = BitConverter.ToInt32(msgData, index);
			index += sizeof(int);
			return IPAddress.NetworkToHostOrder(val);
		}

		private string ReadString(byte[] msgData, ref int index)
		{
			int stringLen = ReadInt(msgData, ref index);
			string stringVal = string.Empty;

			if(stringLen > 0)
			{
				stringVal = Encoding.UTF8.GetString(msgData, index, stringLen);
				index += stringLen;
			}

			return stringVal;
		}

		private byte[] ReadBytes(byte[] msgData, ref int index)
		{
			int bytesLen = ReadInt(msgData, ref index);
			byte[] bytesVal = null;

			if(bytesLen >= 0)
			{
				bytesVal = new byte[bytesLen];
				for(int byteIndex = 0; byteIndex < bytesLen; byteIndex++, index++)
				{
					bytesVal[byteIndex] = msgData[index];
				}
			}

			return bytesVal;
		}
	}
}
