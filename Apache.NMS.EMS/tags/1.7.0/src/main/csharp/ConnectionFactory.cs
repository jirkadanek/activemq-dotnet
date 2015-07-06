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
using System.Collections;
using Apache.NMS.Policies;

namespace Apache.NMS.EMS
{
	/// <summary>
	/// A Factory that can estbalish NMS connections to TIBCO
	/// </summary>
	public class ConnectionFactory : Apache.NMS.IConnectionFactory
	{
		public TIBCO.EMS.ConnectionFactory tibcoConnectionFactory;
		private Uri brokerUri;
		private string clientId;
		private Hashtable properties;

		private IRedeliveryPolicy redeliveryPolicy = new RedeliveryPolicy();

		public ConnectionFactory()
		{
			try
			{
				this.tibcoConnectionFactory = new TIBCO.EMS.ConnectionFactory();
			}
			catch(Exception ex)
			{
				Apache.NMS.Tracer.DebugFormat("Exception instantiating TIBCO.EMS.ConnectionFactory: {0}", ex.Message);
				ExceptionUtil.WrapAndThrowNMSException(ex);
			}

			VerifyConnectionFactory();
		}

		public ConnectionFactory(Uri serverUrl)
			: this(serverUrl, string.Empty)
		{
		}

		public ConnectionFactory(Uri serverUrl, string clientId)
			: this(serverUrl, clientId, null)
		{
		}

		public ConnectionFactory(string serverUrl)
			: this(new Uri(serverUrl))
		{
		}

		public ConnectionFactory(string serverUrl, string clientId)
			: this(new Uri(serverUrl), clientId)
		{
		}

		public ConnectionFactory(string serverUrl, string clientId, Hashtable properties)
			: this(new Uri(serverUrl), clientId, properties)
		{
		}

		public ConnectionFactory(Uri serverUrl, string clientId, Hashtable properties)
		{
			try
			{
				this.tibcoConnectionFactory = new TIBCO.EMS.ConnectionFactory(serverUrl.AbsolutePath, clientId, properties);
				this.brokerUri = serverUrl;
				this.clientId = clientId;
				this.properties = properties;
			}
			catch(Exception ex)
			{
				Apache.NMS.Tracer.DebugFormat("Exception instantiating TIBCO.EMS.ConnectionFactory: {0}", ex.Message);
				ExceptionUtil.WrapAndThrowNMSException(ex);
			}

			VerifyConnectionFactory();
		}

		private void VerifyConnectionFactory()
		{
			if(null == this.tibcoConnectionFactory)
			{
				throw new Apache.NMS.NMSException("Error instantiating TIBCO connection factory object.");
			}
		}

		#region IConnectionFactory Members

		/// <summary>
		/// Creates a new connection to TIBCO.
		/// </summary>
		public Apache.NMS.IConnection CreateConnection()
		{
			Apache.NMS.IConnection connection = null;

			try
			{
				connection = EMSConvert.ToNMSConnection(this.tibcoConnectionFactory.CreateConnection());
				ConfigureConnection(connection);
			}
			catch(Exception ex)
			{
				ExceptionUtil.WrapAndThrowNMSException(ex);
			}

			return connection;
		}

		/// <summary>
		/// Creates a new connection to TIBCO.
		/// </summary>
		public Apache.NMS.IConnection CreateConnection(string userName, string password)
		{
			Apache.NMS.IConnection connection = null;

			try
			{
				connection = EMSConvert.ToNMSConnection(this.tibcoConnectionFactory.CreateConnection(userName, password));
				ConfigureConnection(connection);
			}
			catch(Exception ex)
			{
				ExceptionUtil.WrapAndThrowNMSException(ex);
			}

			return connection;
		}

		/// <summary>
		/// Configure the newly created connection.
		/// </summary>
		/// <param name="connection"></param>
		private void ConfigureConnection(IConnection connection)
		{
			connection.RedeliveryPolicy = this.redeliveryPolicy.Clone() as IRedeliveryPolicy;
			connection.ConsumerTransformer = this.consumerTransformer;
			connection.ProducerTransformer = this.producerTransformer;
		}

		/// <summary>
		/// Get/or set the broker Uri.
		/// </summary>
		public Uri BrokerUri
		{
			get { return this.brokerUri; }
			set
			{
				try
				{
					if(null == this.brokerUri || !this.brokerUri.Equals(value))
					{
						// Re-create the TIBCO connection factory.
						this.brokerUri = value;
						if(null == this.brokerUri)
						{
							this.tibcoConnectionFactory = new TIBCO.EMS.ConnectionFactory();
						}
						else
						{
							if(null == this.clientId)
							{
								this.tibcoConnectionFactory = new TIBCO.EMS.ConnectionFactory(this.brokerUri.OriginalString);
							}
							else
							{
								if(null == this.properties)
								{
									this.tibcoConnectionFactory = new TIBCO.EMS.ConnectionFactory(this.brokerUri.OriginalString, this.clientId);
								}
								else
								{
									this.tibcoConnectionFactory = new TIBCO.EMS.ConnectionFactory(this.brokerUri.OriginalString, this.clientId, this.properties);
								}
							}
						}
					}
				}
				catch(Exception ex)
				{
					ExceptionUtil.WrapAndThrowNMSException(ex);
				}
			}
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
		/// <summary>
		/// A Delegate that is called each time a Message is dispatched to allow the client to do
		/// any necessary transformations on the received message before it is delivered.  The
		/// ConnectionFactory sets the provided delegate instance on each Connection instance that
		/// is created from this factory, each connection in turn passes the delegate along to each
		/// Session it creates which then passes that along to the Consumers it creates.
		/// </summary>
		public ConsumerTransformerDelegate ConsumerTransformer
		{
			get { return this.consumerTransformer; }
			set { this.consumerTransformer = value; }
		}

		private ProducerTransformerDelegate producerTransformer;
		/// <summary>
		/// A delegate that is called each time a Message is sent from this Producer which allows
		/// the application to perform any needed transformations on the Message before it is sent.
		/// The ConnectionFactory sets the provided delegate instance on each Connection instance that
		/// is created from this factory, each connection in turn passes the delegate along to each
		/// Session it creates which then passes that along to the Producers it creates.
		/// </summary>
		public ProducerTransformerDelegate ProducerTransformer
		{
			get { return this.producerTransformer; }
			set { this.producerTransformer = value; }
		}

		#endregion
	}
}
