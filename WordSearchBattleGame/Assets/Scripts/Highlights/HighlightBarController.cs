using UnityEngine;
using UnityEngine.UI;
using WordSearchBattleShared.Models;


namespace WordSearchBattle.Scripts
{
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


        public void Setup(IPosition startLetter, IPosition endLetter, Color color, float opacity, float rows)
        {
            Vector2 direction = new Vector2(endLetter.X, endLetter.Y) - new Vector2(startLetter.X, startLetter.Y);
            var isVert = startLetter.IsVert(endLetter);
            var isDiagonal = startLetter.IsDiagonal(endLetter);

            float hypotenuse = 0f;
            if (isDiagonal)
                hypotenuse = 0.221f; // Sqrt(2) / 1.6f / 4

            float extraHypot = Mathf.Abs(startLetter.Y - endLetter.Y) / rows * hypotenuse;


            var anchorMinX = (float)endLetter.X;
            if (isVert)
                anchorMinX -= Mathf.Abs(startLetter.Y - endLetter.Y) / 2f;
            anchorMinX += Random.Range(-0.15f, 0.15f);
            anchorMinX /= rows;
            anchorMinX -= extraHypot;


            var anchorMaxX = startLetter.X + 1f;
            if (isVert)
                anchorMaxX += Mathf.Abs(startLetter.Y - endLetter.Y) / 2f;
            anchorMaxX += Random.Range(-0.15f, 0.15f);
            anchorMaxX /= rows;
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
            float width = (1 / rows) * 900f;

            // Set position, size, and rotation
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, length);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, width * Random.Range(0.95f, 1.05f));

            float wordLength = startLetter.Distance(endLetter);
            var divideByCount = rows;
            if (isDiagonal)
                divideByCount = Mathf.Sqrt(2 * Mathf.Pow(rows, 2));

            var maxAngleRand = isDiagonal ? 3f : 4f;
            var minAngleRand = isDiagonal ? 0f : 1f;

            float randomRange = Mathf.Lerp(maxAngleRand, minAngleRand, wordLength / divideByCount); // Adjust the range based on word length, Linear intERPolation
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + Random.Range(-randomRange, randomRange);

            //Since our coordinate system starts 0,0 at the top left corner, we need to adjust the angle on diagonals.
            if (isDiagonal)
                angle += 90f;

            rectTransform.rotation = Quaternion.Euler(0, 0, angle);

            image.color = new Color(color.r, color.g, color.b, opacity);
        }
    }
}