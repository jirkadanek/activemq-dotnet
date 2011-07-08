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
using System.Reflection;

namespace Apache.NMS.ZMQ
{
	/// <summary>
	/// A Factory that will instantiate a 32-bit or 64-bit version of the provider
	/// as determined by the runtime environment.  This factory delegates instantiation responsibilities
	/// to the real factory.  This is only in place so that the correct bit-version can be loaded.
	/// This factory indirection is necessary due to the low-level dependency on a 32-bit or 64-bit native DLL.
	/// To avoid a DLL load failure, we have to ensure we dynamically link to the correct version.
	/// </summary>
	public class ConnectionFactory : IConnectionFactory
	{
		// Checking the runtime size of an IntPtr will tell us our runtime environment.
		// 32-bit runtime's IntPtr size is 4.
		// 64-bit runtimes' ItrPtr size is 8.
		static private bool is32bit = (IntPtr.Size == 4);

		private static Type factoryType = null;
		private IConnectionFactory connFactory = null;

		private const string DEFAULT_BROKER_URL = "tcp://localhost:5556";
		private const string ENV_BROKER_URL = "ZMQ_BROKER_URL";

		/// <summary>
		/// Static class constructor that is executed only once before any normal object constructors.
		/// This is the type constructor.
		/// </summary>
		static ConnectionFactory()
		{
			// Load the assembly and get the type.
			string assemblyFileName = (is32bit ? "Apache.NMS.ZMQ32.dll" : "Apache.NMS.ZMQ64.dll");
			Assembly assembly;

			try
			{
				assembly = Assembly.Load(assemblyFileName);
				if(null != assembly)
				{
					Tracer.DebugFormat("Succesfully loaded provider: {0}", assemblyFileName);
					factoryType = assembly.GetType("Apache.NMS.ZMQ.ConnectionFactory", true, true);
				}
			}
			catch(Exception ex)
			{
				Tracer.ErrorFormat("Exception loading assembly {0} failed: {1}", assemblyFileName, ex.Message);
				factoryType = null;
			}
		}

		private static string GetDefaultBrokerUrl()
		{
			string brokerUrl = Environment.GetEnvironmentVariable(ENV_BROKER_URL);

			if(string.IsNullOrEmpty(brokerUrl))
			{
				brokerUrl = DEFAULT_BROKER_URL;
			}

			return brokerUrl;
		}

		public ConnectionFactory()
			: this(GetDefaultBrokerUrl())
		{
		}

		public ConnectionFactory(string brokerUri)
			: this(brokerUri, null)
		{
		}

		public ConnectionFactory(string brokerUri, string clientID)
			: this(new Uri(brokerUri), clientID)
		{
		}

		public ConnectionFactory(Uri brokerUri)
			: this(brokerUri, null)
		{
		}

		public ConnectionFactory(Uri brokerUri, string clientID)
		{
			if(null == factoryType)
			{
				throw new ApplicationException("Could not load the ZMQ connection factory assembly.");
			}

			connFactory = (IConnectionFactory) Activator.CreateInstance(factoryType, new object[] { brokerUri, clientID });
		}

		#region IConnectionFactory Members

		public Uri BrokerUri
		{
			get { return connFactory.BrokerUri; }
			set { connFactory.BrokerUri = value; }
		}

		public ConsumerTransformerDelegate ConsumerTransformer
		{
			get { return connFactory.ConsumerTransformer; }
			set { connFactory.ConsumerTransformer = value; }
		}

		public IConnection CreateConnection(string userName, string password)
		{
			return connFactory.CreateConnection(userName, password);
		}

		public IConnection CreateConnection()
		{
			return connFactory.CreateConnection();
		}

		public ProducerTransformerDelegate ProducerTransformer
		{
			get { return connFactory.ProducerTransformer; }
			set { connFactory.ProducerTransformer = value; }
		}

		public IRedeliveryPolicy RedeliveryPolicy
		{
			get { return connFactory.RedeliveryPolicy; }
			set { connFactory.RedeliveryPolicy = value; }
		}

		#endregion
	}
}
