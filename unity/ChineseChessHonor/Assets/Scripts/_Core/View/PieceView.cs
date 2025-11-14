// 文件路径: Assets/Scripts/_Core/View/PieceView.cs

using TMPro;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class PieceView : MonoBehaviour
{
    [Header("数据关联")]
    public int pieceId;
    public PlayerTeam team;
    public PieceType type;

    [Header("配置")]
    [SerializeField] private PieceSkinData skinData; // 引用我们的UV配置

    [Header("组件引用")]
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private MeshFilter meshFilter;

    private Mesh _instancedMesh; // 用于修改UV的网格实例

    // 临时的皮肤类型，后续会从游戏设置或玩家配置中读取
    private SkinType _currentSkin = SkinType.Wood;

    private void OnDestroy()
    {
        // 清理我们创建的网格实例，防止内存泄漏
        if (_instancedMesh != null)
        {
            Destroy(_instancedMesh);
        }
    }

    /// <summary>
    /// 初始化棋子的视觉表现
    /// </summary>
    public void Initialize(PieceData data)
    {
        Debug.Log($"[PieceView] 初始化棋子 ID: {data.uniqueId}, 类型: {data.type}, 阵营: {data.team}");
        this.pieceId = data.uniqueId;
        this.team = data.team;
        this.type = data.type;

        PlayerTeam materialTeam = MapTeamToMaterialTeam(this.team);
        Debug.Log($"[PieceView] 逻辑阵营 {this.team} 映射到材质阵营 {materialTeam}");

        Material materialInstance = SkinManager.Instance.GetPieceMaterial(_currentSkin, materialTeam);
        if (materialInstance == null)
        {
            Debug.LogError($"[PieceView] 无法为棋子 {type} (队伍 {team}) 获取材质! 将显示为白模。");
            return;
        }
        meshRenderer.material = materialInstance;
        Debug.Log($"[PieceView] 成功应用材质 '{materialInstance.name}' 到 {this.gameObject.name}");

        ApplyUvMapping();
    }

    private PlayerTeam MapTeamToMaterialTeam(PlayerTeam logicalTeam)
    {
        // 根据规则：红方和紫色队友用红方贴图集，黑方和蓝色队友用黑方贴图集
        switch (logicalTeam)
        {
            case PlayerTeam.Red:
            case PlayerTeam.Purple:
                return PlayerTeam.Red;

            case PlayerTeam.Black:
            case PlayerTeam.Blue:
                return PlayerTeam.Black;

            default:
                return PlayerTeam.None;
        }
    }

    private void ApplyUvMapping()
    {
        // --- 步骤 1: 验证所有必要的引用 ---
        if (skinData == null)
        {
            Debug.LogError($"[PieceView] ({gameObject.name}) 致命错误: PieceSkinData 配置未在Inspector中指定!", this);
            return;
        }
        if (meshFilter == null)
        {
            Debug.LogError($"[PieceView] ({gameObject.name}) 致命错误: MeshFilter 引用未在Inspector中指定!", this);
            return;
        }
        if (meshRenderer == null)
        {
            Debug.LogError($"[PieceView] ({gameObject.name}) 致命错误: MeshRenderer 引用未在Inspector中指定!", this);
            return;
        }

        // --- 步骤 2: 诊断原始Mesh ---
        Mesh originalMesh = meshFilter.sharedMesh;
        if (originalMesh == null)
        {
            Debug.LogError($"[PieceView] ({gameObject.name}) 致命错误: 指定的 MeshFilter (在对象 '{meshFilter.gameObject.name}' 上) 没有关联任何 Mesh!", meshFilter.gameObject);
            return;
        }

        // --- 步骤 3: 诊断原始UV数据 ---
        Vector2[] originalUVs = originalMesh.uv;
        if (originalUVs == null || originalUVs.Length == 0)
        {
            // ！！！这是决定性的日志 ！！！
            Debug.LogError($"[PieceView] ({gameObject.name}) 致命错误: 从 Mesh '{originalMesh.name}' (源自 '{meshFilter.gameObject.name}') 中读取到的UV数组为空或null! 请检查FBX导入设置或模型本身。", this);

            // 打印更详细的Mesh信息
            Debug.Log($"[PieceView] 诊断信息: Vertex count={originalMesh.vertexCount}, Triangle count={originalMesh.triangles.Length}");

            // 既然已经确认FBX有UV，那问题可能出在Unity内部。尝试强制重新导入。
#if UNITY_EDITOR
            UnityEditor.AssetImporter.GetAtPath(UnityEditor.AssetDatabase.GetAssetPath(originalMesh)).SaveAndReimport();
            Debug.LogWarning($"[PieceView] 已尝试对 Mesh '{originalMesh.name}' 的源文件进行强制重新导入。请重启播放模式查看是否解决。");
#endif

            return;
        }
        Debug.Log($"[PieceView] ({gameObject.name}) 成功从 Mesh '{originalMesh.name}' 读取到 {originalUVs.Length} 个UV坐标。");

        // --- 步骤 4: 获取并应用UV区域 ---
        Rect uvRect = skinData.GetUVRect(this.type);
        Debug.Log($"[PieceView] ({gameObject.name}) 棋子类型 {this.type} 获取到UV区域: X={uvRect.x}, W={uvRect.width}");

        // --- 步骤 5: 创建网格实例 ---
        if (_instancedMesh == null)
        {
            _instancedMesh = new Mesh
            {
                name = $"{originalMesh.name}_inst_{this.GetInstanceID()}",
                vertices = originalMesh.vertices,
                triangles = originalMesh.triangles,
                normals = originalMesh.normals,
                tangents = originalMesh.tangents,
                uv = originalMesh.uv // <-- 关键: 在创建时就直接把原始UV拷贝过来
            };
            meshFilter.mesh = _instancedMesh;
            Debug.Log($"[PieceView] ({gameObject.name}) 首次创建了网格实例。");
        }

        // --- 步骤 6: 计算并应用新的UV坐标 ---
        Vector2[] newUVs = _instancedMesh.uv; // 从我们的实例中获取UV数组的引用
        for (int i = 0; i < newUVs.Length; i++)
        {
            // 直接在实例的UV数组上修改
            newUVs[i].x = uvRect.x + originalUVs[i].x * uvRect.width; // 计算依然基于原始UV
            newUVs[i].y = uvRect.y + originalUVs[i].y * uvRect.height;
        }

        _instancedMesh.uv = newUVs;
        Debug.Log($"[PieceView] ({gameObject.name}) 成功应用了新的UV映射。");
    }

}