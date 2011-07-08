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

namespace Apache.NMS.ZMQ
{
	public enum NMSMessageType
	{
		BaseMessage,
		TextMessage,
		BytesMessage,
		ObjectMessage,
		MapMessage,
		StreamMessage
	}

	public class DefaultMessageConverter : IMessageConverter
	{
		/// <summary>
		/// Converts 0MQ message to NMS message
		/// </summary>
		/// <param name="message">
		/// 0MQ message object
		/// </param>
		/// <returns>
		/// NMS message object
		/// </returns>
		public virtual IMessage ToNmsMessage(ZmqMessage message)
		{
			BaseMessage answer = CreateNmsMessage(message);

			try
			{
				//answer.NMSCorrelationID = message.ClientId;
				answer.NMSMessageId = "";
				answer.NMSDestination = message.Destination;
				answer.NMSDeliveryMode = MsgDeliveryMode.NonPersistent;
				answer.NMSPriority = MsgPriority.Normal;
				answer.NMSTimestamp = System.DateTime.Now;
				answer.NMSTimeToLive = new System.TimeSpan(0);
				answer.NMSType = "";
			}
			catch(InvalidOperationException)
			{
				// Log error
			}
			return answer;
		}

		/// <summary>
		/// Create base NMS message from 0MQ message 
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
		protected virtual BaseMessage CreateNmsMessage(ZmqMessage message)
		{
			TextMessage textMessage = new TextMessage();
			textMessage.Text = message.Text;
			return textMessage;
		}
	}
}
