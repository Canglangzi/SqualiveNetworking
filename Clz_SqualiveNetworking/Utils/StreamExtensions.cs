// This file is provided under The MIT License as part of SqualiveNetworking.
// Copyright (c) Squalive-Studios
// For additional information please see the included LICENSE.md file or view it on GitHub:
// https://github.com/Squalive/SqualiveNetworking

using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace SqualiveNetworking.Utils
{
    public static class StreamExtensions
    {
        public const byte False = 0;
        public const byte True = 1;
        
        public static bool WriteBool( this ref DataStreamWriter writer, bool value )
        {
            return writer.WriteByte( value.ToByte() );
        }

        public static bool ReadBool( this DataStreamReader reader )
        {
            return ToBoolean( reader.ReadByte() );
        }

        public static bool ToBoolean( this byte value )
        {
            return value == True;
        }

        public static byte ToByte( this bool value )
        {
            return value ? True : False;
        }

        #region Float3

        public static DataStreamWriter WriteFloat3( this ref DataStreamWriter writer, float3 value )
        {
            writer.WriteFloat( value.x );
            writer.WriteFloat( value.y );
            writer.WriteFloat( value.z );
            
            return writer;
        }

        public static float3 ReadFloat3( this ref DataStreamReader reader )
        {
            float3 result = new float3( reader.ReadFloat(), reader.ReadFloat(), reader.ReadFloat() );
            
            return result ;
        }

        #endregion
        
        #region Quaternion

        public static DataStreamWriter Writequaternion( this ref DataStreamWriter writer, quaternion value )
        {
            writer.WriteFloat( value.value.x );
            writer.WriteFloat( value.value.y );
            writer.WriteFloat( value.value.z );
            writer.WriteFloat( value.value.w );
            
            return writer;
        }

        public static quaternion Readquaternion( this ref DataStreamReader reader )
        {
            quaternion result = new quaternion( reader.ReadFloat(), reader.ReadFloat(), reader.ReadFloat(), reader.ReadFloat() );
            
            return result ;
        }

        public static DataStreamWriter WriteQuaternion( this ref DataStreamWriter writer, Quaternion value )
        {
            writer.WriteFloat( value.x );
            writer.WriteFloat( value.y );
            writer.WriteFloat( value.z );
            writer.WriteFloat( value.w );
            
            return writer;
        }

        public static Quaternion ReadQuaternion( this ref DataStreamReader reader )
        {
            Quaternion result = new Quaternion( reader.ReadFloat(), reader.ReadFloat(), reader.ReadFloat(), reader.ReadFloat() );
            
            return result ;
        }

        #endregion
    }
}