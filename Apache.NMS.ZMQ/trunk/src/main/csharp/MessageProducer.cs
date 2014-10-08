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

#define PUBSUB

using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using Apache.NMS.Util;

namespace Apache.NMS.ZMQ
{
	/// <summary>
	/// An object capable of sending messages to some destination
	/// </summary>
	public class MessageProducer : IMessageProducer
	{
		private readonly Session session;
		private Destination destination;

		private MsgDeliveryMode deliveryMode = MsgDeliveryMode.NonPersistent;
		private TimeSpan timeToLive;
		private MsgPriority priority;
		private bool disableMessageID;
		private bool disableMessageTimestamp;

		private ProducerTransformerDelegate producerTransformer;
		public ProducerTransformerDelegate ProducerTransformer
		{
			get { return this.producerTransformer; }
			set { this.producerTransformer = value; }
		}

		public MessageProducer(Session sess, IDestination dest)
		{
			if(null == sess.Connection.Context)
			{
				throw new NMSConnectionException();
			}

			this.session = sess;
			this.destination = (Destination) dest;
			this.destination.InitSender();
		}

		public void Send(IMessage message)
		{
			Send(this.destination, message, this.deliveryMode, this.priority, this.timeToLive, false);
		}

		public void Send(IDestination dest, IMessage message)
		{
			Send(dest, message, this.deliveryMode, this.priority, this.timeToLive, false);
		}

		public void Send(IMessage message, MsgDeliveryMode deliveryMode, MsgPriority priority, TimeSpan timeToLive)
		{
			Send(this.destination, message, deliveryMode, priority, timeToLive, true);
		}

		public void Send(IDestination dest, IMessage message, MsgDeliveryMode deliveryMode, MsgPriority priority, TimeSpan timeToLive)
		{
			Send(destination, message, deliveryMode, priority, timeToLive, true);
		}

		public void Send(IDestination dest, IMessage message, MsgDeliveryMode deliveryMode, MsgPriority priority, TimeSpan timeToLive, bool specifiedTimeToLive)
		{
			if(null == dest)
			{
				return;
			}

			if(null != this.ProducerTransformer)
			{
				IMessage transformedMessage = ProducerTransformer(this.session, this, message);

				if(null != transformedMessage)
				{
					message = transformedMessage;
				}
			}

			// Serialize the message data
			Destination theDest = (Destination) dest;
			List<byte> msgDataBuilder = new List<byte>();

			// Always set the message Id.
			message.NMSMessageId = Guid.NewGuid().ToString();
			message.NMSTimestamp = DateTime.UtcNow;
			if(specifiedTimeToLive)
			{
				message.NMSTimeToLive = timeToLive;
			}

			// Prefix the message with the destination name. The client will subscribe to this destination name
			// in order to receive messages.
			msgDataBuilder.AddRange(Encoding.UTF8.GetBytes(theDest.Name));

			// Encode all meta data (e.g., headers and properties)
			EncodeField(msgDataBuilder, WireFormat.MFT_MESSAGEID, message.NMSMessageId);
			EncodeField(msgDataBuilder, WireFormat.MFT_TIMESTAMP, message.NMSTimestamp.ToBinary());
			if(null != message.NMSType)
			{
				EncodeField(msgDataBuilder, WireFormat.MFT_NMSTYPE, message.NMSType);
			}

			if(null != message.NMSCorrelationID)
			{
				EncodeField(msgDataBuilder, WireFormat.MFT_CORRELATIONID, message.NMSCorrelationID);
			}

			if(null != message.NMSReplyTo)
			{
				EncodeField(msgDataBuilder, WireFormat.MFT_REPLYTO, ((Destination) message.NMSReplyTo).Name);
			}

			EncodeField(msgDataBuilder, WireFormat.MFT_DELIVERYMODE, message.NMSDeliveryMode);
			EncodeField(msgDataBuilder, WireFormat.MFT_PRIORITY, message.NMSPriority);
			EncodeField(msgDataBuilder, WireFormat.MFT_TIMETOLIVE, message.NMSTimeToLive.Ticks);

			IPrimitiveMap properties = message.Properties;
			if(null != properties && properties.Count > 0)
			{
				EncodeField(msgDataBuilder, WireFormat.MFT_HEADERS, ((PrimitiveMap) properties).Marshal());
			}

			if(message is ITextMessage)
			{
				EncodeField(msgDataBuilder, WireFormat.MFT_MSGTYPE, WireFormat.MT_TEXTMESSAGE);
				// Append the message text body to the msg.
				string msgBody = ((ITextMessage) message).Text;

				if(null != msgBody)
				{
					EncodeField(msgDataBuilder, WireFormat.MFT_BODY, msgBody);
				}
			}
			else
			{
				// TODO: Add support for more message types
				EncodeField(msgDataBuilder, WireFormat.MFT_MSGTYPE, WireFormat.MT_MESSAGE);
			}

			// Put the sentinal field marker.
			EncodeField(msgDataBuilder, WireFormat.MFT_NONE, 0);
			theDest.Send(msgDataBuilder.ToArray());
		}

		private void EncodeField(List<byte> msgDataBuilder, int msgFieldType, string fieldData)
		{
			if(null == fieldData)
			{
				fieldData = string.Empty;
			}

			EncodeField(msgDataBuilder, msgFieldType, Encoding.UTF8.GetBytes(fieldData));
		}

		private void EncodeField(List<byte> msgDataBuilder, int msgFieldType, Enum fieldData)
		{
			EncodeField(msgDataBuilder, msgFieldType, Convert.ToInt32(fieldData));
		}

		private void EncodeField(List<byte> msgDataBuilder, int msgFieldType, int fieldData)
		{
			EncodeField(msgDataBuilder, msgFieldType, BitConverter.GetBytes(IPAddress.HostToNetworkOrder(fieldData)));
		}

		private void EncodeField(List<byte> msgDataBuilder, int msgFieldType, long fieldData)
		{
			EncodeField(msgDataBuilder, msgFieldType, BitConverter.GetBytes(IPAddress.HostToNetworkOrder(fieldData)));
		}

		private void EncodeField(List<byte> msgDataBuilder, int msgFieldType, byte[] fieldData)
		{
			// Encode the field type
			msgDataBuilder.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(msgFieldType)));
			EncodeFieldData(msgDataBuilder, fieldData);
		}

		private void EncodeFieldData(List<byte> msgDataBuilder, int fieldData)
		{
			msgDataBuilder.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(fieldData)));
		}

		private void EncodeFieldData(List<byte> msgDataBuilder, string fieldData)
		{
			if(null == fieldData)
			{
				fieldData = string.Empty;
			}

			EncodeFieldData(msgDataBuilder, Encoding.UTF8.GetBytes(fieldData));
		}

		private void EncodeFieldData(List<byte> msgDataBuilder, byte[] fieldData)
		{
			// Encode the field length
			int fieldLength = 0;

			if(null != fieldData)
			{
				fieldLength = fieldData.Length;
			}

			EncodeFieldData(msgDataBuilder, fieldLength);
			if(0 != fieldLength)
			{
				// Encode the field data
				msgDataBuilder.AddRange(fieldData);
			}
		}

		public void Dispose()
		{
			Close();
		}

		public void Close()
		{
			this.destination = null;
		}

		public IMessage CreateMessage()
		{
			return this.session.CreateMessage();
		}

		public ITextMessage CreateTextMessage()
		{
			return this.session.CreateTextMessage();
		}

		public ITextMessage CreateTextMessage(String text)
		{
			return this.session.CreateTextMessage(text);
		}

		public IMapMessage CreateMapMessage()
		{
			return this.session.CreateMapMessage();
		}

		public IObjectMessage CreateObjectMessage(Object body)
		{
			return this.session.CreateObjectMessage(body);
		}

		public IBytesMessage CreateBytesMessage()
		{
			return this.session.CreateBytesMessage();
		}

		public IBytesMessage CreateBytesMessage(byte[] body)
		{
			return this.session.CreateBytesMessage(body);
		}

		public IStreamMessage CreateStreamMessage()
		{
			return this.session.CreateStreamMessage();
		}

		public MsgDeliveryMode DeliveryMode
		{
			get { return this.deliveryMode; }
			set { this.deliveryMode = value; }
		}

		public TimeSpan TimeToLive
		{
			get { return this.timeToLive; }
			set { this.timeToLive = value; }
		}

		/// <summary>
		/// The default timeout for network requests.
		/// </summary>
		public TimeSpan RequestTimeout
		{
			get { return NMSConstants.defaultRequestTimeout; }
			set { }
		}

		public MsgPriority Priority
		{
			get { return this.priority; }
			set { this.priority = value; }
		}

		public bool DisableMessageID
		{
			get { return this.disableMessageID; }
			set { this.disableMessageID = value; }
		}

		public bool DisableMessageTimestamp
		{
			get { return this.disableMessageTimestamp; }
			set { this.disableMessageTimestamp = value; }
		}
	}
}
