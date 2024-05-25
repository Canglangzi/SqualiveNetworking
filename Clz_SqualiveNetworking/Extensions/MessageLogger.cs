using System;
using System.Collections.Generic;

public class MessageStats
{
    public int SentCount { get; set; }
    public int ReceivedCount { get; set; }
    public int LostCount { get; set; }
}

public class MessageLogger
{
    private Dictionary<Type, MessageStats> statsDict;

    public MessageLogger()
    {
        statsDict = new Dictionary<Type, MessageStats>();
    }

    public void LogSentMessage<T>()
    {
        Type messageType = typeof(T);
        if (!statsDict.ContainsKey(messageType))
        {
            statsDict[messageType] = new MessageStats();
        }
        statsDict[messageType].SentCount++;
    }

    public void LogReceivedMessage<T>()
    {
        Type messageType = typeof(T);
        if (!statsDict.ContainsKey(messageType))
        {
            statsDict[messageType] = new MessageStats();
        }
        statsDict[messageType].ReceivedCount++;
    }

    public void LogLostMessage<T>()
    {
        Type messageType = typeof(T);
        if (!statsDict.ContainsKey(messageType))
        {
            statsDict[messageType] = new MessageStats();
        }
        statsDict[messageType].LostCount++;
    }

    public void PrintStats()
    {
        Console.WriteLine("Message Statistics:");
        foreach (var kvp in statsDict)
        {
            Console.WriteLine($"{kvp.Key.Name}: Sent={kvp.Value.SentCount}, Received={kvp.Value.ReceivedCount}, Lost={kvp.Value.LostCount}");
        }
    }
}

class Program
{
    static void Main(string[] args)
    {
        MessageLogger logger = new MessageLogger();

        // Simulate sending and receiving messages
        logger.LogSentMessage<LoginMessage>();
        logger.LogReceivedMessage<LoginMessage>();
        logger.LogLostMessage<UpdateMessage>();

        // Print message statistics
        logger.PrintStats();
    }
}

public class LoginMessage { }
public class UpdateMessage { }
/*// �ڷ�����Ϣ�ĵط�
logger.LogSentMessage<LoginMessage>();

// �ڽ�����Ϣ�ĵط�
logger.LogReceivedMessage<LoginMessage>();

// �ڶ�ʧ��Ϣ�ĵط�
logger.LogLostMessage<UpdateMessage>();*/

/*// ��ӡ��Ϣͳ����Ϣ
logger.PrintStats();*/