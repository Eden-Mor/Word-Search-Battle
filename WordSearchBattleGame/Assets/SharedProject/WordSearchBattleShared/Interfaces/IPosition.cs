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
}