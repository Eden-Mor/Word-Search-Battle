
using UnityEngine;
using WordSearchBattleShared.Models;

public class HighlightManager : MonoBehaviour
{
    public GameObject highlightBarPrefab;
    private RectTransform rectTrans;

    private void Awake()
    {
        rectTrans = GetComponent<RectTransform>();
    }

    public void ResetHighlights()
    {
        foreach (Transform child in rectTrans)
            Destroy(child.gameObject);
    }

    public void CreateHighlightBar(Vector2 start, Vector2 end, IPosition startLetter, IPosition endLetter, float size, float width = 20f, Color? color = null, float opacity = 0.3f)
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

        controller.Setup(start, end, startLetter, endLetter, (Color)color, opacity, width, size);
    }
}
