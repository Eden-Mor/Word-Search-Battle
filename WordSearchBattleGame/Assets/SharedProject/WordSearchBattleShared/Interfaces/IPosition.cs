using System;
using UnityEngine;

namespace WordSearchBattleShared.Models
{
    public interface IPosition
    {
        int X { get; set; }
        int Y { get; set; }

        public float magnitude => (float)Mathf.Sqrt(X * X + Y * Y);
    }
    
    public static class PositionInterfaceExtensions
    {
        public static bool IsDiagonal(this IPosition position, IPosition position2)
            => !(position.IsVert(position2) || position.IsHorz(position2));

        public static bool IsVert(this IPosition position, IPosition position2)
            => position.X == position2.X;
        public static bool IsHorz(this IPosition position, IPosition position2)
            => position.Y == position2.Y;
    }
}