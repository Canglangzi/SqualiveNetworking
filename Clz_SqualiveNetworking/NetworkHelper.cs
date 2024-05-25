using SqualiveNetworking.Message.Processor;
using Unity.Collections;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Error;
using UnityEngine;

namespace SqualiveNetworking
{
    internal static class NetworkHelper
    {
        public static bool BeginSend( NetworkDriver driver, NetworkPipeline pipeline, MessageProcessorHandler messageProcessorHandler, MessageProcessor processor, NetworkConnection connection, out DataStreamWriter writer, int requiredPayloadSize = 0 )
        {
            int status = driver.BeginSend( pipeline, connection, out writer, requiredPayloadSize );

            if ( status != 0 )
            {
#if ENABLE_SQUALIVE_NET_DEBUG
                Debug.Log( $"Failed to begin send message, status code: {status}" );
#endif
                return false;
            }
            
            if ( !messageProcessorHandler.Equals( MessageProcessorHandler.Null ) && !processor.Equals( MessageProcessor.Null ) )
            {
                writer.WriteByte( processor.InternalID );

                messageProcessorHandler.ProcessWrite( processor, ref writer );
            }
            else
            {
                writer.WriteByte( 0 );
            }

            return true;
        }
        
        public static bool BeginSend( NetworkDriver driver, NetworkConnection connection, out DataStreamWriter writer, int requiredPayloadSize = 0 )
        {
            return BeginSend( driver, NetworkPipeline.Null, MessageProcessorHandler.Null, MessageProcessor.Null, connection, out writer, requiredPayloadSize );
        }
        
        public static bool EndSend( NetworkDriver driver, DataStreamWriter writer )
        {
            var status = driver.EndSend( writer );

            if ( status < 0 )
            {
#if ENABLE_SQUALIVE_NET_DEBUG
                Debug.LogError( $"Failed to send data ( {writer.Length} ), status code: {status}" );
#endif
                return false;
            }

            return true;
        }

        private static void EndSend<T>( T netMessage, NetworkDriver driver, ref DataStreamWriter writer ) where T : unmanaged, INetMessage
        {
            writer.WriteUShort( netMessage.MessageID() );

            netMessage.Serialize( ref writer );

            EndSend( driver, writer );
        }

        public static void SendCustomMessage<T>( T netMessage, NetworkDriver driver, ref DataStreamWriter writer ) where T : unmanaged, INetMessage
        {
            writer.SetupCustomMessage();

            EndSend( netMessage, driver, ref writer );
        }

        public static void SendInternalMessage<T>( T netMessage, NetworkDriver driver, ref DataStreamWriter writer ) where T : unmanaged, INetMessage
        {
            writer.SetupInternalMessage();

            EndSend( netMessage, driver, ref writer );
        }

        private static DataStreamWriter SetupInternalMessage( this ref DataStreamWriter writer )
        {
            writer.WriteByte( 0 );
            
            return writer;
        }

        private static DataStreamWriter SetupCustomMessage( this ref DataStreamWriter writer )
        {
            writer.WriteByte( 1 );
            
            return writer;
        }

        public static FixedString32Bytes ToFixedString( this DisconnectReason reason )
        {
            switch ( reason )
            {
                case DisconnectReason.Timeout:
                    return "( Timed Out )";
                case DisconnectReason.AuthenticationFailure:
                    return "( Auth Error )";
                case DisconnectReason.ClosedByRemote:
                    return "( Closed By Remote )";
                case DisconnectReason.ProtocolError:
                    return "( Protocol Error )";
                case DisconnectReason.MaxConnectionAttempts:
                    return "( Connection Attempts Failed )";
                default:
                    return "( Default )";
            }
        }
    }
}