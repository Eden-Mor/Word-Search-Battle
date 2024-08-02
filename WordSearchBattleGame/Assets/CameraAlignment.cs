using UnityEngine;

public class CameraAlignment : MonoBehaviour
{
    public Camera cameraToAlign;
    public RectTransform canvasRectTransform;

    void Start()
    {
        AlignCameraWithCanvas();
    }

    void AlignCameraWithCanvas()
    {
        // Get the position and size of the canvas RectTransform
        Vector2 canvasSize = canvasRectTransform.sizeDelta;
        Vector3[] canvasCorners = new Vector3[4];
        canvasRectTransform.GetWorldCorners(canvasCorners);

        // Calculate the viewport rect for the camera
        float x = canvasCorners[0].x / Screen.width;
        float y = canvasCorners[0].y / Screen.height;
        float width = canvasSize.x / Screen.width;
        float height = canvasSize.y / Screen.height;

        // Set the camera's viewport rect
        cameraToAlign.rect = new Rect(x, y, width, height);
    }

    void Update()
    {
        // Update the camera alignment each frame if needed
        AlignCameraWithCanvas();
    }
}