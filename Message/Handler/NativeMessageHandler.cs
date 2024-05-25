// This file is provided under The MIT License as part of SqualiveNetworking.
// Copyright (c) Squalive-Studios
// For additional information please see the included LICENSE.md file or view it on GitHub:
// https://github.com/Squalive/SqualiveNetworking

using System.Runtime.InteropServices;
using SqualiveNetworking.Utils;

namespace SqualiveNetworking.Message.Handler
{
    public unsafe struct NativeMessageHandler
    {
        /// <summary>
        /// ptr is the job itself
        /// </summary>
        [UnmanagedFunctionPointer( CallingConvention.Cdecl )]
        public delegate void ProcessDelegate( ref MessageReceivedArgs args, void* ptr );

        public bool IsCreated => ProcessFunction.IsCreated;

        public PortableFunctionPointer<ProcessDelegate> ProcessFunction;

        public ushort CompatibleMessageID;
    }
    
    /// <summary>
    /// The struct inherit from this needs to be burst compiled in order to make the function pointer works
    /// </summary>
    public interface INativeMessageHandler 
    {
        NativeMessageHandler StaticInitialize();
    }
}