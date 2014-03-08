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
using System.IO;
using System.Threading;
using NUnit.Framework;

namespace Apache.NMS.ZMQ
{
	/// <summary>
	/// Use to test and verify ZMQ behavior
	/// </summary>
	public class BaseTest
	{
		[TestFixtureSetUp]
		public void TestFixtureSetup()
		{
			////////////////////////////
			// Dependencies check
			////////////////////////////
			string libFolder = Environment.CurrentDirectory;
			string libFileName;

			libFileName = Path.Combine(libFolder, "clrzmq.dll");
			Assert.IsTrue(File.Exists(libFileName), "Missing zmq wrapper file: {0}", libFileName);
			libFileName = Path.Combine(libFolder, "libzmq.dll");
			Assert.IsTrue(File.Exists(libFileName), "Missing zmq library file: {0}", libFileName);
			libFileName = Path.Combine(libFolder, "libzmq64.dll");
			Assert.IsTrue(File.Exists(libFileName), "Missing 64-bit zmq library file: {0}", libFileName);
			libFileName = Path.Combine(libFolder, "Apache.NMS.dll");
			Assert.IsTrue(File.Exists(libFileName), "Missing Apache.NMS library file: {0}", libFileName);
			libFileName = Path.Combine(libFolder, "Apache.NMS.ZMQ.dll");
			Assert.IsTrue(File.Exists(libFileName), "Missing Apache.NMS.ZMQ library file: {0}", libFileName);
		}

		[SetUp]
		public void SetUp()
		{
			// Setup before each test
		}

		[TearDown]
		public void TearDown()
		{
			// Clean up after each test
		}
	}
}



