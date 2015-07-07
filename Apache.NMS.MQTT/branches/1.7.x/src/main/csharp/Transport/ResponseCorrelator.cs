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
using System.Collections;
using System.Threading;
using Apache.NMS.MQTT.Commands;

namespace Apache.NMS.MQTT.Transport
{
    /// <summary>
    /// A Transport that correlates asynchronous send/receive messages into single request/response.
    /// </summary>
    public class ResponseCorrelator : TransportFilter
    {
        private readonly IDictionary requestMap = Hashtable.Synchronized(new Hashtable());
        private int nextCommandId = 1;  // 1 is always CONNECT -> CONNACK
		private Exception error;

        public ResponseCorrelator(ITransport next) : base(next)
        {
        }

        protected override void OnException(ITransport sender, Exception command)
        {
			Dispose(command);
            base.OnException(sender, command);
        }

        internal short GetNextCommandId()
        {
			if (nextCommandId == UInt16.MaxValue)
			{
				nextCommandId = 1;
			}
            return (short) Interlocked.Increment(ref nextCommandId);
        }

        public override void Oneway(Command command)
        {
            command.CommandId = GetNextCommandId();
			command.ResponseRequired = false;
            next.Oneway(command);
        }

        public override FutureResponse AsyncRequest(Command command)
        {
			if (command.IsCONNECT)
			{
				command.CommandId = (short) 1;
			}
			else
			{
            	command.CommandId = GetNextCommandId();
			}
            command.ResponseRequired = true;
            FutureResponse future = new FutureResponse();
	        Exception priorError = null;
	        lock(requestMap.SyncRoot) 
			{
	            priorError = this.error;
	            if(priorError == null) 
				{
		            requestMap[command.CommandId] = future;
	            }
	        }
	
	        if(priorError != null) 
			{
				ErrorResponse response = new ErrorResponse();
				response.Error = priorError;
	            future.Response = response;
                return future;
	        }
			
            next.Oneway(command);

			return future;
        }

        public override Response Request(Command command, TimeSpan timeout)
        {
            FutureResponse future = AsyncRequest(command);
            future.ResponseTimeout = timeout;
            Response response = future.Response;
            return response;
        }

        protected override void OnCommand(ITransport sender, Command command)
        {
            if(command.IsResponse)
            {
                Response response = (Response) command;
                short correlationId = response.CorrelationId;
                FutureResponse future = (FutureResponse) requestMap[correlationId];

                if(future != null)
                {
                    requestMap.Remove(correlationId);
                    future.Response = response;
                }
                else
                {
                    Tracer.DebugFormat("Unknown response ID: {0} for response: {1}",
                                       response.CorrelationId, response);
                }
            }
            else
            {
                this.commandHandler(sender, command);
            }
        }
		
		public override void Stop()
		{
			this.Dispose(new IOException("Stopped"));
			base.Stop();
		}
		
		private void Dispose(Exception error)
		{
			ArrayList requests = null;
			
	        lock(requestMap.SyncRoot) 
			{
	            if(this.error == null) 
				{
	                this.error = error;
	                requests = new ArrayList(requestMap.Values);
	                requestMap.Clear();
	            }
	        }
			
	        if(requests != null)
			{
				foreach(FutureResponse future in requests)
				{
					ErrorResponse response = new ErrorResponse();
					response.Error = error;
		            future.Response = response;
				}
	        }
		}
		
    }
}


