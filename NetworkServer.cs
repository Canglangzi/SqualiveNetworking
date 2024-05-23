// This file is provided under The MIT License as part of SqualiveNetworking.
// Copyright (c) Squalive-Studios
// For additional information please see the included LICENSE.md file or view it on GitHub:
// https://github.com/Squalive/SqualiveNetworking

#if ENABLE_SQUALIVE_NET_BURST
using Unity.Burst;
#endif

using System;
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
        public NetworkDriver Driver;

        public NetworkConnection Connection;
    }
    
    /// <summary>
    /// This uses ref because burst function pointer only supports references or pointers
    /// </summary>
    public delegate void ServerClientConnectedCallback( ref ServerClientConnectedArgs args );

    public delegate void ServerStartedCallback( NetworkDriver driver );
    
    public delegate void ServerStoppedCallback(  );
    
    public static class NetworkServer
    {
        private static NetworkDriver _driver;

        private static NativeList<NetworkConnection> _connections;

        private static NetworkPipeline _fragmentationPipeline, _reliablePipeline, _unreliablePipeline;

        private static JobHandle _serverJobHandle;

        private static bool _initialized;

        private static PortableFunctionPointer<ServerClientConnectedCallback> _clientConnectedPtr;
        
        private static bool EnsureInitialized()
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

        private static bool EnsureUnInitialized()
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
        public static void Start( int maxPlayer, ushort port, bool isIpv6, NetworkSettings settings )
        {
            if ( !EnsureUnInitialized() )
                return;

            _initialized = true;
            
            _driver = NetworkDriver.Create( settings );
            _connections = new NativeList<NetworkConnection>( maxPlayer, Allocator.Persistent );

            var endpoint = isIpv6 ? NetworkEndpoint.AnyIpv6.WithPort( port ) : NetworkEndpoint.AnyIpv4.WithPort( port );

            if ( _driver.Bind( endpoint ) != 0 )
            {
                Debug.LogError(  $"[SERVER]: Failed to bind to port {port}" );
            }

            _fragmentationPipeline = _driver.CreatePipeline( typeof( FragmentationPipelineStage ) );
            _reliablePipeline = _driver.CreatePipeline( typeof( ReliableSequencedPipelineStage ) );
            _unreliablePipeline = _driver.CreatePipeline( typeof( UnreliableSequencedPipelineStage ) );

            _driver.Listen();

            NetworkServerEvent.Initialize( _driver );

            Debug.Log( $"[SERVER]: Start on port: {port}" );
        }

        public static void Stop()
        {
            if ( !EnsureInitialized() )
                return;

            _initialized = false;
            
            _serverJobHandle.Complete();
            
            _driver.Dispose();
            _connections.Dispose();
            
            NetworkServerEvent.DeInitialize();

            Debug.Log( "[SERVER]: Disposing server...." );
        }

        public static void Update()
        {
            if ( !EnsureInitialized() )
                return;
            
            _serverJobHandle.Complete();
            
            NetworkServerEvent.ProcessEvents();

            var connectionJob = new ServerUpdateConnectionJob
            {
                Driver = _driver,
                Connections = _connections,
                ClientConnectedCallback = _clientConnectedPtr,
                ClientConnectedWriter = NetworkServerEvent.GetClientConnectedWriter(),
            };

            var updateJob = new ServerUpdateJob
            {
                Driver = _driver.ToConcurrent(),
                Connections = _connections.AsDeferredJobArray(),
            };

            _serverJobHandle = _driver.ScheduleUpdate();

            _serverJobHandle = connectionJob.Schedule( _serverJobHandle );

            _serverJobHandle = updateJob.Schedule( _connections, 1 , _serverJobHandle );
        }
        
        internal static bool BeginSendUnreliable( int clientID, out DataStreamWriter writer, int requiredPayloadSize = 0 )
        {
            int status = _driver.BeginSend( _unreliablePipeline, _connections[ clientID ], out writer, requiredPayloadSize );

            if ( status != 0 )
            {
#if ENABLE_SQUALIVE_NET_DEBUG
                Debug.Log( $"[SERVER]: Failed to begin send message, status code: {status}" );
#endif
                return false;
            }

            return true;
        }

        internal static void EndSend( DataStreamWriter writer )
        {
            _driver.EndSend( writer );
        }

        public static void SendMessage<T>( SendType sendType, T netMessage, int clientID ) where T : unmanaged, INetMessage
        {
            DataStreamWriter writer;
            
            switch ( sendType )
            {
                default:
                    if ( !BeginSendUnreliable( clientID, out writer ) ) return;
                    break;
            }

            writer.WriteUShort( netMessage.MessageID );

            netMessage.Serialize( ref writer );

            EndSend( writer );
        }

        public static void SetClientConnectedFunctionPtr( ServerClientConnectedCallback clientConnectedCallback )
        {
            _clientConnectedPtr = new PortableFunctionPointer<ServerClientConnectedCallback>( clientConnectedCallback );
        }

#if ENABLE_SQUALIVE_NET_BURST
        [BurstCompile]
#endif
        private struct ServerUpdateConnectionJob : IJob
        {
            public NetworkDriver Driver;

            public NativeList<NetworkConnection> Connections;

            public PortableFunctionPointer<ServerClientConnectedCallback> ClientConnectedCallback;

            [NativeDisableContainerSafetyRestriction]
            public NativeQueue<ServerClientConnectedArgs>.ParallelWriter ClientConnectedWriter;
            
            public void Execute()
            {
                // Clean up connections.
                for ( int i = 0; i < Connections.Length; i++ )
                {
                    if ( !Connections[i].IsCreated )
                    {
                        Connections.RemoveAtSwapBack( i );
                        i--;
                    }
                }
                
                NetworkConnection connection;
                
                while ( ( connection = Driver.Accept() ) != default )
                {
                    Connections.Add( connection );

                    var endPoint = Driver.GetRemoteEndpoint( connection );
                    
#if ENABLE_SQUALIVE_NET_DEBUG
                    Debug.Log( $"[SERVER]: Accepting New Connection From: {endPoint.ToFixedString()}" );
#endif
                    // Trigger client connected here
                    var args = new ServerClientConnectedArgs
                    {
                        Driver = Driver,
                        Connection = connection,
                    };
                    
                    if ( ClientConnectedCallback.IsCreated )
                    {
                        ClientConnectedCallback.Ptr.Invoke( ref args );
                    }

                    ClientConnectedWriter.Enqueue( args );
                }
            }
        }

#if ENABLE_SQUALIVE_NET_BURST
        [BurstCompile]
#endif
        private struct ServerUpdateJob : IJobParallelForDefer
        {
            public NetworkDriver.Concurrent Driver;

            public NativeArray<NetworkConnection> Connections;
            
            public void Execute( int index )
            {
                NetworkEvent.Type cmd;

                var connection = Connections[ index ];
                
                while ( ( cmd = Driver.PopEventForConnection( connection, out var stream ) ) != NetworkEvent.Type.Empty )
                {
                    switch ( cmd )
                    {
                        case NetworkEvent.Type.Disconnect:
                            Connections[ index ] = default;
                            
                            var disconnectReason = (DisconnectReason)stream.ReadByte();
                            
#if ENABLE_SQUALIVE_NET_DEBUG
                            switch ( disconnectReason )
                            {
                                case DisconnectReason.Timeout:
                                    Debug.Log( $"[SERVER]: Client {index} Timed out" );
                                    break;
                                case DisconnectReason.ProtocolError:
                                    Debug.Log( $"[SERVER]: Client {index} Disconnected ( Protocol error )" );
                                    break;
                                case DisconnectReason.ClosedByRemote:
                                    Debug.Log( $"[SERVER]: Client {index} Disconnected ( Closed by remote )" );
                                    break;
                                case DisconnectReason.MaxConnectionAttempts:
                                    Debug.Log( $"[SERVER]: Client {index} Disconnected ( Connection attempts failed )" );
                                    break;
                                default:
                                    Debug.Log( $"[SERVER]: Client {index} Disconnected ({(byte)disconnectReason})" );
                                    break;
                            }
#endif
                            // TODO: Trigger client disconnected here
                            break;
                        case NetworkEvent.Type.Data:
                            // TODO: Trigger message handler here
                            break;
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

        private static NativeQueue<ServerClientConnectedArgs> _clientConnectedArgs;

        internal static void Initialize( NetworkDriver driver )
        {
            _clientConnectedArgs = new NativeQueue<ServerClientConnectedArgs>( Allocator.Persistent );

            ServerStarted?.Invoke( driver );
        }

        internal static void DeInitialize()
        {
            _clientConnectedArgs.Dispose();
            
            ServerStopped?.Invoke();
        }

        public static void ProcessEvents()
        {
            while ( _clientConnectedArgs.TryDequeue( out var args ) )
            {
                ClientConnected?.Invoke( ref args );
            }
        }

        internal static NativeQueue<ServerClientConnectedArgs>.ParallelWriter GetClientConnectedWriter() => _clientConnectedArgs.AsParallelWriter();
    }
}