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

using System.Collections;

namespace Apache.NMS.EMS
{
	public class QueueBrowser : Apache.NMS.IQueueBrowser
	{
		public TIBCO.EMS.QueueBrowser tibcoQueueBrowser;

		public QueueBrowser(TIBCO.EMS.QueueBrowser queueBrowser)
		{
			this.tibcoQueueBrowser = queueBrowser;
		}

		public void  Close()
		{
			this.tibcoQueueBrowser.Close();
		}

		public string MessageSelector
		{
			get { return this.tibcoQueueBrowser.MessageSelector; }
		}

		public IQueue Queue
		{
			get { return EMSConvert.ToNMSQueue(this.tibcoQueueBrowser.Queue); }
		}

		public IEnumerator GetEnumerator()
		{
			// TODO: This enumerator will need to be adapted.  As it is now, the low-level EMS
			// objects will be enumerated.  We need to wrap these objects into the NMS interface
			// types to fit into the provider agnostic system.
			return this.tibcoQueueBrowser.GetEnumerator();
		}
	}
}
