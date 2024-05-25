// This file is provided under The MIT License as part of SqualiveNetworking.
// Copyright (c) Squalive-Studios
// For additional information please see the included LICENSE.md file or view it on GitHub:
// https://github.com/Squalive/SqualiveNetworking

using AOT;
using SqualiveNetworking.Message.Handler;
using SqualiveNetworking.Utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace SqualiveNetworking.Message
{
    internal struct ClientConnectedMessage : INetMessage
    {
        public ushort ClientID;

        public bool IsLocal;

        public ushort MessageID() => 0;

        public void Serialize( ref DataStreamWriter writer )
        {
            writer.WriteUShort( ClientID );

            writer.WriteBool( IsLocal );
        }

        public void Deserialize( ref DataStreamReader reader )
        {
            ClientID = reader.ReadUShort();

            IsLocal = reader.ReadBool();
        }
    }

    [BurstCompile]
    internal unsafe struct ClientConnectedNativeMessageHandler : INativeMessageHandler
    {
        public NativeMessageHandler StaticInitialize()
        {
            return new NativeMessageHandler
            {
                ProcessFunction = new PortableFunctionPointer<NativeMessageHandler.ProcessDelegate>( Process ),
                CompatibleMessageID = 0,
            };
        }
        
        [BurstCompile]
        [MonoPInvokeCallback( typeof( NativeMessageHandler.ProcessDelegate ) )]
        private static void Process( ref MessageReceivedArgs args, void* ptr )
        {
            ClientConnectedMessage message = new ClientConnectedMessage();

            message.Deserialize( ref args.Stream );

            var clientConnectedArgs = new ClientConnectedArgs
            {
                ClientID = message.ClientID,
                IsLocal = message.IsLocal.ToByte(),
            };

            var job = UnsafeUtility.AsRef<NetworkClient.ClientUpdateJob>( ptr );

            if ( job.ClientConnectedPtr.IsCreated )
            {
                job.ClientConnectedPtr.Ptr.Invoke( ref clientConnectedArgs );
            }
            
            job.ClientConnectedWriter.Enqueue( clientConnectedArgs );
        }
    }
}