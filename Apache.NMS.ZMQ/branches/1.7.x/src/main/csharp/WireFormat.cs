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

namespace Apache.NMS.ZMQ
{
	public class WireFormat
	{
		/// <summary>
		/// Message Field Types
		/// IMPORTANT: These assigned numbers cannot change. If new field types
		/// are added, then they must be assigned new numbers. Do not re-use numbers.
		/// </summary>
		public const int MFT_NONE = 0;
		public const int MFT_MESSAGEID = 1;
		public const int MFT_TIMESTAMP = 2;
		public const int MFT_NMSTYPE = 3;
		public const int MFT_CORRELATIONID = 4;
		public const int MFT_REPLYTO = 5;
		public const int MFT_DELIVERYMODE = 6;
		public const int MFT_PRIORITY = 7;
		public const int MFT_TIMETOLIVE = 8;
		public const int MFT_HEADERS = 9;
		public const int MFT_MSGTYPE = 10;
		public const int MFT_BODY = 11;

		// Message Types
		/// <summary>
		/// Message Types
		/// These are the base message types. This is a sub-field of the MFT_MSGTYPE message field type.
		/// IMPORTANT: These numbers cannot be changed. It will break wireformat compatibility.
		/// </summary>
		public const int MT_UNKNOWN = 0;
		public const int MT_MESSAGE = 1;
		public const int MT_TEXTMESSAGE = 2;
		public const int MT_BYTESMESSAGE = 3;
		public const int MT_MAPMESSAGE = 4;
		public const int MT_OBJECTMESSAGE = 5;
		public const int MT_STREAMMESSAGE = 6;

	}
}
