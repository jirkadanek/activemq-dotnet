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

namespace Apache.NMS
{
	/// <summary>
	/// Represents a single unit of work on an IConnection.
	/// So the ISession can be used to perform transactional receive and sends
	/// </summary>
	public interface ISession : IDisposable
	{
		/// <summary>
		/// Creates a producer of messages
		/// </summary>
		IMessageProducer CreateProducer();

		/// <summary>
		/// Creates a producer of messages on a given destination
		/// </summary>
		IMessageProducer CreateProducer(IDestination destination);

		/// <summary>
		/// Creates a consumer of messages on a given destination
		/// </summary>
		IMessageConsumer CreateConsumer(IDestination destination);

		/// <summary>
		/// Creates a consumer of messages on a given destination with a selector
		/// </summary>
		IMessageConsumer CreateConsumer(IDestination destination, string selector);

		/// <summary>
		/// Creates a consumer of messages on a given destination with a selector
		/// </summary>
		IMessageConsumer CreateConsumer(IDestination destination, string selector, bool noLocal);

		/// <summary>
		/// Creates a named durable consumer of messages on a given destination with a selector
		/// </summary>
		IMessageConsumer CreateDurableConsumer(ITopic destination, string name, string selector, bool noLocal);

		/// <summary>
		/// Deletes a durable consumer created with CreateDurableConsumer().
		/// </summary>
		/// <param name="name">Name of the durable consumer</param>
		void DeleteDurableConsumer(string name);

        /// <summary>
        /// Creates a QueueBrowser object to peek at the messages on the specified queue.
        /// </summary>
        /// <param name="queue">
        /// A <see cref="IQueue"/>
        /// </param>
        /// <returns>
        /// A <see cref="IQueueBrowser"/>
        /// </returns>
        /// <exception cref="System.NotSupportedException">
        /// If the Prodiver does not support creation of Queue Browsers.
        /// </exception>
        IQueueBrowser CreateBrowser(IQueue queue);
        
        /// <summary>
        /// Creates a QueueBrowser object to peek at the messages on the specified queue 
        /// using a message selector.
        /// </summary>
        /// <param name="queue">
        /// A <see cref="IQueue"/>
        /// </param>
        /// <param name="selector">
        /// A <see cref="System.String"/>
        /// </param>
        /// <returns>
        /// A <see cref="IQueueBrowser"/>
        /// </returns>
        /// <exception cref="System.NotSupportedException">
        /// If the Prodiver does not support creation of Queue Browsers.
        /// </exception>
        IQueueBrowser CreateBrowser(IQueue queue, string selector);
        
		/// <summary>
		/// Returns the queue for the given name
		/// </summary>
		IQueue GetQueue(string name);

		/// <summary>
		/// Returns the topic for the given name
		/// </summary>
		ITopic GetTopic(string name);

		/// <summary>
		/// Creates a temporary queue
		/// </summary>
		ITemporaryQueue CreateTemporaryQueue();

		/// <summary>
		/// Creates a temporary topic
		/// </summary>
		ITemporaryTopic CreateTemporaryTopic();

		/// <summary>
		/// Delete a destination (Queue, Topic, Temp Queue, Temp Topic).
		/// </summary>
		void DeleteDestination(IDestination destination);

		// Factory methods to create messages

		/// <summary>
		/// Creates a new message with an empty body
		/// </summary>
		IMessage CreateMessage();

		/// <summary>
		/// Creates a new text message with an empty body
		/// </summary>
		ITextMessage CreateTextMessage();

		/// <summary>
		/// Creates a new text message with the given body
		/// </summary>
		ITextMessage CreateTextMessage(string text);

		/// <summary>
		/// Creates a new Map message which contains primitive key and value pairs
		/// </summary>
		IMapMessage CreateMapMessage();

		/// <summary>
		/// Creates a new Object message containing the given .NET object as the body
		/// </summary>
		IObjectMessage CreateObjectMessage(object body);

		/// <summary>
		/// Creates a new binary message
		/// </summary>
		IBytesMessage CreateBytesMessage();

		/// <summary>
		/// Creates a new binary message with the given body
		/// </summary>
		IBytesMessage CreateBytesMessage(byte[] body);

		/// <summary>
		/// Creates a new stream message
		/// </summary>
		IStreamMessage CreateStreamMessage();

		/// <summary>
		/// Closes the session.  There is no need to close the producers and consumers
		/// of a closed session.
		/// </summary>
		void Close();

		#region Transaction methods

		/// <summary>
		/// If this is a transactional session then commit all message
		/// send and acknowledgements for producers and consumers in this session
		/// </summary>
		void Commit();

		/// <summary>
		/// If this is a transactional session then rollback all message
		/// send and acknowledgements for producers and consumers in this session
		/// </summary>
		void Rollback();

		#endregion

		#region Attributes

		TimeSpan RequestTimeout { get; set; }

		bool Transacted { get; }

		AcknowledgementMode AcknowledgementMode { get; }

		#endregion
	}
}
