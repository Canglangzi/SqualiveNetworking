// This file is provided under The MIT License as part of SqualiveNetworking.
// Copyright (c) Squalive-Studios
// For additional information please see the included LICENSE.md file or view it on GitHub:
// https://github.com/Squalive/SqualiveNetworking

using Unity.Burst;
using System;
using System.Collections.Generic;
using AOT;
using SqualiveNetworking.Message;
using SqualiveNetworking.Message.Handler;
using SqualiveNetworking.Message.Processor;
using SqualiveNetworking.Tick;
using SqualiveNetworking.Utils;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Error;
using UnityEngine;

namespace SqualiveNetworking
{
    public struct ClientDisconnectedArgs
    {
        public ushort ClientID;
        
        public DisconnectReason Reason;
    }

    public struct ClientConnectedArgs
    {
        public ushort ClientID;

        public byte IsLocal;
    }
    
    public delegate void ClientDisconnectedCallback( ref ClientDisconnectedArgs args );

    public delegate void ClientConnectedCallback( ref ClientConnectedArgs args );
    
    public static unsafe class NetworkClient
    {
        private static NetworkDriver _driver;
        private static NativeArray<NetworkConnection> _connections;

        private static NetworkConnection Connection => _connections[ 0 ];

        private static NativeNetworkMessageHandlers _internalHandlers;
        
        private static NativeNetworkMessageHandlers _customHandlers;
 public static bool IsInitialized => _initialized;
        private static NetworkPipeline _fragmentationPipeline, _reliablePipeline, _unreliablePipeline;

        private static JobHandle _clientJobHandle;

        private static bool _initialized;

        private static PortableFunctionPointer<ClientDisconnectedCallback> _clientDisconnectedPtr;
        
        private static PortableFunctionPointer<ClientConnectedCallback> _clientConnectedPtr;

        private static PortableFunctionPointer<MessageReceivedCallback> _messageReceivedPtr;

        private static MessageProcessorHandler _messageProcessorHandler;

        private static NativeHashMap<byte, MessageProcessor> _processors;

        internal static readonly SharedStatic<ushort> SharedClientID = SharedStatic<ushort>.GetOrCreate<ClientIDKey>(  );

        internal static readonly SharedStatic<TickSystem> TickSystem = SharedStatic<TickSystem>.GetOrCreate<TickKey>(  );

        public static ushort ClientID => SharedClientID.Data;

        public static bool IsConnected => ClientID != 0 ;

        public static uint CurrentTick => TickSystem.Data.CurrentTick;
        
        private class ClientKey { }
        private class ClientIDKey { }
        private class TickKey { }
        
        private static bool EnsureInitialized()
        {
            if ( !_initialized )
            {
#if ENABLE_SQUALIVE_NET_DEBUG
                throw new Exception( "Client hasn't been initialized" );
#endif
                return false;
            }

            return true;
        }

        private static bool EnsureUnInitialized()
        {
            if ( _initialized )
            {
#if ENABLE_SQUALIVE_NET_DEBUG
                throw new Exception( "Client has been initialized already" );
#endif
                return false;
            }

            return true;
        }

        public static void Initialize( TickSystem tickSystem, NetworkSettings settings )
        {
            if ( !EnsureUnInitialized() )
                return;

            _initialized = true;

            TickSystem.Data = tickSystem;

            SharedClientID.Data = 0;

            _driver = NetworkDriver.Create( settings );

            _connections = new NativeArray<NetworkConnection>( 1, Allocator.Persistent );
            _connections[ 0 ] = default;

            _internalHandlers = new NativeNetworkMessageHandlers( Allocator.Persistent );
            _customHandlers = new NativeNetworkMessageHandlers( Allocator.Persistent );
            
            // Add internal messages layers
            _internalHandlers.AddLayer( new ClientConnectedNativeMessageHandler() );
            
            _fragmentationPipeline = _driver.CreatePipeline( typeof( FragmentationPipelineStage ) );
            _reliablePipeline = _driver.CreatePipeline( typeof( ReliableSequencedPipelineStage ) );
            _unreliablePipeline = _driver.CreatePipeline( typeof( UnreliableSequencedPipelineStage ) );
            
            _clientConnectedPtr = new PortableFunctionPointer<ClientConnectedCallback>( NetworkClientBurst.ClientConnected );

            _messageProcessorHandler = new MessageProcessorHandler( 2, Allocator.Persistent );

            _processors = new NativeHashMap<byte, MessageProcessor>( 32, Allocator.Persistent );

            NetworkClientEvent.Initialize(  );
        }

        public static void DeInitialize()
        {
            if ( !EnsureInitialized() )
                return;
            
            Disconnect();
            
            _internalHandlers.Dispose();
            _customHandlers.Dispose();
            
            _connections.Dispose();
            _driver.Dispose();
            
            _messageProcessorHandler.Dispose();
            _processors.Dispose();
            
#if ENABLE_SQUALIVE_NET_DEBUG
            Debug.Log( "[CLIENT]: Disposing client...." );
#endif
            
            _initialized = false;
            
            NetworkClientEvent.DeInitialize();
        }

        public static void Connect( string ipAddress, ushort port = 27015, NetworkFamily networkFamily = NetworkFamily.Ipv4 )
        {
            if ( !EnsureInitialized() )
                return;
            
            var endPoint = NetworkEndpoint.Parse( ipAddress, port, networkFamily );

            _connections[0] = _driver.Connect( endPoint );
        }

        public static void ConnectToLocal( ushort port = 27015, bool ipv6 = false )
        {
            if ( !EnsureInitialized() )
                return;
            
            _clientJobHandle.Complete();

            var endPoint = ipv6 ? NetworkEndpoint.LoopbackIpv6 : NetworkEndpoint.LoopbackIpv4;

            endPoint = endPoint.WithPort( port );

            _connections[ 0 ] = _driver.Connect( endPoint );
        }

        public static void Disconnect()
        {
            if ( !EnsureInitialized() )
                return;

            SharedClientID.Data = 0;
            
            _clientJobHandle.Complete();
            
            Connection.Disconnect( _driver );

#if ENABLE_SQUALIVE_NET_DEBUG
            Debug.Log( "[CLIENT]: Disconnecting from server...." );
#endif
        }

        /// <summary>
        /// Force an update which is highly unrecommended
        /// </summary>
        public static void Update()
        {
            if ( !EnsureInitialized() )
                return;
            
            _clientJobHandle.Complete();
            
            NetworkClientEvent.ProcessEvents();
            
            _clientJobHandle = _driver.ScheduleUpdate();

            _clientJobHandle = new ClientUpdateJob
            {
                Driver = _driver,
                Connections = _connections,
                InstanceID = ClientID,
                ClientDisconnectedPtr = _clientDisconnectedPtr,
                ClientDisconnectedWriter = NetworkClientEvent.GetClientDisconnectedWriter(),
                ClientConnectedPtr = _clientConnectedPtr,
                ClientConnectedWriter = NetworkClientEvent.GetClientConnectedWriter(),
                MessageReceivedCallback = _messageReceivedPtr,
                MessageReceivedArgsWriter = NetworkClientEvent.GetMessageReceivedWriter(),
                InternalMessageLayers = _internalHandlers.GetReadOnlyLayers(),
                CustomMessageLayers = _customHandlers.GetReadOnlyLayers(),
                MessageProcessorHandler = _messageProcessorHandler,
                Processors = _processors.AsReadOnly(),
            }.Schedule( _clientJobHandle );
        }

        public static void Tick( float deltaTime )
        {
            while ( TickSystem.Data.Update( ref deltaTime ) )
            {
                Update();
            }
        }

        public static void SendMessage<T>( SendType sendType, MessageProcessor messageProcessor, T netMessage ) where T : unmanaged, INetMessage
        {
            DataStreamWriter writer;
            
            switch ( sendType )
            {
                case SendType.Reliable:
                    if ( !NetworkHelper.BeginSend( _driver, _reliablePipeline, _messageProcessorHandler, messageProcessor, Connection, out writer ) ) return;
                    break;
                case SendType.Frag:
                    if ( !NetworkHelper.BeginSend( _driver, _fragmentationPipeline, _messageProcessorHandler, messageProcessor, Connection, out writer ) ) return;
                    break;
                default:
                    if ( !NetworkHelper.BeginSend( _driver, _unreliablePipeline, _messageProcessorHandler, messageProcessor, Connection, out writer ) ) return;
                    break;
            }

            NetworkHelper.SendCustomMessage( netMessage, _driver, ref writer );
        }

        public static void SendMessage<T>( SendType sendType, T netMessage ) where T : unmanaged, INetMessage
        {
            SendMessage( sendType, MessageProcessor.Null, netMessage );
        }
        public static void SetClientDisconnectedPtr( ClientDisconnectedCallback callback )
        {
            _clientDisconnectedPtr = new PortableFunctionPointer<ClientDisconnectedCallback>( callback );
        }
        
        public static void SetMessageReceivedFunctionPtr( MessageReceivedCallback callback )
        {
            _messageReceivedPtr = new PortableFunctionPointer<MessageReceivedCallback>( callback );
        }

        public static void AddNativeMessageHandler<T>( T layer ) where T : unmanaged, INativeMessageHandler
        {
            _customHandlers.AddLayer( layer );
        }

        public static MessageProcessor CreateProcessor<T> ( T[] processorInterface ) where T :  IMessageProcessorStage
        {
            var processor = _messageProcessorHandler.CreateProcessor( processorInterface );

            _processors.TryAdd( processor.InternalID, processor );

            return processor;
        }
        
#if ENABLE_SQUALIVE_NET_BURST
        [BurstCompile]
#endif
        internal struct ClientUpdateJob : IJob
        {
            public NetworkDriver Driver;
            
            public NativeArray<NetworkConnection> Connections;

            public PortableFunctionPointer<ClientDisconnectedCallback> ClientDisconnectedPtr;

            [NativeDisableContainerSafetyRestriction]
            public NativeQueue<ClientDisconnectedArgs>.ParallelWriter ClientDisconnectedWriter;

            public PortableFunctionPointer<ClientConnectedCallback> ClientConnectedPtr;

            [NativeDisableContainerSafetyRestriction]
            public NativeQueue<ClientConnectedArgs>.ParallelWriter ClientConnectedWriter;

            // Use for triggering message received function ptr
            public PortableFunctionPointer<MessageReceivedCallback> MessageReceivedCallback;

            [NativeDisableContainerSafetyRestriction]
            public NativeQueue<MessageReceivedPtr>.ParallelWriter MessageReceivedArgsWriter;
            
            public NativeArray<NativeMessageHandler>.ReadOnly InternalMessageLayers;
            
            public NativeArray<NativeMessageHandler>.ReadOnly CustomMessageLayers;

            public MessageProcessorHandler MessageProcessorHandler;

            public NativeHashMap<byte, MessageProcessor>.ReadOnly Processors;
            
            public ushort InstanceID;

            public void Execute()
            {
                if ( !Connections[ 0 ].IsCreated )
                    return;

                DataStreamReader stream;
                NetworkEvent.Type cmd;

                while ( ( cmd = Connections[ 0 ].PopEvent( Driver, out stream ) ) != NetworkEvent.Type.Empty )
                {
                    switch ( cmd )
                    {
#if ENABLE_SQUALIVE_NET_DEBUG
                        case NetworkEvent.Type.Connect:
                            Debug.Log( $"[CLIENT]: Successfully Connected to {Driver.GetRemoteEndpoint( Connections[ 0 ] ).ToFixedString()}" );
                            break;
#endif
                        case NetworkEvent.Type.Disconnect:
                            var reason = (DisconnectReason)stream.ReadByte();
                            
#if ENABLE_SQUALIVE_NET_DEBUG
                            Debug.Log( $"[CLIENT]: Disconnected from {Driver.GetRemoteEndpoint( Connections[ 0 ] ).ToFixedString()}" );
#endif

                            var args = new ClientDisconnectedArgs
                            {
                                ClientID = InstanceID,
                                Reason = reason,
                            };
                            
                            if ( ClientDisconnectedPtr.IsCreated )
                            {
                                ClientDisconnectedPtr.Ptr.Invoke( ref args );
                            }

                            ClientDisconnectedWriter.Enqueue( args );
                            
                            Connections[ 0 ] = default;
                            break;
                        case NetworkEvent.Type.Data:
                            var streamPtr = stream.GetUnsafeReadOnlyPtr();
                            var streamLength = stream.Length;

                            var processorInternalID = stream.ReadByte();
                            
                            var processor = MessageProcessor.Null;
                            byte hasProcessorOutput = 0;
                            void* processorOutput = default;
                            
                            if ( processorInternalID > 0 && Processors.TryGetValue( processorInternalID, out processor ) )
                            {
                                hasProcessorOutput = MessageProcessorHandler.ProcessRead( processor, ref stream, out processorOutput ) ? StreamExtensions.True : StreamExtensions.False;
                            }
                            
                            var type = stream.ReadByte();

                            var messageID = stream.ReadUShort();

                            var ptr = UnsafeUtility.AddressOf( ref this );
                            
                            // Trigger message handler here
                            var messageReceivedArgs = new MessageReceivedArgs
                            {
                                Connection = Connections[ 0 ],
                                MessageID = messageID,
                                Stream = stream,
                                Processor = processor,
                                HasProcessorOutput = hasProcessorOutput,
                                ProcessorOutputPtr = processorOutput,
                            };

                            switch ( type )
                            {
                                // Internal
                                case 0:
                                    ProcessMessageLayers( ref messageReceivedArgs, ref InternalMessageLayers, ptr );
                                    break;
                                // Custom
                                case 1:
                                    var messageReceivedPtr = new MessageReceivedPtr
                                    {
                                        Args = messageReceivedArgs,
                                        StreamPtr = streamPtr,
                                        Length = streamLength,
                                        BytesRead = stream.GetBytesRead(),
                                    };
                                    
                                    MessageReceivedArgsWriter.Enqueue( messageReceivedPtr );
                                
                                    if ( MessageReceivedCallback.IsCreated )
                                    {
                                        MessageReceivedCallback.Ptr.Invoke( ref messageReceivedArgs );
                                    }
                                    
                                    ProcessMessageLayers( ref messageReceivedArgs, ref CustomMessageLayers, ptr );
                                    break;
                            }
                            
                            break;
                    }
                }
            }

            private void ProcessMessageLayers( ref MessageReceivedArgs args, ref NativeArray<NativeMessageHandler>.ReadOnly layers, void* ptr )
            {
                bool executed = false;
                
                for ( int i = 0; i < layers.Length; i++ )
                {
                    var layer = layers[ i ];

                    if ( layer.IsCreated && layer.CompatibleMessageID == args.MessageID )
                    {
                        layer.ProcessFunction.Ptr.Invoke( ref args, ptr );

                        executed = true;
                    }
                }

                if ( !executed ) 
                    return;
                
                if ( !args.HasProcessorOutput.ToBoolean() )
                    return;
                
                // Free memory is we are done with it
                UnsafeUtility.Free( args.ProcessorOutputPtr, Allocator.Temp );
            }
        }
    }

    /// <summary>
    /// Use this for internal bursted event handling
    /// </summary>
    [BurstCompile]
    internal static class NetworkClientBurst
    {
        [BurstCompile]
        [MonoPInvokeCallback(typeof(ClientConnectedCallback))]
        public static void ClientConnected( ref ClientConnectedArgs args )
        {
            if ( args.IsLocal.ToBoolean() )
            {
                NetworkClient.SharedClientID.Data = args.ClientID;
            }
        }
    }

    public static class NetworkClientEvent
    {
        public static event ClientDisconnectedCallback ClientDisconnected;
        
        public static event ClientConnectedCallback ClientConnected;

        public static MessageReceivedCallback MessageReceived
        {
            get => _messageHandler.MessageReceived;
            set => _messageHandler.MessageReceived = value;
        } 

        private static MessageHandler _messageHandler;
        
        private static NativeQueue<ClientDisconnectedArgs> _clientDisconnectedArgs;
        
        private static NativeQueue<ClientConnectedArgs> _clientConnectedArgs;

        internal static void Initialize()
        {
            _clientDisconnectedArgs = new NativeQueue<ClientDisconnectedArgs>( Allocator.Persistent );
            _clientConnectedArgs = new NativeQueue<ClientConnectedArgs>( Allocator.Persistent );
            _messageHandler = new MessageHandler( "CLIENT", Allocator.Persistent );
        }

        internal static void DeInitialize()
        {
            _clientDisconnectedArgs.Dispose();
            _clientConnectedArgs.Dispose();
            _messageHandler.Dispose();
        }

        internal static  void ProcessEvents()
        {
            while ( _clientDisconnectedArgs.TryDequeue( out var args ) )
            {
                ClientDisconnected?.Invoke( ref args );
            }
            
            while ( _clientConnectedArgs.TryDequeue( out var args ) )
            {
                ClientConnected?.Invoke( ref args );
            }
            
            _messageHandler.Update();
        }

        public static void AddManagedReceivedCallback( ushort messageID, MessageReceivedCallback receivedCallback )
        {
            _messageHandler.AddManagedReceivedCallback( messageID, receivedCallback );
        }

        internal static NativeQueue<ClientDisconnectedArgs>.ParallelWriter GetClientDisconnectedWriter() => _clientDisconnectedArgs.AsParallelWriter();
        
        internal static NativeQueue<ClientConnectedArgs>.ParallelWriter GetClientConnectedWriter() => _clientConnectedArgs.AsParallelWriter();
        
        internal static NativeQueue<MessageReceivedPtr>.ParallelWriter GetMessageReceivedWriter() => _messageHandler.AsParallelWriter();
    }
}