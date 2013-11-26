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
using Apache.NMS.Util;
using Apache.NMS.MQTT.Commands;
using Apache.NMS.MQTT.Transport;

namespace Apache.NMS.MQTT.Protocol
{
	public class MQTTWireFormat : IWireFormat
	{
		private ITransport transport;

		public MQTTWireFormat()
		{
		}

        public void Marshal(Command cmd, BinaryWriter ds)
		{
			MemoryStream buffer = new MemoryStream();
			EndianBinaryWriter writer = new EndianBinaryWriter(buffer);

			byte fixedHeader = cmd.Header;
			cmd.Encode(writer);

			ds.Write(fixedHeader);
			WriteLength((int)buffer.Length, ds);
            ds.Write(buffer.GetBuffer(), 0, (int) buffer.Length);
		}

        public Command Unmarshal(BinaryReader dis)
		{
			byte fixedHeader = dis.ReadByte();

			Command cmd = MQTTCommandFactory.CreateCommand(fixedHeader);

			// Variable length header gives us total Message length to buffer.
			int length = ReadLength(dis);

			if (length != 0)
			{
				byte[] buffer = dis.ReadBytes(length);

				if (buffer.Length != length)
				{
					throw new IOException("Invalid stream read occurred.");
				}

				MemoryStream ms = new MemoryStream(buffer);
				EndianBinaryReader reader = new EndianBinaryReader(ms);

				cmd.Decode(reader);
			}

			// A CONNACK is a response, but if it has an error code, then we create a suitable
			// ErrorResponse here with the correct NMSException in its payload.
			if (cmd.IsCONNACK && cmd.IsErrorResponse)
			{
				CONNACK connAck = cmd as CONNACK;
				ErrorResponse error = new ErrorResponse();
				error.Error = MQTTExceptionFactory.CreateConnectionException(connAck.ReturnCode);
				cmd = error;
			}

			return cmd;
		}

		public ITransport Transport
		{
			get { return this.transport; }
			set { this.transport = value; }
		}

		/// <summary>
		/// Writes the variable length portion of the MQTT Message to the given stream
		/// </summary>
		internal void WriteLength(int length, BinaryWriter writer)
		{
			do 
			{
				byte digit = (byte) (length % 0x80);
				length /= 0x80;
				// if there are more digits to encode, set the top bit of this digit 
				if(length > 0)
				{
					digit |= 0x80;
				}

				writer.Write(digit);
			}
			while (length > 0);
		}

		/// <summary>
		/// Reads the varianle length header from the given stream.
		/// </summary>
		internal int ReadLength(BinaryReader reader)
		{
			int multiplier = 1;
			int length = 0;

			while (true)
			{
				byte digit = reader.ReadByte();
				length += (digit & 0x7F) * multiplier;
				if ((digit & 0x80) == 0)
				{
					break;
				}
				multiplier *= 0x80;
			}

			return length;
		}
	}
}

