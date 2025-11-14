// 文件路径: Assets/Scripts/_Core/View/BoardView.cs

using System.Collections.Generic;
using UnityEngine;

public class BoardView : MonoBehaviour
{
    [Header("配置 (Config)")]
    [Tooltip("引用棋盘的物理和逻辑尺寸配置文件")]
    [SerializeField] private BoardConfig boardConfig;

    [Header("Prefabs & Templates")]
    [Tooltip("用于实例化棋子的基础预制体。我们将通过代码改变它的材质和UV。")]
    [SerializeField] private GameObject piecePrefabTemplate; // <--- 新增的变量

    [Tooltip("用于显示可移动位置的高亮标记预制体。")]
    [SerializeField] private GameObject highlightMovePrefab;

    [Tooltip("用于显示可攻击位置的高亮标记预制体。")]
    [SerializeField] private GameObject highlightAttackPrefab;

    [Header("场景引用 (Scene References)")]
    [Tooltip("场景中用于存放所有棋子对象的父节点。")]
    [SerializeField] private Transform piecesRoot; // <--- 新增的变量

    [Tooltip("场景中用于存放所有高亮标记的父节点。")]
    [SerializeField] private Transform highlightsRoot;

    private Dictionary<int, PieceView> _pieceViews = new Dictionary<int, PieceView>();
    private List<GameObject> _activeHighlights = new List<GameObject>();

    public BoardConfig Config => boardConfig;

    private void Awake()
    {
        if (boardConfig == null)
        {
            Debug.LogError("BoardView 致命错误: BoardConfig 未在Inspector中指定!", this);
        }
    }

    public void OnPieceCreated(Dictionary<int, PieceData> initialPieces)
    {
        ClearBoard();
        foreach (var pair in initialPieces)
        {
            CreatePieceView(pair.Value);
        }
    }

    public void OnPieceUpdated(PieceData pieceData)
    {
        if (boardConfig == null) return;
        if (_pieceViews.TryGetValue(pieceData.uniqueId, out PieceView view))
        {
            // TODO: 在后续步骤中实现平滑移动动画
            // 使用 BoardConfig 进行坐标转换
            view.transform.position = boardConfig.GridToWorld(pieceData.position);
        }
    }

    // 当棋子被移除时调用
    public void OnPieceRemoved(int pieceId)
    {
        if (_pieceViews.TryGetValue(pieceId, out PieceView view))
        {
            Destroy(view.gameObject);
            _pieceViews.Remove(pieceId);
        }
    }

    private void CreatePieceView(PieceData data)
    {
        if (piecePrefabTemplate == null || piecesRoot == null)
        {
            Debug.LogError("BoardView 的 'Piece Prefab Template' 或 'Pieces Root' 未在Inspector中设置!", this);
            return;
        }

        GameObject pieceObj = Instantiate(piecePrefabTemplate, piecesRoot);
        pieceObj.name = $"Piece_{data.type}_{data.team}"; // 给实例化的对象一个清晰的名字
        // 使用 BoardConfig 进行坐标转换
        pieceObj.transform.position = boardConfig.GridToWorld(data.position);

        PieceView view = pieceObj.GetComponent<PieceView>();

        if (view != null)
        {
            // 调用新的初始化方法，让PieceView自己处理外观
            view.Initialize(data);
        }
        else
        {
            Debug.LogError($"实例化的棋子预制体上缺少 PieceView 脚本!", pieceObj);
        }

        _pieceViews[data.uniqueId] = view;
    }

    public void ClearBoard()
    {
        foreach (var view in _pieceViews.Values)
        {
            if (view != null) Destroy(view.gameObject);
        }
        _pieceViews.Clear();
        ClearHighlights();
    }

    // --- 高亮功能 ---
    public void ShowHighlights(List<Vector2Int> movePositions, List<Vector2Int> attackPositions)
    {
        if (boardConfig == null) return;
        ClearHighlights();

        if (highlightsRoot == null) return;

        foreach (var pos in movePositions)
        {
            GameObject highlight = Instantiate(highlightMovePrefab, highlightsRoot);
            // 使用 BoardConfig 进行坐标转换
            highlight.transform.position = boardConfig.GridToWorld(pos);
            _activeHighlights.Add(highlight);
        }

        foreach (var pos in attackPositions)
        {
            GameObject highlight = Instantiate(highlightAttackPrefab, highlightsRoot);
            // 使用 BoardConfig 进行坐标转换
            highlight.transform.position = boardConfig.GridToWorld(pos);
            _activeHighlights.Add(highlight);
        }
    }

    public void ClearHighlights()
    {
        foreach (var highlight in _activeHighlights)
        {
            if (highlight != null) Destroy(highlight);
        }
        _activeHighlights.Clear();
    }
}