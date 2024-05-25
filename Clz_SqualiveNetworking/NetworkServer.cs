// This file is provided under The MIT License as part of SqualiveNetworking.
// Copyright (c) Squalive-Studios
// For additional information please see the included LICENSE.md file or view it on GitHub:
// https://github.com/Squalive/SqualiveNetworking

using Unity.Burst;
using System;
using System.Runtime.CompilerServices;
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
using DataStreamWriter = Unity.Collections.DataStreamWriter;

namespace SqualiveNetworking
{
    public struct ServerClientConnectedArgs
    {
        public ushort ClientID;
        
        public NetworkDriver Driver;

        public NetworkConnection Connection;
    }

    public struct ServerClientDisconnectedArgs
    {
        public ushort ClientID;
        
        public DisconnectReason Reason;
        
        public NetworkConnection Connection;
    }
    
    /// <summary>
    /// This uses ref because burst function pointer only supports references or pointers
    /// </summary>
    public delegate void ServerClientConnectedCallback( ref ServerClientConnectedArgs args );

    public delegate void ServerClientDisconnectedCallback( ref ServerClientDisconnectedArgs args );

    public delegate void ServerStartedCallback( NetworkDriver driver );
    
    public delegate void ServerStoppedCallback(  );
    
    public static class NetworkServer
    {
        public static bool Initialized => _driver.IsCreated && _initialized && _connectionsMap.IsCreated;
        
        private static NetworkDriver _driver;

        private static NativeParallelHashMap<ushort, NetworkConnection> _connectionsMap;

        private static NativeNetworkMessageHandlers _internalHandlers;
        
        private static NativeNetworkMessageHandlers _customHandlers;

        private static NetworkPipeline _fragmentationPipeline, _reliablePipeline, _unreliablePipeline;

        private static JobHandle _serverJobHandle;

        private static bool _initialized;

        private static PortableFunctionPointer<ServerClientConnectedCallback> _clientConnectedPtr;
        
        private static PortableFunctionPointer<ServerClientDisconnectedCallback> _clientDisconnectedPtr;

        private static PortableFunctionPointer<MessageReceivedCallback> _messageReceivedPtr;

        private static MessageProcessorHandler _messageProcessorHandler;

        private static NativeHashMap<byte, MessageProcessor> _processors;

        private static int _maxPlayer;
        
        internal static readonly SharedStatic<TickSystem> TickSystem = SharedStatic<TickSystem>.GetOrCreate<TickKey>(  );

        public static uint CurrentTick => TickSystem.Data.CurrentTick;
        
        private class TickKey { }
        
        public static bool EnsureInitialized()
        {
            if ( !_initialized || !_driver.IsCreated )
            {
#if ENABLE_SQUALIVE_NET_DEBUG
                throw new Exception( "Server hasn't been initialized" );
#endif
                return false;
            }

            return true;
        }

        public static bool EnsureUnInitialized()
        {
            if ( _initialized || _driver.IsCreated )
            {
#if ENABLE_SQUALIVE_NET_DEBUG
                throw new Exception( "Server has been initialized already" );
#endif
                return false;
            }

            return true;
        }

        /// <summary>
        /// This uses the default baselib interface
        /// </summary>
        public static void Start<T>( int maxPlayer, ushort port, bool isIpv6, TickSystem tickSystem, NetworkSettings settings, T networkInterface ) where T : unmanaged, INetworkInterface
        {
            if ( !EnsureUnInitialized() )
                return;

            _initialized = true;

            _maxPlayer = maxPlayer;
            TickSystem.Data = tickSystem;

            _driver = NetworkDriver.Create( networkInterface, settings );
            _connectionsMap = new NativeParallelHashMap<ushort, NetworkConnection>( maxPlayer, Allocator.Persistent );
            
            _internalHandlers = new NativeNetworkMessageHandlers( Allocator.Persistent );
            _customHandlers = new NativeNetworkMessageHandlers( Allocator.Persistent );

            var endpoint = isIpv6 ? NetworkEndpoint.AnyIpv6.WithPort( port ) : NetworkEndpoint.AnyIpv4.WithPort( port );

            if ( _driver.Bind( endpoint ) != 0 )
            {
#if ENABLE_SQUALIVE_NET_DEBUG
                Debug.LogError(  $"[SERVER]: Failed to bind to port {port}" );
#endif
            }

            _fragmentationPipeline = _driver.CreatePipeline( typeof( FragmentationPipelineStage ), typeof( ReliableSequencedPipelineStage ) );
            _reliablePipeline = _driver.CreatePipeline( typeof( ReliableSequencedPipelineStage ) );
            _unreliablePipeline = _driver.CreatePipeline( typeof( UnreliableSequencedPipelineStage ) );

            _messageProcessorHandler = new MessageProcessorHandler( 1, Allocator.Persistent );
            _processors = new NativeHashMap<byte, MessageProcessor>( 32, Allocator.Persistent );

            _driver.Listen();

            NetworkServerEvent.Initialize( _driver );

#if ENABLE_SQUALIVE_NET_DEBUG
            Debug.Log( $"[SERVER]: Start on port: {port}" );
#endif
        }

        public static void Start( int maxPlayer, ushort port, TickSystem tickSystem, NetworkSettings settings ) =>
            Start( maxPlayer, port, false, tickSystem, settings, new UDPNetworkInterface() );

        /// <summary>
        /// Start with tickrate 50
        /// </summary>
        /// <param name="maxPlayer"></param>
        /// <param name="port"></param>
        /// <param name="settings"></param>
        public static void Start( int maxPlayer, ushort port, NetworkSettings settings ) =>
            Start( maxPlayer, port, false, new TickSystem( true, 50 ), settings, new UDPNetworkInterface() );

        /// <summary>
        /// Start with tickrate 50
        /// </summary>
        /// <param name="maxPlayer"></param>
        /// <param name="port"></param>
        public static void Start( int maxPlayer, ushort port ) =>
            Start( maxPlayer, port, false, new TickSystem( true, 50 ), new NetworkSettings( Allocator.Temp ), new UDPNetworkInterface() );

        public static void Stop()
        {
            if ( !EnsureInitialized() )
                return;
            
            _initialized = false;
            
            _serverJobHandle.Complete();

            foreach ( var pair in _connectionsMap.AsReadOnly() )
            {
                pair.Value.Disconnect( _driver );
            }
            
            _driver.ScheduleUpdate().Complete();
            
            _internalHandlers.Dispose();
            _customHandlers.Dispose();
            
            _driver.Dispose();
            _connectionsMap.Dispose();
            
            _messageProcessorHandler.Dispose();
            _processors.Dispose();
            
            NetworkServerEvent.DeInitialize();
            
#if ENABLE_SQUALIVE_NET_DEBUG
            Debug.Log( "[SERVER]: Disposing server...." );
#endif
        }

        /// <summary>
        /// Force a update
        /// </summary>
        public static void Update()
        {
            if ( !EnsureInitialized() )
                return;
            
            _serverJobHandle.Complete();
            
            NetworkServerEvent.ProcessEvents();

            var connectionJob = new ServerUpdateConnectionJob
            {
                MaxPlayer = _maxPlayer,
                Driver = _driver,
                Connections = _connectionsMap,
                ReliablePipeline = _reliablePipeline,
                ClientConnectedCallback = _clientConnectedPtr,
                ClientConnectedWriter = NetworkServerEvent.GetClientConnectedWriter(),
            };

            var updateJob = new ServerUpdateJob
            {
                Driver = _driver.ToConcurrent(),
                Connections = _connectionsMap,
                ClientDisconnectedCallback = _clientDisconnectedPtr,
                ClientDisconnectedArgsWriter = NetworkServerEvent.GetClientDisconnectedWriter(),
                MessageReceivedCallback = _messageReceivedPtr,
                MessageReceivedArgsWriter = NetworkServerEvent.GetMessageReceivedWriter(),
                InternalMessageLayers = _internalHandlers.GetReadOnlyLayers(),
                CustomMessageLayers = _customHandlers.GetReadOnlyLayers(),
                MessageProcessorHandler = _messageProcessorHandler,
                Processors = _processors.AsReadOnly(),
            };

            _serverJobHandle = _driver.ScheduleUpdate();

            _serverJobHandle = connectionJob.Schedule( _serverJobHandle );

            _serverJobHandle = updateJob.Schedule( _serverJobHandle );
        }

        /// <summary>
        /// using the internal tick system to perform update at a appropriate time
        /// </summary>
        /// <param name="deltaTime">times in between frames</param>
        public static void Tick( float deltaTime )
        {
            while ( TickSystem.Data.Update( ref deltaTime ) )
            {
                Update();
            }
        }

        public static void SendMessage<T>( SendType sendType, T netMessage, ushort clientID ) where T : unmanaged, INetMessage
        {
            SendMessage( sendType, MessageProcessor.Null, netMessage, clientID );
        }

        public static void SendMessage<T>( SendType sendType, MessageProcessor messageProcessor, T netMessage, ushort clientID ) where T : unmanaged, INetMessage
        {
            var connection = _connectionsMap[ clientID ];

            SendMessage( sendType, messageProcessor, netMessage, connection );
        }

        private static void SendMessage<T>( SendType sendType, MessageProcessor messageProcessor, T netMessage, NetworkConnection connection ) where T : unmanaged, INetMessage
        {
            DataStreamWriter writer;
            
            switch ( sendType )
            {
                case SendType.Reliable:
                    if ( !NetworkHelper.BeginSend( _driver, _reliablePipeline, _messageProcessorHandler, messageProcessor, connection, out writer ) ) return;
                    break;
                case SendType.Frag:
                    if ( !NetworkHelper.BeginSend( _driver, _fragmentationPipeline, _messageProcessorHandler, messageProcessor, connection, out writer ) ) return;
                    break;
                default:
                    if ( !NetworkHelper.BeginSend( _driver, _unreliablePipeline, _messageProcessorHandler, messageProcessor, connection, out writer ) ) return;
                    break;
            }

            NetworkHelper.SendCustomMessage( netMessage, _driver, ref writer );
        }

        public static void SendMessageToAll<T>( SendType sendType, MessageProcessor messageProcessor, T netMessage ) where T : unmanaged, INetMessage
        {
            _serverJobHandle.Complete();

            var map = _connectionsMap.GetValueArray( Allocator.Temp );

            foreach ( var pair in _connectionsMap.AsReadOnly() )
            {
                SendMessage( sendType, messageProcessor, netMessage, pair.Value );
            }

            map.Dispose();
        }

        public static void SendMessageToAll<T>( SendType sendType, MessageProcessor messageProcessor, T netMessage, ushort exceptID ) where T : unmanaged, INetMessage
        {
            _serverJobHandle.Complete();
            
            var map = _connectionsMap.GetKeyValueArrays( Allocator.Temp );

            for ( int i = 0; i < map.Length; i++ )
            {
                if ( map.Keys[ i ] != exceptID )
                    SendMessage( sendType, messageProcessor, netMessage, map.Values[ i ] );
            }

            map.Dispose();
        }

        public static void SendMessageToAll<T>( SendType sendType, T netMessage ) where T : unmanaged, INetMessage
        {
            SendMessageToAll( sendType, MessageProcessor.Null,  netMessage );
        }

        public static void SendMessageToAll<T>( SendType sendType, T netMessage, ushort exceptID )
            where T : unmanaged, INetMessage
        {
            SendMessageToAll( sendType, MessageProcessor.Null,  netMessage, exceptID );
        }
        
        public static void SetClientConnectedFunctionPtr( ServerClientConnectedCallback clientConnectedCallback )
        {
            _clientConnectedPtr = new PortableFunctionPointer<ServerClientConnectedCallback>( clientConnectedCallback );
        }

        public static void SetClientConnectedFunctionPtr( ServerClientDisconnectedCallback clientDisconnectedCallback )
        {
            _clientDisconnectedPtr = new PortableFunctionPointer<ServerClientDisconnectedCallback>( clientDisconnectedCallback );
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
        private struct ServerUpdateConnectionJob : IJob
        {
            public int MaxPlayer;
            
            public NetworkDriver Driver;

            public NativeParallelHashMap<ushort, NetworkConnection> Connections;

            public PortableFunctionPointer<ServerClientConnectedCallback> ClientConnectedCallback;

            [NativeDisableContainerSafetyRestriction]
            public NativeQueue<ServerClientConnectedArgs>.ParallelWriter ClientConnectedWriter;

            public NetworkPipeline ReliablePipeline;
            
            public void Execute()
            {
                // Clean up connections.
                var connections = Connections.GetKeyValueArrays( Allocator.Temp );

                for ( int i = 0; i < connections.Length; i++ )
                {
                    if ( !connections.Values[ i ].IsCreated )
                    {
                        Connections.Remove( connections.Keys[ i ] );
                    }
                }

                connections.Dispose();
                
                NetworkConnection connection;
                
                while ( ( connection = Driver.Accept() ) != default )
                {
                    var id = GetNewID();

                    // This means the server has already maxed out
                    if ( id == 0 )
                    {
                        Driver.Disconnect( connection );
                        continue;
                    }
                    
                    Connections.Add( id, connection );

                    var endPoint = Driver.GetRemoteEndpoint( connection );
                    
#if ENABLE_SQUALIVE_NET_DEBUG
                    Debug.Log( $"[SERVER]: Accepting New Connection({id}) From: {endPoint.ToFixedString()}" );
#endif
                    // Trigger client connected here
                    var args = new ServerClientConnectedArgs
                    {
                        ClientID = id,
                        Driver = Driver,
                        Connection = connection,
                    };
                    
                    if ( ClientConnectedCallback.IsCreated )
                    {
                        ClientConnectedCallback.Ptr.Invoke( ref args );
                    }

                    ClientConnectedWriter.Enqueue( args );

                    foreach ( var pair in Connections )
                    {
                        if ( !NetworkHelper.BeginSend( Driver, ReliablePipeline, MessageProcessorHandler.Null, MessageProcessor.Null, pair.Value, out var writer, 0 ) ) continue;

                        NetworkHelper.SendInternalMessage( new ClientConnectedMessage
                        {
                            ClientID = id,
                            IsLocal = pair.Key == id,
                        }, Driver, ref writer );
                    }
                }
            }

            private ushort GetNewID()
            {
                for ( ushort i = 1; i <= MaxPlayer; i++ )
                {
                    if ( !Connections.ContainsKey( i ) )
                    {
                        return i;
                    }
                }

                return 0;
            }
        }

#if ENABLE_SQUALIVE_NET_BURST
        [BurstCompile]
#endif
        private unsafe struct ServerUpdateJob : IJob
        {
            public NetworkDriver.Concurrent Driver;

            public NativeParallelHashMap<ushort, NetworkConnection> Connections;

            // Use for triggering client disconnect function ptr
            public PortableFunctionPointer<ServerClientDisconnectedCallback> ClientDisconnectedCallback;

            [NativeDisableContainerSafetyRestriction]
            public NativeQueue<ServerClientDisconnectedArgs>.ParallelWriter ClientDisconnectedArgsWriter;

            // Use for triggering message received function ptr
            public PortableFunctionPointer<MessageReceivedCallback> MessageReceivedCallback;

            [NativeDisableContainerSafetyRestriction]
            public NativeQueue<MessageReceivedPtr>.ParallelWriter MessageReceivedArgsWriter;

            public NativeArray<NativeMessageHandler>.ReadOnly InternalMessageLayers;
            
            public NativeArray<NativeMessageHandler>.ReadOnly CustomMessageLayers;

            public MessageProcessorHandler MessageProcessorHandler;

            public NativeHashMap<byte, MessageProcessor>.ReadOnly Processors;
            
            public void Execute()
            {
                foreach ( var pair in Connections )
                {
                    NetworkEvent.Type cmd;
                    
                    while ( ( cmd = Driver.PopEventForConnection( pair.Value, out var stream ) ) != NetworkEvent.Type.Empty )
                    {
                        switch ( cmd )
                        {
                            case NetworkEvent.Type.Disconnect:
                                pair.Value = default;
                                
                                var disconnectReason = (DisconnectReason)stream.ReadByte();
                                
    #if ENABLE_SQUALIVE_NET_DEBUG
                                // switch ( disconnectReason )
                                // {
                                //     case DisconnectReason.Timeout:
                                //         Debug.Log( $"[SERVER]: Client {pair.Key} Timed out" );
                                //         break;
                                //     case DisconnectReason.ProtocolError:
                                //         Debug.Log( $"[SERVER]: Client {pair.Key} Disconnected ( Protocol error )" );
                                //         break;
                                //     case DisconnectReason.ClosedByRemote:
                                //         Debug.Log( $"[SERVER]: Client {pair.Key} Disconnected ( Closed by remote )" );
                                //         break;
                                //     case DisconnectReason.MaxConnectionAttempts:
                                //         Debug.Log( $"[SERVER]: Client {pair.Key} Disconnected ( Connection attempts failed )" );
                                //         break;
                                //     default:
                                //         Debug.Log( $"[SERVER]: Client {pair.Key} Disconnected ({(byte)disconnectReason})" );
                                //         break;
                                // }
                                Debug.Log( $"[SERVER]: Client {pair.Key} Disconnected {disconnectReason.ToFixedString()}" );
    #endif
                                // Trigger client disconnected here
                                var args = new ServerClientDisconnectedArgs
                                {
                                    ClientID = pair.Key,
                                    Reason = disconnectReason,
                                    Connection = pair.Value,
                                };
                                
                                ClientDisconnectedArgsWriter.Enqueue( args );

                                if ( ClientDisconnectedCallback.IsCreated )
                                {
                                    ClientDisconnectedCallback.Ptr.Invoke( ref args );
                                }
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
                                    ClientID = pair.Key,
                                    Connection = pair.Value,
                                    MessageID = messageID,
                                    Stream = stream,
                                    Processor = processor,
                                    HasProcessorOutput = hasProcessorOutput,
                                    ProcessorOutputPtr = processorOutput,
                                };

                                switch ( type )
                                {
                                    case 0:
                                        ProcessMessageLayers( ref messageReceivedArgs, ref InternalMessageLayers, ptr );
                                        break;
                                    // Custom Message
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
            }

            private void ProcessMessageLayers( ref MessageReceivedArgs args, ref NativeArray<NativeMessageHandler>.ReadOnly layers, void* ptr )
            {
                for ( int i = 0; i < layers.Length; i++ )
                {
                    var layer = layers[ i ];

                    if ( layer.IsCreated && layer.CompatibleMessageID == args.MessageID )
                    {
                        layer.ProcessFunction.Ptr.Invoke( ref args, ptr );
                    }
                }
            }
        }
    }

    public static class NetworkServerEvent
    {
        public static event ServerStartedCallback ServerStarted;

        public static event ServerStoppedCallback ServerStopped;
        
        public static event ServerClientConnectedCallback ClientConnected;
        
        public static event ServerClientDisconnectedCallback ClientDisconnected;

        public static MessageReceivedCallback MessageReceived
        {
            get => _messageHandler.MessageReceived;
            set => _messageHandler.MessageReceived = value;
        } 

        private static NativeQueue<ServerClientConnectedArgs> _clientConnectedArgs;
        
        private static NativeQueue<ServerClientDisconnectedArgs> _clientDisconnectedArgs;

        private static MessageHandler _messageHandler;

        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        internal static void Initialize( NetworkDriver driver )
        {
            _clientConnectedArgs = new NativeQueue<ServerClientConnectedArgs>( Allocator.Persistent );
            _clientDisconnectedArgs = new NativeQueue<ServerClientDisconnectedArgs>( Allocator.Persistent );

            _messageHandler = new MessageHandler( "SERVER", Allocator.Persistent );

            ServerStarted?.Invoke( driver );
        }

        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        internal static void DeInitialize()
        {
            _clientConnectedArgs.Dispose();
            _clientDisconnectedArgs.Dispose();
            
            _messageHandler.Dispose();
            
            ServerStopped?.Invoke();
        }

        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        internal static void ProcessEvents()
        {
            while ( _clientConnectedArgs.TryDequeue( out var args ) )
            {
                ClientConnected?.Invoke( ref args );
            }
            
            while ( _clientDisconnectedArgs.TryDequeue( out var args ) )
            {
                ClientDisconnected?.Invoke( ref args );
            }
            
            _messageHandler.Update();
        }

        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        public static void AddManagedReceivedCallback( ushort messageID, MessageReceivedCallback receivedCallback )
        {
            _messageHandler.AddManagedReceivedCallback( messageID, receivedCallback );
        }

        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        internal static NativeQueue<ServerClientConnectedArgs>.ParallelWriter GetClientConnectedWriter() => _clientConnectedArgs.AsParallelWriter();
        
        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        internal static NativeQueue<ServerClientDisconnectedArgs>.ParallelWriter GetClientDisconnectedWriter() => _clientDisconnectedArgs.AsParallelWriter();
        
        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        internal static NativeQueue<MessageReceivedPtr>.ParallelWriter GetMessageReceivedWriter() => _messageHandler.AsParallelWriter();
    }
}