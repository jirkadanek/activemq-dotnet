//
// Licensed to the Apache Software Foundation (ASF) under one or more
// contributor license agreements.  See the NOTICE file distributed with
// this work for additional information regarding copyright ownership.
// The ASF licenses this file to You under the Apache License, Version 2.0
// (the "License"); you may not use this file except in compliance with
// the License.  You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
using System;
using System.Net.Sockets;
using Apache.NMS.Test;
using Apache.NMS.MQTT;
using NUnit.Framework;

namespace Apache.NMS.MQTT.Test
{
	[TestFixture]
	public class ConnectionFactoryTest
	{
        [SetUp]
        public void SetUp()
        {
            Apache.NMS.Tracer.Trace = new NmsConsoleTracer();
        }

        [Test]
        [TestCase("tcp://${activemqhost}:1883")]
//      [TestCase("stomp:failover:(tcp://${activemqhost}:1883?keepAlive=false&wireFormat.maxInactivityDuration=1000)")]
//      [TestCase("stomp:failover:(tcp://${activemqhost}:1883?keepAlive=false&wireFormat.maxInactivityDuration=1000)?connection.asyncSend=false")]
//		[TestCase("stomp:tcp://${activemqhost}:1883?connection.asyncsend=false")]
//		[TestCase("stomp:tcp://${activemqhost}:1883?connection.InvalidParameter=true", ExpectedException = typeof(NMSConnectionException))]
//		[TestCase("stomp:tcp://${activemqhost}:1883?connection.InvalidParameter=true", ExpectedException = typeof(NMSConnectionException))]
//		[TestCase("stomp:(tcp://${activemqhost}:1883)?connection.asyncSend=false", ExpectedException = typeof(NMSConnectionException))]
//		[TestCase("stomp:tcp://InvalidHost:1883", ExpectedException = typeof(NMSConnectionException))]
//		[TestCase("stomp:tcp://InvalidHost:1883", ExpectedException = typeof(NMSConnectionException))]
//		[TestCase("stomp:tcp://InvalidHost:1883?connection.asyncsend=false", ExpectedException = typeof(NMSConnectionException))]
//		[TestCase("ftp://${activemqhost}:1883", ExpectedException = typeof(NMSConnectionException))]
//		[TestCase("http://${activemqhost}:1883", ExpectedException = typeof(NMSConnectionException))]
//		[TestCase("discovery://${activemqhost}:1888", ExpectedException = typeof(NMSConnectionException))]
//		[TestCase("sms://${activemqhost}:1883", ExpectedException = typeof(NMSConnectionException))]
//		[TestCase("stomp:multicast://${activemqhost}:6155", ExpectedException = typeof(NMSConnectionException))]
//		[TestCase("(tcp://${activemqhost}:1883,tcp://${activemqhost}:1883)", ExpectedException = typeof(UriFormatException))]
//		[TestCase("tcp://${activemqhost}:1883,tcp://${activemqhost}:1883", ExpectedException = typeof(UriFormatException))]
        public void TestURI(string connectionURI)
        {
            IConnectionFactory factory = new ConnectionFactory(
				NMSTestSupport.ReplaceEnvVar(connectionURI));
            Assert.IsNotNull(factory);
            using(IConnection connection = factory.CreateConnection("", ""))
            {
                Assert.IsNotNull(connection);
            }
        }

        [Test]
        [TestCase("tcp://${activemqhost}:1883")]
        public void TestConnectionStarts(string connectionURI)
        {
			NMS.Tracer.Trace = new NmsConsoleTracer();
            IConnectionFactory factory = new ConnectionFactory(
				NMSTestSupport.ReplaceEnvVar(connectionURI));
            Assert.IsNotNull(factory);
            using(IConnection connection = factory.CreateConnection("", ""))
            {
                Assert.IsNotNull(connection);
				// This should trigger a CONNECT frame and CONNACK response.
				connection.Start();
            }
        }

	}
}

