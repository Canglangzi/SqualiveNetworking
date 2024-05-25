using Unity.Collections;
using Unity.Mathematics;
using Unity.Networking.Transport;

/*// 使用扩展方法写入 Vector3
float3 position = new float3(1.0f, 2.0f, 3.0f);
writer.WriteVector3(position);

// 使用扩展方法读取 Vector3
float3 newPosition = reader.ReadVector3();*/
public static class DataStreamWriterExtensions
{
    public static void WriteVector3(this DataStreamWriter writer, float3 vector)
    {
        writer.WriteFloat(vector.x);
        writer.WriteFloat(vector.y);
        writer.WriteFloat(vector.z);
    }
}

public static class DataStreamReaderExtensions
{
    public static float3 ReadVector3(this DataStreamReader reader)
    {
        float x = reader.ReadFloat();
        float y = reader.ReadFloat();
        float z = reader.ReadFloat();
        return new float3(x, y, z);
    }
}
