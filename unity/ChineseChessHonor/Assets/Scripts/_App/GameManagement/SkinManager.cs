// 文件路径: Assets/Scripts/_App/GameManagement/SkinManager.cs

using System.Collections.Generic;
using UnityEngine;

public class SkinManager : PersistentSingleton<SkinManager>
{
    // 使用字典嵌套，结构: SkinType -> PlayerTeam -> Material
    private Dictionary<SkinType, Dictionary<PlayerTeam, Material>> _skinMaterials;

    protected override void Awake()
    {
        base.Awake();
        LoadAllSkinMaterials();
    }

    private void LoadAllSkinMaterials()
    {
        _skinMaterials = new Dictionary<SkinType, Dictionary<PlayerTeam, Material>>();
        Debug.Log("[SkinManager] 开始加载所有皮肤材质...");

        // --- 加载木材皮肤 (Wood) ---
        var woodMaterials = new Dictionary<PlayerTeam, Material>();
        woodMaterials[PlayerTeam.Red] = Resources.Load<Material>("Materials/Pieces/Wood/M_Piece_Wood_Red");
        woodMaterials[PlayerTeam.Black] = Resources.Load<Material>("Materials/Pieces/Wood/M_Piece_Wood_Black");
        woodMaterials[PlayerTeam.Blue] = Resources.Load<Material>("Materials/Pieces/Wood/M_Piece_Wood_Blue");
        woodMaterials[PlayerTeam.Purple] = Resources.Load<Material>("Materials/Pieces/Wood/M_Piece_Wood_Purple");
        _skinMaterials[SkinType.Wood] = woodMaterials;

        // --- 验证加载 ---
        foreach (var teamMaterialPair in woodMaterials)
        {
            if (teamMaterialPair.Value == null)
            {
                Debug.LogError($"[SkinManager] 加载失败! 无法在 Resources/Materials/Pieces/Wood/ 路径下找到材质: M_Piece_Wood_{teamMaterialPair.Key}");
            }
            else
            {
                Debug.Log($"[SkinManager] 成功加载材质: {teamMaterialPair.Value.name}");
            }
        }
    }

    /// <summary>
    /// 获取指定皮肤和阵营的棋子材质实例。
    /// </summary>
    /// <param name="skin">皮肤类型</param>
    /// <param name="team">玩家阵营</param>
    /// <returns>材质实例，如果找不到则返回null</returns>
    public Material GetPieceMaterial(SkinType skin, PlayerTeam team)
    {
        Debug.Log($"[SkinManager] 请求材质: 皮肤={skin}, 阵营={team}");
        if (_skinMaterials.TryGetValue(skin, out var teamMaterials))
        {
            if (teamMaterials.TryGetValue(team, out var material))
            {
                if (material != null)
                {
                    Debug.Log($"[SkinManager] 找到并返回材质实例: {material.name}");
                    return new Material(material);
                }
            }
        }

        Debug.LogWarning($"[SkinManager] 未找到皮肤 {skin} 和阵营 {team} 对应的材质!");
        return null;
    }

}