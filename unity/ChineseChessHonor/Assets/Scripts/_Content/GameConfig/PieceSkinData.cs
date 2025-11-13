// 文件路径: Assets/Scripts/_Content/GameConfig/PieceSkinData.cs

using UnityEngine;

[CreateAssetMenu(fileName = "PieceSkinData", menuName = "ChessHonor/Configs/Piece Skin Data")]
public class PieceSkinData : ScriptableObject
{
    [Header("图集信息")]
    [Tooltip("图集包含了多少个棋子 (例如 7)")]
    public int pieceCountInAtlas = 7;

    /// <summary>
    /// 根据棋子类型获取其在图集中的UV矩形区域。
    /// </summary>
    /// <param name="type">棋子类型</param>
    /// <returns>一个Rect，表示UV的(x, y, width, height)</returns>
    public Rect GetUVRect(PieceType type)
    {
        if (pieceCountInAtlas <= 0) return Rect.zero;

        // PieceType枚举的顺序必须和贴图集中的顺序完全一致
        int index = (int)type;
        float width = 1.0f / pieceCountInAtlas;
        float x = index * width;

        return new Rect(x, 0, width, 1.0f);
    }
}