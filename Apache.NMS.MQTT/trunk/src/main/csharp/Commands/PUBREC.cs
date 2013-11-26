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
using System.IO;
using Apache.NMS.MQTT.Transport;
using Apache.NMS.MQTT.Protocol;

namespace Apache.NMS.MQTT.Commands
{
	public class PUBREC : BaseCommand
	{
		public const byte TYPE = 5;
		public const byte DEFAULT_HEADER = 0x50;

		public PUBREC() : base(new Header(DEFAULT_HEADER))
		{
		}

		public PUBREC(Header header) : base(header)
		{
		}

		public override int CommandType
		{
			get { return TYPE; }
		}

		public override bool IsPUBREC
		{
			get { return true; }
		}
	}
}

