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
using Apache.NMS.Policies;
using Apache.NMS.Util;

namespace Apache.NMS.ZMQ
{
	/// <summary>
	/// A Factory that can estbalish NMS connections to ZMQ subscriber
	/// </summary>
	public class ConnectionFactory : IConnectionFactory
	{
		private Uri brokerUri;
		private string clientId;
		private IRedeliveryPolicy redeliveryPolicy = new RedeliveryPolicy();

		private const string DEFAULT_BROKER_URL = "tcp://localhost:5556";
		private const string ENV_BROKER_URL = "ZMQ_BROKER_URL";

		public ConnectionFactory()
			: this(GetDefaultBrokerUrl())
		{
		}

		public ConnectionFactory(string rawBrokerUri)
			: this(rawBrokerUri, null)
		{
		}

		public ConnectionFactory(string rawBrokerUri, string clientID)
			: this(URISupport.CreateCompatibleUri(rawBrokerUri), clientID)
		{
		}

		public ConnectionFactory(Uri rawBrokerUri)
			: this(rawBrokerUri, null)
		{
		}

		public ConnectionFactory(Uri rawBrokerUri, string clientID)
		{
			this.BrokerUri = rawBrokerUri;
			if(this.BrokerUri.Port < 1)
			{
				throw new NMSConnectionException("Missing connection port number.");
			}

			if(null == clientID)
			{
				clientID = Guid.NewGuid().ToString();
			}

			this.ClientId = clientID;
		}

		/// <summary>
		/// Get the default connection Uri if none is specified.
		/// The environment variable is checked first.
		/// </summary>
		/// <returns></returns>
		private static string GetDefaultBrokerUrl()
		{
			string brokerUrl = Environment.GetEnvironmentVariable(ENV_BROKER_URL);

			if(string.IsNullOrEmpty(brokerUrl))
			{
				brokerUrl = DEFAULT_BROKER_URL;
			}

			return brokerUrl;
		}

		#region IConnectionFactory Members


		/// <summary>
		/// Creates a new connection to ZMQ.
		/// </summary>
		public IConnection CreateConnection()
		{
			return CreateConnection(string.Empty, string.Empty, false);
		}

		/// <summary>
		/// Creates a new connection to ZMQ.
		/// </summary>
		public IConnection CreateConnection(string userName, string password)
		{
			return CreateConnection(userName, password, false);
		}

		/// <summary>
		/// Creates a new connection to ZMQ.
		/// </summary>
		public IConnection CreateConnection(string userName, string password, bool useLogging)
		{
			Connection connection = new Connection(this.BrokerUri);

			connection.RedeliveryPolicy = this.redeliveryPolicy.Clone() as IRedeliveryPolicy;
			connection.ConsumerTransformer = this.consumerTransformer;
			connection.ProducerTransformer = this.producerTransformer;
			connection.ClientId = this.ClientId;
			return connection;
		}

		/// <summary>
		/// Get/or set the broker Uri.
		/// </summary>
		public Uri BrokerUri
		{
			get { return this.brokerUri; }
			set
			{
				Tracer.InfoFormat("BrokerUri set {0}", value.OriginalString);
				this.brokerUri = new Uri(URISupport.StripPrefix(value.OriginalString, "zmq:"));
			}
		}

		public string ClientId
		{
			get { return this.clientId; }
			set { this.clientId = value; }
		}

		/// <summary>
		/// Get/or set the redelivery policy that new IConnection objects are
		/// assigned upon creation.
		/// </summary>
		public IRedeliveryPolicy RedeliveryPolicy
		{
			get { return this.redeliveryPolicy; }
			set
			{
				if(value != null)
				{
					this.redeliveryPolicy = value;
				}
			}
		}

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

		#endregion
	}
}
