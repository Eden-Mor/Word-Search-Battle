using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using Assets.Scripts.Board.Objects;
using Assets.Scripts.GameData;
using TMPro;
using System.Text;
using System;

#nullable enable

namespace Assets.Scripts.Board
{
    public class GridManager : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        [SerializeField] private GameDataObject _gameData;

        public Action<string> actionOnWordSelect;
        public int rows;
        public int columns;
        public GameObject cellPrefab;
        private GridCell[,]? grid;
        private GridCell? firstCell = null;
        private GridCell? lastCell = null;


        public void CreateGrid()
        {
            grid = new GridCell[rows, columns];

            RectTransform rt = GetComponent<RectTransform>();
            float parentWidth = rt.rect.width;
            float parentHeight = rt.rect.height;
            float cellWidth = parentWidth / columns;
            float cellHeight = parentHeight / rows;

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < columns; c++)
                {
                    GameObject cellObj = Instantiate(cellPrefab, this.transform);
                    var cellRt = cellObj.GetComponent<RectTransform>();
                    var cellCol = cellObj.GetComponent<BoxCollider2D>();
                    var cellText = cellObj.GetComponent<TextMeshProUGUI>();

                    //cellText.text = r.ToString() + "," + c.ToString();
                    cellText.text = _gameData._letterGrid[r, c].ToString();

                    float xPos = (c * cellWidth) + (cellWidth / 2) - parentWidth / 2;
                    float yPos = (r * cellHeight) + (cellHeight / 2) - parentHeight / 2;

                    cellRt.anchoredPosition = new Vector2(xPos, yPos);
                    cellCol.size = new Vector2(cellWidth, cellHeight);
                    cellRt.sizeDelta = new Vector2(cellWidth, cellHeight);

                    GridCell cell = cellObj.GetComponent<GridCell>();
                    cell.Initialize(r, c);
                    grid[r, c] = cell;
                }
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            GridCell? cell = GetCellUnderPointer(eventData);
            if (cell == null)
                return;

            firstCell = cell;
            HighlightCell(cell);
        }

        private bool IsDiagonalSelection(GridCell startCell, GridCell endCell)
            => Mathf.Abs(startCell.Column - endCell.Column) == Mathf.Abs(startCell.Row - endCell.Row);

        private bool IsStraightSelection(GridCell startCell, GridCell endCell)
            => startCell.Column == endCell.Column || startCell.Row == endCell.Row;

        private bool IsValidSelection(GridCell startCell, GridCell endCell)
            => IsDiagonalSelection(startCell, endCell) || IsStraightSelection(startCell, endCell);

        public void OnDrag(PointerEventData eventData)
        {
            GridCell? cell = GetCellUnderPointer(eventData);

            if (cell == null || firstCell == null)
                return;

            if (!IsValidSelection(firstCell, cell))
                return;

            ClearSelection(firstCell, lastCell);
            lastCell = cell;
            foreach (var gridCell in GetCellSelection(firstCell, cell))
                HighlightCell(gridCell);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            StringBuilder sb = new();

            foreach (var cell in GetCellSelection(firstCell, lastCell))
                sb.Append(_gameData._letterGrid[cell.Row, cell.Column]);
            
            Debug.Log("Pointer Up - Value: " + sb.ToString());

            ClearSelection(firstCell, lastCell);
            actionOnWordSelect?.Invoke(sb.ToString());
        }

        private GridCell? GetCellUnderPointer(PointerEventData eventData)
        {
            // Convert screen point to world point and get the cell
            Vector2 worldPoint = Camera.main.ScreenToWorldPoint(eventData.position);
            RaycastHit2D hit = Physics2D.Raycast(worldPoint, Vector2.zero);
            if (hit.collider != null)
                return hit.collider.GetComponent<GridCell>();

            return null;
        }

        private void HighlightCell(GridCell cell)
        {
            cell.SetHighlight(Color.black); // Change the color to the desired highlight color
        }

        private void ClearSelection(GridCell? startCell, GridCell? endCell)
        {
            foreach (GridCell cell in GetCellSelection(startCell, endCell))
                cell.ClearHighlight();
        }

        private List<GridCell> GetCellSelection(GridCell? startCell, GridCell? endCell)
        {
            List<GridCell> values = new();

            if (grid == null)
                throw new System.Exception("Grid was not created in GridManager before it was attempted to be used.");

            if (startCell == null)
                return values;

            if (endCell == null)
                return new List<GridCell>() { startCell };

            int smaller = startCell.Column == endCell.Column ? startCell.Row : startCell.Column;
            int bigger = startCell.Column == endCell.Column ? endCell.Row : endCell.Column;
            (smaller, bigger) = smaller < bigger ? (smaller, bigger) : (bigger, smaller);

            if (IsDiagonalSelection(startCell, endCell))
            {
                //check direction, for loop, add item per grid
                // We know its a diagonal, so the available numbers for directions is either 1 3 7 or 9
                // if start.Row < end.Row then we know its either 3 or 9
                // if the start.Col > end.Col we know we need to go downwards (default is up)
                //  1 2 3  NW N NE
                //  4 5 6   W    E
                //  7 8 9  SW S SE
                DirectionEnum direction = startCell.Row > endCell.Row ? DirectionEnum.NW : DirectionEnum.NE;
                if (startCell.Column > endCell.Column)
                    direction += 6;

                for (int t = 0; t + smaller <= bigger; t++)
                {
                    int valueRow = (direction is DirectionEnum.NE or DirectionEnum.SE)
                        ? startCell.Row + t
                        : startCell.Row - t;

                    int valueCol = (direction is DirectionEnum.NE or DirectionEnum.NW)
                        ? startCell.Column + t
                        : startCell.Column - t;

                    values.Add(grid[valueRow, valueCol]);
                }

            }
            else if (IsStraightSelection(startCell, endCell))
            {
                DirectionEnum direction =
                    startCell.Row == endCell.Row
                        ? startCell.Column > endCell.Column
                            ? DirectionEnum.W
                            : DirectionEnum.E
                        : startCell.Row > endCell.Row
                            ? DirectionEnum.N
                            : DirectionEnum.S;

                for (int t = 0; t + smaller <= bigger; t++)
                {
                    int valueRow = (direction is DirectionEnum.N or DirectionEnum.S)
                        ? smaller + t
                        : startCell.Row;

                    int valueCol = (direction is DirectionEnum.E or DirectionEnum.W)
                        ? smaller + t 
                        : startCell.Column;

                    values.Add(grid[valueRow, valueCol]);
                }

                if (direction is DirectionEnum.W or DirectionEnum.N)
                    values.Reverse();

            }

            return values;
        }
    }

}
