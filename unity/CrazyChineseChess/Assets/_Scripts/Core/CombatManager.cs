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
        for (int i = 0; i < allActivePieces.Count; i++)
        {
            for (int j = i + 1; j < allActivePieces.Count; j++)
            {
                PieceComponent pieceA = allActivePieces[i];
                PieceComponent pieceB = allActivePieces[j];

                if (pieceA == null || pieceB == null || pieceA.RTState.IsDead || pieceB.RTState.IsDead) continue;

                // 【修改】移除了跳过友方单位的判断
                // if (pieceA.PieceData.Color == pieceB.PieceData.Color) continue;

                float sqrDist = Vector3.SqrMagnitude(pieceA.transform.localPosition - pieceB.transform.localPosition);
                if (sqrDist < COLLISION_DISTANCE_SQUARED)
                {
                    ResolveCollision(pieceA, pieceB);
                }
            }
        }
    }

    /// <summary>
    /// 【已重构】处理两个棋子之间的碰撞结果，包含敌方和友方逻辑。
    /// </summary>
    private void ResolveCollision(PieceComponent pieceA, PieceComponent pieceB)
    {
        // 如果是友方单位碰撞
        if (pieceA.PieceData.Color == pieceB.PieceData.Color)
        {
            ResolveFriendlyCollision(pieceA, pieceB);
        }
        // 如果是敌方单位碰撞
        else
        {
            ResolveEnemyCollision(pieceA, pieceB);
        }
    }

    /// <summary>
    /// 【新增】处理敌方单位的碰撞逻辑。
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
    /// 【新增】处理友方单位的碰撞逻辑。
    /// </summary>
    private void ResolveFriendlyCollision(PieceComponent pieceA, PieceComponent pieceB)
    {
        RealTimePieceState stateA = pieceA.RTState;
        RealTimePieceState stateB = pieceB.RTState;

        //只要满足任何一个攻击条件，就触发逻辑，如果车远距离过来，帅主动上去送死，并且帅静止不处于攻击状态。车会被反杀
        if (stateA.IsAttacking && stateB.IsVulnerable || stateA.IsVulnerable && stateB.IsAttacking) 
        {
            int valueA = PieceValue.GetValue(pieceA.PieceData.Type);
            int valueB = PieceValue.GetValue(pieceB.PieceData.Type);

            Debug.Log($"[Combat-Friendly] 友方碰撞！ {pieceA.name} (价值:{valueA}) 与 {pieceB.name} (价值:{valueB}) 相撞。");

            if (valueA > valueB)
            {
                // A价值更高，B死亡
                Debug.Log($"[Combat-Friendly] {pieceA.name} 价值更高，{pieceB.name} 被摧毁。");
                Kill(pieceB);
            }
            else if (valueB > valueA)
            {
                // B价值更高，A死亡
                Debug.Log($"[Combat-Friendly] {pieceB.name} 价值更高，{pieceA.name} 被摧毁。");
                Kill(pieceA);
            }
            else // 价值相等
            {
                // 价值相等，如双车碰撞，则同归于尽
                Debug.Log($"[Combat-Friendly] 双方价值相等，同归于尽！");
                Kill(pieceA);
                Kill(pieceB);
            }
        }
    }

    /// <summary>
    /// 封装了杀死一个棋子的所有必要操作。
    /// </summary>
    private void Kill(PieceComponent piece)
    {
        if (piece == null || piece.RTState.IsDead) return;

        Vector2Int currentLogicalPos = piece.RTState.LogicalPosition;
        Debug.Log($"[Combat] 棋子 {piece.name} 在逻辑坐标 {currentLogicalPos} 被击杀！");

        piece.RTState.IsDead = true;


        // 【核心修改】CombatManager不再直接修改BoardState。
        // BoardState只记录静止棋子。一个移动中的棋子被击杀，它本来也就不在BoardState里。
        // 如果被击杀的是一个静止的棋子，我们需要移除它。
        if (!piece.RTState.IsMoving)
        {
            boardState.RemovePieceAt(currentLogicalPos);
            Debug.Log($"[Combat-Kill] 已从BoardState的 {currentLogicalPos} 移除一个静止的棋子。");
        }
        else
        {
            Debug.Log($"[Combat-Kill] 一个移动中的棋子被击杀，无需操作BoardState。");
        }

        // 销毁GameObject的操作保持不变，这会终止它的协程
        GameObject.Destroy(piece.gameObject);

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