/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

/*
 *
 *  Marshaler code for OpenWire format for MessageId
 *
 *  NOTE!: This file is auto generated - do not modify!
 *         if you need to make a change, please see the Java Classes
 *         in the nms-activemq-openwire-generator module
 *
 */

using System;
using System.Collections;
using System.IO;

using Apache.NMS.ActiveMQ.Commands;
using Apache.NMS.ActiveMQ.OpenWire;
using Apache.NMS.ActiveMQ.OpenWire.V6;

namespace Apache.NMS.ActiveMQ.OpenWire.V6
{
    /// <summary>
    ///  Marshalling code for Open Wire Format for MessageId
    /// </summary>
    class MessageIdMarshaller : BaseDataStreamMarshaller
    {
        /// <summery>
        ///  Creates an instance of the Object that this marshaller handles.
        /// </summery>
        public override DataStructure CreateObject() 
        {
            return new MessageId();
        }

        /// <summery>
        ///  Returns the type code for the Object that this Marshaller handles..
        /// </summery>
        public override byte GetDataStructureType() 
        {
            return MessageId.ID_MESSAGEID;
        }

        // 
        // Un-marshal an object instance from the data input stream
        // 
        public override void TightUnmarshal(OpenWireFormat wireFormat, Object o, BinaryReader dataIn, BooleanStream bs) 
        {
            base.TightUnmarshal(wireFormat, o, dataIn, bs);

            MessageId info = (MessageId)o;
            info.ProducerId = (ProducerId) TightUnmarshalCachedObject(wireFormat, dataIn, bs);
            info.ProducerSequenceId = TightUnmarshalLong(wireFormat, dataIn, bs);
            info.BrokerSequenceId = TightUnmarshalLong(wireFormat, dataIn, bs);
        }

        //
        // Write the booleans that this object uses to a BooleanStream
        //
        public override int TightMarshal1(OpenWireFormat wireFormat, Object o, BooleanStream bs)
        {
            MessageId info = (MessageId)o;

            int rc = base.TightMarshal1(wireFormat, o, bs);
            rc += TightMarshalCachedObject1(wireFormat, (DataStructure)info.ProducerId, bs);
            rc += TightMarshalLong1(wireFormat, info.ProducerSequenceId, bs);
            rc += TightMarshalLong1(wireFormat, info.BrokerSequenceId, bs);

            return rc + 0;
        }

        // 
        // Write a object instance to data output stream
        //
        public override void TightMarshal2(OpenWireFormat wireFormat, Object o, BinaryWriter dataOut, BooleanStream bs)
        {
            base.TightMarshal2(wireFormat, o, dataOut, bs);

            MessageId info = (MessageId)o;
            TightMarshalCachedObject2(wireFormat, (DataStructure)info.ProducerId, dataOut, bs);
            TightMarshalLong2(wireFormat, info.ProducerSequenceId, dataOut, bs);
            TightMarshalLong2(wireFormat, info.BrokerSequenceId, dataOut, bs);
        }

        // 
        // Un-marshal an object instance from the data input stream
        // 
        public override void LooseUnmarshal(OpenWireFormat wireFormat, Object o, BinaryReader dataIn) 
        {
            base.LooseUnmarshal(wireFormat, o, dataIn);

            MessageId info = (MessageId)o;
            info.ProducerId = (ProducerId) LooseUnmarshalCachedObject(wireFormat, dataIn);
            info.ProducerSequenceId = LooseUnmarshalLong(wireFormat, dataIn);
            info.BrokerSequenceId = LooseUnmarshalLong(wireFormat, dataIn);
        }

        // 
        // Write a object instance to data output stream
        //
        public override void LooseMarshal(OpenWireFormat wireFormat, Object o, BinaryWriter dataOut)
        {

            MessageId info = (MessageId)o;

            base.LooseMarshal(wireFormat, o, dataOut);
            LooseMarshalCachedObject(wireFormat, (DataStructure)info.ProducerId, dataOut);
            LooseMarshalLong(wireFormat, info.ProducerSequenceId, dataOut);
            LooseMarshalLong(wireFormat, info.BrokerSequenceId, dataOut);
        }
    }
}