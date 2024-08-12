using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class HighlightBarController : MonoBehaviour
{
    private void Awake()
    {
        image = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();
        parentTransform = rectTransform.parent.GetComponent<RectTransform>();
    }

    private RectTransform parentTransform;
    private RectTransform rectTransform;
    private Image image;


    //TO DO - ON DIAGONALS SET ANCHORS AS IF THE LINE HAD 0 ROTATION,
    //    W
    //    O
    //[   R   ]
    //    D
    //    S

    public void Setup(Vector2 start, Vector2 end, Vector2 anchorMin, Vector2 anchorMax, Color color, float opacity, float width, float lengthCorrection)
    {
        rectTransform.position = (start + end) / 2;


        var pos = rectTransform.localPosition;
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.localPosition = pos;

        // Calculate direction and length based on the difference between the start and end points
        Vector2 direction = end - start;


        // Calculate the length in local space based on the parent size and anchor positions
        Vector2 anchorSize = new(
            (anchorMax.x - anchorMin.x + lengthCorrection / 2) * parentTransform.rect.width,
            (anchorMax.y - anchorMin.y - lengthCorrection / 2) * parentTransform.rect.height
        );

        float length = anchorSize.magnitude;

        // Set position, size, and rotation
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, length);
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, width);

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        rectTransform.rotation = Quaternion.Euler(0, 0, angle);

        image.color = new Color(color.r, color.g, color.b, opacity);
    }
}
