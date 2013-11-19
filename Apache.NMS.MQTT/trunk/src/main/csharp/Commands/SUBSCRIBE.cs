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
using Apache.NMS.MQTT.Transport;

namespace Apache.NMS.MQTT.Commands
{
	/// <summary>
	/// The payload contains a list of topic names to which the client can subscribe, and
    /// the QoS level. These strings are UTF-encoded.
	/// </summary>
	public class SUBSCRIBE : BaseCommand
	{
		public const byte TYPE = 7;

		public override int CommandType
		{
			get { return TYPE; }
		}

		public override string CommandName
		{
			get { return "SUBSCRIBE"; }
		}

		public override bool IsSUBSCRIBE
		{
			get { return true; }
		}
	}
}

