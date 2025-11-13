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
        if (skinData == null)
        {
            Debug.LogError("[PieceView] 缺少 PieceSkinData 配置!", this);
            return;
        }

        Rect uvRect = skinData.GetUVRect(this.type);
        Debug.Log($"[PieceView] 棋子类型 {this.type} 获取到UV区域: X={uvRect.x}, Y={uvRect.y}, W={uvRect.width}, H={uvRect.height}");

        if (_instancedMesh == null)
        {
            Mesh originalMesh = meshFilter.sharedMesh;
            if (originalMesh == null)
            {
                Debug.LogError("[PieceView] MeshFilter 上没有找到原始Mesh!", this);
                return;
            }
            _instancedMesh = new Mesh
            {
                name = $"{originalMesh.name}_inst_{this.GetInstanceID()}",
                vertices = originalMesh.vertices,
                triangles = originalMesh.triangles,
                normals = originalMesh.normals,
                tangents = originalMesh.tangents
            };
            meshFilter.mesh = _instancedMesh;
            Debug.Log($"[PieceView] 首次为 {this.gameObject.name} 创建了网格实例。");
        }

        Vector2[] originalUVs = meshFilter.sharedMesh.uv;
        if (originalUVs.Length == 0)
        {
            Debug.LogError("[PieceView] 警告: 原始模型没有UV坐标! 无法应用贴图。", this);
            return;
        }
        Vector2[] newUVs = new Vector2[originalUVs.Length];

        for (int i = 0; i < originalUVs.Length; i++)
        {
            newUVs[i].x = uvRect.x + originalUVs[i].x * uvRect.width;
            newUVs[i].y = uvRect.y + originalUVs[i].y * uvRect.height;
        }

        _instancedMesh.uv = newUVs;
        Debug.Log($"[PieceView] 成功为 {this.gameObject.name} 应用了新的UV映射。");
    }

    // --- 以下是旧代码中保留的辅助方法 ---
    public static Vector2Int WorldToGrid(Vector3 worldPos)
    {
        int x = Mathf.RoundToInt(worldPos.x);
        int y = Mathf.RoundToInt(worldPos.z);
        return new Vector2Int(x, y);
    }

    public static Vector3 GridToWorld(Vector2Int gridPos)
    {
        return new Vector3(gridPos.x, 0, gridPos.y);
    }
}