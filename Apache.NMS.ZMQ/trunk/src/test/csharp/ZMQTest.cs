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
using System.Messaging;
using NUnit.Framework;
using System.Threading;
using System.IO;

namespace Apache.NMS.ZMQ
{
	/// <summary>
	/// Use to test and verify ZMQ behavior
	/// </summary>
	[TestFixture]
	public class ZMQTest
	{
		private bool receivedTestMessage = true;

		[SetUp]
		public void SetUp()
		{
			// Setup before each test
		}

		[TearDown]
		public void TearDown()
		{
			// Clean up after each test
		}

		[Test]
		public void TestReceive()
		{
			////////////////////////////
			// Dependencies check
			////////////////////////////
			string libFolder = System.Environment.CurrentDirectory;
			string libFileName;

			libFileName = Path.Combine(libFolder, "libzmq.dll");
			Assert.IsTrue(File.Exists(libFileName), "Missing zmq library file: {0}", libFileName);
			libFileName = Path.Combine(libFolder, "clrzmq.dll");
			Assert.IsTrue(File.Exists(libFileName), "Missing zmq wrapper file: {0}", libFileName);
			libFileName = Path.Combine(libFolder, "Apache.NMS.dll");
			Assert.IsTrue(File.Exists(libFileName), "Missing Apache.NMS library file: {0}", libFileName);
			libFileName = Path.Combine(libFolder, "Apache.NMS.ZMQ.dll");
			Assert.IsTrue(File.Exists(libFileName), "Missing Apache.NMS.ZMQ library file: {0}", libFileName);

			////////////////////////////
			// Factory check
			////////////////////////////
			IConnectionFactory factory = new ConnectionFactory("tcp://localhost:5556", "");
			Assert.IsNotNull(factory, "Error creating connection factory.");

			////////////////////////////
			// Connection check
			////////////////////////////
			IConnection connection = null;
			try
			{
				connection = factory.CreateConnection();
				Assert.IsNotNull(connection, "problem creating connection class, usually problem with libzmq and clrzmq ");
			}
			catch(System.Exception ex1)
			{
				Assert.Fail("Problem creating connection, make sure dependencies are present. Error: {0}", ex1.Message);
			}

			////////////////////////////
			// Session check
			////////////////////////////
			ISession session = connection.CreateSession();
			// Is session good?
			Assert.IsNotNull(session, "Error creating Session.");

			////////////////////////////
			// Consumer check
			////////////////////////////
			IQueue testQueue = session.GetQueue("ZMQTestQueue");
			Assert.IsNotNull(testQueue, "Error creating test queue.");
			IMessageConsumer consumer = session.CreateConsumer(testQueue);
			Assert.IsNotNull(consumer, "Error creating consumer.");

			consumer.Listener += OnMessage;

			////////////////////////////
			// Producer check
			////////////////////////////
			IMessageProducer producer = session.CreateProducer(testQueue);
			Assert.IsNotNull(consumer, "Error creating producer.");

			ITextMessage testMsg = producer.CreateTextMessage("Zero Message.");
			Assert.IsNotNull(testMsg, "Error creating test message.");

			producer.Send(testMsg);

			////////////////////////////
			// Listener check
			////////////////////////////
			DateTime startWaitTime = DateTime.Now;
			TimeSpan maxWaitTime = TimeSpan.FromSeconds(10);

			while(!receivedTestMessage)
			{
				if((DateTime.Now - startWaitTime) > maxWaitTime)
				{
					Assert.Fail("Timeout waiting for message receive.");
				}

				Thread.Sleep(5);
			}
		}

		/// <summary>
		/// Receive messages sent to consumer.
		/// </summary>
		/// <param name="message"></param>
		private void OnMessage(IMessage message)
		{
			Assert.IsInstanceOf<ITextMessage>(message, "Wrong message type received.");
			ITextMessage textMsg = (ITextMessage) message;
			Assert.AreEqual(textMsg.Text, "Zero Message.");
			receivedTestMessage = true;
		}
	}
}



