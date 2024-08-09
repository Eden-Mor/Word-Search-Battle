
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

        if (startLetter.magnitude > endLetter.magnitude)
        {
            // Swap the positions if startLetter has a greater magnitude than endLetter
            IPosition temp = startLetter;
            startLetter = endLetter;
            endLetter = temp;
        }

        size -= 0.75f;

        var minAnchor = new Vector2(startLetter.X / size, 1 - startLetter.Y / size);
        var maxAnchor = new Vector2(endLetter.X / size, 1 - endLetter.Y / size);

        controller.Setup(start, end, minAnchor, maxAnchor, (Color)color, opacity, width);
    }
}
