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
		private int receivedMsgCount = 0;

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
					using(IDestination testDestination = session.GetDestination(destination))
					{
						Assert.IsNotNull(testDestination, "Error creating test destination: {0}", destination);
						Assert.IsInstanceOf(destinationType, testDestination, "Wrong destintation type.");
					}
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
					using(IDestination testDestination = session.GetDestination(destination))
					{
						Assert.IsNotNull(testDestination, "Error creating test destination: {0}", destination);
						using(IMessageProducer producer = session.CreateProducer(testDestination))
						{
							Assert.IsNotNull(producer, "Error creating producer on {0}", destination);
							Assert.IsInstanceOf<MessageProducer>(producer, "Wrong producer type.");
						}
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
					using(IDestination testDestination = session.GetDestination(destination))
					{
						Assert.IsNotNull(testDestination, "Error creating test destination: {0}", destination);
						using(IMessageConsumer consumer = session.CreateConsumer(testDestination))
						{
							Assert.IsNotNull(consumer, "Error creating consumer on {0}", destination);
							Assert.IsInstanceOf<MessageConsumer>(consumer, "Wrong consumer type.");
						}
					}
				}
			}
		}

		[Test]
		public void TestSendReceive(
			// inproc, ipc, tcp, pgm, or epgm
			[Values("zmq:tcp://localhost:5556", "zmq:inproc://localhost:5557")]
			string connectionName,
			[Values("queue://ZMQTestQueue", "topic://ZMQTestTopic", "temp-queue://ZMQTempQueue", "temp-topic://ZMQTempTopic")]
			string destinationName)
		{
			IConnectionFactory factory = NMSConnectionFactory.CreateConnectionFactory(new Uri(connectionName));
			Assert.IsNotNull(factory, "Error creating connection factory.");

			this.receivedMsgCount = 0;
			using(IConnection connection = factory.CreateConnection())
			{
				Assert.IsNotNull(connection, "Problem creating connection class. Usually problem with libzmq and clrzmq ");
				using(ISession session = connection.CreateSession())
				{
					Assert.IsNotNull(session, "Error creating Session.");
					using(IDestination testDestination = session.GetDestination(destinationName))
					{
						Assert.IsNotNull(testDestination, "Error creating test destination: {0}", destinationName);
						using(IMessageConsumer consumer = session.CreateConsumer(testDestination))
						{
							Assert.IsNotNull(consumer, "Error creating consumer on {0}", destinationName);
							int sendMsgCount = 0;
							try
							{
								consumer.Listener += OnMessage;
								using(IMessageProducer producer = session.CreateProducer(testDestination))
								{
									Assert.IsNotNull(consumer, "Error creating producer on {0}", destinationName);
									ITextMessage testMsg = producer.CreateTextMessage("Zero Message.");
									Assert.IsNotNull(testMsg, "Error creating test message.");

									// Wait for the message
									DateTime startWaitTime = DateTime.Now;
									TimeSpan maxWaitTime = TimeSpan.FromSeconds(5);

									// Continually send the message to compensate for the
									// slow joiner problem inherent to spinning up the
									// internal dispatching threads in ZeroMQ.
									while(this.receivedMsgCount < 1)
									{
										++sendMsgCount;
										producer.Send(testMsg);
										if((DateTime.Now - startWaitTime) > maxWaitTime)
										{
											Assert.Fail("Timeout waiting for message receive.");
										}

										Thread.Sleep(1);
									}
								}
							}
							finally
							{
								consumer.Listener -= OnMessage;
								Console.WriteLine("Sent {0} msgs.\nReceived {1} msgs", sendMsgCount, this.receivedMsgCount);
							}
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
			this.receivedMsgCount++;
		}
	}
}
