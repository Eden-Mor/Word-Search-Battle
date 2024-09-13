using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using Assets.Scripts.Board.Objects;
using Assets.Scripts.GameData;
using TMPro;
using System.Text;
using System;
using WordSearchBattleShared.Enums;
using WordSearchBattleShared.Models;
using System.Linq;
using WordSearchBattleShared.Helpers;
using UnityEngine.UI;

namespace Assets.Scripts.Board
{
    public class GridManager : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        [SerializeField] private GameDataObject _gameData;

        public Action<WordItem> OnWordSelect;
        public int rows;
        public int columns;
        public GameObject cellPrefab;
        private GridCell[,] grid;
        private GridCell firstCell = null;
        private GridCell lastCell = null;
        private float cellWidth;
        private float cellHeight;
        private float parentWidth;
        private float parentHeight;


        private RectTransform m_parentTransform;
        private RectTransform parentTransform => m_parentTransform ??= GetComponent<RectTransform>();


        public void CreateGrid()
        {
            // Clear existing children
            foreach (Transform child in parentTransform)
                Destroy(child.gameObject);

            grid = new GridCell[rows, columns];

            GridLayoutGroup gl = GetComponent<GridLayoutGroup>();
            parentWidth = parentTransform.rect.width;
            parentHeight = parentTransform.rect.height;
            cellWidth = parentWidth / columns;
            cellHeight = parentHeight / rows;

            gl.cellSize = new(cellWidth, cellHeight);

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    GameObject cellObj = Instantiate(cellPrefab, this.transform);
                    var cellRt = cellObj.GetComponent<RectTransform>();
                    var cellCol = cellObj.GetComponent<BoxCollider2D>();
                    var cellText = cellObj.GetComponent<TextMeshProUGUI>();

                    cellText.text = _gameData._letterGrid[x, y].ToString();

                    cellCol.size = new Vector2(cellWidth, cellHeight);

                    GridCell cell = cellObj.GetComponent<GridCell>();
                    cell.Initialize(x, y);
                    grid[x, y] = cell;
                }
            }
        }

        public GameObject GetCellAtPosition(IPosition position)
            => GetComponent<RectTransform>().GetChild(position.Y * rows + position.X).gameObject;


        public Vector2 GetCellSize()
        {
            return new Vector2(cellWidth, cellHeight);
        }

        public Vector2 GetNormalizedVectorPositionOfCell(IPosition position, bool fromCenter = false)
        {
            var obj = GetCellAtPosition(position);
            if (obj == null)
                return Vector2.zero;

            var childRectTrans = obj.GetComponent<RectTransform>();

            return childRectTrans.position;
        }


        public void OnPointerDown(PointerEventData eventData)
        {
            GridCell cell = GetCellUnderPointer(eventData);
            if (cell == null)
                return;

            firstCell = cell;
            HighlightCell(cell);
        }


        public void OnDrag(PointerEventData eventData)
        {
            GridCell cell = GetCellUnderPointer(eventData);

            if (cell == null || firstCell == null)
                return;

            if (!PositionHelper.IsValidSelection(firstCell, cell))
                return;

            ClearSelection(firstCell, lastCell);
            lastCell = cell;

            var selectionData = GetCellSelection(firstCell, cell);
            if (selectionData != null)
                foreach (var gridCell in selectionData.Item1)
                    HighlightCell(gridCell);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            StringBuilder sb = new();

            var selectionData = GetCellSelection(firstCell, lastCell);
            if (selectionData == null)
                return;

            foreach (var cell in selectionData.Item1)
                sb.Append(_gameData._letterGrid[cell.X, cell.Y]);

            ClearSelection(firstCell, lastCell);

            WordItem wordItem = new()
            {
                Word = sb.ToString(),
                Direction = selectionData.Item2,
                StartX = selectionData.Item1.FirstOrDefault().X,
                StartY = selectionData.Item1.FirstOrDefault().Y
            };

            OnWordSelect?.Invoke(wordItem);
        }


        private GridCell GetCellUnderPointer(PointerEventData eventData)
        {
            // Convert screen point to world point and get the cell
            Vector2 worldPoint = Camera.main.ScreenToWorldPoint(eventData.position);
            RaycastHit2D hit = Physics2D.Raycast(worldPoint, Vector2.zero);
            if (hit.collider != null)
                return hit.collider.GetComponent<GridCell>();

            return null;
        }

        private void HighlightCell(GridCell cell)
            => cell.SetHighlight(Color.black);

        private void ClearSelection(GridCell startCell, GridCell endCell)
        {
            var selectionData = GetCellSelection(startCell, endCell);
            if (selectionData != null)
                foreach (var cell in selectionData.Item1)
                    cell.ClearHighlight();
        }

        private Tuple<List<GridCell>, DirectionEnum> GetCellSelection(GridCell startCell, GridCell endCell)
        {
            if (grid == null)
                throw new Exception("Grid was not created in GridManager before it was attempted to be used.");

            if (startCell == null)
                return null;

            List<GridCell> values = new();
            DirectionEnum direction = DirectionEnum.Center;

            if (endCell == null)
            {
                values.Add(startCell);
                return new Tuple<List<GridCell>, DirectionEnum>(values, direction);
            }

            direction = PositionHelper.GetDirection(startCell, endCell);
            var length = PositionHelper.Length(startCell, endCell);

            for (int t = 0; t <= length; t++)
            {
                var cell = PositionHelper.GetEndPosition(startCell, t, direction);

                if (rows > cell.X && columns > cell.Y && cell.X >= 0 && cell.Y >= 0)
                    values.Add(grid[cell.X, cell.Y]);
                else
                    Debug.Log("Fix this later, not sure how we got here.");
            }

            return new Tuple<List<GridCell>, DirectionEnum>(values, direction);
        }

    }

}
