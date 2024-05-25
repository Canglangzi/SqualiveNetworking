// This file is provided under The MIT License as part of SqualiveNetworking.
// Copyright (c) Squalive-Studios
// For additional information please see the included LICENSE.md file or view it on GitHub:
// https://github.com/Squalive/SqualiveNetworking

using AOT;
using SqualiveNetworking.Utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace SqualiveNetworking.Message.Processor
{
    [BurstCompile]
    public unsafe struct TickProcessorStage : IMessageProcessorStage
    {
        public MessageProcessorStage StaticInitialize()
        {
            return new MessageProcessorStage
            {
                ProcessWrittenFunction = new PortableFunctionPointer<MessageProcessWritten>( ProcessWritten ),
                ProcessReadFunction = new PortableFunctionPointer<MessageProcessRead>( ProcessRead ),
            };
        }

        [BurstCompile]
        [MonoPInvokeCallback( typeof( MessageProcessWritten ) )]
        private static void ProcessWritten( ref bool isServer, ref DataStreamWriter stream )
        {
            var currentTick = isServer ? NetworkServer.TickSystem.Data.CurrentTick : NetworkClient.TickSystem.Data.CurrentTick; 

            stream.WriteUInt( currentTick );
        }

        [BurstCompile]
        [MonoPInvokeCallback( typeof( MessageProcessRead ) )]
        private static void ProcessRead( ref bool isServer, ref DataStreamReader stream, out void* output )
        {
            var currentTick = stream.ReadUInt();

            output = UnsafeUtility.Malloc( UnsafeUtility.SizeOf<uint>(), UnsafeUtility.AlignOf<uint>(), Allocator.Temp );

            *(uint*)output = currentTick;
        }
    }
    
    public static unsafe class TickExtensions
    {
        public static uint AsTick( this MessageReceivedArgs args )
        {
            return args.HasProcessorOutput.ToBoolean() ? UnsafeUtility.AsRef<uint>( args.ProcessorOutputPtr ) : 0;
        }
    }
}