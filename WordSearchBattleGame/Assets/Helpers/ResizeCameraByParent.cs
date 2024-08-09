using Assets.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResizeCameraByParent : MonoBehaviour
{
    private Camera cam;
    private RectTransform parentRect;
    public float tolerance = 10f;
    private float currentSize = 0f;

    void Start()
    {
        cam = GetComponent<Camera>();
        parentRect = GetComponentInParent<RectTransform>();
        SetSize();
    }

    private void SetSize()
    {
        if (!FloatHelper.HasFloatChanged(this.currentSize, parentRect.position.y, this.tolerance))
            return;

        this.currentSize = cam.orthographicSize;
        cam.orthographicSize = parentRect.position.y;
    }


    void Update()
    {
        SetSize();
    }
}
