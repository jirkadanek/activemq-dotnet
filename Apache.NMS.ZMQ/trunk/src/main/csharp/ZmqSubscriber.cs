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
using CLRZMQ = ZMQ;

namespace Apache.NMS.ZMQ
{
	public class ZmqSubscriber : IDisposable
	{
		/// <summary>
		/// Socket object
		/// </summary>
		private CLRZMQ.Socket m_Subscriber = null;

		/// <summary>
		/// Context binding string
		/// </summary>
		private string m_Binding;

		public ZmqSubscriber(Connection connection, IDestination destination, string selector)
		{
			if(null != Connection.Context)
			{
				m_Subscriber = Connection.Context.Socket(CLRZMQ.SocketType.SUB);
			}
			Connect(connection.ClientId, connection.BrokerUri.LocalPath, selector);
		}

		private void Connect(string clientId, string binding, string selector)
		{
			m_Binding = binding;
			if(null != m_Subscriber)
			{
				if(!string.IsNullOrEmpty(clientId))
				{
					m_Subscriber.StringToIdentity(clientId, Encoding.Unicode);
				}
				m_Subscriber.Connect(binding);
				m_Subscriber.Subscribe(!string.IsNullOrEmpty(selector) ? selector : "", System.Text.Encoding.ASCII);
			}
		}

		public void Dispose()
		{
			if(null != m_Subscriber)
			{
				m_Subscriber.Dispose();
				m_Subscriber = null;
			}
		}

		#region Properties
		internal CLRZMQ.Socket Subscriber
		{
			get { return m_Subscriber; }
		}

		public string Binding
		{
			get { return m_Binding; }
		}
		#endregion
	}
}
