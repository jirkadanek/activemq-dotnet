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

using Apache.NMS;

namespace Apache.NMS.MQTT.Protocol
{
	public class MQTTExceptionFactory
	{
		private MQTTExceptionFactory ()
		{
		}

		static NMSException CreateConnectionException(short errorCode)
		{
			NMSException result = null;

			if (errorCode == 1)
			{
				result = new NMSException("Invalid MQTT Protocol Version specified");
			}
			else if(errorCode == 2)
			{
				result = new InvalidClientIDException("Client ID not accepted by Broker");
			}
			else if(errorCode == 3)
			{
				result = new InvalidClientIDException("Server is Unavailable");
			}
			else if(errorCode == 4)
			{
				result = new NMSSecurityException("Bad user anem or password provided.");
			}
			else if(errorCode == 5)
			{
				result = new NMSSecurityException("User is not Authorized.");
			}
			else
			{
				result = new NMSException("Received unknown error code.");
			}

			return result;
		}
	}
}

