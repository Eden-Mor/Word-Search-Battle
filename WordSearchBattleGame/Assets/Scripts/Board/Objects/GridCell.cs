using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WordSearchBattleShared.Models;

namespace Assets.Scripts.Board.Objects
{
    public class GridCell : MonoBehaviour, IPosition
    {
        public int debugX;
        public int debugY;
        public int X { get; set; }
        public int Y { get; set; }

        private TextMeshProUGUI textRenderer;

        private void Awake() 
            => textRenderer = GetComponent<TextMeshProUGUI>();

        public void Initialize(int x, int y)
        {
            X = x;
            Y = y;
            debugX = x; debugY = y;
        }

        public void SetHighlight(Color color) 
            => textRenderer.color = color;

        public void ClearHighlight() 
            => textRenderer.color = Color.white;
    }

}
