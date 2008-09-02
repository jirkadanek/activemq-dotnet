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
//
//  NOTE!: This file is autogenerated - do not modify!
//         if you need to make a change, please see the Groovy scripts in the
//         activemq-core module
//

using System;
using System.Collections;

using Apache.NMS.ActiveMQ.OpenWire;
using Apache.NMS.ActiveMQ.Commands;

namespace Apache.NMS.ActiveMQ.Commands
{
    /// <summary>
    ///  The ActiveMQ MessagePull Command
    /// </summary>
    public class MessagePull : BaseCommand
    {
        public const byte ID_MessagePull = 20;
    			
        ConsumerId consumerId;
        ActiveMQDestination destination;
        long timeout;

		public override string ToString() {
            return GetType().Name + "["
                + " ConsumerId=" + ConsumerId
                + " Destination=" + Destination
                + " Timeout=" + Timeout
                + " ]";

		}

        public override byte GetDataStructureType() {
            return ID_MessagePull;
        }


        // Properties

        public ConsumerId ConsumerId
        {
            get { return consumerId; }
            set { this.consumerId = value; }            
        }

        public ActiveMQDestination Destination
        {
            get { return destination; }
            set { this.destination = value; }            
        }

        public long Timeout
        {
            get { return timeout; }
            set { this.timeout = value; }            
        }

    }
}
