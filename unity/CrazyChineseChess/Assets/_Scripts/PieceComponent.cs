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
        // OnStartClient ������ BoardRenderer.Awake ֮ǰ������
        // �������ǲ���������ֱ�ӵ��� SetupVisuals

        // �����������ã���� BoardRenderer �Ѿ���������ֱ�����
        TrySetupVisuals();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        // ��������ͨ��������������⣬��Ϊ�˴���ͳһ�ͽ�׳�ԣ�Ҳʹ��ͬ���߼�
        TrySetupVisuals();
    }

    private void OnEnable()
    {
        // �����¼����Է������� BoardRenderer ����ǰ�ͱ�����
        BoardRenderer.OnInstanceReady += TrySetupVisuals;
    }

    private void OnDisable()
    {
        // ����ϰ�ߣ��ڶ��󱻽��û�����ʱȡ�����ģ���ֹ�ڴ�й©
        BoardRenderer.OnInstanceReady -= TrySetupVisuals;
    }

    /// <summary>
    /// ����ִ���Ӿ����á�ֻ���� BoardRenderer ��������δ��ʼ��ʱ�Ż�ִ�С�
    /// </summary>
    private void TrySetupVisuals()
    {
        // ����Ѿ���ʼ���������� BoardRenderer ��δ׼���ã���ֱ�ӷ���
        if (_visualsInitialized || BoardRenderer.Instance == null)
        {
            return;
        }

        // --- ������ԭ SetupVisuals ���߼� ---
        if (!IsServer)
        {
            this.BoardPosition = BoardRenderer.Instance.GetBoardPosition(this.transform.localPosition);
        }

        BoardRenderer.Instance.SetupPieceVisuals(this);

        // ֻҪ�Ӿ�������ɣ�����ζ�������Ѿ������ڡ�����Ϸ�����У�
        // ��ʱ���ǿ��԰�ȫ�س�ʼ������ʵʱ״̬��
        // �������ڵ������������Ϳͻ��ˡ�
        if (this.RTState == null)
        {
            this.RTState = new RealTimePieceState();
            // ʹ�� BoardPosition ��Ϊ��ʼ�� LogicalPosition
            this.RTState.LogicalPosition = this.BoardPosition;
        }

        // ���Ϊ�ѳ�ʼ������ȡ�����ģ���Ϊ����ֻ��Ҫִ��һ��
        _visualsInitialized = true;
        BoardRenderer.OnInstanceReady -= TrySetupVisuals;
    }

    public override void OnStartNetwork()
    {
        // ������ FishNet �汾���£���ʱ OnStartNetwork ��� OnEnable �磬
        // ���ǿ���������Ҳ����һ�γ��ԣ�ȷ������һʧ��
        base.OnStartNetwork();
        TrySetupVisuals();
    }

    /// <summary>
    /// [Observers Rpc] �ɷ��������ã��������пͻ��˲��Ŵ����ӵ��ƶ�������
    /// </summary>
    [ObserversRpc]
    public void Observer_PlayMoveAnimation(Vector2Int from, Vector2Int to)
    {
        Debug.Log($"[{(IsServer ? "Server" : "Client")}] �յ��ƶ�ָ����� {this.name} ���� {from} �ƶ��� {to}");
        if (BoardRenderer.Instance == null) return;

        // 1. ���嶯�����ȸ��µĻص� (����������Ҫ)
        System.Action<PieceComponent, float> onProgressUpdate = null;
        if (IsServer)
        {
            onProgressUpdate = (pc, progress) =>
            {
                if (pc != null && pc.RTState != null) pc.RTState.MoveProgress = progress;
            };
        }

        // 2. ���嶯����ɺ�Ļص� (�������Ϳͻ��˶���Ҫ)
        System.Action<PieceComponent> onComplete = (pc) =>
        {
            if (pc == null || pc.RTState == null) return;

            // ����������ƶ���������������ִ���κ������߼�
            if (pc.RTState.IsDead)
            {
                if (IsServer)
                {
                    // ���ڷ������ϣ���Ҫ���������Ӵӡ��ƶ��С��б����Ƴ�
                    var rtController = GameManager.Instance.GetCurrentGameMode() as RealTimeModeController;
                    rtController?.Server_OnMoveAnimationComplete(pc);
                }
                return;
            }

            // --- �Դ�����ӵĴ��� ---

            // ����˫���ı����߼����� (BoardState)
            // ����Ϊ��ȷ��RuleEngine���κ�һ�˶����õ����µ����̲���
            GameManager.Instance.CurrentBoardState.MovePiece(from, to);

            // �����������������߼�λ������
            pc.BoardPosition = to;
            pc.RTState.ResetToDefault(to);

            // ��������Ҫִ�ж����״̬ͬ��������
            if (IsServer)
            {
                Debug.Log($"[Server-State] {pc.name} �ƶ���ɣ��������߼��Ѹ����� {to}��");
                var rtController = GameManager.Instance.GetCurrentGameMode() as RealTimeModeController;
                rtController?.Server_OnMoveAnimationComplete(pc);
            }
            else // �ͻ��˽����ӡ��־ȷ��
            {
                Debug.Log($"[Client-State] {pc.name} �ƶ���ɣ��ͻ����߼�λ���Ѹ����� {to}��");
            }
        };

        // 3. �����Ӿ�����ƶ�����
        BoardRenderer.Instance.MovePiece(from, to, onProgressUpdate, onComplete);
    }

}