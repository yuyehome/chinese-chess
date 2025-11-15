// 文件路径: Assets/Scripts/_App/UI/Views/PlayerInfoView.cs

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerInfoView : MonoBehaviour
{
    [Header("UI 引用")]
    [SerializeField] private Image playerAvatarImage;
    [SerializeField] private TMP_Text nicknameText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text rankText;

    /// <summary>
    /// 使用玩家数据更新UI显示。
    /// </summary>
    public void UpdateView(PlayerProfile profile)
    {
        // TODO: 后续将从profile.avatarId加载真实头像
        // playerAvatarImage.sprite = ...; 

        nicknameText.text = profile.nickname;
        levelText.text = $"Lv.{profile.eloRating / 100}"; // 临时用elo模拟等级
        goldText.text = profile.goldCoins.ToString("N0"); // "N0"格式化为带逗号的整数
        rankText.text = GetRankName(profile.eloRating); // 临时用elo模拟段位
    }

    // 临时的段位名称转换函数
    private string GetRankName(int elo)
    {
        if (elo < 1000) return "青铜";
        if (elo < 1500) return "白银";
        if (elo < 2000) return "黄金";
        return "铂金";
    }

    /// <summary>
    /// 更新玩家头像
    /// </summary>
    public void UpdateAvatar(Texture2D avatarTexture)
    {
        if (avatarTexture != null)
        {
            // 从Texture2D创建Sprite
            Sprite avatarSprite = Sprite.Create(avatarTexture, new Rect(0, 0, avatarTexture.width, avatarTexture.height), new Vector2(0.5f, 0.5f));
            playerAvatarImage.sprite = avatarSprite;
        }
    }


}