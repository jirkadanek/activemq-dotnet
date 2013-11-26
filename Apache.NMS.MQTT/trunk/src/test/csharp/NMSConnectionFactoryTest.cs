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
using System.Net.Sockets;
using Apache.NMS.Test;
using NUnit.Framework;

namespace Apache.NMS.MQTT.Test
{
    [TestFixture]
    public class NMSConnectionFactoryTest
    {
//      [Test]
//      [TestCase("mqtt:tcp://${activemqhost}:61613")]
//      [TestCase("stomp:failover:(tcp://${activemqhost}:61616?keepAlive=false&wireFormat.maxInactivityDuration=1000)")]
//      [TestCase("stomp:failover:(tcp://${activemqhost}:61616?keepAlive=false&wireFormat.maxInactivityDuration=1000)?connection.asyncSend=false")]
//		[TestCase("stomp:tcp://${activemqhost}:61613?connection.asyncsend=false")]
//		[TestCase("stomp:tcp://${activemqhost}:61613?connection.InvalidParameter=true", ExpectedException = typeof(NMSConnectionException))]
//		[TestCase("stomp:tcp://${activemqhost}:61613?connection.InvalidParameter=true", ExpectedException = typeof(NMSConnectionException))]
//		[TestCase("stomp:(tcp://${activemqhost}:61613)?connection.asyncSend=false", ExpectedException = typeof(NMSConnectionException))]
//		[TestCase("stomp:tcp://InvalidHost:61613", ExpectedException = typeof(NMSConnectionException))]
//		[TestCase("stomp:tcp://InvalidHost:61613", ExpectedException = typeof(NMSConnectionException))]
//		[TestCase("stomp:tcp://InvalidHost:61613?connection.asyncsend=false", ExpectedException = typeof(NMSConnectionException))]
//		[TestCase("ftp://${activemqhost}:61613", ExpectedException = typeof(NMSConnectionException))]
//		[TestCase("http://${activemqhost}:61613", ExpectedException = typeof(NMSConnectionException))]
//		[TestCase("discovery://${activemqhost}:6155", ExpectedException = typeof(NMSConnectionException))]
//		[TestCase("sms://${activemqhost}:61613", ExpectedException = typeof(NMSConnectionException))]
//		[TestCase("stomp:multicast://${activemqhost}:6155", ExpectedException = typeof(NMSConnectionException))]
//		[TestCase("(tcp://${activemqhost}:61613,tcp://${activemqhost}:61613)", ExpectedException = typeof(UriFormatException))]
//		[TestCase("tcp://${activemqhost}:61613,tcp://${activemqhost}:61613", ExpectedException = typeof(UriFormatException))]
        public void TestURI(string connectionURI)
        {
            NMSConnectionFactory factory = new NMSConnectionFactory(
				NMSTestSupport.ReplaceEnvVar(connectionURI));
            Assert.IsNotNull(factory);
            Assert.IsNotNull(factory.ConnectionFactory);
            using(IConnection connection = factory.CreateConnection("", ""))
            {
                Assert.IsNotNull(connection);
            }
        }
	}
}
