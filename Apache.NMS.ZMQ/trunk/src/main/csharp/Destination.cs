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
using ZeroMQ;
using System.Diagnostics;

namespace Apache.NMS.ZMQ
{
	/// <summary>
	/// Summary description for Destination.
	/// </summary>
	public abstract class Destination : IDestination
	{
		public static Encoding encoding = Encoding.UTF8;

		protected Session session;
		/// <summary>
		/// Socket object
		/// </summary>
		protected ZmqSocket producerEndpoint = null;
		protected ZmqSocket consumerEndpoint = null;
		protected string destinationName;

		private bool disposed = false;

		/// <summary>
		/// Construct the Destination with a defined physical name.
		/// </summary>
		/// <param name="name"></param>
		protected Destination(Session session, string destName)
		{
			this.session = session;
			this.destinationName = destName;
		}

		~Destination()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing)
		{
			if(disposed)
			{
				return;
			}

			if(disposing)
			{
				try
				{
					OnDispose();
				}
				catch(Exception ex)
				{
					Tracer.ErrorFormat("Exception disposing Destination {0}: {1}", this.Name, ex.Message);
				}
			}

			disposed = true;
		}

		/// <summary>
		/// Child classes can override this method to perform clean-up logic.
		/// </summary>
		protected virtual void OnDispose()
		{
			if(null != this.producerEndpoint)
			{
				if(null != this.session
					&& null != this.session.Connection)
				{
					this.session.Connection.ReleaseProducer(this.producerEndpoint);
				}

				this.producerEndpoint = null;
			}

			if(null != this.consumerEndpoint)
			{
				this.session.Connection.ReleaseConsumer(this.consumerEndpoint);
				this.consumerEndpoint = null;
			}
		}

		public string Name
		{
			get { return this.destinationName; }
		}

		public bool IsTopic
		{
			get
			{
				return this.DestinationType == DestinationType.Topic
					|| this.DestinationType == DestinationType.TemporaryTopic;
			}
		}

		public bool IsQueue
		{
			get
			{
				return this.DestinationType == DestinationType.Queue
					|| this.DestinationType == DestinationType.TemporaryQueue;
			}
		}

		public bool IsTemporary
		{
			get
			{
				return this.DestinationType == DestinationType.TemporaryQueue
					|| this.DestinationType == DestinationType.TemporaryTopic;
			}
		}

		/// <summary>
		/// hashCode for this instance
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			int answer = 37;

			answer = this.Name.GetHashCode();

			if(IsTopic)
			{
				answer ^= 0xfabfab;
			}

			return answer;
		}

		/// <summary>
		/// if the object passed in is equivalent, return true
		/// </summary>
		/// <param name="obj">the object to compare</param>
		/// <returns>true if this instance and obj are equivalent</returns>
		public override bool Equals(Object obj)
		{
			bool result = (this == obj);

			if(!result && obj != null && obj is Destination)
			{
				Destination other = (Destination) obj;
				result = (this.DestinationType == other.DestinationType
							&& this.Name.Equals(other.Name));
			}

			return result;
		}

		public abstract DestinationType DestinationType
		{
			get;
		}

		internal void InitSender()
		{
			if(null == this.producerEndpoint)
			{
				this.producerEndpoint = this.session.Connection.GetProducer();
			}
		}

		internal void InitReceiver()
		{
			if(null == this.consumerEndpoint)
			{
				Connection connection = this.session.Connection;

				this.consumerEndpoint = connection.GetConsumer();
				// Must subscribe first before connecting to the endpoint binding
				this.consumerEndpoint.Subscribe(Destination.encoding.GetBytes(this.destinationName));
				this.consumerEndpoint.Connect(connection.GetConsumerBindingPath());
			}
		}

		internal void Subscribe(string prefixName)
		{
			InitReceiver();
			this.consumerEndpoint.Subscribe(Destination.encoding.GetBytes(prefixName));
		}

		internal void Unsubscribe(string prefixName)
		{
			if(null != this.consumerEndpoint)
			{
				this.consumerEndpoint.Unsubscribe(Destination.encoding.GetBytes(prefixName));
			}
		}

		internal SendStatus Send(string msg)
		{
			Debug.Assert(null != this.producerEndpoint, "Call InitSender() before calling Send().");
			return this.producerEndpoint.Send(msg, Destination.encoding);
		}

		internal SendStatus Send(byte[] buffer)
		{
			Debug.Assert(null != this.producerEndpoint, "Call InitSender() before calling Send().");
			return this.producerEndpoint.Send(buffer);
		}

		internal string ReceiveString(TimeSpan timeout)
		{
			this.InitReceiver();
			return this.consumerEndpoint.Receive(Destination.encoding, timeout);
		}

		internal byte[] ReceiveBytes(TimeSpan timeout, out int size)
		{
			this.InitReceiver();
			return this.consumerEndpoint.Receive(null, timeout, out size);
		}

		internal byte[] ReceiveBytes(SocketFlags flags, out int size)
		{
			this.InitReceiver();
			return this.consumerEndpoint.Receive(null, flags, out size);
		}

		internal Frame ReceiveFrame()
		{
			// TODO: Implement
			this.InitReceiver();
			return null;
		}

		internal ZmqMessage ReceiveMessage()
		{
			// TODO: Implement
			this.InitReceiver();
			return null;
		}
	}
}

