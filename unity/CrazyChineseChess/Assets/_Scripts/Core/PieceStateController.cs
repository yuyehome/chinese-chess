// File: _Scripts/Core/PieceStateController.cs

using UnityEngine;

public class PieceStateController : MonoBehaviour
{
    private MeshRenderer meshRenderer; // 【新增】缓存渲染器引用
    private MaterialPropertyBlock propBlock; // 【新增】用于高效修改材质属性
    // --- 核心状态属性 ---
    public bool IsDead { get; private set; } = false;
    public bool IsMoving { get; private set; } = false;

    // 注意：这里的"实体/虚无"和"可被攻击"是两个独立的状态
    // IsEthereal 更偏向于“穿透性/无敌”，而 IsVulnerable 决定了是否会“掉血”
    public bool IsEthereal { get; private set; } = false;    // 虚无状态 (未来用于隐身等)
    public bool IsVulnerable { get; private set; } = true;  // 可被攻击状态
    public bool IsAttacking { get; private set; } = false;   // 正在攻击状态

    // --- 引用 ---
    [HideInInspector] public PieceComponent pieceComponent; // 对自身PieceComponent的引用

    // --- 移动相关 ---
    private IPieceMovementStrategy movementStrategy; // 当前棋子的移动策略

    private void Awake()
    {
        pieceComponent = GetComponent<PieceComponent>();
        if (pieceComponent == null)
        {
            Debug.LogError("PieceStateController 必须挂载在有 PieceComponent 的对象上!");
        }

        // 【新增】获取组件并初始化
        meshRenderer = GetComponent<MeshRenderer>();
        propBlock = new MaterialPropertyBlock();
        meshRenderer.GetPropertyBlock(propBlock); // 获取初始属性
    }

    /// <summary>
    /// 【新增】高亮这个棋子。
    /// </summary>
    public void Highlight(Color color)
    {
        if (meshRenderer == null) return;
        propBlock.SetColor("_EmissionColor", color * 2.0f); // 设置自发光颜色
        meshRenderer.SetPropertyBlock(propBlock);
    }

    /// <summary>
    /// 【新增】清除高亮。
    /// </summary>
    public void ClearHighlight()
    {
        if (meshRenderer == null) return;
        propBlock.SetColor("_EmissionColor", Color.black); // 恢复默认自发光（黑色）
        meshRenderer.SetPropertyBlock(propBlock);
    }

    /// <summary>
    /// 初始化棋子的状态和移动策略。
    /// </summary>
    public void Initialize(Piece pieceData)
    {
        // 根据棋子类型，分配不同的移动策略
        movementStrategy = PieceStrategyFactory.GetStrategy(pieceData.Type);

        // 设置初始状态
        SetStationaryState();
    }

    /// <summary>
    /// 当棋子开始移动时调用。
    /// </summary>
    public void OnMoveStart()
    {
        IsMoving = true;
        // 委托给策略来更新初始移动状态
        movementStrategy?.UpdateStateOnMoveStart(this);
    }

    /// <summary>
    /// 在移动过程中每帧调用。
    /// </summary>
    /// <param name="moveProgress">移动进度 (0.0 to 1.0)</param>
    public void OnMoveUpdate(float moveProgress)
    {
        // 委托给策略来更新过程中的状态
        movementStrategy?.UpdateStateOnMoveUpdate(this, moveProgress);
    }

    /// <summary>
    /// 当棋子移动结束时调用。
    /// </summary>
    public void OnMoveEnd()
    {
        IsMoving = false;
        SetStationaryState();
    }

    /// <summary>
    /// 核心方法：设置棋子的具体状态
    /// </summary>
    public void SetStates(bool isVulnerable, bool isAttacking, bool isEthereal = false)
    {
        this.IsVulnerable = isVulnerable;
        this.IsAttacking = isAttacking;
        this.IsEthereal = isEthereal; // 默认为false，即实体
    }

    /// <summary>
    /// 受到攻击时的处理逻辑。
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
        if (IsDead) return; // 防止重复调用Die()

        IsDead = true;
        Debug.Log($"{pieceComponent.PieceData.Color} {pieceComponent.PieceData.Type} at {pieceComponent.BoardPosition} has died.");

        // 【核心修改】不再由自己销毁，而是通知 GameManager
        GameManager.Instance.ReportPieceDeath(pieceComponent.BoardPosition);

        // 可以在这里播放死亡特效，特效播放完毕后再通知销毁
        // 例如： ParticleSystem deathEffect = Instantiate(...);
        // Destroy(gameObject, deathEffect.main.duration);

        // 【修改】为了立即看到效果，我们暂时先禁用碰撞和渲染，再交由BoardRenderer销毁
        GetComponent<Collider>().enabled = false;
        GetComponent<MeshRenderer>().enabled = false;

        // 找到BoardRenderer并请求销毁
        FindObjectOfType<BoardRenderer>().RequestDestroyPiece(gameObject);
    }

    /// <summary>
    /// 设置为静止状态的默认值。
    /// </summary>
    private void SetStationaryState()
    {
        IsVulnerable = true;
        IsAttacking = false;
        IsEthereal = false;
    }

    // --- 碰撞处理 ---
    private void OnCollisionEnter(Collision collision)
    {
        // 仅在实时模式下处理碰撞
        if (GameModeSelector.SelectedMode != GameModeType.RealTime) return;

        // 检查碰撞对方是否也是棋子
        var otherPieceState = collision.gameObject.GetComponent<PieceStateController>();
        if (otherPieceState == null) return;

        // 检查是否是敌方棋子
        if (otherPieceState.pieceComponent.PieceData.Color == this.pieceComponent.PieceData.Color) return;

        // --- 核心吃子/双亡逻辑 ---
        if (this.IsAttacking && otherPieceState.IsVulnerable)
        {
            Debug.Log($"[{this.name}] attacks and kills [{otherPieceState.name}]");
            otherPieceState.TakeDamage();
        }
    }
}