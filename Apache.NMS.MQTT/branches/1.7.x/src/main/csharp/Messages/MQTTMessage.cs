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
using Apache.NMS.Util;
using Apache.NMS.MQTT.Commands;

namespace Apache.NMS.MQTT.Messages
{
	public delegate void AcknowledgeHandler(MQTTMessage message);

	public class MQTTMessage : IMessage, ICloneable
	{
		private readonly PUBLISH publish = new PUBLISH();
		private Connection connection;
		private Topic destination;
		private short messageId;
		private byte[] content;
        private bool compressed;
		private int redeliveryCounter;
        private bool persistent;

		public event AcknowledgeHandler Acknowledger;

        private bool readOnlyMsgProperties;
        private bool readOnlyMsgBody;

		public static MQTTMessage Transform(IMessage message)
		{
			return (MQTTMessage) message;
		}

		public MQTTMessage() : base()
		{
		}

        public override int GetHashCode()
        {
            return messageId != 0 ? messageId : base.GetHashCode();
        }

		public virtual object Clone()
		{
			return this.MemberwiseClone();
		}

        public override bool Equals(object that)
        {
            if(that is MQTTMessage)
            {
                return Equals((MQTTMessage) that);
            }
            return false;
        }

        public virtual bool Equals(MQTTMessage that)
        {
            short oMsg = that.MessageId;
            short thisMsg = this.MessageId;
            
            return thisMsg != 0 && oMsg != 0 && oMsg == thisMsg;
        }
        
		public void Acknowledge()
		{
		    if(null == Acknowledger)
			{
				throw new NMSException("No Acknowledger has been associated with this message: " + this);
			}
		    
            Acknowledger(this);
		}

	    public virtual void ClearBody()
		{
			this.ReadOnlyBody = false;
			this.Content = null;
		}

		public virtual void ClearProperties()
		{
		}

		protected void FailIfReadOnlyBody()
		{
			if(ReadOnlyBody == true)
			{
				throw new MessageNotWriteableException("Message is in Read-Only mode.");
			}
		}

		protected void FailIfWriteOnlyBody()
		{
			if( ReadOnlyBody == false )
			{
				throw new MessageNotReadableException("Message is in Write-Only mode.");
			}
		}

		#region Properties

        public byte[] Content
        {
            get { return content; }
            set { this.content = value; }
        }

        public bool Compressed
        {
            get { return compressed; }
            set { this.compressed = value; }
        }

        public virtual bool ReadOnlyProperties
        {
            get { return this.readOnlyMsgProperties; }
            set { this.readOnlyMsgProperties = value; }
        }

        public virtual bool ReadOnlyBody
        {
            get { return this.readOnlyMsgBody; }
            set { this.readOnlyMsgBody = value; }
        }

		public IPrimitiveMap Properties
		{
			get
			{
				throw new NotSupportedException("MQTT does not support Message properties.");
			}
		}

		public Connection Connection
		{
			get { return this.connection; }
			set { this.connection = value; }
		}

		/// <summary>
		/// The correlation ID used to correlate messages with conversations or long running business processes
		/// </summary>
		public string NMSCorrelationID
		{
			get { return String.Empty; }
			set {}
		}

		/// <summary>
		/// The destination of the message
		/// </summary>
		public IDestination NMSDestination
		{
			get { return destination; }
            set { this.destination = value as Topic; }
		}

		/// <summary>
		/// The time in milliseconds that this message should expire in
		/// </summary>
		public TimeSpan NMSTimeToLive
		{
			get { return TimeSpan.MaxValue; }
			set {}
		}

		/// <summary>
		/// The message ID which is set by the provider
		/// </summary>
		public string NMSMessageId
		{
			get { return this.messageId.ToString(); }
			set { this.messageId = Int16.Parse(value); }
		}

		/// <summary>
		/// Whether or not this message is persistent
		/// </summary>
		public MsgDeliveryMode NMSDeliveryMode
		{
			get { return (persistent ? MsgDeliveryMode.Persistent : MsgDeliveryMode.NonPersistent); }
			set { persistent = (MsgDeliveryMode.Persistent == value); }
		}

		/// <summary>
		/// The Priority on this message
		/// </summary>
		public MsgPriority NMSPriority
		{
			get { return MsgPriority.Normal; }
			set {}
		}

		/// <summary>
		/// Returns true if this message has been redelivered to this or another consumer before being acknowledged successfully.
		/// </summary>
		public bool NMSRedelivered
		{
			get { return (redeliveryCounter > 0); }

            set
            {
                if(value == true)
                {
                    if(this.redeliveryCounter <= 0)
                    {
                        this.redeliveryCounter = 1;
                    }
                }
                else
                {
                    if(this.redeliveryCounter > 0)
                    {
                        this.redeliveryCounter = 0;
                    }
                }
            }
		}

		/// <summary>
		/// The destination that the consumer of this message should send replies to
		/// </summary>
		public IDestination NMSReplyTo
		{
			get { return null; }
			set { }
		}

		/// <summary>
		/// The timestamp the broker added to the message
		/// </summary>
		public DateTime NMSTimestamp
		{
			get { return DateTime.Now; }
			set {}
		}

		/// <summary>
		/// The type name of this message
		/// </summary>
		public string NMSType
		{
			get { return publish.CommandName; }
			set {  }
		}

		public short MessageId
		{
			get { return this.messageId; }
			set { this.messageId = value; }
		}

		#endregion

	}
}

