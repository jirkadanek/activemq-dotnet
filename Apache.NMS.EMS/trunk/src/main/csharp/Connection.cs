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
using Apache.NMS.Util;

namespace Apache.NMS.EMS
{
    /// <summary>
    /// Represents a NMS connection to TIBCO.
    /// </summary>
    ///
    public class Connection : Apache.NMS.IConnection
    {
    	private Apache.NMS.AcknowledgementMode acknowledgementMode;
    	public readonly TIBCO.EMS.Connection tibcoConnection;
		private ConnectionMetaData metaData = null;
		private readonly Atomic<bool> started = new Atomic<bool>(false);
		private bool closed = false;
    	private bool disposed = false;

		public Connection(TIBCO.EMS.Connection cnx)
		{
			this.tibcoConnection = cnx;
			this.tibcoConnection.ExceptionHandler += this.HandleTibcoException;
		}

		~Connection()
		{
			Dispose(false);
		}

    	#region IStartable Members

		/// <summary>
        /// Starts message delivery for this connection.
        /// </summary>
        public void Start()
        {
			if(started.CompareAndSet(false, true))
			{
				this.tibcoConnection.Start();
			}
		}

		public bool IsStarted
		{
			get { return this.started.Value; }
		}

		#endregion

		#region IStoppable Members

		/// <summary>
        /// Stop message delivery for this connection.
        /// </summary>
        public void Stop()
        {
			if(started.CompareAndSet(true, false))
			{
	            this.tibcoConnection.Stop();
			}
		}

		#endregion

		#region IConnection Members

		/// <summary>
        /// Creates a new session to work on this connection
        /// </summary>
		public Apache.NMS.ISession CreateSession()
        {
            return CreateSession(acknowledgementMode);
        }
        
        /// <summary>
        /// Creates a new session to work on this connection
        /// </summary>
		public Apache.NMS.ISession CreateSession(Apache.NMS.AcknowledgementMode mode)
        {
			bool isTransacted = (Apache.NMS.AcknowledgementMode.Transactional == mode);
			return EMSConvert.ToNMSSession(this.tibcoConnection.CreateSession(isTransacted,
			                                                  EMSConvert.ToSessionMode(mode)));
		}

		public void Close()
		{
			lock(this)
			{
				if(closed)
				{
					return;
				}

				this.tibcoConnection.ExceptionHandler -= this.HandleTibcoException;
				this.tibcoConnection.Stop();
				this.tibcoConnection.Close();
				closed = true;
			}
		}

		#endregion

		#region IDisposable Members

		public void Dispose()
        {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected void Dispose(bool disposing)
		{
			if(disposed)
			{
				return;
			}

			if(disposing)
			{
				// Dispose managed code here.
			}

			try
			{
				Close();
			}
			catch
			{
				// Ignore errors.
			}

			disposed = true;
		}

		#endregion

		#region Attributes

		/// <summary>
		/// The default timeout for network requests.
		/// </summary>
		public TimeSpan RequestTimeout
		{
			get { return Apache.NMS.NMSConstants.defaultRequestTimeout; }
			set { }
		}

		public Apache.NMS.AcknowledgementMode AcknowledgementMode
        {
            get { return acknowledgementMode; }
            set { acknowledgementMode = value; }
        }

        public string ClientId
        {
            get { return this.tibcoConnection.ClientID; }
            set { this.tibcoConnection.ClientID = value; }
        }

		/// <summary>
		/// Gets the Meta Data for the NMS Connection instance.
		/// </summary>
		public IConnectionMetaData MetaData
		{
			get { return this.metaData ?? (this.metaData = new ConnectionMetaData()); }
		}

		#endregion

		/// <summary>
		/// A delegate that can receive transport level exceptions.
		/// </summary>
		public event ExceptionListener ExceptionListener;

		/// <summary>
		/// An asynchronous listener that is notified when a Fault tolerant connection
		/// has been interrupted.
		/// </summary>
		public event ConnectionInterruptedListener ConnectionInterruptedListener;

		/// <summary>
		/// An asynchronous listener that is notified when a Fault tolerant connection
		/// has been resumed.
		/// </summary>
		public event ConnectionResumedListener ConnectionResumedListener;

		private void HandleTibcoException(object sender, TIBCO.EMS.EMSExceptionEventArgs arg)
		{
			if(ExceptionListener != null)
			{
				ExceptionListener(arg.Exception);
			}
			else
			{
				Apache.NMS.Tracer.Error(arg.Exception);
			}
		}

		private void HandleTransportInterrupted()
		{
			Tracer.Debug("Transport has been Interrupted.");

			if(this.ConnectionInterruptedListener != null && !this.closed)
			{
				try
				{
					this.ConnectionInterruptedListener();
				}
				catch
				{
				}
			}
		}

		private void HandleTransportResumed()
		{
			Tracer.Debug("Transport has resumed normal operation.");

			if(this.ConnectionResumedListener != null && !this.closed)
			{
				try
				{
					this.ConnectionResumedListener();
				}
				catch
				{
				}
			}
		}
	}
}
