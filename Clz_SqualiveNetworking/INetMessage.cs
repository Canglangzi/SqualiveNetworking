// This file is provided under The MIT License as part of SqualiveNetworking.
// Copyright (c) Squalive-Studios
// For additional information please see the included LICENSE.md file or view it on GitHub:
// https://github.com/Squalive/SqualiveNetworking


using SqualiveNetworking.Message.Processor;
using Unity.Collections;
using Unity.Networking.Transport;

namespace SqualiveNetworking
{
    public enum SendType
    {
        None = 0,
        
        Unreliable, 
        
        Reliable,
        
        Frag,
    }

    public struct MessageReceivedArgs
    {
        public ushort ClientID;
        
        public NetworkConnection Connection;

        public ushort MessageID;

        public DataStreamReader Stream;

        public byte HasProcessorOutput;

        public MessageProcessor Processor;

        public unsafe void* ProcessorOutputPtr;
    }

    internal unsafe struct MessageReceivedPtr
    {
        public MessageReceivedArgs Args;

        public void* StreamPtr;

        public int Length;

        public int BytesRead;
    }

    public delegate void MessageReceivedCallback( ref MessageReceivedArgs args );
    
    public interface INetMessage
    {
        void Serialize( ref DataStreamWriter writer );

        void Deserialize( ref DataStreamReader reader );
        
        ushort MessageID();
    }
}