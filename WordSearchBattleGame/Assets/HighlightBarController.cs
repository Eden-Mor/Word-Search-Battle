using UnityEngine;
using UnityEngine.UI;

public class HighlightBarController : MonoBehaviour
{
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        image = GetComponent<Image>();
    }

    private RectTransform rectTransform;
    private Image image;

    public void Setup(Vector2 start, Vector2 end, Color color, float opacity, float width)
    {
        Vector2 direction = end - start;
        float length = direction.magnitude;

        rectTransform.sizeDelta = new Vector2(length, width);
        rectTransform.position = (start + end) / 2;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        rectTransform.rotation = Quaternion.Euler(0, 0, angle);

        image.color = new Color(color.r, color.g, color.b, opacity);
    }
}
