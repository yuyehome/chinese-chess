// File: _Scripts/MovementStrategies/PieceStrategyFactory.cs

using System.Collections.Generic;

public static class PieceStrategyFactory
{
    private static readonly Dictionary<PieceType, IPieceMovementStrategy> strategies = new Dictionary<PieceType, IPieceMovementStrategy>()
    {
        { PieceType.Chariot, new PhysicalMoverStrategy() },
        { PieceType.Soldier, new PhysicalMoverStrategy() },
        { PieceType.General, new PhysicalMoverStrategy() },
        { PieceType.Advisor, new PhysicalMoverStrategy() },

        { PieceType.Cannon, new CannonStrategy() },
        { PieceType.Horse, new HorseStrategy() },
        { PieceType.Elephant, new ElephantStrategy() },
    };

    private static readonly IPieceMovementStrategy defaultStrategy = new PhysicalMoverStrategy();

    public static IPieceMovementStrategy GetStrategy(PieceType pieceType)
    {
        if (strategies.TryGetValue(pieceType, out IPieceMovementStrategy strategy))
        {
            return strategy;
        }
        return defaultStrategy;
    }
}