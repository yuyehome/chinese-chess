// 文件路径: Assets/Scripts/_App/UI/Views/PlayerSlotView.cs

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class PlayerSlotView : MonoBehaviour
{
    [Header("基础信息 UI")]
    [SerializeField] private GameObject playerDataContainer; // 包含以下所有元素的容器
    [SerializeField] private Image playerAvatarImage;
    [SerializeField] private TMP_Text nicknameText;
    [SerializeField] private Image rankIconImage;
    [SerializeField] private TMP_Text pingText;
    [SerializeField] private Button kickPlayerButton; // 仅房主可见并对其他玩家生效

    [Header("空槽位 UI")]
    [SerializeField] private GameObject emptySlotOverlay;

    [Header("备战 UI")]
    [SerializeField] private GameObject selectedPiecesContainer; // “已抢棋子”的根节点
    [SerializeField] private Image highlightOutline; // 用于高亮外发光

    [Header("已选棋子 Prefab")]
    [SerializeField] private GameObject selectedPieceIconPrefab; // 用于实例化显示已选棋子的图标

    private List<GameObject> _pieceIcons = new List<GameObject>();

    private void Awake()
    {
        // 初始状态设置
        SetEmpty(true);
        SetHighlight(false);
        kickPlayerButton.gameObject.SetActive(false);
    }

    /// <summary>
    /// 设置槽位为有玩家状态
    /// </summary>
    public void SetPlayer(PlayerProfile profile, bool isHost, bool canBeKicked)
    {
        playerDataContainer.SetActive(true);
        emptySlotOverlay.SetActive(false);

        nicknameText.text = profile.nickname;
        pingText.text = "---ms"; // 初始ping
        // TODO: 加载头像和段位图标

        kickPlayerButton.gameObject.SetActive(isHost && canBeKicked);
    }

    /// <summary>
    /// 设置槽位为空状态
    /// </summary>
    public void SetEmpty(bool isEmpty)
    {
        playerDataContainer.SetActive(!isEmpty);
        emptySlotOverlay.SetActive(isEmpty);
    }

    /// <summary>
    // 更新网络延迟显示
    /// </summary>
    public void UpdatePing(int ping)
    {
        pingText.text = $"{ping}ms";
    }

    /// <summary>
    /// 控制高亮外发光效果的显隐
    /// </summary>
    public void SetHighlight(bool isHighlighted)
    {
        highlightOutline.gameObject.SetActive(isHighlighted);
    }

    /// <summary>
    /// 添加一个已选棋子图标（带动画）
    /// </summary>
    public void AddSelectedPiece(Sprite pieceSprite, Vector3 startWorldPosition)
    {
        if (selectedPieceIconPrefab == null) return;

        GameObject iconInstance = Instantiate(selectedPieceIconPrefab, selectedPiecesContainer.transform);
        iconInstance.GetComponent<Image>().sprite = pieceSprite;

        // TODO: 在后续步骤中实现从 startWorldPosition 飞到目标位置的动画
        Debug.Log($"[PlayerSlotView] 添加棋子图标 {pieceSprite.name}，起始位置 {startWorldPosition}");
    }

    /// <summary>
    /// 清空所有已选棋子图标
    /// </summary>
    public void ClearSelectedPieces()
    {
        foreach (var icon in _pieceIcons)
        {
            Destroy(icon);
        }
        _pieceIcons.Clear();
    }
}