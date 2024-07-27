using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Board.Objects
{
    public class GridCell : MonoBehaviour
    {
        public int Row { get; private set; }
        public int Column { get; private set; }
        private TextMeshProUGUI textRenderer;

        private void Awake() 
            => textRenderer = GetComponent<TextMeshProUGUI>();

        public void Initialize(int row, int column)
        {
            Row = row;
            Column = column;
        }

        public void SetHighlight(Color color) 
            => textRenderer.color = color;

        public void ClearHighlight() 
            => textRenderer.color = Color.white;
    }

}
