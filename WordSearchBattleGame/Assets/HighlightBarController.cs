using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class HighlightBarController : MonoBehaviour
{
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        image = GetComponent<Image>();
    }

    private RectTransform rectTransform;
    private Image image;



    public void Setup(Vector2 start, Vector2 end, Vector2 anchorMin, Vector2 anchorMax, Color color, float opacity, float width)
    {
        Vector2 direction = (end - start) / 2;
        float length = direction.magnitude * 4f;

        rectTransform.position = (start + end) / 2;

        //save pos and set anchor min and max
        var pos = rectTransform.localPosition;
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.localPosition = pos;

        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, length);
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, width);

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        rectTransform.rotation = Quaternion.Euler(0, 0, angle);

        image.color = new Color(color.r, color.g, color.b, opacity);
    }
}
