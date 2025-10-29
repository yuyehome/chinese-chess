// File: _Scripts/PieceComponent.cs

using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

/// <summary>
/// ����������Prefab�ϵġ����֤�������
/// ����Ϊ���������������ӵ��Ӿ�����(GameObject)���䱳��Ķ����߼����ݡ�
/// [�������]: �̳���NetworkBehaviour��ʹ���Ϊһ���������
/// </summary>
public class PieceComponent : NetworkBehaviour // �̳� NetworkBehaviour ������ MonoBehaviour
{
    /// <summary>
    /// [SyncVar] ���ӵĴ����ݶ��壨���͡���ɫ����
    /// SyncVarȷ������������ڷ������ϸı�ʱ�����Զ�ͬ�������пͻ��ˡ�
    /// </summary>
    [SyncVar]
    public Piece PieceData;

    /// <summary>
    /// [SyncVar] �����������߼�����ϵ�е�λ�á�
    /// </summary>
    [SyncVar]
    public Vector2Int BoardPosition;

    /// <summary>
    /// ����ʵʱģʽ��ʹ�õ����Ӷ�̬״̬����
    /// ���״̬������ʱ�ģ�����Ҫ����ͬ�����ɸ��ԵĿ��������й���
    /// </summary>
    public RealTimePieceState RTState { get; set; }


    /// <summary>
    /// FishNet�ص�: ������������ڿͻ����ϱ����ɲ�����ʱ���á�
    /// ���ǿͻ������ӡ��������ĵط���
    /// </summary>
    public override void OnStartClient()
    {
        base.OnStartClient();

        // OnStartClient ֻ��������ģʽ�±����ã���˲���Ӱ�쵥��ģʽ
        // �ڿͻ��ˣ�������Ҫ������ BoardRenderer ע�ᣬ�Ա��ܱ�����͹���
        BoardRenderer renderer = FindObjectOfType<BoardRenderer>();
        if (renderer != null)
        {
            // ʹ�ôӷ�����ͬ�������� BoardPosition ����ע��
            renderer.RegisterNetworkedPiece(this.gameObject, this.BoardPosition);
            Debug.Log($"[Client] ���� {name} ��λ�� {BoardPosition} �ɹ���BoardRendererע�ᡣ");
        }
        else
        {
            Debug.LogError($"[PieceComponent] �ͻ���δ���ҵ�BoardRendererʵ����ע������ {name}!");
        }
    }
}