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
using System.Collections.Specialized;

namespace Apache.NMS.MQTT
{
	/// <summary>
	/// MQTT Topic Destination.  
	/// </summary>
	public class Topic : ITopic
	{
		private string name;
		private StringDictionary options = null;

		public Topic(string name)
		{
			this.name = name;
		}

		public void Dispose()
		{
		}

		public string TopicName 
		{ 
			get { return this.name; }
		}

		public DestinationType DestinationType 
		{ 
			get { return DestinationType.Topic; }
		}
		
		public bool IsTopic
		{ 
			get { return true; }
		}
		
		public bool IsQueue 
		{ 
			get { return false; }
		}
		
		public bool IsTemporary 
		{ 
			get { return false; }
		}

		/// <summary>
		/// Dictionary of name/value pairs representing option values specified
		/// in the URI used to create this Destination.  A null value is returned
		/// if no options were specified.
		/// </summary>
		internal StringDictionary Options
		{
			get { return this.options; }
		}
	}
}

