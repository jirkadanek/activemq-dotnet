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
using NUnit.Framework;

namespace Apache.NMS.Test
{
	[TestFixture]
	public class BadConsumeTest : NMSTestSupport
	{
		protected IConnection connection;
		protected ISession session;

		[SetUp]
		public override void SetUp()
		{
			connection = CreateConnection(GetTestClientId());
			connection.Start();
			session = connection.CreateSession(AcknowledgementMode.AutoAcknowledge);
		}

		[TearDown]
		public override void TearDown()
		{
			if(null != session)
			{
				session.Dispose();
				session = null;
			}

			if(null != connection)
			{
				connection.Dispose();
				connection = null;
			}
		}

		[Test]
		[ExpectedException(Handler="ExceptionValidationCheck")]
		public void TestBadConsumerException()
		{
			session.CreateConsumer(null);
		}

		public void ExceptionValidationCheck(Exception ex)
		{
			Assert.IsNotNull(ex as NMSException, "Invalid exception was thrown.");
		}
	}
}
