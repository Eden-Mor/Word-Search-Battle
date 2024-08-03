using System;
using System.Collections.Generic;
using UnityEngine;
using WordSearchBattleShared.Enums;
using WordSearchBattleShared.Models;

namespace WordSearchBattleShared.Helpers
{
	public static class PositionHelper
	{
		public static readonly Dictionary<DirectionEnum, (int dx, int dy)> DirectionOffsets = new()
		{
			{ DirectionEnum.N, (0, 1) },
			{ DirectionEnum.NE, (1, 1) },
			{ DirectionEnum.NW, (-1, 1) },
			{ DirectionEnum.S, (0, -1) },
			{ DirectionEnum.SE, (1, -1) },
			{ DirectionEnum.SW, (-1, -1) },
			{ DirectionEnum.E, (1, 0) },
			{ DirectionEnum.W, (-1, 0) },
			{ DirectionEnum.Center, (0, 0) }
		};

		public static bool IsDiagonalSelection(IPosition startCell, IPosition endCell)
			=> Mathf.Abs(startCell.Y - endCell.Y) == Mathf.Abs(startCell.X - endCell.X);

		public static bool IsStraightSelection(IPosition startCell, IPosition endCell)
			=> startCell.Y == endCell.Y || startCell.X == endCell.X;

		public static bool IsValidSelection(IPosition startCell, IPosition endCell)
			=> IsDiagonalSelection(startCell, endCell) || IsStraightSelection(startCell, endCell);

		public static IPosition GetEndPosition(IPosition startCell, int length, DirectionEnum direction)
		{
            var (dx, dy) = DirectionOffsets[direction];
            Position newCell = new()
            {
                X = startCell.X + dx * length,
                Y = startCell.Y + dy * length
            };

            return newCell;
		}

		public static DirectionEnum GetDirection(IPosition start, IPosition end)
		{
			int dx = end.X - start.X;
			int dy = end.Y - start.Y;

			if (dx == 0 && dy == 0)
				return DirectionEnum.Center;
			else if (dx == 0)
				return dy > 0 ? DirectionEnum.N : DirectionEnum.S;
			else if (dy == 0)
				return dx > 0 ? DirectionEnum.E : DirectionEnum.W;
			else if (dx > 0 && dy > 0)
				return DirectionEnum.NE;
			else if (dx > 0 && dy < 0)
				return DirectionEnum.SE;
			else if (dx < 0 && dy > 0)
				return DirectionEnum.NW;
			else // dx < 0 && dy < 0
				return DirectionEnum.SW;
		}

		public static int Length(IPosition start, IPosition end)
			=> Mathf.Max(Mathf.Abs(start.X - end.X), Mathf.Abs(start.Y - end.Y));
	}
}