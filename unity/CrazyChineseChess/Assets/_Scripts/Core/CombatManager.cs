// File: _Scripts/Core/CombatManager.cs

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 负责处理实时模式下所有战斗相关的逻辑，包括敌方和友方的碰撞检测与伤害判定。
/// </summary>
public class CombatManager
{
    private BoardState boardState;
    private BoardRenderer boardRenderer;
    private GameManager gameManager;

    // 碰撞检测的距离阈值（距离的平方），用于性能优化
    private const float COLLISION_DISTANCE_SQUARED = 0.0175f * 0.0175f;

    public CombatManager(BoardState state, BoardRenderer renderer)
    {
        this.boardState = state;
        this.boardRenderer = renderer;
        this.gameManager = GameManager.Instance;
    }

    /// <summary>
    /// 每帧调用的主处理函数，对所有活动的棋子进行两两配对的碰撞检测。
    /// </summary>
    public void ProcessCombat(List<PieceComponent> allActivePieces)
    {
        for (int i = 0; i < allActivePieces.Count; i++)
        {
            for (int j = i + 1; j < allActivePieces.Count; j++)
            {
                PieceComponent pieceA = allActivePieces[i];
                PieceComponent pieceB = allActivePieces[j];

                if (pieceA == null || pieceB == null || pieceA.RTState.IsDead || pieceB.RTState.IsDead) continue;

                float sqrDist = Vector3.SqrMagnitude(pieceA.transform.localPosition - pieceB.transform.localPosition);
                if (sqrDist < COLLISION_DISTANCE_SQUARED)
                {
                    ResolveCollision(pieceA, pieceB);
                }
            }
        }
    }

    /// <summary>
    /// 碰撞裁决的入口，根据碰撞双方的阵营分发给不同的处理方法。
    /// </summary>
    private void ResolveCollision(PieceComponent pieceA, PieceComponent pieceB)
    {
        if (pieceA.PieceData.Color == pieceB.PieceData.Color)
        {
            ResolveFriendlyCollision(pieceA, pieceB);
        }
        else
        {
            ResolveEnemyCollision(pieceA, pieceB);
        }
    }

    /// <summary>
    /// 处理敌方单位的碰撞逻辑。
    /// </summary>
    private void ResolveEnemyCollision(PieceComponent pieceA, PieceComponent pieceB)
    {
        RealTimePieceState stateA = pieceA.RTState;
        RealTimePieceState stateB = pieceB.RTState;

        bool aCanDamageB = stateA.IsAttacking && stateB.IsVulnerable;
        bool bCanDamageA = stateB.IsAttacking && stateA.IsVulnerable;

        if (aCanDamageB && bCanDamageA)
        {
            Debug.Log($"[Combat-Enemy] 碰撞双亡！ {pieceA.name} 与 {pieceB.name} 同归于尽。");
            Kill(pieceA);
            Kill(pieceB);
        }
        else if (aCanDamageB)
        {
            Kill(pieceB);
        }
        else if (bCanDamageA)
        {
            Kill(pieceA);
        }
    }

    /// <summary>
    /// 处理友方单位的碰撞逻辑，引入了棋子价值裁决机制。
    /// </summary>
    private void ResolveFriendlyCollision(PieceComponent pieceA, PieceComponent pieceB)
    {
        RealTimePieceState stateA = pieceA.RTState;
        RealTimePieceState stateB = pieceB.RTState;

        // 友伤判定前提：至少一方处于攻击状态，另一方处于可被攻击状态
        if (stateA.IsAttacking && stateB.IsVulnerable || stateA.IsVulnerable && stateB.IsAttacking)
        {
            int valueA = PieceValue.GetValue(pieceA.PieceData.Type);
            int valueB = PieceValue.GetValue(pieceB.PieceData.Type);

            Debug.Log($"[Combat-Friendly] 友方碰撞！ {pieceA.name} (价值:{valueA}) 与 {pieceB.name} (价值:{valueB}) 相撞。");

            if (valueA > valueB)
            {
                Debug.Log($"[Combat-Friendly] {pieceA.name} 价值更高，{pieceB.name} 被摧毁。");
                Kill(pieceB);
            }
            else if (valueB > valueA)
            {
                Debug.Log($"[Combat-Friendly] {pieceB.name} 价值更高，{pieceA.name} 被摧毁。");
                Kill(pieceA);
            }
            else // 价值相等
            {
                Debug.Log($"[Combat-Friendly] 双方价值相等，同归于尽！");
                Kill(pieceA);
                Kill(pieceB);
            }
        }
    }

    /// <summary>
    /// 封装了“杀死”一个棋子的所有必要操作。
    /// </summary>
    private void Kill(PieceComponent piece)
    {
        if (piece == null || piece.RTState.IsDead) return;

        Vector2Int currentLogicalPos = piece.RTState.LogicalPosition;
        Debug.Log($"[Combat] 棋子 {piece.name} 在逻辑坐标 {currentLogicalPos} 被击杀！");

        piece.RTState.IsDead = true;

        // 如果被击杀的是一个静止的棋子，需要从BoardState中移除
        if (!piece.RTState.IsMoving)
        {
            boardState.RemovePieceAt(currentLogicalPos);
            Debug.Log($"[Combat-Kill] 已从BoardState的 {currentLogicalPos} 移除一个静止的棋子。");
        }
        else
        {
            Debug.Log($"[Combat-Kill] 一个移动中的棋子被击杀，无需操作BoardState（它已不在其中）。");
        }

        // 销毁GameObject，这将自动终止其移动协程
        GameObject.Destroy(piece.gameObject);

        // 检查是否击杀了将/帅，如果是则触发游戏结束
        if (piece.PieceData.Type == PieceType.General)
        {
            GameStatus status = (piece.PieceData.Color == PlayerColor.Black)
                                ? GameStatus.RedWin
                                : GameStatus.BlackWin;
            gameManager.HandleEndGame(status);
        }
    }
}