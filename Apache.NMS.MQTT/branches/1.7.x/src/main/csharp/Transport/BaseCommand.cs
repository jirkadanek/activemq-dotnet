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
using Apache.NMS.MQTT.Protocol;

namespace Apache.NMS.MQTT.Transport
{
    public abstract class BaseCommand : Command, ICloneable
    {
		private Header header;
        private short commandId;
        private bool responseRequired = false;

		public BaseCommand(Header header)
		{
			this.header = header;
		}

		public byte Header
		{
			get { return this.header.RawValue; }
			set { this.header.RawValue = value; }
		}

        public short CommandId
        {
            get { return commandId; }
            set { this.commandId = value; }
        }

		public virtual int CommandType
		{
			get { return -1; }
		}

		public virtual string CommandName
		{
			get { return this.GetType().Name; }
		}

        public override int GetHashCode()
        {
            return (CommandId * 37) + CommandType;
        }

        public override bool Equals(Object that)
        {
            if(that is BaseCommand)
            {
                BaseCommand thatCommand = (BaseCommand) that;
                return this.CommandType == thatCommand.CommandType &&
                       this.CommandId == thatCommand.CommandId;
            }
            return false;
        }

        public override String ToString()
        {
            string answer = CommandName;
            if(answer.Length == 0)
            {
                answer = base.ToString();
            }
            return answer + ": id = " + CommandId;
        }

        public virtual bool ResponseRequired
        {
            get { return responseRequired; }
            set { responseRequired = value; }
        }

		public virtual bool IsResponse
		{
			get { return false; }
		}

		public virtual bool IsErrorResponse
		{
			get { return false; }
		}

		public virtual bool IsMessageDispatch
		{
			get { return false; }
		}

        public virtual bool IsCONNECT
        {
			get { return false; }
        }

        public virtual bool IsCONNACK
        {
			get { return false; }
        }

        public virtual bool IsDISCONNECT
        {
			get { return false; }
        }

        public virtual bool IsPINGREQ
        {
			get { return false; }
        }

        public virtual bool IsPINGRESP
        {
			get { return false; }
        }

        public virtual bool IsPUBACK
        {
			get { return false; }
        }

        public virtual bool IsPUBLISH
        {
			get { return false; }
        }

        public virtual bool IsPUBREC
        {
			get { return false; }
        }

        public virtual bool IsPUBREL
        {
			get { return false; }
        }

        public virtual bool IsPUBCOMP
		{
			get { return false; }
        }

        public virtual bool IsSUBACK
        {
			get { return false; }
        }

        public virtual bool IsSUBSCRIBE
        {
			get { return false; }
        } 

        public virtual bool IsUNSUBACK
        {
			get { return false; }
        }

        public virtual bool IsUNSUBSCRIBE
        {
			get { return false; }
        }

        public virtual Object Clone()
        {
            return this.MemberwiseClone();
        }

		public int HashCode(object value)
		{
			if(value != null)
			{
				return value.GetHashCode();
			}
			else
			{
				return -1;
			}
		}

		public virtual void Encode(BinaryWriter writer)
		{
			throw new NotImplementedException("Command doesn't implement Encode");
		}

		public virtual void Decode(BinaryReader reader)
		{
			throw new NotImplementedException("Command doesn't implement Decode");
		}
    }
}

