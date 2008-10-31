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
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using Apache.NMS;
using ISession = Apache.NMS.ISession;

namespace Apache.NMS.WCF
{
	/// <summary>
	/// Channel for receiving messages.
	/// </summary>
	public class NmsInputChannel : NmsChannelBase, IInputChannel
	{
		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="NmsInputChannel"/> class.
		/// </summary>
		/// <param name="bufferManager">The buffer manager.</param>
		/// <param name="encoderFactory">The encoder factory.</param>
		/// <param name="address">The address.</param>
		/// <param name="parent">The parent.</param>
		/// <exception cref="T:System.ArgumentNullException">
		/// 	<paramref name="channelManager"/> is null.</exception>
		public NmsInputChannel(BufferManager bufferManager, MessageEncoderFactory encoderFactory, EndpointAddress address, NmsChannelListener parent)
			: base(bufferManager, encoderFactory, address, parent, parent.Destination, parent.DestinationType)
		{
			_localAddress = address;
			_messages = new InputQueue<Message>();
		}

		#endregion

		//Hands the message off to other components higher up the
		//channel stack that have previously called BeginReceive() 
		//and are waiting for messages to arrive on this channel.
		internal void Dispatch(Message message)
		{
			_messages.EnqueueAndDispatch(message);
		}

		/// <summary>
		/// Gets the property.
		/// </summary>
		/// <typeparam name="T">The type of the property to attempt to retrieve.</typeparam>
		public override T GetProperty<T>()
		{
			if (typeof(T) == typeof(IInputChannel))
			{
				return (T)(object)this;
			}

			T messageEncoderProperty = Encoder.GetProperty<T>();
			if (messageEncoderProperty != null)
			{
				return messageEncoderProperty;
			}

			return base.GetProperty<T>();
		}

		#region IInputChannel Members

		/// <summary>
		/// Returns the message received, if one is available. If a message is not available, blocks for a default interval of time.
		/// </summary>
		/// <returns>
		/// The <see cref="T:System.ServiceModel.Channels.Message" /> received. 
		/// </returns>
		public Message Receive()
		{
			return Receive(DefaultReceiveTimeout);
		}

		/// <summary>
		/// Returns the message received, if one is available. If a message is not available, blocks for a specified interval of time.
		/// </summary>
		/// <returns>
		/// The <see cref="T:System.ServiceModel.Channels.Message" /> received. 
		/// </returns>
		/// <param name="timeout">The <see cref="T:System.TimeSpan" /> that specifies how long the receive operation has to complete before timing out and throwing a <see cref="T:System.TimeoutException" />.</param>
		/// <exception cref="T:System.TimeoutException">The specified <paramref name="timeout" /> is exceeded before the operation is completed.</exception>
		/// <exception cref="T:System.ArgumentOutOfRangeException">The timeout specified is less than zero.</exception>
		public Message Receive(TimeSpan timeout)
		{
			Message message;
			if (TryReceive(timeout, out message))
			{
				return message;
			}
			throw new TimeoutException(String.Format("Receive timed out after {0}.  The time allotted to this operation may have been a portion of a longer timeout.", timeout));
		}

		/// <summary>
		/// Tries to receive a message within a specified interval of time. 
		/// </summary>
		/// <returns>
		/// true if a message is received before the <paramref name="timeout" /> has been exceeded; otherwise false.
		/// </returns>
		/// <param name="timeout">The <see cref="T:System.IAsyncResult" /> returned by a call to one of the <see cref="System.ServiceModel.Channels.IInputChannel.BeginReceive" /> methods.</param>
		/// <param name="message">The <see cref="T:System.ServiceModel.Channels.Message" /> received. </param>
		/// <exception cref="T:System.TimeoutException">The specified <paramref name="timeout" /> is exceeded before the operation is completed.</exception>
		/// <exception cref="T:System.ArgumentOutOfRangeException">The timeout specified is less than zero.</exception>
		public bool TryReceive(TimeSpan timeout, out Message message)
		{
			NmsChannelHelper.ValidateTimeout(timeout);
			return _messages.Dequeue(timeout, out message);
		}

		/// <summary>
		/// Begins an asynchronous operation to receive a message that has a state object associated with it. 
		/// </summary>
		/// <returns>
		/// The <see cref="T:System.IAsyncResult" /> that references the asynchronous message reception. 
		/// </returns>
		/// <param name="callback">The <see cref="T:System.AsyncCallback" /> delegate that receives the notification of the asynchronous operation completion.</param>
		/// <param name="state">An object, specified by the application, that contains state information associated with the asynchronous operation.</param>
		public IAsyncResult BeginReceive(AsyncCallback callback, object state)
		{
			return BeginReceive(DefaultReceiveTimeout, callback, state);
		}

		/// <summary>
		/// Begins an asynchronous operation to receive a message that has a specified time out and state object associated with it. 
		/// </summary>
		/// <returns>
		/// The <see cref="T:System.IAsyncResult" /> that references the asynchronous receive operation.
		/// </returns>
		/// <param name="timeout">The <see cref="T:System.TimeSpan" /> that specifies the interval of time to wait for a message to become available.</param>
		/// <param name="callback">The <see cref="T:System.AsyncCallback" /> delegate that receives the notification of the asynchronous operation completion.</param>
		/// <param name="state">An object, specified by the application, that contains state information associated with the asynchronous operation.</param>
		/// <exception cref="T:System.TimeoutException">The specified <paramref name="timeout" /> is exceeded before the operation is completed.</exception>
		/// <exception cref="T:System.ArgumentOutOfRangeException">The timeout specified is less than zero.</exception>
		public IAsyncResult BeginReceive(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return BeginTryReceive(timeout, callback, state);
		}

		/// <summary>
		/// Completes an asynchronous operation to receive a message. 
		/// </summary>
		/// <returns>
		/// The <see cref="T:System.ServiceModel.Channels.Message" /> received. 
		/// </returns>
		/// <param name="result">The <see cref="T:System.IAsyncResult" /> returned by a call to one of the <see cref="System.ServiceModel.Channels.IInputChannel.BeginReceive" /> methods.</param>
		public Message EndReceive(IAsyncResult result)
		{
			return _messages.EndDequeue(result);
		}

		/// <summary>
		/// Begins an asynchronous operation to receive a message that has a specified time out and state object associated with it. 
		/// </summary>
		/// <returns>
		/// The <see cref="T:System.IAsyncResult" /> that references the asynchronous receive operation.
		/// </returns>
		/// <param name="timeout">The <see cref="T:System.TimeSpan" /> that specifies the interval of time to wait for a message to become available.</param>
		/// <param name="callback">The <see cref="T:System.AsyncCallback" /> delegate that receives the notification of the asynchronous operation completion.</param>
		/// <param name="state">An object, specified by the application, that contains state information associated with the asynchronous operation.</param>
		/// <exception cref="T:System.TimeoutException">The specified <paramref name="timeout" /> is exceeded before the operation is completed.</exception>
		/// <exception cref="T:System.ArgumentOutOfRangeException">The timeout specified is less than zero.</exception>
		public IAsyncResult BeginTryReceive(TimeSpan timeout, AsyncCallback callback, object state)
		{
			NmsChannelHelper.ValidateTimeout(timeout);
			return _messages.BeginDequeue(timeout, callback, state);
		}

		/// <summary>
		/// Completes the specified asynchronous operation to receive a message.
		/// </summary>
		/// <returns>
		/// true if a message is received before the specified interval of time elapses; otherwise false.
		/// </returns>
		/// <param name="result">The <see cref="T:System.IAsyncResult" /> returned by a call to the <see cref="M:System.ServiceModel.Channels.IInputChannel.BeginTryReceive(System.TimeSpan,System.AsyncCallback,System.Object)" /> method.</param>
		/// <param name="message">The <see cref="T:System.ServiceModel.Channels.Message" /> received. </param>
		public bool EndTryReceive(IAsyncResult result, out Message message)
		{
			return _messages.EndDequeue(result, out message);
		}

		/// <summary>
		/// Returns a value that indicates whether a message has arrived within a specified interval of time.
		/// </summary>
		/// <returns>
		/// true if a message has arrived before the <paramref name="timeout" /> has been exceeded; otherwise false.
		/// </returns>
		/// <param name="timeout">The <see cref="T:System.TimeSpan" /> specifies the maximum interval of time to wait for a message to arrive before timing out.</param>
		/// <exception cref="T:System.TimeoutException">The specified <paramref name="timeout" /> is exceeded before the operation is completed.</exception>
		/// <exception cref="T:System.ArgumentOutOfRangeException">The timeout specified is less than zero.</exception>
		public bool WaitForMessage(TimeSpan timeout)
		{
			NmsChannelHelper.ValidateTimeout(timeout);
			return _messages.WaitForItem(timeout);
		}

		/// <summary>
		/// Begins an asynchronous wait-for-a-message-to-arrive operation that has a specified time out and state object associated with it. 
		/// </summary>
		/// <returns>
		/// The <see cref="T:System.IAsyncResult" /> that references the asynchronous operation to wait for a message to arrive.
		/// </returns>
		/// <param name="timeout">The <see cref="T:System.TimeSpan" /> that specifies the interval of time to wait for a message to become available.</param>
		/// <param name="callback">The <see cref="T:System.AsyncCallback" /> delegate that receives the notification of the asynchronous operation completion.</param>
		/// <param name="state">An object, specified by the application, that contains state information associated with the asynchronous operation.</param>
		/// <exception cref="T:System.TimeoutException">The specified <paramref name="timeout" /> is exceeded before the operation is completed.</exception>
		/// <exception cref="T:System.ArgumentOutOfRangeException">The timeout specified is less than zero.</exception>
		public IAsyncResult BeginWaitForMessage(TimeSpan timeout, AsyncCallback callback, object state)
		{
			NmsChannelHelper.ValidateTimeout(timeout);
			return _messages.BeginWaitForItem(timeout, callback, state);
		}

		/// <summary>
		/// Completes the specified asynchronous wait-for-a-message operation.
		/// </summary>
		/// <returns>
		/// true if a message has arrived before the timeout has been exceeded; otherwise false.
		/// </returns>
		/// <param name="result">The <see cref="T:System.IAsyncResult" /> that identifies the <see cref="M:System.ServiceModel.Channels.IInputChannel.BeginWaitForMessage(System.TimeSpan,System.AsyncCallback,System.Object)" /> operation to finish, and from which to retrieve an end result.</param>
		public bool EndWaitForMessage(IAsyncResult result)
		{
			return _messages.EndWaitForItem(result);
		}

		/// <summary>
		/// Gets the address on which the input channel receives messages. 
		/// </summary>
		/// <returns>
		/// The <see cref="T:System.ServiceModel.EndpointAddress" /> on which the input channel receives messages. 
		/// </returns>
		public EndpointAddress LocalAddress
		{
			get { return _localAddress; }
		}

		#endregion

		/// <summary>
		/// Inserts processing on a communication object after it transitions to the closing state due to the invocation of a synchronous abort operation.
		/// </summary>
		protected override void OnAbort()
		{
			OnClose(TimeSpan.Zero);
		}

		/// <summary>
		/// Inserts processing on a communication object after it transitions to the closing state due to the invocation of a synchronous close operation.
		/// </summary>
		/// <param name="timeout">The <see cref="T:System.TimeSpan" /> that specifies how long the on close operation has to complete before timing out.</param>
		/// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="timeout" /> is less than zero.</exception>
		protected override void OnClose(TimeSpan timeout)
		{
			NmsChannelHelper.ValidateTimeout(timeout);
			_messages.Close();
		}

		/// <summary>
		/// Completes an asynchronous operation on the close of a communication object.
		/// </summary>
		/// <param name="result">The <see cref="T:System.IAsyncResult" /> that is returned by a call to the <see cref="M:System.ServiceModel.Channels.CommunicationObject.OnEndClose(System.IAsyncResult)" /> method.</param>
		protected override void OnEndClose(IAsyncResult result)
		{
			CompletedAsyncResult.End(result);
		}

		/// <summary>
		/// Inserts processing after a communication object transitions to the closing state due to the invocation of an asynchronous close operation.
		/// </summary>
		/// <returns>
		/// The <see cref="T:System.IAsyncResult" /> that references the asynchronous on close operation. 
		/// </returns>
		/// <param name="timeout">The <see cref="T:System.TimeSpan" /> that specifies how long the on close operation has to complete before timing out.</param>
		/// <param name="callback">The <see cref="T:System.AsyncCallback" /> delegate that receives notification of the completion of the asynchronous on close operation.</param>
		/// <param name="state">An object, specified by the application, that contains state information associated with the asynchronous on close operation.</param>
		/// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="timeout" /> is less than zero.</exception>
		protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
		{
			OnClose(timeout);
			return new CompletedAsyncResult(callback, state);
		}

		/// <summary>
		/// Inserts processing on a communication object after it transitions into the opening state which must complete within a specified interval of time.
		/// </summary>
		/// <param name="timeout">The <see cref="T:System.TimeSpan" /> that specifies how long the on open operation has to complete before timing out.</param>
		/// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="timeout" /> is less than zero.</exception>
		/// <exception cref="T:System.TimeoutException">The interval of time specified by <paramref name="timeout" /> that was allotted for the operation was exceeded before the operation was completed.</exception>
		protected override void OnOpen(TimeSpan timeout)
		{
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
		/// <exception cref="T:System.TimeoutException">The interval of time specified by the timeout that was allotted for the operation was exceeded before the operation was completed.</exception>
		protected override void OnEndOpen(IAsyncResult result)
		{
			CompletedAsyncResult.End(result);
		}

		#region Private members

		private readonly InputQueue<Message> _messages;
		private EndpointAddress _localAddress;

		#endregion
	}
}