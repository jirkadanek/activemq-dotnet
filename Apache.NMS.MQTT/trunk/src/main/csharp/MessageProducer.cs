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
using Apache.NMS.Util;
using Apache.NMS.MQTT.Commands;
using Apache.NMS.MQTT.Util;

namespace Apache.NMS.MQTT
{
	public class MessageProducer : IMessageProducer
	{
		private readonly Session session;
		private readonly object closedLock = new object();
		private bool closed = false;

		private MsgDeliveryMode msgDeliveryMode = NMSConstants.defaultDeliveryMode;
		private TimeSpan requestTimeout;
		protected bool disposed = false;

		private readonly MessageTransformation messageTransformation;

		public MessageProducer(Session session, TimeSpan requestTimeout)
		{
			this.session = session;
			this.RequestTimeout = requestTimeout;
			this.messageTransformation = session.Connection.MessageTransformation;
		}

		~MessageProducer()
		{
			Dispose(false);
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
			}

			disposed = true;
		}

		public void Close()
		{
			lock(closedLock)
			{
				if(closed)
				{
					return;
				}

				Shutdown();
				if(Tracer.IsDebugEnabled)
				{
					Tracer.DebugFormat("Remove of Producer[{0}] sent.", this.ProducerId);
				}
			}
		}

		/// <summary>
		/// Called from the Parent session to deactivate this Producer, when a parent
		/// is closed all children are automatically removed from the broker so this
		/// method circumvents the need to send a Remove command to the broker.
		/// </summary>
		internal void Shutdown()
		{
			lock(closedLock)
			{
				if(closed)
				{
					return;
				}

				try
				{
					session.RemoveProducer(info.ProducerId);
				}
				catch(Exception ex)
				{
					Tracer.ErrorFormat("Error during producer close: {0}", ex);
				}

				closed = true;
			}
		}

		public void Send(IMessage message)
		{
			Send(info.Destination, message, this.msgDeliveryMode, this.msgPriority, this.msgTimeToLive, false);
		}

		public void Send(IDestination destination, IMessage message)
		{
			Send(destination, message, this.msgDeliveryMode, this.msgPriority, this.msgTimeToLive, false);
		}

		public void Send(IMessage message, MsgDeliveryMode deliveryMode, MsgPriority priority, TimeSpan timeToLive)
		{
			Send(info.Destination, message, deliveryMode, priority, timeToLive, true);
		}

		public void Send(IDestination destination, IMessage message, MsgDeliveryMode deliveryMode, MsgPriority priority, TimeSpan timeToLive)
		{
			Send(destination, message, deliveryMode, priority, timeToLive, true);
		}

		protected void Send(IDestination destination, IMessage message, MsgDeliveryMode deliveryMode, MsgPriority priority, TimeSpan timeToLive, bool specifiedTimeToLive)
		{
		}

		private ProducerTransformerDelegate producerTransformer;
		public ProducerTransformerDelegate ProducerTransformer
		{
			get { return this.producerTransformer; }
			set { this.producerTransformer = value; }
		}

		public IMessage CreateMessage()
		{
			return session.CreateMessage();
		}

		public ITextMessage CreateTextMessage()
		{
			return session.CreateTextMessage();
		}

		public ITextMessage CreateTextMessage(string text)
		{
			return session.CreateTextMessage(text);
		}

		public IMapMessage CreateMapMessage()
		{
			return session.CreateMapMessage();
		}

		public IObjectMessage CreateObjectMessage(object body)
		{
			return session.CreateObjectMessage(body);
		}

		public IBytesMessage CreateBytesMessage()
		{
			return session.CreateBytesMessage();
		}

		public IBytesMessage CreateBytesMessage(byte[] body)
		{
			return session.CreateBytesMessage(body);
		}

		public IStreamMessage CreateStreamMessage()
		{
			return session.CreateStreamMessage();
		}

	}
}

