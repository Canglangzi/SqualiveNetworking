// This file is provided under The MIT License as part of SqualiveNetworking.
// Copyright (c) Squalive-Studios
// For additional information please see the included LICENSE.md file or view it on GitHub:
// https://github.com/Squalive/SqualiveNetworking

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using UnityEngine;

namespace SqualiveNetworking.Utils
{
    /// <summary>Defines log message types.</summary>
    public enum LogType : byte
    {
        /// <summary>Logs that are used for investigation during development.</summary>
        Debug = 0,
        /// <summary>Logs that provide general information about application flow.</summary>
        Info,
        /// <summary>Logs that highlight abnormal or unexpected events in the application flow.</summary>
        Warning,
        /// <summary>Logs that highlight problematic events in the application flow which will cause unexpected behavior if not planned for.</summary>
        Error
    }

    /// <summary>Provides functionality for logging messages.</summary>
    public static class SqualiveLogger
    {
        /// <summary>Encapsulates a method used to log messages.</summary>
        /// <param name="log">The message to log.</param>
        public delegate void LogMethod( string log );

        private static Dictionary<LogType, LogMethod> _logMethods;

        private static readonly SharedStatic<bool> Initialized = SharedStatic<bool>.GetOrCreate<LoggerKey>();
        
        private class LoggerKey { }
        private class LoggerKey2 { }

        /// <summary>Initializes <see cref="SqualiveLogger"/> with all log types enabled.</summary>
        /// <param name="logMethod">The method to use when logging all types of messages.</param>
        public static void Initialize( LogMethod logMethod ) =>
            Initialize( logMethod, logMethod, logMethod, logMethod );
        /// <summary>Initializes <see cref="SqualiveLogger"/> with the supplied log methods.</summary>
        /// <param name="debugMethod">The method to use when logging debug messages. Set to <see langword="null"/> to disable debug logs.</param>
        /// <param name="infoMethod">The method to use when logging info messages. Set to <see langword="null"/> to disable info logs.</param>
        /// <param name="warningMethod">The method to use when logging warning messages. Set to <see langword="null"/> to disable warning logs.</param>
        /// <param name="errorMethod">The method to use when logging error messages. Set to <see langword="null"/> to disable error logs.</param>
        public static void Initialize( LogMethod debugMethod, LogMethod infoMethod, LogMethod warningMethod, LogMethod errorMethod )
        {
            if ( !EnsureUnInitialized() )
                return;

            _logMethods = new Dictionary<LogType, LogMethod>( 4 );

            AddMethod( LogType.Debug, debugMethod );
            AddMethod( LogType.Info, infoMethod );
            AddMethod( LogType.Warning, warningMethod );
            AddMethod( LogType.Error, errorMethod );
            
            Initialized.Data = true;
        }

        public static void DeInitialize()
        {
            if ( !EnsureInitialized() )
                return;

            _logMethods.Clear();
            
            Initialized.Data = false;
        }

        private static void AddMethod( LogType logType, LogMethod logMethod )
        {
            if ( logMethod != null )
            {
                _logMethods[ logType ] = logMethod;
            }
        }

        private static bool EnsureInitialized()
        {
            if ( !Initialized.Data )
            {
#if ENABLE_SQUALIVE_NET_DEBUG
                throw new Exception( "SqualiveLogger hasn't been initialized" );
#endif
                return false;
            }

            return true;
        }

        private static bool EnsureUnInitialized()
        {
            if ( Initialized.Data )
            {
#if ENABLE_SQUALIVE_NET_DEBUG
                throw new Exception( "SqualiveLogger has been initialized already" );
#endif
                return false;
            }

            return true;
        }

        public static void Log( string message )
        {
            Log( LogType.Info, message );
        }

        public static void LogError( string message )
        {
            Log( LogType.Error, message );
        }

        public static void LogWarning( string message )
        {
            Log( LogType.Warning, message );
        }

        public static void LogDebug( string message )
        {
            Log( LogType.Debug, message );
        }

        public static void Log( string logName, string message )
        {
            Log( LogType.Info, logName, message );
        }

        public static void LogError( string logName, string message )
        {
            Log( LogType.Error, logName, message );
        }

        public static void LogWarning( string logName, string message )
        {
            Log( LogType.Warning, logName, message );
        }

        public static void LogDebug( string logName, string message )
        {
            Log( LogType.Debug, logName, message );
        }

        #region Burst

        public static void Log_Bursted( string message )
        {
            Log_Bursted( LogType.Info, message );
        }

        public static void LogError_Bursted( string message )
        {
            Log_Bursted( LogType.Error, message );
        }

        public static void LogWarning_Bursted( string message )
        {
            Log_Bursted( LogType.Warning, message );
        }

        public static void LogDebug_Bursted( string message )
        {
            Log_Bursted( LogType.Debug, message );
        }

        #endregion

        /// <summary>Logs a message.</summary>
        /// <param name="logType">The type of log message that is being logged.</param>
        /// <param name="message">The message to log.</param>
        public static void Log( LogType logType, string message )
        {
            if ( !EnsureInitialized() )
                return;

            _logMethods[ logType ]( message );
        }

        /// <summary>Logs a message.</summary>
        /// <param name="logType">The type of log message that is being logged.</param>
        /// <param name="message">The message to log.</param>
        public static void Log_Bursted( LogType logType, string message )
        {
            switch ( logType )
            {
                case LogType.Info:
                    Debug.Log( message );
                    break;
                case LogType.Error:
                    Debug.LogError( message );
                    break;
                case LogType.Warning:
                    Debug.LogWarning( message );
                    break;
            }
        }
        
        /// <summary>Logs a message.</summary>
        /// <param name="logType">The type of log message that is being logged.</param>
        /// <param name="logName">Who is logging this message.</param>
        /// <param name="message">The message to log.</param>
        public static void Log( LogType logType, string logName, string message )
        {
            if ( !EnsureInitialized() )
                return;

            _logMethods[ logType ]( $"[{logName}]: {message}" );
        }
    }
}