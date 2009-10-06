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

namespace Apache.NMS.EMS
{
	class StreamMessage : Apache.NMS.EMS.Message, Apache.NMS.IStreamMessage
	{
		public TIBCO.EMS.StreamMessage tibcoStreamMessage
		{
			get { return this.tibcoMessage as TIBCO.EMS.StreamMessage; }
			set { this.tibcoMessage = value; }
		}

		public StreamMessage(TIBCO.EMS.StreamMessage message)
			: base(message)
		{
		}

		#region IStreamMessage Members

		public bool ReadBoolean()
		{
			return this.tibcoStreamMessage.ReadBoolean();
		}

		public byte ReadByte()
		{
			return this.tibcoStreamMessage.ReadByte();
		}

		public int ReadBytes(byte[] value)
		{
			return this.tibcoStreamMessage.ReadBytes(value);
		}

		public char ReadChar()
		{
			return this.tibcoStreamMessage.ReadChar();
		}

		public double ReadDouble()
		{
			return this.tibcoStreamMessage.ReadDouble();
		}

		public short ReadInt16()
		{
			return this.tibcoStreamMessage.ReadShort();
		}

		public int ReadInt32()
		{
			return this.tibcoStreamMessage.ReadInt();
		}

		public long ReadInt64()
		{
			return this.tibcoStreamMessage.ReadLong();
		}

		public object ReadObject()
		{
			return this.tibcoStreamMessage.ReadObject();
		}

		public float ReadSingle()
		{
			return this.tibcoStreamMessage.ReadFloat();
		}

		public string ReadString()
		{
			return this.tibcoStreamMessage.ReadString();
		}

		public void Reset()
		{
			this.tibcoStreamMessage.Reset();
		}

		public void WriteBoolean(bool value)
		{
			this.tibcoStreamMessage.WriteBoolean(value);
		}

		public void WriteByte(byte value)
		{
			this.tibcoStreamMessage.WriteByte(value);
		}

		public void WriteBytes(byte[] value, int offset, int length)
		{
			this.tibcoStreamMessage.WriteBytes(value, offset, length);
		}

		public void WriteBytes(byte[] value)
		{
			this.tibcoStreamMessage.WriteBytes(value);
		}

		public void WriteChar(char value)
		{
			this.tibcoStreamMessage.WriteChar(value);
		}

		public void WriteDouble(double value)
		{
			this.tibcoStreamMessage.WriteDouble(value);
		}

		public void WriteInt16(short value)
		{
			this.tibcoStreamMessage.WriteShort(value);
		}

		public void WriteInt32(int value)
		{
			this.tibcoStreamMessage.WriteInt(value);
		}

		public void WriteInt64(long value)
		{
			this.tibcoStreamMessage.WriteLong(value);
		}

		public void WriteObject(object value)
		{
			this.tibcoStreamMessage.WriteObject(value);
		}

		public void WriteSingle(float value)
		{
			this.tibcoStreamMessage.WriteFloat(value);
		}

		public void WriteString(string value)
		{
			this.tibcoStreamMessage.WriteString(value);
		}

		#endregion
	}
}
