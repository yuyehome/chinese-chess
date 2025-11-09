// 文件路径: Assets/Scripts/_Core/View/BoardView.cs

using System.Collections.Generic;
using UnityEngine;

public class BoardView : MonoBehaviour
{
    public GameObject piecePrefab;

    private Dictionary<int, PieceView> _pieceViews = new Dictionary<int, PieceView>();

    // 响应棋子创建事件 (可批量创建)
    public void OnPieceCreated(Dictionary<int, PieceData> newPieces)
    {
        foreach (var pieceData in newPieces.Values)
        {
            if (!_pieceViews.ContainsKey(pieceData.uniqueId))
            {
                CreatePieceView(pieceData);
            }
        }
    }

    // 响应单个棋子更新事件
    public void OnPieceUpdated(PieceData updatedPieceData)
    {
        if (_pieceViews.TryGetValue(updatedPieceData.uniqueId, out PieceView pieceView))
        {
            // 更新视觉目标位置
            pieceView.UpdateTargetPosition(updatedPieceData.position);
            // TODO: 未来可以在这里处理状态变化，如播放中毒特效等
        }
    }

    // 响应棋子移除事件
    public void OnPieceRemoved(int pieceId)
    {
        if (_pieceViews.TryGetValue(pieceId, out PieceView pieceView))
        {
            // 在销毁前播放吃子音效
            AudioManager.Instance.PlaySFX("sfx_piece_die");
            // 播放死亡动画/特效
            Destroy(pieceView.gameObject, 0.5f); // 延迟销毁以播放动画
            _pieceViews.Remove(pieceId);
        }
    }

    private void CreatePieceView(PieceData pieceData)
    {
        if (piecePrefab == null)
        {
            Debug.LogError("Piece Prefab 未在BoardView中设置!");
            return;
        }

        GameObject pieceObject = Instantiate(piecePrefab, transform);
        PieceView pieceView = pieceObject.GetComponent<PieceView>();
        pieceView.Initialize(pieceData);

        var renderer = pieceObject.GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = (pieceData.team == PlayerTeam.Red) ? Color.red : Color.black;
        }

        _pieceViews.Add(pieceData.uniqueId, pieceView);
    }
}