using UnityEngine;
using UnityEngine.UI;
using WordSearchBattleShared.Models;

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


    public void Setup(Vector2 start, Vector2 end, IPosition startLetter, IPosition endLetter, Color color, float opacity, float width, float rows)
    {
        var isDiagonal = startLetter.IsDiagonal(endLetter);

        rectTransform.position = (start + end) / 2;
        Vector2 direction = end - start;
        var isVert = startLetter.IsVert(endLetter);

        float hypotenuse = 0f;
        if (isDiagonal)
            hypotenuse = Mathf.Sqrt(2) / 1.6f;


        float extraHypot =  Mathf.Abs(startLetter.Y - endLetter.Y) / rows * hypotenuse / 4;


        var anchorMinX = isVert
            ? (endLetter.X - Mathf.Abs(startLetter.Y - endLetter.Y) / 2f) / rows
            : (endLetter.X) / rows;
            anchorMinX -= extraHypot;


        var anchorMaxX = isVert
            ? (startLetter.X + 1f + Mathf.Abs(startLetter.Y - endLetter.Y) / 2f) / rows
            : (startLetter.X + 1f) / rows;
            anchorMaxX += extraHypot;


        var midValue = (startLetter.Y + endLetter.Y + 1f) / 2;

        var midMinY = (midValue + 0.25f) / rows;
        var midMaxY = (midValue - 0.25f) / rows;

        rectTransform.anchorMin = new Vector2(anchorMinX, 1 - midMinY);
        rectTransform.anchorMax = new Vector2(anchorMaxX, 1 - midMaxY);

        rectTransform.anchoredPosition = Vector2.zero;


        Vector2 anchorSize = new(
            (rectTransform.anchorMax.x - rectTransform.anchorMin.x) * parentTransform.rect.width,
            (rectTransform.anchorMax.y - rectTransform.anchorMin.y) * parentTransform.rect.height
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
