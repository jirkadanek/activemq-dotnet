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

namespace Apache.NMS.MQTT.Transport
{
    /// <summary>
    /// Represents a generic Command class that is sent or received via a ITransport
	/// instance.  Commands are marshalled and unmarshalled from binary form.
    /// </summary>
    public interface Command : ICloneable
    {
		byte Header
		{
			get;
		}

		int CommandType
		{
			get;
		}

		string CommandName
		{
			get;
		}

        short CommandId
        {
			get;
        }

        bool ResponseRequired
        {
			get;
        }

		bool IsResponse
		{
			get; 
		}

		bool IsErrorResponse
		{
			get; 
		}

		bool IsMessageDispatch
		{
			get; 
		}

        bool IsCONNECT
        {
			get;
        }

        bool IsCONNACK
        {
			get;
        }

        bool IsDISCONNECT
        {
			get;
        }

        bool IsPINGREQ
        {
			get;
        }

        bool IsPINGRESP
        {
			get;
        }

        bool IsPUBACK
        {
			get;
        }

        bool IsPUBLISH
        {
			get;
        }

        bool IsPUBREC
        {
			get;
        }

        bool IsPUBREL
        {
			get;
        }

        bool IsPUBCOMP
        {
			get;
        }

        bool IsSUBACK
        {
			get;
        }

        bool IsSUBSCRIBE
        {
			get;
        }

        bool IsUNSUBACK
        {
			get;
        }

        bool IsUNSUBSCRIBE
        {
			get;
        }

		void Encode(BinaryWriter writer);

		void Decode(BinaryReader reader);

    }
}

