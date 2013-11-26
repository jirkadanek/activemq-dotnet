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
using Apache.NMS.MQTT.Commands;
using Apache.NMS.MQTT.Transport;

namespace Apache.NMS.MQTT.Protocol
{
	public class MQTTCommandFactory
	{
		private MQTTCommandFactory()
		{
		}

		public static Command CreateCommand(byte fixedHeader)
		{
			Header header = new Header(fixedHeader);

			Command result = null;

			switch (header.Type)
			{
			case 1:
				result = new CONNECT(header);
				break;
			case 2:
				result = new CONNACK(header);
				break;
			case 3:
				result = new PUBLISH(header);
				break;
			case 4:
				result = new PUBACK(header);
				break;
			case 5:
				result = new PUBREC(header);
				break;
			case 6:
				result = new PUBREL(header);
				break;
			case 7:
				result = new PUBCOMP(header);
				break;
			case 8:
				result = new SUBSCRIBE(header);
				break;
			case 9:
				result = new SUBACK(header);
				break;
			case 10:
				result = new UNSUBSCRIBE(header);
				break;
			case 11:
				result = new UNSUBACK(header);
				break;
			case 12:
				result = new PINGREQ(header);
				break;
			case 13:
				result = new PINGRESP(header);
				break;
			case 14:
				result = new DISCONNECT(header);
				break;
			default:
				throw new NMSException("Unknown Command received");
			}

			return result;
		}
	}
}

