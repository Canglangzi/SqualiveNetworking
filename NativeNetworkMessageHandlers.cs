// This file is provided under The MIT License as part of SqualiveNetworking.
// Copyright (c) Squalive-Studios
// For additional information please see the included LICENSE.md file or view it on GitHub:
// https://github.com/Squalive/SqualiveNetworking

using System;
using SqualiveNetworking.Message.Handler;
using Unity.Collections;

namespace SqualiveNetworking
{
    internal struct NativeNetworkMessageHandlers : IDisposable
    {
        private NativeList<NativeMessageHandler> _messageLayers;

        private bool IsCreated => _messageLayers.IsCreated;

        public NativeNetworkMessageHandlers( Allocator allocator )
        {
            _messageLayers = new NativeList<NativeMessageHandler>( 16, allocator );
        }
        
        public void Dispose()
        {
            if ( !EnsureCreated() )
                return;
            
            _messageLayers.Dispose();
        }

        public void AddLayer( INativeMessageHandler handler )
        {
            if ( !EnsureCreated() )
                return;

            var layerStruct = handler.StaticInitialize();

            _messageLayers.Add( layerStruct );
        }

        private bool EnsureCreated()
        {
            if ( !IsCreated )
            {
#if ENABLE_SQUALIVE_NET_DEBUG
                throw new Exception( "Layers hasnt been initialized yet" );
#endif
                return false;
            }

            return true;
        }

        public NativeArray<NativeMessageHandler>.ReadOnly GetReadOnlyLayers() => _messageLayers.AsReadOnly();
    }
}