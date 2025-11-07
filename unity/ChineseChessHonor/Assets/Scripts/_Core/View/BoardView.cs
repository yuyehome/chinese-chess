// 文件路径: Assets/Scripts/_Core/View/BoardView.cs

using System.Collections.Generic;
using UnityEngine;

public class BoardView : MonoBehaviour
{
    public GameObject piecePrefab; // 用于实例化棋子的Prefab

    private Dictionary<int, PieceView> _pieceViews = new Dictionary<int, PieceView>();

    // 当游戏状态更新时，此方法被调用
    public void OnGameStateUpdated(GameState newState)
    {
        var activePieceIds = new HashSet<int>(_pieceViews.Keys);

        // 遍历最新的状态
        foreach (var pieceData in newState.pieces.Values)
        {
            if (_pieceViews.ContainsKey(pieceData.uniqueId))
            {
                // 已存在的棋子，更新它的目标位置
                _pieceViews[pieceData.uniqueId].UpdateTargetPosition(pieceData.position);
                activePieceIds.Remove(pieceData.uniqueId);
            }
            else
            {
                // 新棋子，创建它
                CreatePieceView(pieceData);
            }
        }

        // 移除已经不在最新状态中的棋子 (被吃掉的)
        foreach (var deadPieceId in activePieceIds)
        {
            DestroyPieceView(deadPieceId);
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

        // 根据阵营设置不同颜色以作区分
        var renderer = pieceObject.GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = (pieceData.team == PlayerTeam.Red) ? Color.red : Color.black;
        }

        _pieceViews.Add(pieceData.uniqueId, pieceView);
    }

    private void DestroyPieceView(int pieceId)
    {
        if (_pieceViews.TryGetValue(pieceId, out PieceView pieceView))
        {
            // TODO: 播放死亡动画/特效
            Destroy(pieceView.gameObject);
            _pieceViews.Remove(pieceId);
        }
    }
}