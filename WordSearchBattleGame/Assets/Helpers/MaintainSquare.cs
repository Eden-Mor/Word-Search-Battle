using Assets.Helpers;
using Assets.Scripts.Board;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class MaintainSquareWithPadding : MonoBehaviour
{
    private RectTransform rectTransform;

    // Padding around the square
    public float paddingHeight = 0f;
    public float paddingWidth = 0f;

    //tolerance for currentSize
    public float tolerance = 1f;

    private float currentSize = 0;

    public UnityEvent onResize;


    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        UpdateSize();
    }

    void Update()
    {
        UpdateSize();
    }

    void UpdateSize()
    {
        // Get the canvas size
        RectTransform canvasRect = GetComponentInParent<Canvas>().GetComponent<RectTransform>();
        float canvasWidth = canvasRect.rect.width;
        float canvasHeight = canvasRect.rect.height;

        // Calculate the new size based on the smaller dimension, considering padding
        float newSize = Mathf.Min(canvasWidth - paddingWidth, canvasHeight - paddingHeight);

        // Ensure the new size is not negative
        newSize = Mathf.Max(newSize, 0);

        if (!FloatHelper.HasFloatChanged(newSize, currentSize, this.tolerance))
            return;

        this.currentSize = newSize;

        // Set the new size to maintain a square shape
        rectTransform.sizeDelta = new Vector2(newSize, newSize);

        // Call the method to update the grid size
        try
        {
            onResize?.Invoke();
        }
        catch (Exception ex)
        {
            Debug.Log(ex.Message);
        }
    }
}