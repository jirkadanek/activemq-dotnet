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
using System.Messaging;

namespace Apache.NMS.ZMQ
{
	/// <summary>
	/// ZMQ provider of ISession
	/// </summary>
	public class Session : ISession
	{
		private Connection connection;
		private AcknowledgementMode acknowledgementMode;
		private MessageQueueTransaction messageQueueTransaction;
		private IMessageConverter messageConverter;

		public Session(Connection connection, AcknowledgementMode acknowledgementMode)
		{
			this.connection = connection;
			this.acknowledgementMode = acknowledgementMode;
			MessageConverter = connection.MessageConverter;
			if(this.acknowledgementMode == AcknowledgementMode.Transactional)
			{
				MessageQueueTransaction = new MessageQueueTransaction();
			}
		}

		public void Dispose()
		{
			if(MessageQueueTransaction != null)
			{
				MessageQueueTransaction.Dispose();
			}
		}

		#region Producer methods
		public IMessageProducer CreateProducer()
		{
			return CreateProducer(null);
		}

		public IMessageProducer CreateProducer(IDestination destination)
		{
			throw new NotSupportedException("Producer is not supported/implemented");
		}
		#endregion

		#region Consumer methods
		public IMessageConsumer CreateConsumer(IDestination destination)
		{
			return CreateConsumer(destination, null);
		}

		public IMessageConsumer CreateConsumer(IDestination destination, string selector)
		{
			return CreateConsumer(destination, selector, false);
		}

		public IMessageConsumer CreateConsumer(IDestination destination, string selector, bool noLocal)
		{
			// Subscriber client reads messages from a publisher and forwards messages 
			// through the message consumer 
			return new MessageConsumer(this, acknowledgementMode, new ZmqSubscriber(connection, destination, selector));
		}

		public IMessageConsumer CreateDurableConsumer(ITopic destination, string name, string selector, bool noLocal)
		{
			throw new NotSupportedException("Durable Topic subscribers are not supported/implemented");
		}

		public void DeleteDurableConsumer(string name)
		{
			throw new NotSupportedException("Durable Topic subscribers are not supported/implemented");
		}
		#endregion

		public IQueueBrowser CreateBrowser(IQueue queue)
		{
			throw new NotImplementedException();
		}

		public IQueueBrowser CreateBrowser(IQueue queue, string selector)
		{
			throw new NotImplementedException();
		}

		public IQueue GetQueue(string name)
		{
			return new Queue(name);
		}

		public ITopic GetTopic(string name)
		{
			return new Topic(name);
		}

		public ITemporaryQueue CreateTemporaryQueue()
		{
			return new TemporaryQueue();
		}

		public ITemporaryTopic CreateTemporaryTopic()
		{
			return new TemporaryTopic();
		}

		/// <summary>
		/// Delete a destination (Queue, Topic, Temp Queue, Temp Topic).
		/// </summary>
		public void DeleteDestination(IDestination destination)
		{
			// Nothing to delete.  Resources automatically disappear.
			return;
		}

		public IMessage CreateMessage()
		{
			return new BaseMessage();
		}

		public ITextMessage CreateTextMessage()
		{
			return new TextMessage();
		}

		public ITextMessage CreateTextMessage(string text)
		{
			return new TextMessage(text);
		}

		public IMapMessage CreateMapMessage()
		{
			return new MapMessage();
		}

		public IBytesMessage CreateBytesMessage()
		{
			return new BytesMessage();
		}

		public IBytesMessage CreateBytesMessage(byte[] body)
		{
			BytesMessage answer = new BytesMessage();
			answer.Content = body;
			return answer;
		}

		public IStreamMessage CreateStreamMessage()
		{
			return new StreamMessage();
		}

		public IObjectMessage CreateObjectMessage(Object body)
		{
			return new ObjectMessage(body);
		}

		public void Commit()
		{
			if(!Transacted)
			{
				throw new InvalidOperationException("You cannot perform a Commit() on a non-transacted session. Acknowlegement mode is: " + acknowledgementMode);
			}
			messageQueueTransaction.Commit();
		}

		public void Rollback()
		{
			if(!Transacted)
			{
				throw new InvalidOperationException("You cannot perform a Commit() on a non-transacted session. Acknowlegement mode is: " + acknowledgementMode);
			}
			messageQueueTransaction.Abort();
		}

		// Properties
		public Connection Connection
		{
			get { return connection; }
		}

		/// <summary>
		/// The default timeout for network requests.
		/// </summary>
		public TimeSpan RequestTimeout
		{
			get { return NMSConstants.defaultRequestTimeout; }
			set { }
		}

		public bool Transacted
		{
			get { return acknowledgementMode == AcknowledgementMode.Transactional; }
		}

		public AcknowledgementMode AcknowledgementMode
		{
			get { throw new NotImplementedException(); }
		}

		public MessageQueueTransaction MessageQueueTransaction
		{
			get
			{
				if(null != messageQueueTransaction
					&& messageQueueTransaction.Status != MessageQueueTransactionStatus.Pending)
				{
					messageQueueTransaction.Begin();
				}

				return messageQueueTransaction;
			}
			set { messageQueueTransaction = value; }
		}

		public IMessageConverter MessageConverter
		{
			get { return messageConverter; }
			set { messageConverter = value; }
		}

		private ConsumerTransformerDelegate consumerTransformer;
		public ConsumerTransformerDelegate ConsumerTransformer
		{
			get { return this.consumerTransformer; }
			set { this.consumerTransformer = value; }
		}

		private ProducerTransformerDelegate producerTransformer;
		public ProducerTransformerDelegate ProducerTransformer
		{
			get { return this.producerTransformer; }
			set { this.producerTransformer = value; }
		}

		public void Close()
		{
			Dispose();
		}
	}
}
