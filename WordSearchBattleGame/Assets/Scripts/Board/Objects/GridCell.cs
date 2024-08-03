using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WordSearchBattleShared.Models;

namespace Assets.Scripts.Board.Objects
{
    public class GridCell : MonoBehaviour, IPosition
    {
        public int X { get; set; }
        public int Y { get; set; }

        private TextMeshProUGUI textRenderer;

        private void Awake() 
            => textRenderer = GetComponent<TextMeshProUGUI>();

        public void Initialize(int row, int column)
        {
            X = row;
            Y = column;
        }

        public void SetHighlight(Color color) 
            => textRenderer.color = color;

        public void ClearHighlight() 
            => textRenderer.color = Color.white;
    }

}
