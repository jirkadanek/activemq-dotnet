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
using System.Text;

namespace Apache.NMS.Util
{
#if NET_2_0

	public class AtomicBoolean : Atomic<bool>
	{
		public AtomicBoolean(bool defaultValue)
				: base(defaultValue)
		{
		}
	}

#else

	public class AtomicBoolean
	{
		private bool atomicValue;

		public bool Value
		{
			get
			{
				lock(this)
				{
					return atomicValue;
				}
			}
			set
			{
				lock(this)
				{
					atomicValue = value;
				}
			}
		}

		public AtomicBoolean(bool defaultValue)
		{
			atomicValue = defaultValue;
		}

		public bool CompareAndSet(bool expected, bool newValue)
		{
			lock(this)
			{
				if(atomicValue == expected)
				{
					atomicValue = newValue;
					return true;
				}

				return false;
			}
		}
	}
#endif
}
