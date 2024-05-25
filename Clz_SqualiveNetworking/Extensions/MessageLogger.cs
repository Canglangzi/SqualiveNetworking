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
/*// 在发送消息的地方
logger.LogSentMessage<LoginMessage>();

// 在接收消息的地方
logger.LogReceivedMessage<LoginMessage>();

// 在丢失消息的地方
logger.LogLostMessage<UpdateMessage>();*/

/*// 打印消息统计信息
logger.PrintStats();*/