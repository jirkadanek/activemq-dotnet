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

namespace Apache.NMS.MQTT.Commands
{
	/// <summary>
	/// The payload part of a PUBLISH message contains application-specific data only. No
    /// assumptions are made about the nature or content of the data, and this part of the
    /// message is treated as a BLOB.
	/// 
    /// If you want an application to apply compression to the payload data, you need to define
    /// in the application the appropriate payload flag fields to handle the compression details.
    /// You cannot define application-specific flags in the fixed or variable headers.
	/// </summary>
	public class PUBLISH
	{
		public const byte TYPE = 3;

		public int CommandType
		{
			get { return TYPE; }
		}

		public int CommandName
		{
			get { return "PUBLISH"; }
		}

		private byte qosLevel;
		public byte QoSLevel
		{
			get { return this.qosLevel; }
			set { this.qosLevel = value; }
		}

		private bool duplicate;
		public bool Duplicate
		{
			get { return this.duplicate; }
			set { this.duplicate = value; }
		}

		private bool retain;
		public bool Retain
		{
			get { return this.retain; }
			set { this.retain = value; }
		}

		private short messageId;
		public short MessageId
		{
			get { return this.messageId; }
			set { this.messageId = value; }
		}

		private String topicName;
		public String TopicName
		{
			get { return this.topicName; }
			set { this.topicName = value; }
		}

		private byte[] payload;
		public byte[] Payload
		{
			get { return this.payload; }
			set { this.payload = value; }
		}

		public PUBLISH()
		{
		}
	}
}

