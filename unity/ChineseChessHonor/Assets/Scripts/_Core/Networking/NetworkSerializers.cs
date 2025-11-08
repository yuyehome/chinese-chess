// 文件路径: Assets/Scripts/_Core/Networking/NetworkSerializers.cs

using Mirror;

public static class NetworkSerializers
{
    // --- PieceData ---
    public static void WritePieceData(this NetworkWriter writer, PieceData data)
    {
        writer.WriteInt(data.uniqueId);
        writer.WriteInt((int)data.team);
        writer.WriteInt((int)data.type);
        writer.WriteVector2Int(data.position);
        writer.WriteInt((int)data.status);
        writer.WriteInt(data.heroId);
    }

    public static PieceData ReadPieceData(this NetworkReader reader)
    {
        return new PieceData
        {
            uniqueId = reader.ReadInt(),
            team = (PlayerTeam)reader.ReadInt(),
            type = (PieceType)reader.ReadInt(),
            position = reader.ReadVector2Int(),
            status = (PieceStatus)reader.ReadInt(),
            heroId = reader.ReadInt()
        };
    }

    // 你可以在这里为其他自定义数据类型（如PlayerProfile）添加更多的序列化器
}