// This file is provided under The MIT License as part of SqualiveNetworking.
// Copyright (c) Squalive-Studios
// For additional information please see the included LICENSE.md file or view it on GitHub:
// https://github.com/Squalive/SqualiveNetworking

using System;
using System.Runtime.InteropServices;
using SqualiveNetworking.Utils;
using Unity.Collections;

namespace SqualiveNetworking.Message.Batch
{
    public struct BatchMessage : IDisposable
    {
        // internal NativeList<>
        
        public void Dispose()
        {
        }
    }

    public struct BatchMessageProcessor
    {
        public PortableFunctionPointer<IBatchMessage.MessageSerialized> SerializeFunction;
    }

    public interface IBatchMessage
    {
        [UnmanagedFunctionPointer( CallingConvention.Cdecl )]
        public delegate void MessageSerialized( ref DataStreamWriter stream );
        
        BatchMessageProcessor StaticInitialize();
        
        ushort MessageID();
    }
}