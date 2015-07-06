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
using NUnit.Framework;

namespace Apache.NMS.Test
{
	[TestFixture]
	public class ReqResponseTempQueueTest : NMSTestSupport
	{
		[Test]
		public void TestTempQueueHoldsMessagesWithConsumers()
		{
			using(IConnection connection = CreateConnection())
			{
				using(ISession session = connection.CreateSession(AcknowledgementMode.AutoAcknowledge))
				{
					connection.Start();
					ITemporaryQueue queue = session.CreateTemporaryQueue();
					IMessageProducer producer = session.CreateProducer(queue);

					producer.DeliveryMode = (MsgDeliveryMode.NonPersistent);

					for(int correlationCount = 0; correlationCount < 100; correlationCount++)
					{
						string correlationId = string.Format("CID_{0}", correlationCount);
						IMessageConsumer consumer = null;

						try
						{
							ITextMessage message = session.CreateTextMessage("Hello");

							consumer = session.CreateConsumer(queue, string.Format("JMSCorrelationID = '{0}'", correlationId));
							message.NMSCorrelationID = correlationId;
							producer.Send(message);

							IMessage message2 = consumer.Receive(TimeSpan.FromMilliseconds(1000));
							Assert.IsNotNull(message2);
							Assert.AreEqual(correlationId, message2.NMSCorrelationID, "Received the wrong correlation ID message.");
							Assert.IsInstanceOf(typeof(ITextMessage), message2);
							Assert.AreEqual(((ITextMessage)message2).Text, message.Text);
						}
						finally
						{
							if(null != consumer)
							{
								consumer.Close();
							}
						}
					}
				}
			}
		}
	}
}
