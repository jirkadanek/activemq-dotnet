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

namespace Apache.NMS.WCF
{
	/// <summary>
	/// A helper class for the NMS transport.
	/// </summary>
	internal class NmsChannelHelper
	{
		/// <summary>
		/// Ensures that the specified timeout value is not negative.
		/// </summary>
		/// <param name="timeout">The timeout that needs to be validated.</param>
		/// <exception cref="ArgumentOutOfRangeException">the timeout value was negative.</exception>
		public static void ValidateTimeout(TimeSpan timeout)
		{
			if(timeout < TimeSpan.Zero)
			{
				throw new ArgumentOutOfRangeException("timeout", timeout, "Timeout must be greater than or equal to TimeSpan.Zero. To disable timeout, specify TimeSpan.MaxValue.");
			}
		}

		/// <summary>
		/// Gets the name of the queue from the URI.
		/// </summary>
		/// <param name="uri">The URI of the message queue.</param>
		public static string GetQueueName(Uri uri)
		{
			return uri.LocalPath.TrimStart('/');
		}

		/// <summary>
		/// Gets the destination.
		/// </summary>
		/// <param name="session">The session.</param>
		/// <param name="destination">The destination.</param>
		/// <param name="destinationType">Type of the destination.</param>
		public static IDestination GetDestination(NMS.ISession session, string destination, DestinationType destinationType)
		{
			switch(destinationType)
			{
			case DestinationType.Topic:
			return session.GetTopic(destination);
			case DestinationType.TemporaryQueue:
			return session.CreateTemporaryQueue();
			case DestinationType.TemporaryTopic:
			return session.CreateTemporaryTopic();
			default:
			return session.GetQueue(destination);
			}
		}
	}
}