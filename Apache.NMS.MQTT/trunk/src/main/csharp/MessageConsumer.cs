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
using System.Threading;
using System.Collections.Generic;
using System.Collections.Specialized;
using Apache.NMS.MQTT.Commands;
using Apache.NMS.MQTT.Util;
using Apache.NMS.MQTT.Threads;
using Apache.NMS.Util;

namespace Apache.NMS.MQTT
{
	public class MessageConsumer : IMessageConsumer, IDispatcher
	{
        private readonly MessageTransformation messageTransformation;
        private readonly MessageDispatchChannel unconsumedMessages;
        private readonly LinkedList<MessageDispatch> dispatchedMessages = new LinkedList<MessageDispatch>();

		private readonly Atomic<bool> started = new Atomic<bool>();

        private Exception failureError;
		private ThreadPoolExecutor executor;

		private event MessageListener listener;

		public MessageConsumer()
		{
		}

		#region Property Accessors

        public Exception FailureError
        {
            get { return this.failureError; }
            set { this.failureError = value; }
        }

		private ConsumerTransformerDelegate consumerTransformer;
		/// <summary>
		/// A Delegate that is called each time a Message is dispatched to allow the client to do
		/// any necessary transformations on the received message before it is delivered.
		/// </summary>
		public ConsumerTransformerDelegate ConsumerTransformer
		{
			get { return this.consumerTransformer; }
			set { this.consumerTransformer = value; }
		}

		#endregion

	}
}

