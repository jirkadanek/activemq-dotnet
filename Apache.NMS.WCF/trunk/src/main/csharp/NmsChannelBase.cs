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

using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Apache.NMS.WCF
{
	/// <summary>
	/// Base class for communication channels.
	/// </summary>
	public abstract class NmsChannelBase : ChannelBase
	{
		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="NmsChannelBase"/> class.
		/// </summary>
		/// <param name="bufferManager">The buffer manager.</param>
		/// <param name="encoderFactory">The encoder factory.</param>
		/// <param name="address">The address.</param>
		/// <param name="parent">The parent.</param>
		/// <param name="destination">The destination.</param>
		/// <param name="destinationType">Type of the destionation.</param>
		public NmsChannelBase(BufferManager bufferManager, MessageEncoderFactory encoderFactory, EndpointAddress address, ChannelManagerBase parent, string destination, DestinationType destinationType)
			: base(parent)
		{
			_bufferManager = bufferManager;
			_encoder = encoderFactory.CreateSessionEncoder();
			_address = address;
			_destination = destination;
			_destinationType = destinationType;
		}

		#endregion

		#region Public properties

		/// <summary>
		/// Gets the remote address.
		/// </summary>
		/// <value>The remote address.</value>
		public EndpointAddress RemoteAddress
		{
			get { return _address; }
		}

		/// <summary>
		/// Gets or sets the buffer manager.
		/// </summary>
		/// <value>The buffer manager.</value>
		public BufferManager BufferManager
		{
			get { return _bufferManager; }
		}

		/// <summary>
		/// Gets or sets the encoder.
		/// </summary>
		/// <value>The encoder.</value>
		public MessageEncoder Encoder
		{
			get { return _encoder; }
		}

		/// <summary>
		/// Gets the destination.
		/// </summary>
		/// <value>The destination.</value>
		public string Destination
		{
			get { return _destination; }
		}

		/// <summary>
		/// Gets the type of the destination.
		/// </summary>
		/// <value>The type of the destination.</value>
		public DestinationType DestinationType
		{
			get { return _destinationType; }
		}

		#endregion

		#region Private members

		private readonly EndpointAddress _address;
		private readonly BufferManager _bufferManager;
		private readonly MessageEncoder _encoder;
		private readonly string _destination;
		private readonly DestinationType _destinationType;

		#endregion
	}
}