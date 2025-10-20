// File: _Scripts/PieceComponent.cs

using UnityEngine; // <--- ��������һ��

public class PieceComponent : MonoBehaviour
{
    public Vector2Int BoardPosition { get; set; }
    public Piece PieceData { get; set; }

    // ================== �������뿪ʼ ==================
    /// <summary>
    /// ����ʵʱģʽ��ʹ�õ����Ӷ�̬״̬��
    /// �ڻغ���ģʽ�£����ֵ���� null��
    /// </summary>
    public RealTimePieceState RTState { get; set; }
    // ================== ����������� ==================
}