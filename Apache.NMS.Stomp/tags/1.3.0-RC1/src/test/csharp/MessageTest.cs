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
using NUnit.Framework.Extensions;
using Apache.NMS.Util;
using Apache.NMS.Test;

namespace Apache.NMS.Stomp.Test
{
    [TestFixture]
    public class MessageTest : NMSTestSupport
    {
        protected static string DESTINATION_NAME = "MessagePropsDestination";
        protected static string TEST_CLIENT_ID = "MessagePropsClientId";

        protected bool		a = true;
        protected byte		b = 123;
        protected char		c = 'c';
        protected short		d = 0x1234;
        protected int		e = 0x12345678;
        protected long		f = 0x1234567812345678;
        protected string	g = "Hello World!";
        protected bool		h = false;
        protected byte		i = 0xFF;
        protected short		j = -0x1234;
        protected int		k = -0x12345678;
        protected long		l = -0x1234567812345678;
        protected float		m = 2.1F;
        protected double	n = 2.3;

        [RowTest]
        [Row(MsgDeliveryMode.Persistent)]
        [Row(MsgDeliveryMode.NonPersistent)]
        public void SendReceiveMessageIdComparisonTest(MsgDeliveryMode deliveryMode)
        {
            using(IConnection connection = CreateConnection(TEST_CLIENT_ID + ":" + new Random().Next()))
            {
                connection.Start();
                using(ISession session = connection.CreateSession(AcknowledgementMode.AutoAcknowledge))
                {
                    IDestination destination = SessionUtil.GetDestination(session, DESTINATION_NAME);
                    using(IMessageConsumer consumer = session.CreateConsumer(destination))
                    using(IMessageProducer producer = session.CreateProducer(destination))
                    {
                        producer.DeliveryMode = deliveryMode;
                        producer.RequestTimeout = receiveTimeout;
                        IMessage request1 = session.CreateMessage();
                        IMessage request2 = session.CreateMessage();
                        IMessage request3 = session.CreateMessage();

                        producer.Send(request1);
                        producer.Send(request2);
                        producer.Send(request3);

                        IMessage message1 = consumer.Receive(receiveTimeout);
                        Assert.IsNotNull(message1, "No message returned!");
                        IMessage message2 = consumer.Receive(receiveTimeout);
                        Assert.IsNotNull(message2, "No message returned!");
                        IMessage message3 = consumer.Receive(receiveTimeout);
                        Assert.IsNotNull(message3, "No message returned!");

                        Assert.AreNotEqual(message1.NMSMessageId, message2.NMSMessageId);
                        Assert.AreNotEqual(message1.NMSMessageId, message3.NMSMessageId);
                        Assert.AreNotEqual(message2.NMSMessageId, message3.NMSMessageId);
                    }
                }
            }
        }

        [RowTest]
        [Row(MsgDeliveryMode.Persistent)]
        [Row(MsgDeliveryMode.NonPersistent)]
        public void SendReceiveMessageProperties(MsgDeliveryMode deliveryMode)
        {
            using(IConnection connection = CreateConnection(TEST_CLIENT_ID + ":" + new Random().Next()))
            {
                connection.Start();
                using(ISession session = connection.CreateSession(AcknowledgementMode.AutoAcknowledge))
                {
                    IDestination destination = SessionUtil.GetDestination(session, DESTINATION_NAME);
                    using(IMessageConsumer consumer = session.CreateConsumer(destination))
                    using(IMessageProducer producer = session.CreateProducer(destination))
                    {
                        producer.DeliveryMode = deliveryMode;
                        producer.RequestTimeout = receiveTimeout;
                        IMessage request = session.CreateMessage();
                        request.Properties["a"] = a;
                        request.Properties["b"] = b;
                        request.Properties["c"] = c;
                        request.Properties["d"] = d;
                        request.Properties["e"] = e;
                        request.Properties["f"] = f;
                        request.Properties["g"] = g;
                        request.Properties["h"] = h;
                        request.Properties["i"] = i;
                        request.Properties["j"] = j;
                        request.Properties["k"] = k;
                        request.Properties["l"] = l;
                        request.Properties["m"] = m;
                        request.Properties["n"] = n;
                        producer.Send(request);

                        IMessage message = consumer.Receive(receiveTimeout);
                        Assert.IsNotNull(message, "No message returned!");
                        Assert.AreEqual(request.Properties.Count, message.Properties.Count, "Invalid number of properties.");
                        Assert.AreEqual(deliveryMode, message.NMSDeliveryMode, "NMSDeliveryMode does not match");
                        Assert.AreEqual(ToHex(f), ToHex(message.Properties.GetLong("f")), "map entry: f as hex");

                        // use generic API to access entries
                        Assert.AreEqual(a.ToString(), message.Properties["a"], "generic map entry: a");
                        Assert.AreEqual(b.ToString(), message.Properties["b"], "generic map entry: b");
                        Assert.AreEqual(c.ToString(), message.Properties["c"], "generic map entry: c");
                        Assert.AreEqual(d.ToString(), message.Properties["d"], "generic map entry: d");
                        Assert.AreEqual(e.ToString(), message.Properties["e"], "generic map entry: e");
                        Assert.AreEqual(f.ToString(), message.Properties["f"], "generic map entry: f");
                        Assert.AreEqual(g.ToString(), message.Properties["g"], "generic map entry: g");
                        Assert.AreEqual(h.ToString(), message.Properties["h"], "generic map entry: h");
                        Assert.AreEqual(i.ToString(), message.Properties["i"], "generic map entry: i");
                        Assert.AreEqual(j.ToString(), message.Properties["j"], "generic map entry: j");
                        Assert.AreEqual(k.ToString(), message.Properties["k"], "generic map entry: k");
                        Assert.AreEqual(l.ToString(), message.Properties["l"], "generic map entry: l");
                        Assert.AreEqual(m.ToString(), message.Properties["m"], "generic map entry: m");
                        Assert.AreEqual(n.ToString(), message.Properties["n"], "generic map entry: n");

                        // use type safe APIs
                        Assert.AreEqual(a, message.Properties.GetBool("a"),   "map entry: a");
                        Assert.AreEqual(b, message.Properties.GetByte("b"),   "map entry: b");
                        Assert.AreEqual(c.ToString(), message.Properties.GetString("c"),   "map entry: c");
                        Assert.AreEqual(d, message.Properties.GetShort("d"),  "map entry: d");
                        Assert.AreEqual(e, message.Properties.GetInt("e"),    "map entry: e");
                        Assert.AreEqual(f, message.Properties.GetLong("f"),   "map entry: f");
                        Assert.AreEqual(g, message.Properties.GetString("g"), "map entry: g");
                        Assert.AreEqual(h, message.Properties.GetBool("h"),   "map entry: h");
                        Assert.AreEqual(i, message.Properties.GetByte("i"),   "map entry: i");
                        Assert.AreEqual(j, message.Properties.GetShort("j"),  "map entry: j");
                        Assert.AreEqual(k, message.Properties.GetInt("k"),    "map entry: k");
                        Assert.AreEqual(l, message.Properties.GetLong("l"),   "map entry: l");
                        Assert.AreEqual(m, message.Properties.GetFloat("m"),  "map entry: m");
                        Assert.AreEqual(n, message.Properties.GetDouble("n"), "map entry: n");
                    }
                }
            }
        }
    }
}

