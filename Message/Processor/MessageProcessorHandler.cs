// This file is provided under The MIT License as part of SqualiveNetworking.
// Copyright (c) Squalive-Studios
// For additional information please see the included LICENSE.md file or view it on GitHub:
// https://github.com/Squalive/SqualiveNetworking

using System;
using Unity.Collections;

namespace SqualiveNetworking.Message.Processor
{
    internal unsafe struct MessageProcessorHandler : IDisposable, IEquatable<MessageProcessorHandler>
    {
        public static MessageProcessorHandler Null => default;
        
        private NativeList<MessageProcessorStage> _stages;

        private NativeList<MessageProcessor> _processors;

        private byte _id;

        private bool IsCreated => _stages.IsCreated;
        
        public MessageProcessorHandler( byte id, Allocator allocator )
        {
            _id = id;
            _stages = new NativeList<MessageProcessorStage>( 16, allocator );
            _processors = new NativeList<MessageProcessor>( 16, allocator );
        }

        public MessageProcessor CreateProcessor<T>( T[] processorInterfaces ) where T: IMessageProcessorStage
        {
            if ( !EnsureInitialized() )
                return default;

            if ( _stages.Length >= 32 )
            {
#if ENABLE_SQUALIVE_NET_DEBUG
                throw new Exception( "Way too many message processors" );
#endif
                return default;
            }

            var internalID = (byte)( _processors.Length + 1 );

            var messageProcessor = new MessageProcessor
            {
                InternalID = internalID,
                StartIndex = _stages.Length,
            };

            foreach ( var processorStageInterface in processorInterfaces )
            {
                var processorStage = processorStageInterface.StaticInitialize();
                
                _stages.Add( processorStage );
            }

            messageProcessor.EndIndex = _stages.Length;

            _processors.Add( messageProcessor );

            return messageProcessor;
        }

        /// <returns>False if no processor has occured</returns>
        public bool ProcessWrite( MessageProcessor processor, ref DataStreamWriter stream )
        {
            if ( !CheckStageIndex( processor.StartIndex ) || !CheckStageIndex( processor.EndIndex ) )
            {
                return false;
            }
            
            var isServer = _id == 1;
            
            for ( int i = processor.StartIndex; i < processor.EndIndex; i++ )
            {
                var stage = _stages[ i ];
                
                if ( !stage.IsCreated )
                {
#if ENABLE_SQUALIVE_NET_DEBUG
                    throw new Exception(
                        $"Stage {i} hasnt been created yet, please make sure all stages were created properly" );
#endif
                    return false;
                }

                stage.ProcessWrittenFunction.Ptr.Invoke( ref isServer, ref stream );
            }

            return true;
        }

        /// <para>Output is the first proceessor in the stages</para>
        /// <returns>False if no processor has occured</returns>
        public bool ProcessRead( MessageProcessor processor, ref DataStreamReader stream, out void* output )
        {
            output = default;
            
            if ( !CheckStageIndex( processor.StartIndex ) || !CheckStageIndex( processor.EndIndex ) )
            {
                return false;
            }
            
            var isServer = _id == 1;

            for ( int i = processor.StartIndex; i < processor.EndIndex; i++ )
            {
                var stage = _stages[ i ];

                if ( !stage.IsCreated )
                {
#if ENABLE_SQUALIVE_NET_DEBUG
                    throw new Exception(
                        $"Stage {i} hasnt been created yet, please make sure all stages were created properly" );
#endif
                    output = default;
                
                    return false;
                }
            
                stage.ProcessReadFunction.Ptr.Invoke( ref isServer, ref stream, out output );
            }

            return true;
        }

        private bool CheckStageIndex( int index )
        {
            if ( index > _stages.Length || index < 0 )
            {
#if ENABLE_SQUALIVE_NET_DEBUG
                throw new ArgumentOutOfRangeException(
                    $"target index was out of range: stage length: {_stages.Length}, target: {index}" );
#endif
                return false;
            }

            return true;
        }

        private bool EnsureInitialized()
        {
            if ( !IsCreated )
            {
#if ENABLE_SQUALIVE_NET_DEBUG
                throw new NullReferenceException( "Message Processor Handler hasnt been created! you cannot perform any actions!" );
#endif
                return false;
            }

            return true;
        }
        
        public void Dispose()
        {
            _stages.Dispose();
            _processors.Dispose();
        }

        public bool Equals( MessageProcessorHandler other )
        {
            return _id == other._id;
        }

        public override int GetHashCode()
        {
            return _stages.GetHashCode();
        }
    }
}