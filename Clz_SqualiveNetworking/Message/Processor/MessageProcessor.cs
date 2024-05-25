// This file is provided under The MIT License as part of SqualiveNetworking.
// Copyright (c) Squalive-Studios
// For additional information please see the included LICENSE.md file or view it on GitHub:
// https://github.com/Squalive/SqualiveNetworking

using System;
using System.Runtime.InteropServices;
using SqualiveNetworking.Utils;
using Unity.Collections;

namespace SqualiveNetworking.Message.Processor
{
    [UnmanagedFunctionPointer( CallingConvention.Cdecl )]
    public delegate void MessageProcessWritten( ref bool isServer, ref DataStreamWriter stream );
    
    [UnmanagedFunctionPointer( CallingConvention.Cdecl )]
    public unsafe delegate void MessageProcessRead( ref bool isServer, ref DataStreamReader stream, out void* output );
    
    public struct MessageProcessor : IEquatable<MessageProcessor>
    {
        public static MessageProcessor Null => default;
        
        internal byte InternalID;

        internal int StartIndex;

        internal int EndIndex;

        public bool Equals( MessageProcessor other )
        {
            return InternalID == other.InternalID;
        }

        public override int GetHashCode()
        {
            return InternalID;
        }
    }
    
    public struct MessageProcessorStage
    {
        internal PortableFunctionPointer<MessageProcessWritten> ProcessWrittenFunction;
        
        internal PortableFunctionPointer<MessageProcessRead> ProcessReadFunction;

        public bool IsCreated => ProcessWrittenFunction.IsCreated && ProcessReadFunction.IsCreated;
    }
    
    /// <summary>
    /// All structs inherit from this needs to be tagged with burst compiled
    /// </summary>
    public interface IMessageProcessorStage
    {
        MessageProcessorStage StaticInitialize();
    }

}