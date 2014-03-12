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
using System.Text;
using ZeroMQ;


namespace Apache.NMS.ZMQ
{
	/// <summary>
	/// An object capable of sending messages to some destination
	/// </summary>
	public class MessageProducer : IMessageProducer
	{
		private readonly Session session;
		private IDestination destination;

		private MsgDeliveryMode deliveryMode;
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
			this.destination = dest;
		}

		public void Send(IMessage message)
		{
			Send(this.Destination, message);
		}

		public void Send(IMessage message, MsgDeliveryMode deliveryMode, MsgPriority priority, TimeSpan timeToLive)
		{
			Send(this.Destination, message, deliveryMode, priority, timeToLive);
		}

		public void Send(IDestination dest, IMessage message)
		{
			Send(dest, message, this.DeliveryMode, this.Priority, this.TimeToLive);
		}

		public void Send(IDestination dest, IMessage message, MsgDeliveryMode deliveryMode, MsgPriority priority, TimeSpan timeToLive)
		{
			// UNUSED_PARAM(deliveryMode);	// No concept of different delivery modes in ZMQ
			// UNUSED_PARAM(priority);		// No concept of priority messages in ZMQ
			// UNUSED_PARAM(timeToLive);	// No concept of time-to-live in ZMQ

			if(null != this.ProducerTransformer)
			{
				IMessage transformedMessage = ProducerTransformer(this.session, this, message);

				if(null != transformedMessage)
				{
					message = transformedMessage;
				}
			}

			// TODO: Support encoding of all message types + all meta data (e.g., headers and properties)

			// Prefix the message with the destination name. The client will subscribe to this destination name
			// in order to receive messages.
			Destination theDest = (Destination) dest;

			string msg = theDest.Name + ((ITextMessage) message).Text;
			theDest.Send(Encoding.UTF8.GetBytes(msg), this.session.Connection.RequestTimeout);
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

		public IDestination Destination
		{
			get { return this.destination; }
			set { this.destination = value; }
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
