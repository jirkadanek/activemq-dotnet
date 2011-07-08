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
	/// <summary>
	/// Summary description for Destination.
	/// </summary>
	public abstract class Destination : IDestination
	{

		private String name = "";

		/// <summary>
		/// The Default Constructor
		/// </summary>
		protected Destination()
		{
		}

		/// <summary>
		/// Construct the Destination with a defined physical name.
		/// </summary>
		/// <param name="name"></param>
		protected Destination(String destName)
		{
			Name = destName;
		}

		public String Name
		{
			get { return this.name; }
			set
			{
				this.name = value;
				if(!this.name.Contains("\\"))
				{
					// Destinations must have paths in them.  If no path specified, then
					// default to local machine.
					this.name = ".\\" + this.name;
				}
			}
		}

		public bool IsTopic
		{
			get
			{
				return DestinationType == DestinationType.Topic
					|| DestinationType == DestinationType.TemporaryTopic;
			}
		}

		public bool IsQueue
		{
			get
			{
				return DestinationType == DestinationType.Queue
					|| DestinationType == DestinationType.TemporaryQueue;
			}
		}

		public bool IsTemporary
		{
			get
			{
				return DestinationType == DestinationType.TemporaryQueue
					|| DestinationType == DestinationType.TemporaryTopic;
			}
		}

		/// <summary>
		/// </summary>
		/// <returns>string representation of this instance</returns>
		public override String ToString()
		{
			switch(DestinationType)
			{
			case DestinationType.Topic:
				return "topic://" + Name;

			case DestinationType.TemporaryTopic:
				return "temp-topic://" + Name;

			case DestinationType.TemporaryQueue:
				return "temp-queue://" + Name;

			default:
				return "queue://" + Name;
			}
		}

		/// <summary>
		/// hashCode for this instance
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			int answer = 37;

			if(this.name != null)
			{
				answer = name.GetHashCode();
			}

			if(IsTopic)
			{
				answer ^= 0xfabfab;
			}

			return answer;
		}

		/// <summary>
		/// if the object passed in is equivalent, return true
		/// </summary>
		/// <param name="obj">the object to compare</param>
		/// <returns>true if this instance and obj are equivalent</returns>
		public override bool Equals(Object obj)
		{
			bool result = (this == obj);

			if(!result && obj != null && obj is Destination)
			{
				Destination other = (Destination) obj;
				result = (this.DestinationType == other.DestinationType
							&& this.name.Equals(other.name));
			}

			return result;
		}

		public abstract DestinationType DestinationType
		{
			get;
		}
	}
}

