// File: _Scripts/PieceComponent.cs
using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

/// <summary>
/// ����������Prefab�ϵ����绯�����
/// �������������°�FishNet��������ȷʵ�֡�
/// </summary>
public class PieceComponent : NetworkBehaviour
{
    // �����ĸĶ����� readonly �ؼ��ּӻ�����
    // �������� FishNet ILPP ��Ҫ��
    public readonly SyncVar<PieceType> Type = new SyncVar<PieceType>();
    public readonly SyncVar<PlayerColor> Color = new SyncVar<PlayerColor>();

    public Piece PieceData => new Piece(Type.Value, Color.Value);

    public Vector2Int BoardPosition { get; set; }
    public RealTimePieceState RTState { get; set; }

    private bool _visualsInitialized = false;

    /// <summary>
    /// [Server-Side Logic]
    /// ��ʼ�� [SyncVar] ��ֵ��
    /// ���ǲ������� .Value���������ֶα����������� readonly ���򲻳�ͻ��
    /// </summary>
    public void Initialize(Piece piece, Vector2Int position)
    {
        Type.Value = piece.Type;
        Color.Value = piece.Color;
        BoardPosition = position;
        gameObject.name = $"{piece.Color}_{piece.Type}_{position.x}_{position.y}";
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (_visualsInitialized) return;
        SetupVisuals();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        SetupVisuals();
    }

    private void SetupVisuals()
    {
        if (_visualsInitialized) return;

        if (!IsServer)
        {
            this.BoardPosition = BoardRenderer.Instance.GetBoardPosition(this.transform.localPosition);
        }

        if (BoardRenderer.Instance != null)
        {
            Debug.Log($"[{(IsServer ? "Server" : "Client")}] ���� {gameObject.name} (Type: {Type.Value}, Color: {Color.Value}) �����ɣ����������Ӿ�Ч��������: {BoardPosition}");
            BoardRenderer.Instance.SetupPieceVisuals(this);
            _visualsInitialized = true;
        }
        else
        {
            Debug.LogError($"[{(IsServer ? "Server" : "Client")}] ���� {gameObject.name} ����ʱ��BoardRenderer.Instance Ϊ�գ��Ӿ�����ʧ�ܡ�");
        }
    }
}