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
using System.Collections.ObjectModel;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Apache.NMS.WCF
{
	/// <summary>
	/// Factory for message channels.
	/// </summary>
	public class NmsChannelFactory : ChannelFactoryBase<IOutputChannel>
	{
		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="NmsChannelFactory"/> class.
		/// </summary>
		/// <param name="context">The context.</param>
		/// <param name="transportElement">The binding element.</param>
		internal NmsChannelFactory(NmsTransportBindingElement transportElement, BindingContext context)
			: base(context.Binding)
		{
			Collection<MessageEncodingBindingElement> messageEncoderBindingElements = context.BindingParameters.FindAll<MessageEncodingBindingElement>();
			if(messageEncoderBindingElements.Count > 1)
			{
				throw new InvalidOperationException("More than one MessageEncodingBindingElement was found in the BindingParameters of the BindingContext");
			}
			_encoderFactory = (messageEncoderBindingElements.Count == 0)
				? NmsConstants.DefaultMessageEncoderFactory
				: messageEncoderBindingElements[0].CreateMessageEncoderFactory();

			_bufferManager = BufferManager.CreateBufferManager(transportElement.MaxBufferPoolSize, Int32.MaxValue);
			_destination = transportElement.Destination;
			_destinationType = transportElement.DestinationType;

			Tracer.DebugFormat("Destination ({0}) : {1}", _destinationType, _destination);
		}

		#endregion

		#region Implementation of ChannelFactoryBase

		/// <summary>
		/// Inserts processing on a communication object after it transitions into the opening state which must complete within a specified interval of time.
		/// </summary>
		/// <param name="timeout">The <see cref="T:System.TimeSpan" /> that specifies how long the on open operation has to complete before timing out.</param>
		/// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="timeout" /> is less than zero.</exception>
		/// <exception cref="T:System.TimeoutException">The interval of time specified by <paramref name="timeout" /> that was allotted for the operation was exceeded before the operation was completed.</exception>
		protected override void OnOpen(TimeSpan timeout)
		{
			NmsChannelHelper.ValidateTimeout(timeout);
		}

		/// <summary>
		/// Inserts processing on a communication object after it transitions to the opening state due to the invocation of an asynchronous open operation.
		/// </summary>
		/// <returns>
		/// The <see cref="T:System.IAsyncResult" /> that references the asynchronous on open operation. 
		/// </returns>
		/// <param name="timeout">The <see cref="T:System.TimeSpan" /> that specifies how long the on open operation has to complete before timing out.</param>
		/// <param name="callback">The <see cref="T:System.AsyncCallback" /> delegate that receives notification of the completion of the asynchronous on open operation.</param>
		/// <param name="state">An object, specified by the application, that contains state information associated with the asynchronous on open operation.</param>
		/// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="timeout" /> is less than zero.</exception>
		protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new CompletedAsyncResult(callback, state);
		}

		/// <summary>
		/// Completes an asynchronous operation on the open of a communication object.
		/// </summary>
		/// <param name="result">The <see cref="T:System.IAsyncResult" /> that is returned by a call to the <see cref="M:System.ServiceModel.Channels.CommunicationObject.OnEndOpen(System.IAsyncResult)" /> method.</param>
		protected override void OnEndOpen(IAsyncResult result)
		{
			CompletedAsyncResult.End(result);
		}

		/// <summary>
		/// When implemented in a derived class, provides an extensibility point when creating channels.
		/// </summary>
		/// <returns>
		/// An NMS channel with the specified addresses.
		/// </returns>
		/// <param name="address">The <see cref="T:System.ServiceModel.EndpointAddress" /> of the remote endpoint to which the channel sends messages.</param>
		/// <param name="via">The <see cref="T:System.Uri" /> that contains the transport address to which messages are sent on the output channel.</param>
		protected override IOutputChannel OnCreateChannel(EndpointAddress address, Uri via)
		{
			return new NmsOutputChannel(BufferManager, MessageEncoderFactory, address, this, via);
		}

		#endregion

		/// <summary>
		/// Invoked during the transition of a communication object into the closing state.
		/// </summary>
		protected override void OnClosed()
		{
			base.OnClosed();
			_bufferManager.Clear();
		}

		/// <summary>
		/// Gets the buffer manager.
		/// </summary>
		public BufferManager BufferManager
		{
			get { return _bufferManager; }
		}

		/// <summary>
		/// Gets the message encoder factory.
		/// </summary>
		public MessageEncoderFactory MessageEncoderFactory
		{
			get { return _encoderFactory; }
		}

		/// <summary>
		/// Gets the destination.
		/// </summary>
		/// <value>The destination.</value>
		public string Destination
		{
			get { return _destination; }
		}

		/// <summary>
		/// Gets the type of the destination.
		/// </summary>
		/// <value>The type of the destination.</value>
		public DestinationType DestinationType
		{
			get { return _destinationType; }
		}

		#region Private members

		private readonly BufferManager _bufferManager;
		private readonly MessageEncoderFactory _encoderFactory;
		private readonly string _destination;
		private readonly DestinationType _destinationType;

		#endregion
	}
}