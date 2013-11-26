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

using System;
using Apache.NMS.MQTT.Transport;

namespace Apache.NMS.MQTT.Messages
{
    /*
     *
     *  Command code for OpenWire format for MessageDispatch
     *
     *  NOTE!: This file is auto generated - do not modify!
     *         if you need to make a change, please see the Java Classes
     *         in the nms-activemq-openwire-generator module
     *
     */
    public class MessageDispatch
    {
        Topic destination;
        MQTTMessage message;

        ///
        /// <summery>
        ///  Returns a string containing the information for this DataStructure
        ///  such as its type and value of its elements.
        /// </summery>
        ///
        public override string ToString()
        {
            return GetType().Name + "[ " + 
                "Destination = " + Destination + ", " + 
                "Message = " + Message + " ]";
        }

        public Topic Destination
        {
            get { return destination; }
            set { this.destination = value; }
        }

        public MQTTMessage Message
        {
            get { return message; }
            set { this.message = value; }
        }

        public override int GetHashCode()
        {
            int answer = 0;

            answer = (answer * 37) + Destination.GetHashCode();
            answer = (answer * 37) + Message.GetHashCode();

            return answer;
        }

        public override bool Equals(object that)
        {
            if(that is MessageDispatch)
            {
                return Equals((MessageDispatch) that);
            }

            return false;
        }

        public virtual bool Equals(MessageDispatch that)
        {
            if(!Equals(this.Destination, that.Destination))
            {
                return false;
            }
            if(!Equals(this.Message, that.Message))
            {
                return false;
            }

            return true;
        }

        ///
        /// <summery>
        ///  Return an answer of true to the isMessageDispatch() query.
        /// </summery>
        ///
        public bool IsMessageDispatch
        {
            get { return true; }
        }
    };
}

