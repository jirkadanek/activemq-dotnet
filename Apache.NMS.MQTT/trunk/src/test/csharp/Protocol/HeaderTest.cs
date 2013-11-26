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
using Apache.NMS.Test;
using Apache.NMS.MQTT.Protocol;
using NUnit.Framework;

namespace Apache.NMS.MQTT.Test.Protocol
{
	[TestFixture]
	public class HeaderTest
	{
		public HeaderTest()
		{
		}

        [Test]
		public void TestHeaderProperties()
		{
			Header header = new Header(1, 0, false, false);

			Assert.AreEqual(1, header.Type, "Wrong Type returned");
			Assert.AreEqual(0, header.QoS, "Wrong QoS returned");
			Assert.AreEqual(false, header.Dup, "Wrong Dup flag returned");
			Assert.AreEqual(false, header.Retain, "Wrong Retain flag returned");

			header.Type = 2;
			header.QoS = 1;
			header.Retain = true;
			header.Dup = true;

			Assert.AreEqual(2, header.Type, "Wrong Type returned");
			Assert.AreEqual(1, header.QoS, "Wrong QoS returned");
			Assert.AreEqual(true, header.Dup, "Wrong Dup flag returned");
			Assert.AreEqual(true, header.Retain, "Wrong Retain flag returned");
		}
	}
}

