using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HighlightManager : MonoBehaviour
{
    public GameObject highlightBarPrefab;

    public void CreateHighlightBar(Vector2 start, Vector2 end, float width = 20f, Color? color = null, float opacity = 0.5f)
    {
        if (color == null)
            color = Color.yellow;

        GameObject highlightBar = Instantiate(highlightBarPrefab, transform);
        HighlightBarController controller = highlightBar.GetComponent<HighlightBarController>();
        controller.Setup(start, end, (Color)color, opacity, width);
    }
}
