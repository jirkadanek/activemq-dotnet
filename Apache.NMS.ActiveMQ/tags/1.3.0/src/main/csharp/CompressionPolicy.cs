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
using System.IO.Compression;

namespace Apache.NMS.ActiveMQ
{
    /// <summary>
    /// Default Compression policy for NMS.ActiveMQ uses the built in GZipStream
    /// to compress and decompress the message body.  This is not compatible with
    /// compression used by ActiveMQ so users should use this with caution.
    /// </summary>
    public class CompressionPolicy : ICompressionPolicy
    {
        public Stream CreateCompressionStream(Stream data)
        {
            return new GZipStream(data, CompressionMode.Compress);
        }

        public Stream CreateDecompressionStream(Stream data)
        {
            return new GZipStream(data, CompressionMode.Decompress);
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}