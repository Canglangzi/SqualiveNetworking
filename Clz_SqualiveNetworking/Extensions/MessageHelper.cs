using SqualiveNetworking;
using SqualiveNetworking.Message.Processor;
using SqualiveNetworking.Utils;

namespace CLz_SqualiveNetworkingHelper
{
    public static class MessageHelper
    {
        /// <summary>
        /// �������������Ϣ
        /// </summary>
        /// <typeparam name="T">��Ϣ����</typeparam>
        /// <param name="message">��Ϣʵ��</param>
        /// <param name="sendType">�������ͣ��ɿ��򲻿ɿ���</param>
        /// <param name="processor">��Ϣ������</param>
        public static void SendToServer<T>(this T message, SendType sendType = SendType.Unreliable, MessageProcessor processor = default) where T : unmanaged, INetMessage
        {
            NetworkClient.SendMessage(sendType, processor, message);
        }

        /// <summary>
        /// ���ض��ͻ��˷�����Ϣ
        /// </summary>
        /// <typeparam name="T">��Ϣ����</typeparam>
        /// <param name="message">��Ϣʵ��</param>
        /// <param name="clientID">�ͻ���ID</param>
        /// <param name="sendType">�������ͣ��ɿ��򲻿ɿ���</param>
        /// <param name="processor">��Ϣ������</param>
        public static void SendToClient<T>(this T message, ushort clientID = 0, SendType sendType = SendType.Unreliable, MessageProcessor processor = default) where T : unmanaged, INetMessage
        {
            NetworkServer.SendMessage(sendType, processor, message, clientID);
        }

        /// <summary>
        /// �����пͻ��˷�����Ϣ
        /// </summary>
        /// <typeparam name="T">��Ϣ����</typeparam>
        /// <param name="message">��Ϣʵ��</param>
        /// <param name="sendType">�������ͣ��ɿ��򲻿ɿ���</param>
        /// <param name="processor">��Ϣ������</param>
        public static void SendToAll<T>(this T message, SendType sendType = SendType.Unreliable, MessageProcessor processor = default) where T : unmanaged, INetMessage
        {
            NetworkServer.SendMessageToAll(sendType, processor, message);
        }

        /// <summary>
        /// ����������Ϳɿ���Ϣ
        /// </summary>
        /// <typeparam name="T">��Ϣ����</typeparam>
        /// <param name="message">��Ϣʵ��</param>
        /// <param name="processor">��Ϣ������</param>
        public static void SendToServerReliable<T>(this T message, MessageProcessor processor = default) where T : unmanaged, INetMessage
        {
            NetworkClient.SendMessage(SendType.Reliable, processor, message);
        }

        /// <summary>
        /// ���ض��ͻ��˷��Ϳɿ���Ϣ
        /// </summary>
        /// <typeparam name="T">��Ϣ����</typeparam>
        /// <param name="message">��Ϣʵ��</param>
        /// <param name="clientID">�ͻ���ID</param>
        /// <param name="processor">��Ϣ������</param>
        public static void SendToClientReliable<T>(this T message, ushort clientID = 0, MessageProcessor processor = default) where T : unmanaged, INetMessage
        {
            NetworkServer.SendMessage(SendType.Reliable, processor, message, clientID);
        }

        /// <summary>
        /// �����пͻ��˷��Ϳɿ���Ϣ
        /// </summary>
        /// <typeparam name="T">��Ϣ����</typeparam>
        /// <param name="message">��Ϣʵ��</param>
        /// <param name="processor">��Ϣ������</param>
        public static void SendToAllReliable<T>(this T message, MessageProcessor processor = default) where T : unmanaged, INetMessage
        {
            NetworkServer.SendMessageToAll(SendType.Reliable, processor, message);
        }

        /// <summary>
        /// ����������Ͳ��ɿ���Ϣ
        /// </summary>
        /// <typeparam name="T">��Ϣ����</typeparam>
        /// <param name="message">��Ϣʵ��</param>
        /// <param name="processor">��Ϣ������</param>
        public static void SendToServerUnreliable<T>(this T message, MessageProcessor processor = default) where T : unmanaged, INetMessage
        {
            NetworkClient.SendMessage(SendType.Unreliable, processor, message);
        }

        /// <summary>
        /// ���ض��ͻ��˷��Ͳ��ɿ���Ϣ
        /// </summary>
        /// <typeparam name="T">��Ϣ����</typeparam>
        /// <param name="message">��Ϣʵ��</param>
        /// <param name="clientID">�ͻ���ID</param>
        /// <param name="processor">��Ϣ������</param>
        public static void SendToClientUnreliable<T>(this T message, ushort clientID = 0, MessageProcessor processor = default) where T : unmanaged, INetMessage
        {
            NetworkServer.SendMessage(SendType.Unreliable, processor, message, clientID);
        }

        /// <summary>
        /// �����пͻ��˷��Ͳ��ɿ���Ϣ
        /// </summary>
        /// <typeparam name="T">��Ϣ����</typeparam>
        /// <param name="message">��Ϣʵ��</param>
        /// <param name="processor">��Ϣ������</param>
        public static void SendToAllUnreliable<T>(this T message, MessageProcessor processor = default) where T : unmanaged, INetMessage
        {
            NetworkServer.SendMessageToAll(SendType.Unreliable, processor, message);
        }
    }
}
