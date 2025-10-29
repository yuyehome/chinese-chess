// File: _Scripts/PieceComponent.cs

using UnityEngine;

/// <summary>
/// ����������Prefab�ϵġ����֤�������
/// ����Ϊ���������������ӵ��Ӿ�����(GameObject)���䱳��Ķ����߼����ݡ�
/// </summary>
public class PieceComponent : MonoBehaviour
{
    /// <summary>
    /// �����������߼�����ϵ�е�λ�á�
    /// ���ھ�ֹ���ӣ���������BoardState�е�λ�ã������ƶ������ӣ���������Ŀ��λ�á�
    /// </summary>
    public Vector2Int BoardPosition { get; set; }

    /// <summary>
    /// ���ӵĴ����ݶ��壨���͡���ɫ����
    /// </summary>
    public Piece PieceData { get; set; }

    /// <summary>
    /// ����ʵʱģʽ��ʹ�õ����Ӷ�̬״̬����
    /// �ڻغ���ģʽ�£����ֵ������Ϊ null���Ӷ�ʵ��ģʽ���롣
    /// </summary>
    public RealTimePieceState RTState { get; set; }
}