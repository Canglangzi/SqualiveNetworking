// This file is provided under The MIT License as part of SqualiveNetworking.
// Copyright (c) Squalive-Studios
// For additional information please see the included LICENSE.md file or view it on GitHub:
// https://github.com/Squalive/SqualiveNetworking

using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Networking.Transport;
using UnityEngine;
using DataStreamReader = Unity.Collections.DataStreamReader;
using LogType = SqualiveNetworking.Utils.LogType;

namespace SqualiveNetworking
{
    public static class NetworkClient
    {
        private static NetworkDriver _driver;

        private static NativeArray<NetworkConnection> _connections;

        private static NetworkConnection Connection => _connections[ 0 ];

        private static NetworkPipeline _fragmentationPipeline, _reliablePipeline, _unreliablePipeline;

        private static JobHandle _clientJobHandle;

        private static bool _initialized;

        private static readonly SharedStatic<bool> SharedIsConnected = SharedStatic<bool>.GetOrCreate<ClientKey>(  );

        public static bool IsConnected => SharedIsConnected.Data;
        
        private class ClientKey
        {
            
        }
        
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

        public static void Initialize( NetworkSettings settings )
        {
            if ( !EnsureUnInitialized() )
                return;

            _initialized = true;

            SharedIsConnected.Data = false;

            _driver = NetworkDriver.Create( settings );

            _connections = new NativeArray<NetworkConnection>( 1, Allocator.Persistent );

            _connections[0] = default;
            
            _fragmentationPipeline = _driver.CreatePipeline( typeof( FragmentationPipelineStage ) );
            _reliablePipeline = _driver.CreatePipeline( typeof( ReliableSequencedPipelineStage ) );
            _unreliablePipeline = _driver.CreatePipeline( typeof( UnreliableSequencedPipelineStage ) );
        }

        public static void DeInitialize()
        {
            if ( !EnsureInitialized() )
                return;
            
            Disconnect();
            
            _connections.Dispose();
            _driver.Dispose();
            
            Debug.Log( "[CLIENT]: Disposing client...." );
            
            _initialized = false;
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
            
            _clientJobHandle.Complete();
            
            Connection.Disconnect( _driver );
            
            Debug.Log( "[CLIENT]: Disconnecting from server...." );
        }

        public static void Update()
        {
            if ( !EnsureInitialized() )
                return;
            
            _clientJobHandle.Complete();
            
            _clientJobHandle = _driver.ScheduleUpdate();

            _clientJobHandle = new ClientUpdateJob
            {
                Driver = _driver,
                Connections = _connections,
            }.Schedule( _clientJobHandle );
        }
        
#if ENABLE_SQUALIVE_NET_BURST
        [BurstCompile]
#endif
        private struct ClientUpdateJob : IJob
        {
            public NetworkDriver Driver;
            
            public NativeArray<NetworkConnection> Connections;

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
                        case NetworkEvent.Type.Connect:
                            
#if ENABLE_SQUALIVE_NET_DEBUG
                            Debug.Log( $"[CLIENT]: Successfully Connected to {Driver.GetRemoteEndpoint( Connections[ 0 ] ).ToFixedString()}" );
#endif
                            // TODO: Add connected function pointer here
                            break;
                        case NetworkEvent.Type.Disconnect:
#if ENABLE_SQUALIVE_NET_DEBUG
                            Debug.Log( $"[CLIENT]: Disconnected from {Driver.GetRemoteEndpoint( Connections[ 0 ] ).ToFixedString()}" );
#endif
                            Connections[ 0 ] = default;
                            break;
                        case NetworkEvent.Type.Data:
                            break;
                    }
                }
            }
        }
    }
}