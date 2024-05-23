// This file is provided under The MIT License as part of SqualiveNetworking.
// Copyright (c) Squalive-Studios
// For additional information please see the included LICENSE.md file or view it on GitHub:
// https://github.com/Squalive/SqualiveNetworking



namespace SqualiveNetworking
{
    public enum SendType
    {
        None = 0,
        
        Unreliable, 
        
        Reliable,
        
        Frag,
        
        Notify,
    }
    
    public interface INetMessage
    {
        ushort MessageID { get; }

        void Serialize( ref Unity.Collections.DataStreamWriter writer );

        void Deserialize( ref Unity.Collections.DataStreamReader reader );
    }
}