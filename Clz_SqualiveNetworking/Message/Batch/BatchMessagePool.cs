// This file is provided under The MIT License as part of SqualiveNetworking.
// Copyright (c) Squalive-Studios
// For additional information please see the included LICENSE.md file or view it on GitHub:
// https://github.com/Squalive/SqualiveNetworking

using System;
using Unity.Collections;

namespace SqualiveNetworking.Message.Batch
{
    public unsafe struct BatchMessagePool : IDisposable
    {
        // int -- ID
        private NativeParallelHashMap<int, IntPtr> _ptrs;

        private bool IsCreated => _ptrs.IsCreated;

        private readonly int _poolCapacity;

        private readonly AllocatorManager.AllocatorHandle _allocator;

        internal BatchMessagePool( int poolCapacity, Allocator allocator )
        {
            _allocator = allocator;
            
            _poolCapacity = poolCapacity;
            
            _ptrs = new NativeParallelHashMap<int, IntPtr>( 64, _allocator );
        }

        internal BatchMessage GetOrCreate<T>( int id, T message ) where T : unmanaged, IBatchMessage
        {
            if ( !EnsureInitialized() )
                return default;

            BatchMessage batchMessage = new BatchMessage();
        
            if ( _ptrs.TryGetValue( id, out var ptr ) )
            {
                if ( AsMessagesHashmap( ptr, out var hashMap ) )
                {
                    hashMap->TryAdd( message.MessageID(), batchMessage );
                }
            }
            else
            {
                var poolHashmap = new NativeParallelHashMap<ushort, BatchMessage>( _poolCapacity, _allocator );

                ptr = ( IntPtr )( &poolHashmap );

                _ptrs[ id ] = ptr;

                poolHashmap.Add( message.MessageID(), batchMessage );
            }

            return batchMessage;
        }

        private bool EnsureInitialized()
        {
            if ( !IsCreated )
            {
#if ENABLE_SQUALIVE_NET_DEBUG
                throw new NullReferenceException( "BatchMessagePool hasnt been setup yet" );
#endif
                return false;
            }

            return true;
        }

        public void Dispose()
        {
            foreach ( var pair in _ptrs )
            {
                if ( AsMessagesHashmap( pair.Value, out var hashMap ) )
                {
                    hashMap->Dispose();
                    
                    pair.Value = IntPtr.Zero;
                }
            }
            
            _ptrs.Dispose();
        }

        private static bool AsMessagesHashmap( IntPtr ptr, out NativeParallelHashMap<ushort, BatchMessage>* hashMap )
        {
            hashMap = default;
            
            if ( ptr != IntPtr.Zero )
            {
                hashMap = (NativeParallelHashMap<ushort, BatchMessage>*)ptr;
                
                return hashMap->IsCreated;
            }

            return false;
        }
    }
}