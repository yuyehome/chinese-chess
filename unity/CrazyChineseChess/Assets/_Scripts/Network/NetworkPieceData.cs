// File: _Scripts/Network/NetworkPieceData.cs

using UnityEngine;

/// <summary>
/// һ���������Ľṹ�壬������������Ч�ش��䵥�����ӵĺ���״̬��
/// ���������κ�Unity������Ӷ�����˿��Ա�FishNet��Ч���л���
/// </summary>
public struct NetworkPieceData
{
    // ����Ψһ��ʶ (�ڱ�����Ϸ��)
    public readonly byte PieceId;
    // �������
    public readonly PieceType Type;
    public readonly PlayerColor Color;
    // �����������ϵ��߼�λ��
    public readonly Vector2Int Position;

    public NetworkPieceData(byte pieceId, PieceType type, PlayerColor color, Vector2Int position)
    {
        PieceId = pieceId;
        Type = type;
        Color = color;
        Position = position;
    }
}