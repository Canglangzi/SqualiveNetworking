// This file is provided under The MIT License as part of SqualiveNetworking.
// Copyright (c) Squalive-Studios
// For additional information please see the included LICENSE.md file or view it on GitHub:
// https://github.com/Squalive/SqualiveNetworking

using SqualiveNetworking.Message.Processor;

namespace SqualiveNetworking.Message
{
    public static class MessageHelper
    {
        public static void SendToServer<T>( this T message, SendType sendType = SendType.Unreliable, MessageProcessor processor = default ) where T : unmanaged, INetMessage
        {
            NetworkClient.SendMessage( sendType, processor, message );
        }

        public static void SendToClient<T>( this T message, ushort clientID = 0, SendType sendType = SendType.Unreliable, MessageProcessor processor = default ) where T : unmanaged, INetMessage
        {
            NetworkServer.SendMessage( sendType, processor, message, clientID );
        }

        public static void SendToAll<T>( this T message, SendType sendType = SendType.Unreliable, MessageProcessor processor = default ) where T : unmanaged, INetMessage
        {
            NetworkServer.SendMessageToAll( sendType, processor, message );
        }
    }
}