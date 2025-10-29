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

    /// <summary>
    /// [Observers Rpc] �ɷ��������ã��������пͻ��˲��Ŵ����ӵ��ƶ�������
    /// </summary>
    [ObserversRpc]
    public void Observer_PlayMoveAnimation(Vector2Int from, Vector2Int to)
    {
        Debug.Log($"[{(IsServer ? "Server" : "Client")}] �յ��ƶ�ָ����� {this.name} ���� {from} �ƶ��� {to}");

        // ÿ���ͻ��˶������Լ���BoardRenderer�������Ӿ�����
        // ע�⣺������߼��ص���onProgressUpdate, onComplete��ֻ�ڷ������������壬
        // ��Ϊֻ�з���������Ҫ���ݶ������ȸ���Ȩ������Ϸ״̬��
        if (BoardRenderer.Instance != null)
        {
            BoardRenderer.Instance.MovePiece(
                from, to,
                onProgressUpdate: (pc, progress) => {
                    // ����ص�ֻ�ڷ�������ִ��
                    if (!IsServer) return;
                    if (pc != null && pc.RTState != null) pc.RTState.MoveProgress = progress;
                },
                onComplete: (pc) => {
                    // ����ص�Ҳֻ�ڷ�������ִ��
                    if (!IsServer) return;

                    if (pc != null && pc.RTState != null && !pc.RTState.IsDead)
                    {
                        // �������ڶ�����ɺ�������յ��߼�״̬
                        GameManager.Instance.CurrentBoardState.SetPieceAt(pc.RTState.MoveEndPos, pc.PieceData);
                        pc.BoardPosition = pc.RTState.MoveEndPos;
                        pc.RTState.ResetToDefault(pc.RTState.MoveEndPos);

                        // �ӷ�������movingPieces�б����Ƴ�
                        var rtController = GameManager.Instance.GetCurrentGameMode() as RealTimeModeController;
                        rtController?.Server_OnMoveAnimationComplete(pc);

                        Debug.Log($"[Server-State] {pc.name} �ƶ���ɣ�������״̬�������� {pc.RTState.MoveEndPos}��");
                    }
                    else if (pc != null)
                    {
                        // �ӷ�������movingPieces�б����Ƴ�
                        var rtController = GameManager.Instance.GetCurrentGameMode() as RealTimeModeController;
                        rtController?.Server_OnMoveAnimationComplete(pc);
                        Debug.Log($"[Server-State] ������������ {pc.name} ������������ִ�������߼���");
                    }
                }
            );
        }
    }

}