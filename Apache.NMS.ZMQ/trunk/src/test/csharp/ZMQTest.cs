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
using System.Threading;
using NUnit.Framework;

namespace Apache.NMS.ZMQ
{
	[TestFixture]
	public class ZMQTest : BaseTest
	{
		private bool receivedTestMessage = true;

		[Test]
		public void TestConnection()
		{
			IConnectionFactory factory = NMSConnectionFactory.CreateConnectionFactory(new Uri("zmq:tcp://localhost:5556"));
			Assert.IsNotNull(factory, "Error creating connection factory.");
			using(IConnection connection = factory.CreateConnection())
			{
				Assert.IsNotNull(connection, "Problem creating connection class. Usually problem with libzmq and clrzmq ");
				Assert.IsInstanceOf<Connection>(connection, "Wrong connection type.");
			}
		}

		[Test]
		public void TestSession()
		{
			IConnectionFactory factory = NMSConnectionFactory.CreateConnectionFactory(new Uri("zmq:tcp://localhost:5556"));
			Assert.IsNotNull(factory, "Error creating connection factory.");
			using(IConnection connection = factory.CreateConnection())
			{
				Assert.IsNotNull(connection, "Problem creating connection class. Usually problem with libzmq and clrzmq ");
				using(ISession session = connection.CreateSession())
				{
					Assert.IsNotNull(session, "Error creating session.");
					Assert.IsInstanceOf<Session>(session, "Wrong session type.");
				}
			}
		}

		[Test, Sequential]
		public void TestDestinations(
			[Values("queue://ZMQTestQueue", "topic://ZMQTestTopic", "temp-queue://ZMQTempQueue", "temp-topic://ZMQTempTopic")]
			string destination,
			[Values(typeof(Queue), typeof(Topic), typeof(TemporaryQueue), typeof(TemporaryTopic))]
			Type destinationType)
		{
			IConnectionFactory factory = NMSConnectionFactory.CreateConnectionFactory(new Uri("zmq:tcp://localhost:5556"));
			Assert.IsNotNull(factory, "Error creating connection factory.");
			using(IConnection connection = factory.CreateConnection())
			{
				Assert.IsNotNull(connection, "Problem creating connection class. Usually problem with libzmq and clrzmq ");
				using(ISession session = connection.CreateSession())
				{
					Assert.IsNotNull(session, "Error creating session.");
					IDestination testDestination = session.GetDestination(destination);
					Assert.IsNotNull(testDestination, "Error creating test destination: {0}", destination);
					Assert.IsInstanceOf(destinationType, testDestination, "Wrong destintation type.");
				}
			}
		}

		[Test]
		public void TestProducers(
			[Values("queue://ZMQTestQueue", "topic://ZMQTestTopic", "temp-queue://ZMQTempQueue", "temp-topic://ZMQTempTopic")]
			string destination)
		{
			IConnectionFactory factory = NMSConnectionFactory.CreateConnectionFactory(new Uri("zmq:tcp://localhost:5556"));
			Assert.IsNotNull(factory, "Error creating connection factory.");
			using(IConnection connection = factory.CreateConnection())
			{
				Assert.IsNotNull(connection, "Problem creating connection class. Usually problem with libzmq and clrzmq ");
				using(ISession session = connection.CreateSession())
				{
					Assert.IsNotNull(session, "Error creating session.");
					IDestination testDestination = session.GetDestination(destination);
					Assert.IsNotNull(testDestination, "Error creating test destination: {0}", destination);
					using(IMessageProducer producer = session.CreateProducer(testDestination))
					{
						Assert.IsNotNull(producer, "Error creating producer on {0}", destination);
						Assert.IsInstanceOf<MessageProducer>(producer, "Wrong producer type.");
					}
				}
			}
		}

		[Test]
		public void TestConsumers(
			[Values("queue://ZMQTestQueue:", "topic://ZMQTestTopic", "temp-queue://ZMQTempQueue", "temp-topic://ZMQTempTopic")]
			string destination)
		{
			IConnectionFactory factory = NMSConnectionFactory.CreateConnectionFactory(new Uri("zmq:tcp://localhost:5556"));
			Assert.IsNotNull(factory, "Error creating connection factory.");
			using(IConnection connection = factory.CreateConnection())
			{
				Assert.IsNotNull(connection, "Problem creating connection class. Usually problem with libzmq and clrzmq ");
				using(ISession session = connection.CreateSession())
				{
					Assert.IsNotNull(session, "Error creating session.");
					IDestination testDestination = session.GetDestination(destination);
					Assert.IsNotNull(testDestination, "Error creating test destination: {0}", destination);
					using(IMessageConsumer consumer = session.CreateConsumer(testDestination))
					{
						Assert.IsNotNull(consumer, "Error creating consumer on {0}", destination);
						Assert.IsInstanceOf<MessageConsumer>(consumer, "Wrong consumer type.");
					}
				}
			}
		}

		[Test]
		public void TestSendReceive(
			[Values("queue://ZMQTestQueue", "topic://ZMQTestTopic", "temp-queue://ZMQTempQueue", "temp-topic://ZMQTempTopic")]
			string destination)
		{
			IConnectionFactory factory = NMSConnectionFactory.CreateConnectionFactory(new Uri("zmq:tcp://localhost:5556"));
			Assert.IsNotNull(factory, "Error creating connection factory.");
			using(IConnection connection = factory.CreateConnection())
			{
				Assert.IsNotNull(connection, "Problem creating connection class. Usually problem with libzmq and clrzmq ");
				using(ISession session = connection.CreateSession())
				{
					Assert.IsNotNull(session, "Error creating Session.");
					IDestination testDestination = session.GetDestination(destination);
					Assert.IsNotNull(testDestination, "Error creating test destination: {0}", destination);
					using(IMessageConsumer consumer = session.CreateConsumer(testDestination))
					{
						Assert.IsNotNull(consumer, "Error creating consumer on {0}", destination);
						consumer.Listener += OnMessage;
						using(IMessageProducer producer = session.CreateProducer(testDestination))
						{
							Assert.IsNotNull(consumer, "Error creating producer on {0}", destination);
							ITextMessage testMsg = producer.CreateTextMessage("Zero Message.");
							Assert.IsNotNull(testMsg, "Error creating test message.");
							producer.Send(testMsg);
						}

						// Wait for the message
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
				}
			}
		}

		/// <summary>
		/// Receive messages sent to consumer.
		/// </summary>
		/// <param name="message"></param>
		private void OnMessage(IMessage message)
		{
			Assert.IsInstanceOf<TextMessage>(message, "Wrong message type received.");
			ITextMessage textMsg = (ITextMessage) message;
			Assert.AreEqual(textMsg.Text, "Zero Message.");
			receivedTestMessage = true;
		}
	}
}
