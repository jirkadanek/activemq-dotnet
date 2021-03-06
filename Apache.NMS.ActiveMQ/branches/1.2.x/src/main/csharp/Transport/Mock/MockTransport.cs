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
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Apache.NMS.ActiveMQ.Commands;
using Apache.NMS.ActiveMQ.Transport;
using Apache.NMS.ActiveMQ.Threads;
using Apache.NMS.Util;

#if NETCF
using ThreadInterruptedException = System.Exception;
#endif

namespace Apache.NMS.ActiveMQ.Transport.Mock
{	
	/// <summary>
	/// Transport used for testing, mimics the behaviour of a normal Transport and allows
	/// messages to be sent and received 
	/// </summary>
	public class MockTransport : ITransport
	{
        #region Properties
        
        private bool failOnSendMessage = false;
        private int numSentMessagesBeforeFail = -1;
        private int numSentMessages = 0;
        private bool failOnReceiveMessage = false;
        private int numReceivedMessagesBeforeFail = 0;
        private int numReceivedMessages = 0;
        private bool failOnKeepAliveInfoSends = false;
        private int numSentKeepAliveInfosBeforeFail = 0;
        private int numSentKeppAliveInfos = 0;
        private int nextCommandId = 0;
        private CommandHandler commandHandler;
        private CommandHandler outgoingCommandHandler;
        private ExceptionHandler exceptionHandler;
        private InterruptedHandler interruptedHandler;
        private ResumedHandler resumedHandler;
        private bool disposed = false;
        private bool started = false;
        private TimeSpan requestTimeout = TimeSpan.FromMilliseconds(Timeout.Infinite);
        private TaskRunner asyncResponseTask;
        private Queue<Command> receiveQueue = new Queue<Command>();
        private IResponseBuilder responseBuilder = new OpenWireResponseBuilder();

        #endregion   
        
        #region Async Response Task
        
        private class AsyncResponseTask : Task
        {
            private MockTransport parent;
            
            public AsyncResponseTask( MockTransport parent )
            {
                this.parent = parent;
            }
            
            public bool Iterate()
            {   
                Command command = null;
                
                lock(this.parent.receiveQueue)
                {
                    if( this.parent.receiveQueue.Count == 0 )
                    {
                        return false;
                    }
                    
                    // Grab everything that's currently in the Queue, 
                    command = this.parent.receiveQueue.Dequeue();
                }
                
                if( command.IsMessage ) {
                    this.parent.NumReceivedMessages++;

                    if( this.parent.FailOnReceiveMessage && 
                        this.parent.NumReceivedMessages > this.parent.NumReceivedMessagesBeforeFail ) {
                        
                        Tracer.Debug("MockTransport Async Task: Performing configured receive failure." );
                        this.parent.Exception(this.parent, new IOException( "Failed to Receive Message."));
                    }
                }                
                             
                // Send all the responses.
                Tracer.Debug("MockTransport Async Task: Simulate receive of Command: " + command.ToString() );
                this.parent.Command(this.parent, command);
                
                return parent.receiveQueue.Count != 0;
            }
        }
        
        #endregion        

        public MockTransport()
        {
            Tracer.Debug("Creating Async Response task");
            asyncResponseTask = DefaultThreadPools.DefaultTaskRunnerFactory.CreateTaskRunner(new AsyncResponseTask(this),
                                "ActiveMQ MockTransport Worker: " + this.GetHashCode().ToString());            
        }
        
        ~MockTransport()
        {
            Dispose(false);
        }
        
        public Response Request(Command command)
        {
            return this.Request(command, TimeSpan.FromMilliseconds(Timeout.Infinite));
        }

        public Response Request(Command command, TimeSpan timeout)
        {
            Tracer.Debug("MockTransport sending Request Command: " + command.ToString() );
            
            if( command.IsMessage ) {
                this.numSentMessages++;
    
                if( this.failOnSendMessage && this.numSentMessages > this.numSentMessagesBeforeFail ) {
                    throw new IOException( "Failed to Send Message.");
                }
            }

            // Notify external Client of command that we "sent"
            if( this.OutgoingCommand != null )
            {
                this.OutgoingCommand(this, command);
            }

            command.CommandId = Interlocked.Increment(ref this.nextCommandId);
            command.ResponseRequired = true;
            
            return this.responseBuilder.BuildResponse(command);
        }
        
        public void Oneway(Command command)
        {
            Tracer.Debug("MockTransport sending oneway Command: " + command.ToString() );

            if( command.IsMessage ) {
                this.numSentMessages++;

                if( this.failOnSendMessage && this.numSentMessages > this.numSentMessagesBeforeFail ) {
                    Tracer.Debug("MockTransport Oneway send, failing as per configuration." );
                    throw new IOException( "Failed to Send Message.");
                }
            }

            if( command.IsKeepAliveInfo ) {
                this.numSentKeppAliveInfos++;

                if( this.failOnKeepAliveInfoSends && this.numSentKeppAliveInfos > this.numSentKeepAliveInfosBeforeFail ) {
                    Tracer.Debug("MockTransport Oneway send, failing as per configuration." );
                    throw new IOException( "Failed to Send Message.");
                }
            }
            
            // Process and send any new Commands back.

            // Let the Response Builder give us the Commands to send to the Client App.
            List<Command> results = this.responseBuilder.BuildIncomingCommands(command);
            
            lock(this.receiveQueue)
            {
                foreach( Command result in results )
                {
                    this.receiveQueue.Enqueue(result);
                }
            }
            
            this.asyncResponseTask.Wakeup();
            
            // Send the Command to the Outgoing Command Snoop Hook.
            if( this.OutgoingCommand != null ) {
                Tracer.Debug("MockTransport Oneway, Notifying Outgoing linstener." );
                this.OutgoingCommand(this, command);
            }
        }
        
        public FutureResponse AsyncRequest(Command command)
        {
            FutureResponse response = new FutureResponse();
            
            // Delegate to the Request method, it doesn't block.
            response.Response = this.Request(command);
            
            return response;
        }
        
        public void Start()
        {
            if(commandHandler == null)
            {
                throw new InvalidOperationException("command cannot be null when Start is called.");
            }

            if(exceptionHandler == null)
            {
                throw new InvalidOperationException("exception cannot be null when Start is called.");
            }

            this.started = true;
        }
                
        public void Stop()
        {
            this.started = false;
        }
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            this.started = false;
            this.disposed = true;
        }
        
        /// <summary>
        /// Injects a Command into the Transports inbound message queue, the Commands in the
        /// inbound Queue are dispatched to the registered CommnadHandler instance for 
        /// processing, this simulates receiving a message from an external source, e.g.
        /// receiving a new message from the Broker.
        /// </summary>
        /// <param name="command">
        /// A <see cref="Command"/>
        /// </param>
        public void InjectCommand(Command command)
        {
            lock(this.receiveQueue)
            {
                this.receiveQueue.Enqueue(command);
            }
            
            this.asyncResponseTask.Wakeup();
        }

        public Object Narrow(Type type)
        {
            if( this.GetType().Equals(type) )
            {
                return this;
            }

            return null;
        }
        
		#region Property Accessors
		
        public TimeSpan RequestTimeout
        {
            get{ return requestTimeout; }
            set{ this.requestTimeout = value; }
        }

        public CommandHandler Command
        {
            get { return commandHandler; }
            set { this.commandHandler = value; }
        }

        public CommandHandler OutgoingCommand
        {
            get { return outgoingCommandHandler; }
            set { this.outgoingCommandHandler = value; }
        }
        
        public ExceptionHandler Exception
        {
            get { return exceptionHandler; }
            set { this.exceptionHandler = value; }
        }

        public InterruptedHandler Interrupted
        {
            get { return interruptedHandler; }
            set { this.interruptedHandler = value; }
        }
        
        public ResumedHandler Resumed
        {
            get { return resumedHandler; }
            set { this.resumedHandler = value; }
        }
        
        public bool IsDisposed
        {
            get{ return this.disposed; }
        }
        
        public bool IsStarted
        {
            get{ return this.started; }
        }
                
		public bool FailOnSendMessage
		{
			get{ return failOnSendMessage; }
			set{ this.failOnSendMessage = value; }			
		}
		
		public int NumSentMessagesBeforeFail
		{
			get { return numSentMessagesBeforeFail ; }
			set { numSentMessagesBeforeFail = value; }
		}
		
		public int NumSentMessages
		{
			get { return numSentMessages; }
			set { numSentMessages = value; }
		}
		
		public bool FailOnReceiveMessage
		{
			get { return failOnReceiveMessage; }
			set { failOnReceiveMessage = value; }
		}

		public int NumReceivedMessagesBeforeFail
		{
			get { return numReceivedMessagesBeforeFail; }
			set { numReceivedMessagesBeforeFail = value; }
		}

		public int NumReceivedMessages
		{
			get { return numReceivedMessages; }
			set { numReceivedMessages = value; }
		}

        public bool FailOnKeepAliveInfoSends
        {
            get { return failOnKeepAliveInfoSends; }
            set { failOnKeepAliveInfoSends = value; }
        }

        public int NumSentKeepAliveInfosBeforeFail
        {
            get { return numSentKeepAliveInfosBeforeFail; }
            set { numSentKeepAliveInfosBeforeFail = value; }
        }

        public int NumSentKeppAliveInfos
        {
            get { return numSentKeppAliveInfos; }
            set { numSentKeppAliveInfos = value; }
        }

        public bool IsFaultTolerant
        {
            get{ return false; }
        }

        public bool IsConnected
        {
            get{ return true; }
        }

        public Uri RemoteAddress
        {
            get{ return new Uri("mock://mock"); }
        }
        
        #endregion
	}
}
