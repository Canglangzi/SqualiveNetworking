using SqualiveNetworking;
using SqualiveNetworking.Message.Processor;
using SqualiveNetworking.Utils;

namespace CLz_SqualiveNetworkingHelper
{
    public static class MessageHelper
    {
        /// <summary>
        /// 向服务器发送消息
        /// </summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <param name="message">消息实例</param>
        /// <param name="sendType">发送类型（可靠或不可靠）</param>
        /// <param name="processor">消息处理器</param>
        public static void SendToServer<T>(this T message, SendType sendType = SendType.Unreliable, MessageProcessor processor = default) where T : unmanaged, INetMessage
        {
            NetworkClient.SendMessage(sendType, processor, message);
        }

        /// <summary>
        /// 向特定客户端发送消息
        /// </summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <param name="message">消息实例</param>
        /// <param name="clientID">客户端ID</param>
        /// <param name="sendType">发送类型（可靠或不可靠）</param>
        /// <param name="processor">消息处理器</param>
        public static void SendToClient<T>(this T message, ushort clientID = 0, SendType sendType = SendType.Unreliable, MessageProcessor processor = default) where T : unmanaged, INetMessage
        {
            NetworkServer.SendMessage(sendType, processor, message, clientID);
        }

        /// <summary>
        /// 向所有客户端发送消息
        /// </summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <param name="message">消息实例</param>
        /// <param name="sendType">发送类型（可靠或不可靠）</param>
        /// <param name="processor">消息处理器</param>
        public static void SendToAll<T>(this T message, SendType sendType = SendType.Unreliable, MessageProcessor processor = default) where T : unmanaged, INetMessage
        {
            NetworkServer.SendMessageToAll(sendType, processor, message);
        }

        /// <summary>
        /// 向服务器发送可靠消息
        /// </summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <param name="message">消息实例</param>
        /// <param name="processor">消息处理器</param>
        public static void SendToServerReliable<T>(this T message, MessageProcessor processor = default) where T : unmanaged, INetMessage
        {
            NetworkClient.SendMessage(SendType.Reliable, processor, message);
        }

        /// <summary>
        /// 向特定客户端发送可靠消息
        /// </summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <param name="message">消息实例</param>
        /// <param name="clientID">客户端ID</param>
        /// <param name="processor">消息处理器</param>
        public static void SendToClientReliable<T>(this T message, ushort clientID = 0, MessageProcessor processor = default) where T : unmanaged, INetMessage
        {
            NetworkServer.SendMessage(SendType.Reliable, processor, message, clientID);
        }

        /// <summary>
        /// 向所有客户端发送可靠消息
        /// </summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <param name="message">消息实例</param>
        /// <param name="processor">消息处理器</param>
        public static void SendToAllReliable<T>(this T message, MessageProcessor processor = default) where T : unmanaged, INetMessage
        {
            NetworkServer.SendMessageToAll(SendType.Reliable, processor, message);
        }

        /// <summary>
        /// 向服务器发送不可靠消息
        /// </summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <param name="message">消息实例</param>
        /// <param name="processor">消息处理器</param>
        public static void SendToServerUnreliable<T>(this T message, MessageProcessor processor = default) where T : unmanaged, INetMessage
        {
            NetworkClient.SendMessage(SendType.Unreliable, processor, message);
        }

        /// <summary>
        /// 向特定客户端发送不可靠消息
        /// </summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <param name="message">消息实例</param>
        /// <param name="clientID">客户端ID</param>
        /// <param name="processor">消息处理器</param>
        public static void SendToClientUnreliable<T>(this T message, ushort clientID = 0, MessageProcessor processor = default) where T : unmanaged, INetMessage
        {
            NetworkServer.SendMessage(SendType.Unreliable, processor, message, clientID);
        }

        /// <summary>
        /// 向所有客户端发送不可靠消息
        /// </summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <param name="message">消息实例</param>
        /// <param name="processor">消息处理器</param>
        public static void SendToAllUnreliable<T>(this T message, MessageProcessor processor = default) where T : unmanaged, INetMessage
        {
            NetworkServer.SendMessageToAll(SendType.Unreliable, processor, message);
        }
    }
}
