// This file is provided under The MIT License as part of SqualiveNetworking.
// Copyright (c) Squalive-Studios
// For additional information please see the included LICENSE.md file or view it on GitHub:
// https://github.com/Squalive/SqualiveNetworking

using System;
using Unity.Burst;

namespace SqualiveNetworking.Utils
{
    public struct PortableFunctionPointer<T> where T : Delegate
    {
        internal readonly FunctionPointer<T> Ptr;
        
        public PortableFunctionPointer( T executeDelegate )
        {
            Ptr = BurstCompiler.CompileFunctionPointer( executeDelegate );
        }
    }
}