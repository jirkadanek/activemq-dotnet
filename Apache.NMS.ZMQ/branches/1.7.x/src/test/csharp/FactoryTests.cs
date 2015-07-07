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
using NUnit.Framework;

namespace Apache.NMS.ZMQ
{
	[TestFixture]
	public class FactoryTests : BaseTest
	{
		[Test]
		public void TestFactory()
		{
			IConnectionFactory factory = NMSConnectionFactory.CreateConnectionFactory(new Uri("zmq:tcp://localhost:5556"));
			Assert.IsNotNull(factory, "Error creating connection factory.");
			Assert.IsInstanceOf<ConnectionFactory>(factory, "Wrong factory type.");
			Assert.AreEqual(factory.BrokerUri.Port, 5556, "Wrong port.");
		}

		[Test]
		public void TestFactoryClientId()
		{
			IConnectionFactory factory = NMSConnectionFactory.CreateConnectionFactory(new Uri("zmq:tcp://localhost:5556"), "MyClientId");
			Assert.IsNotNull(factory, "Error creating connection factory.");
			Assert.IsInstanceOf<ConnectionFactory>(factory, "Wrong factory type.");
			Assert.AreEqual(factory.BrokerUri.Port, 5556, "Wrong port.");
			ConnectionFactory zmqConnectionFactory = (ConnectionFactory) factory;
			Assert.AreEqual(zmqConnectionFactory.ClientId, "MyClientId", "Wrong client Id.");
		}

		[Test, ExpectedException(typeof(NMSConnectionException))]
		public void TestFactoryUriMissingPort()
		{
			IConnectionFactory factory = NMSConnectionFactory.CreateConnectionFactory(new Uri("zmq:tcp://localhost"));
		}

		[Test]
		public void TestFactoryDefault()
		{
			IConnectionFactory factory = new ConnectionFactory();
			Assert.IsNotNull(factory, "Error creating connection factory.");
			Assert.AreEqual(factory.BrokerUri.Port, 5556, "Wrong default port.");
		}

		[Test]
		public void TestFactoryDirectString()
		{
			IConnectionFactory factory = new ConnectionFactory("tcp://localhost:5556");
			Assert.IsNotNull(factory, "Error creating connection factory.");
			Assert.AreEqual(factory.BrokerUri.Port, 5556, "Wrong port.");
		}

		[Test]
		public void TestFactoryDirectStringClientId()
		{
			IConnectionFactory factory = new ConnectionFactory("tcp://localhost:5556", "DirectClientId");
			Assert.IsNotNull(factory, "Error creating connection factory.");
			Assert.AreEqual(factory.BrokerUri.Port, 5556, "Wrong port.");
			ConnectionFactory zmqConnectionFactory = (ConnectionFactory) factory;
			Assert.AreEqual(zmqConnectionFactory.ClientId, "DirectClientId", "Wrong client Id.");
		}

		[Test]
		public void TestFactoryDirectUri()
		{
			IConnectionFactory factory = new ConnectionFactory(new Uri("tcp://localhost:5556"));
			Assert.IsNotNull(factory, "Error creating connection factory.");
			Assert.AreEqual(factory.BrokerUri.Port, 5556, "Wrong port.");
		}

		[Test]
		public void TestFactoryDirectUriClientId()
		{
			IConnectionFactory factory = new ConnectionFactory(new Uri("tcp://localhost:5556"), "DirectClientId");
			Assert.IsNotNull(factory, "Error creating connection factory.");
			Assert.AreEqual(factory.BrokerUri.Port, 5556, "Wrong port.");
			ConnectionFactory zmqConnectionFactory = (ConnectionFactory) factory;
			Assert.AreEqual(zmqConnectionFactory.ClientId, "DirectClientId", "Wrong client Id.");
		}
	}
}
