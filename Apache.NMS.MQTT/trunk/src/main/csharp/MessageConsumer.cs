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
using Apache.NMS.MQTT.Messages;
using Apache.NMS.MQTT.Util;
using Apache.NMS.MQTT.Threads;
using Apache.NMS.Util;

namespace Apache.NMS.MQTT
{
	public class MessageConsumer : IMessageConsumer, IDispatcher
	{
        private readonly Session session;
        private readonly MessageTransformation messageTransformation;
        private readonly MessageDispatchChannel unconsumedMessages;
        private readonly LinkedList<MessageDispatch> dispatchedMessages = new LinkedList<MessageDispatch>();

		private readonly Atomic<bool> started = new Atomic<bool>();

        private Exception failureError;
		private ThreadPoolExecutor executor;
		private int consumerId;

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


		public event MessageListener Listener
		{
			add
			{
				CheckClosed();

				bool wasStarted = this.session.Started;

				if(wasStarted)
				{
					this.session.Stop();
				}

				listener += value;
				//this.session.Redispatch(this.unconsumedMessages);

				if(wasStarted)
				{
					this.session.Start();
				}
			}
			remove { listener -= value; }
		}

		public int ConsumerId
		{
			get { return this.consumerId; }
		}

		#endregion

		public void Start()
		{
			if(this.unconsumedMessages.Closed)
			{
				return;
			}

			this.started.Value = true;
			this.unconsumedMessages.Start();
			this.session.Executor.Wakeup();
		}

		public void Stop()
		{
			this.started.Value = false;
			this.unconsumedMessages.Stop();
		}

		public bool Iterate()
		{
			if(this.listener != null)
			{
				MessageDispatch dispatch = this.unconsumedMessages.DequeueNoWait();
				if(dispatch != null)
				{
					//this.Dispatch(dispatch);
					return true;
				}
			}

			return false;
		}

		private void CheckClosed()
		{
			if(this.unconsumedMessages.Closed)
			{
				throw new NMSException("The Consumer has been Closed");
			}
		}

		private void CheckMessageListener()
		{
			if(this.listener != null)
			{
				throw new NMSException("Cannot set Async listeners on Consumers with a prefetch limit of zero");
			}
		}

	    protected bool IsAutoAcknowledge
		{
			get { return this.session.IsAutoAcknowledge; }
		}

        protected bool IsIndividualAcknowledge
		{
			get { return this.session.IsIndividualAcknowledge; }
		}

        protected bool IsClientAcknowledge
		{
			get { return this.session.IsClientAcknowledge; }
		}

	    internal bool Closed
	    {
            get { return this.unconsumedMessages.Closed; }
	    }

	}
}

