// File: _Scripts/Core/CombatManager.cs

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 负责处理实时模式下所有战斗相关的逻辑，如碰撞检测和伤害判定。
/// </summary>
public class CombatManager
{
    private BoardState boardState;
    private BoardRenderer boardRenderer;
    private GameManager gameManager;

    // 碰撞检测的距离阈值，棋子半径约为0.0175f，我们用稍小一点的值
    private const float COLLISION_DISTANCE_SQUARED = 0.0175f * 0.0175f;

    public CombatManager(BoardState state, BoardRenderer renderer)
    {
        this.boardState = state;
        this.boardRenderer = renderer;
        this.gameManager = GameManager.Instance; // 获取GameManager的引用
    }

    /// <summary>
    /// 每帧调用的主处理函数，用于检测并处理所有可能的战斗。
    /// </summary>
    public void ProcessCombat(List<PieceComponent> allActivePieces)
    {

        // 执行两两配对的碰撞检测
        for (int i = 0; i < allActivePieces.Count; i++)
        {
            for (int j = i + 1; j < allActivePieces.Count; j++)
            {
                PieceComponent pieceA = allActivePieces[i];
                PieceComponent pieceB = allActivePieces[j];

                // 跳过无效或已死亡的棋子，或属于同一方的棋子
                if (pieceA == null || pieceB == null || pieceA.RTState.IsDead || pieceB.RTState.IsDead) continue;
                if (pieceA.PieceData.Color == pieceB.PieceData.Color) continue;

                // 检查物理距离是否足够近以发生碰撞
                float sqrDist = Vector3.SqrMagnitude(pieceA.transform.position - pieceB.transform.position);

                //if (Time.frameCount % 120 == 0)
                //{
                //    // 新增一个 if 判断，只在我们关心的两个棋子之间打印日志
                //    if ((pieceA.name == "Red_Cannon_7_2" && pieceB.name == "Black_Horse_7_9") ||
                //    (pieceA.name == "Black_Horse_7_9" && pieceB.name == "Red_Cannon_7_2"))
                //    {
                //        // 在这里，我们可以无条件打印，不再需要距离判断
                //        Debug.Log($"[Debug-Distance] " +
                //                  $"{pieceA.name} vs {pieceB.name}. " +
                //                  $"距离平方: {sqrDist}. " +
                //                  $"A坐标: {pieceA.transform.position}, " +
                //                  $"B坐标: {pieceB.transform.position}");
                //    }

                //}

                if (sqrDist < COLLISION_DISTANCE_SQUARED)
                {
                    Debug.Log($"[Combat-Check] 检测到碰撞! 棋子A: {pieceA.name}, 棋子B: {pieceB.name}, 距离平方: {sqrDist}");
                    ResolveCollision(pieceA, pieceB);
                }
            }
        }
    }

    /// <summary>
    /// 处理两个棋子之间的碰撞结果。
    /// </summary>
    private void ResolveCollision(PieceComponent pieceA, PieceComponent pieceB)
    {
        RealTimePieceState stateA = pieceA.RTState;
        RealTimePieceState stateB = pieceB.RTState;

        Debug.Log($"[Combat-Resolve] -- 棋子A ({pieceA.name}) 状态: IsAttacking={stateA.IsAttacking}, IsVulnerable={stateA.IsVulnerable}");
        Debug.Log($"[Combat-Resolve] -- 棋子B ({pieceB.name}) 状态: IsAttacking={stateB.IsAttacking}, IsVulnerable={stateB.IsVulnerable}");

        // 根据双方的攻击和易伤状态，判断伤害
        bool aCanDamageB = stateA.IsAttacking && stateB.IsVulnerable;
        bool bCanDamageA = stateB.IsAttacking && stateA.IsVulnerable;

        if (aCanDamageB && bCanDamageA)
        {
            // 双方都满足条件，同归于尽
            Debug.Log($"[Combat] 碰撞双亡！ {pieceA.name} 与 {pieceB.name} 同归于尽。");
            Kill(pieceA);
            Kill(pieceB);
        }
        else if (aCanDamageB)
        {
            // 只有A能伤害B
            Kill(pieceB);
        }
        else if (bCanDamageA)
        {
            // 只有B能伤害A
            Kill(pieceA);
        }
    }

    /// <summary>
    /// 封装了杀死一个棋子的所有必要操作。
    /// </summary>
    private void Kill(PieceComponent piece)
    {
        if (piece == null || piece.RTState.IsDead) return;

        // 【核心修改】在所有逻辑操作之前，先销毁GameObject。
        // 这会触发协程中的null检查，从而立即停止动画。
        // 我们需要棋子当前的逻辑位置来移除，而不是未来的目标位置。
        Vector2Int currentLogicalPos = piece.RTState.LogicalPosition;

        Debug.Log($"[Combat] 棋子 {piece.name} 在坐标 {piece.BoardPosition} 被击杀！");

        // 1. 标记为死亡状态
        piece.RTState.IsDead = true;

        // 2. 移除视觉对象
        // 注意：我们直接销毁GameObject，而不是通过BoardRenderer的方法
        // 因为BoardRenderer的方法依赖pieceObjects数组，而那个数组在飞行中不准
        GameObject.Destroy(piece.gameObject);

        // 3. 更新棋盘逻辑数据 (使用正确的当前位置)
        boardState.RemovePieceAt(currentLogicalPos);
        Debug.Log($"[Combat-Kill] 已从BoardState的 {currentLogicalPos} 移除棋子。");


        // 检查是否击杀了将/帅，如果是则结束游戏
        if (piece.PieceData.Type == PieceType.General)
        {
            GameStatus status = (piece.PieceData.Color == PlayerColor.Black)
                                ? GameStatus.RedWin
                                : GameStatus.BlackWin;
            gameManager.HandleEndGame(status);
        }

        // TODO: 可以在这里触发死亡特效、音效等
    }
}