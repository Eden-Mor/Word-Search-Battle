using UnityEngine;
using WordSearchBattleShared.Models;


namespace WordSearchBattle.Scripts
{
    public class HighlightManager : MonoBehaviour
    {
        public GameObject highlightBarPrefab;

        public void ResetHighlights()
        {
            if (transform == null)
                return;

            foreach (Transform child in transform)
                Destroy(child.gameObject);
        }

        public void CreateHighlightBar(IPosition startLetter, IPosition endLetter, float size, Color? color = null, float opacity = 0.3f)
        {
            if (color == null)
                color = Color.yellow;

            GameObject highlightBar = Instantiate(highlightBarPrefab, transform);
            HighlightBarController controller = highlightBar.GetComponent<HighlightBarController>();

            if (endLetter.X > startLetter.X || (endLetter.X == startLetter.X && endLetter.Y > startLetter.Y))
            {
                // Sort start so it is calculated correctly
                IPosition temp = startLetter;
                startLetter = endLetter;
                endLetter = temp;
            }

            controller.Setup(startLetter, endLetter, (Color)color, opacity, size);
        }
    }
}