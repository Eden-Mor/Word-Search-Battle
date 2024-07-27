using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using Assets.Scripts.Board.Objects;
using Assets.Scripts.GameData;
using UnityEngine.UI;
using TMPro;

namespace Assets.Scripts.Board
{
    public class GridManager : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        [SerializeField] private GameDataObject _gameData;

        public int rows;
        public int columns;
        public GameObject cellPrefab;
        private GridCell[,] grid;
        private List<GridCell> selectedCells = new();


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
            Debug.Log("Pointer Down");
            GridCell cell = GetCellUnderPointer(eventData);
            if (cell == null)
                return;

            ClearSelection();
            selectedCells.Clear();
            AddCellToSelection(cell);
        }

        public void OnDrag(PointerEventData eventData)
        {
            Debug.Log("Dragging");
            GridCell cell = GetCellUnderPointer(eventData);
            if (cell != null && !selectedCells.Contains(cell))
                AddCellToSelection(cell);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Debug.Log("Pointer Up");
            // Process the selected cells here
            foreach (GridCell cell in selectedCells)
                Debug.Log($"Selected Cell: {cell.Row}, {cell.Column}");
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

        private void AddCellToSelection(GridCell cell)
        {
            selectedCells.Add(cell);
            cell.SetHighlight(Color.black); // Change the color to the desired highlight color
        }

        private void ClearSelection()
        {
            foreach (GridCell cell in selectedCells)
                cell.ClearHighlight();
        }

        public void HighlightCellsExternally(List<Vector2Int> cellsToHighlight, Color color)
        {
            foreach (Vector2Int position in cellsToHighlight)
            {
                if (position.x < 0 || position.x >= rows || position.y < 0 || position.y >= columns)
                    continue;
                
                GridCell cell = grid[position.x, position.y];
                cell.SetHighlight(color);
            }
        }
    }

}
