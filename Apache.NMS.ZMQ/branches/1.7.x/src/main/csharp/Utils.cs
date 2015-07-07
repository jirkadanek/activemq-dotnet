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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Apache.NMS.ZMQ
{
	public class ZMQUtils
	{

		public static string GetDestinationName(IDestination destination)
		{
			switch(destination.DestinationType)
			{
			case DestinationType.Topic: return ((Topic) destination).TopicName;
			case DestinationType.Queue: return ((Queue) destination).QueueName;
			case DestinationType.TemporaryTopic: return ((TemporaryTopic) destination).TopicName;
			case DestinationType.TemporaryQueue: return ((TemporaryQueue) destination).QueueName;
			default: return string.Empty;
			}
		}
	}
}
