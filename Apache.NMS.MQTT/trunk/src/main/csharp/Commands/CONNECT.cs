//
// Licensed to the Apache Software Foundation (ASF) under one or more
// contributor license agreements.  See the NOTICE file distributed with
// this work for additional information regarding copyright ownership.
// The ASF licenses this file to You under the Apache License, Version 2.0
// (the "License"); you may not use this file except in compliance with
// the License.  You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// 
using System;
using System.IO;
using System.Text;
using Apache.NMS.Util;
using Apache.NMS.MQTT.Transport;
using Apache.NMS.MQTT.Protocol;

namespace Apache.NMS.MQTT.Commands
{
	/// <summary>
	/// Connection initiation command sent from client to server.
	/// 
	/// The payload contains one or more UTF-8 encoded strings. They specify a unqiue
    /// identifier for the client, a Will topic and message and the User Name and Password
    /// to use. All but the first are optional and their presence is determined based on flags
    /// in the variable header.
	/// </summary>
	public class CONNECT : BaseCommand
	{
		public const byte TYPE = 1;
		public const byte DEFAULT_HEADER = 0x10;
		public const String PROTOCOL_NAME = "MQIsdp";
		private static byte[] PROTOCOL_NAME_ENCODED;

		/// <summary>
		/// Static init of properly encoded UTF8 bytes for the Protocol Name, this saves
		/// us the work of encoding the same value for every message send.
		/// </summary>
		static CONNECT()
		{
			MemoryStream stream = new MemoryStream();
			EndianBinaryWriter writer = new EndianBinaryWriter(stream);
			short value = (short) Encoding.UTF8.GetByteCount(PROTOCOL_NAME);
			writer.Write(value);
			writer.Write(Encoding.UTF8.GetBytes(PROTOCOL_NAME));

			PROTOCOL_NAME_ENCODED = stream.ToArray();
		}

		public CONNECT() : base(new Header(DEFAULT_HEADER))
		{
		}

		public CONNECT(Header header) : base(header)
		{
		}

		public override int CommandType
		{
			get { return TYPE; }
		}

		public override bool IsCONNECT
		{
			get { return true; }
		}

		private byte version = 3;
		public byte Version
		{
			get { return this.version; }
			set { this.version = value; }
		}

		private bool willRetain;
		public bool WillRetain
		{
			get { return this.willRetain; }
			set { this.willRetain = value; }
		}

		private byte willQoS = 3;
		public byte WillQoS
		{
			get { return this.willQoS; }
			set { this.willQoS = value; }
		}

		private String username;
		public String UserName
		{
			get { return this.username; }
			set { this.username = value; }
		}

		private String password;
		public String Password
		{
			get { return this.password; }
			set { this.password = value; }
		}

		private String clientId;
		public String ClientId
		{
			get { return this.clientId; }
			set { this.clientId = value; }
		}

		private bool cleanSession;
		public bool CleanSession
		{
			get { return this.cleanSession; }
			set { this.cleanSession = value; }
		}

		private short keepAliveTimer = 10;
		public short KeepAliveTimer
		{
			get { return this.keepAliveTimer; }
			set { this.keepAliveTimer = value; }
		}

		private String willTopic;
		public String WillTopic
		{
			get { return this.willTopic; }
			set { this.willTopic = value; }
		}

		private String willMessage;
		public String WillMessage
		{
			get { return this.willMessage; }
			set { this.willMessage = value; }
		}

		public override void Encode(BinaryWriter writer)
		{
			writer.Write(PROTOCOL_NAME_ENCODED);
			writer.Write(Version);

			byte contentFlags = 0;

			if (!String.IsNullOrEmpty(username))
			{
				contentFlags |= 0x80;
			}
			if (!String.IsNullOrEmpty(username))
			{
				contentFlags |= 0x40;
			}
			if (!String.IsNullOrEmpty(WillTopic) && !String.IsNullOrEmpty(WillMessage))
			{
				contentFlags |= 0x04;
				if (WillRetain)
				{
					contentFlags |= 0x20;
				}
				contentFlags |= (byte)((WillQoS << 3) & 0x18);
			}
			if (CleanSession)
			{
				contentFlags |= 0x02;
			}

			writer.Write(contentFlags);
			writer.Write(KeepAliveTimer);
			writer.Write(ClientId);

			if (!String.IsNullOrEmpty(WillTopic) && !String.IsNullOrEmpty(WillMessage))
			{
				writer.Write(WillTopic);
				writer.Write(WillMessage);
			}
			if (!String.IsNullOrEmpty(username))
			{
				writer.Write(UserName);
			}
			if (!String.IsNullOrEmpty(username))
			{
				writer.Write(Password);
			}
		}

		public override void Decode(BinaryReader reader)
		{
			String protocolName = reader.ReadString();
			if (!PROTOCOL_NAME.Equals(protocolName))
			{
				throw new IOException("Invalid Protocol Name: " + protocolName);
			}

			this.version = reader.ReadByte();
			byte contentFlags = reader.ReadByte();

			bool hasUsername = (contentFlags & 0x80) != 0;
			bool hasPassword = (contentFlags & 0x40) != 0;
			bool hasWillTopic = (contentFlags & 0x04) != 0;

			WillRetain = (contentFlags & 0x20) != 0;
			WillQoS = (byte)((contentFlags & 0x18) >> 3);
			CleanSession = (contentFlags & 0x02) != 0;

			KeepAliveTimer = reader.ReadInt16();
			ClientId = reader.ReadString();
			if (hasWillTopic)
			{
				WillTopic = reader.ReadString();
				WillMessage = reader.ReadString();
			}
			if (hasUsername)
			{
				UserName = reader.ReadString();
			}
			if (hasPassword)
			{
				Password = reader.ReadString();
			}
		}
	}
}

