// File: _Scripts/Core/PieceStateController.cs

using UnityEngine;

public class PieceStateController : MonoBehaviour
{
    private MeshRenderer meshRenderer; // ��������������Ⱦ������
    private MaterialPropertyBlock propBlock; // �����������ڸ�Ч�޸Ĳ�������
    // --- ����״̬���� ---
    public bool IsDead { get; private set; } = false;
    public bool IsMoving { get; private set; } = false;

    // ע�⣺�����"ʵ��/����"��"�ɱ�����"������������״̬
    // IsEthereal ��ƫ���ڡ���͸��/�޵С����� IsVulnerable �������Ƿ�ᡰ��Ѫ��
    public bool IsEthereal { get; private set; } = false;    // ����״̬ (δ�����������)
    public bool IsVulnerable { get; private set; } = true;  // �ɱ�����״̬
    public bool IsAttacking { get; private set; } = false;   // ���ڹ���״̬

    // --- ���� ---
    [HideInInspector] public PieceComponent pieceComponent; // ������PieceComponent������

    // --- �ƶ���� ---
    private IPieceMovementStrategy movementStrategy; // ��ǰ���ӵ��ƶ�����

    private void Awake()
    {
        pieceComponent = GetComponent<PieceComponent>();
        if (pieceComponent == null)
        {
            Debug.LogError("PieceStateController ����������� PieceComponent �Ķ�����!");
        }

        // ����������ȡ�������ʼ��
        meshRenderer = GetComponent<MeshRenderer>();
        propBlock = new MaterialPropertyBlock();
        meshRenderer.GetPropertyBlock(propBlock); // ��ȡ��ʼ����
    }

    /// <summary>
    /// ������������������ӡ�
    /// </summary>
    public void Highlight(Color color)
    {
        if (meshRenderer == null) return;
        propBlock.SetColor("_EmissionColor", color * 2.0f); // �����Է�����ɫ
        meshRenderer.SetPropertyBlock(propBlock);
    }

    /// <summary>
    /// �����������������
    /// </summary>
    public void ClearHighlight()
    {
        if (meshRenderer == null) return;
        propBlock.SetColor("_EmissionColor", Color.black); // �ָ�Ĭ���Է��⣨��ɫ��
        meshRenderer.SetPropertyBlock(propBlock);
    }

    /// <summary>
    /// ��ʼ�����ӵ�״̬���ƶ����ԡ�
    /// </summary>
    public void Initialize(Piece pieceData)
    {
        // �����������ͣ����䲻ͬ���ƶ�����
        movementStrategy = PieceStrategyFactory.GetStrategy(pieceData.Type);

        // ���ó�ʼ״̬
        SetStationaryState();
    }

    /// <summary>
    /// �����ӿ�ʼ�ƶ�ʱ���á�
    /// </summary>
    public void OnMoveStart()
    {
        IsMoving = true;
        // ί�и����������³�ʼ�ƶ�״̬
        movementStrategy?.UpdateStateOnMoveStart(this);
    }

    /// <summary>
    /// ���ƶ�������ÿ֡���á�
    /// </summary>
    /// <param name="moveProgress">�ƶ����� (0.0 to 1.0)</param>
    public void OnMoveUpdate(float moveProgress)
    {
        // ί�и����������¹����е�״̬
        movementStrategy?.UpdateStateOnMoveUpdate(this, moveProgress);
    }

    /// <summary>
    /// �������ƶ�����ʱ���á�
    /// </summary>
    public void OnMoveEnd()
    {
        IsMoving = false;
        SetStationaryState();
    }

    /// <summary>
    /// ���ķ������������ӵľ���״̬
    /// </summary>
    public void SetStates(bool isVulnerable, bool isAttacking, bool isEthereal = false)
    {
        this.IsVulnerable = isVulnerable;
        this.IsAttacking = isAttacking;
        this.IsEthereal = isEthereal; // Ĭ��Ϊfalse����ʵ��
    }

    /// <summary>
    /// �ܵ�����ʱ�Ĵ����߼���
    /// </summary>
    public void TakeDamage()
    {
        if (IsVulnerable)
        {
            Die();
        }
    }

    private void Die()
    {
        if (IsDead) return; // ��ֹ�ظ�����Die()

        IsDead = true;
        Debug.Log($"{pieceComponent.PieceData.Color} {pieceComponent.PieceData.Type} at {pieceComponent.BoardPosition} has died.");

        // �������޸ġ��������Լ����٣�����֪ͨ GameManager
        GameManager.Instance.ReportPieceDeath(pieceComponent.BoardPosition);

        // ���������ﲥ��������Ч����Ч������Ϻ���֪ͨ����
        // ���磺 ParticleSystem deathEffect = Instantiate(...);
        // Destroy(gameObject, deathEffect.main.duration);

        // ���޸ġ�Ϊ����������Ч����������ʱ�Ƚ�����ײ����Ⱦ���ٽ���BoardRenderer����
        GetComponent<Collider>().enabled = false;
        GetComponent<MeshRenderer>().enabled = false;

        // �ҵ�BoardRenderer����������
        FindObjectOfType<BoardRenderer>().RequestDestroyPiece(gameObject);
    }

    /// <summary>
    /// ����Ϊ��ֹ״̬��Ĭ��ֵ��
    /// </summary>
    private void SetStationaryState()
    {
        IsVulnerable = true;
        IsAttacking = false;
        IsEthereal = false;
    }

    // --- ��ײ���� ---
    private void OnCollisionEnter(Collision collision)
    {
        // ����ʵʱģʽ�´�����ײ
        if (GameModeSelector.SelectedMode != GameModeType.RealTime) return;

        // �����ײ�Է��Ƿ�Ҳ������
        var otherPieceState = collision.gameObject.GetComponent<PieceStateController>();
        if (otherPieceState == null) return;

        // ����Ƿ��ǵз�����
        if (otherPieceState.pieceComponent.PieceData.Color == this.pieceComponent.PieceData.Color) return;

        // --- ���ĳ���/˫���߼� ---
        if (this.IsAttacking && otherPieceState.IsVulnerable)
        {
            Debug.Log($"[{this.name}] attacks and kills [{otherPieceState.name}]");
            otherPieceState.TakeDamage();
        }
    }
}