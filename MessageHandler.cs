// This file is provided under The MIT License as part of SqualiveNetworking.
// Copyright (c) Squalive-Studios
// For additional information please see the included LICENSE.md file or view it on GitHub:
// https://github.com/Squalive/SqualiveNetworking

using System;
using System.Collections.Generic;
using SqualiveNetworking.Utils;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace SqualiveNetworking
{
    public unsafe struct MessageHandler : IDisposable
    {
        internal MessageReceivedCallback MessageReceived;

        private readonly string _logName;
        
        private NativeQueue<MessageReceivedPtr> _messageReceivedPtrs;
        
        private Dictionary<ushort, MessageReceivedCallback> _registeredManagedReceivedCallbacks;
        
        private bool _useInternalMessageHandler;

        public MessageHandler( string logName, Allocator allocator )
        {
            MessageReceived = null;
            
            _logName = logName;
            
            _useInternalMessageHandler = false;
            
            _messageReceivedPtrs = new NativeQueue<MessageReceivedPtr>( allocator );

            _registeredManagedReceivedCallbacks = new Dictionary<ushort, MessageReceivedCallback>();
        }

        public void Update()
        {
            while ( _messageReceivedPtrs.TryDequeue( out var ptr ) )
            {
                if ( !ptr.Args.Connection.IsCreated )
                {
                    ReleaseOutputPtr( ref ptr );
                    continue;
                }
                
                var na = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<byte>( ptr.StreamPtr, ptr.Length, Allocator.Invalid );

                // We have to do this otherwise unity will complain about it
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                var safetyHandle = AtomicSafetyHandle.GetTempMemoryHandle();

                NativeArrayUnsafeUtility.SetAtomicSafetyHandle( ref na, safetyHandle );
#endif
                
                var stream = new DataStreamReader( na );

                for ( int i = 0; i < ptr.BytesRead; i++ )
                {
                    stream.ReadByte();
                }

                var args = ptr.Args;
                args.Stream = stream;
                
                MessageReceived?.Invoke( ref args );

                if ( !_useInternalMessageHandler )
                {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    AtomicSafetyHandle.Release( safetyHandle );
#endif
                    ReleaseOutputPtr( ref ptr );
                    continue;
                }
                
                if ( _registeredManagedReceivedCallbacks.TryGetValue( args.MessageID, out var callback ) )
                {
                    callback( ref args );
                }
                    
                ReleaseOutputPtr( ref ptr );
                
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.Release( safetyHandle );
#endif
            }
        }

        private static void ReleaseOutputPtr( ref MessageReceivedPtr ptr )
        {
            if ( !ptr.Args.HasProcessorOutput.ToBoolean() )
                return;

            UnsafeUtility.Free( ptr.Args.ProcessorOutputPtr, Allocator.Temp );
        }
        
        public void Dispose()
        {
            MessageReceived = null;
            
            _registeredManagedReceivedCallbacks.Clear();
            _registeredManagedReceivedCallbacks = null;
            
            _messageReceivedPtrs.Dispose();
        }
        
        internal void AddManagedReceivedCallback( ushort messageID, MessageReceivedCallback receivedCallback )
        {
            if ( !_registeredManagedReceivedCallbacks.TryAdd( messageID, receivedCallback ) )
            {
#if ENABLE_SQUALIVE_NET_DEBUG
                SqualiveLogger.LogError( _logName, $"MessageID ({messageID}) has already been registered, please considering changing the message id on: {receivedCallback.Method.Name}" );
#endif
                return;
            }

            _useInternalMessageHandler = true;
        }

        internal NativeQueue<MessageReceivedPtr>.ParallelWriter AsParallelWriter() =>
            _messageReceivedPtrs.AsParallelWriter();
    }
}