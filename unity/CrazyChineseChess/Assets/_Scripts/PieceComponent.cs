using UnityEngine;

/// <summary>
/// ����������Prefab�ϵġ����֤�������
/// �����������ӵ��Ӿ�����(GameObject)���߼�����(PieceData, BoardPosition)��
/// </summary>
public class PieceComponent : MonoBehaviour
{
    // �����������߼�����ϵ�е�λ��
    public Vector2Int BoardPosition { get; set; }

    // ���ӵĴ����ݶ��壨���͡���ɫ��
    public Piece PieceData { get; set; }

    /// <summary>
    /// ����ʵʱģʽ��ʹ�õ����Ӷ�̬״̬��
    /// �ڻغ���ģʽ�£����ֵ������Ϊ null���Ӷ�ʵ��ģʽ���롣
    /// </summary>
    public RealTimePieceState RTState { get; set; }
}