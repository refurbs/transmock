﻿/***************************************
//   Copyright 2014 - Svetoslav Vasilev

//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at

//     http://www.apache.org/licenses/LICENSE-2.0

//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
*****************************************/

/// -----------------------------------------------------------------------------------------------------------
/// Module      :  IAsyncStreamingServer.cs
/// Description :  This interface defines the operations of an asyncronous streaming server.
/// -----------------------------------------------------------------------------------------------------------
/// 
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransMock.Communication.NamedPipes
{
    /// <summary>
    /// Defines the operations of a streaming server
    /// </summary>
    public interface IAsyncStreamingServer : IDisposable
    {
        /// <summary>
        /// Event indicating when a client connection has been established
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Design",
            "CA1009:DeclareEventHandlersCorrectly",
            Justification = "Incompatibility with generic event handlers")]
        event EventHandler<ClientConnectedEventArgs> ClientConnected;

        /// <summary>
        /// Event indicating when all the data from a client has been read
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1009:DeclareEventHandlersCorrectly",
            Justification = "Incompatibility with generic event handlers")]
        event EventHandler<AsyncReadEventArgs> ReadCompleted;

        /// <summary>
        /// Starts the server
        /// </summary>
        /// <returns>A boolean indicating of whether the server started successfully or not</returns>
        bool Start();

        /// <summary>
        /// Stops the server
        /// </summary>
        void Stop();

        /// <summary>
        /// Disconnects a specific client form the server
        /// </summary>
        /// <param name="connectionId">The id of the client connection to be disconnected</param>
        void Disconnect(int connectionId);       

        /// <summary>
        /// Writes all bytes to the specified client connection
        /// </summary>
        /// <param name="connectionId">The id of the client connection to write the data to</param>
        /// <param name="data">The data to be written to the client as an array of bytes</param>
        void WriteAllBytes(int connectionId, byte[] data);

        /// <summary>
        /// Writes the provided stream's contents to the specified client connection
        /// </summary>
        /// <param name="connectionId">The id of the client connection to write the data to</param>
        /// <param name="data">The data to be written to the client as a stream</param>
        void WriteStream(int connectionId, Stream data);
    }    
}
