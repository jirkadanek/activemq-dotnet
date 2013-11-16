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

namespace Apache.NMS.MQTT.Transport
{
    public abstract class BaseCommand : Command, ICloneable
    {
        private int commandId;
        private bool responseRequired = false;

        public int CommandId
        {
            get { return commandId; }
            set { this.commandId = value; }
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
            // Since we are a derived class use the base's Clone()
            // to perform the shallow copy. Since it is shallow it
            // will include our derived class. Since we are derived,
            // this method is an override.
            BaseCommand o = (BaseCommand) base.Clone();

            return o;
        }
    }
}

