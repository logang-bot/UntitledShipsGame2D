using UnityEngine;

// Sizes the HUD rails to exactly the pillarbox bars on either side of the
// 9:16 playfield. The bars are measured in screen pixels; the canvas scales
// with screen height (1920x1080 reference, per the HUD design), so widths are
// divided by its scale factor to land in canvas units.
[ExecuteAlways] // Runs in Edit mode too, so Game view previews sidebar sizing without pressing Play
public class HUDSidebarFitter : MonoBehaviour
{
    public AspectRatioFitter aspectFitter; // drag the Main Camera here
    public RectTransform leftSidebar;
    public RectTransform rightSidebar;

    private Canvas canvas;
    private int lastScreenWidth;
    private int lastScreenHeight;
    private float lastScaleFactor;

    void Update()
    {
        if (canvas == null) canvas = GetComponent<Canvas>();
        float scale = canvas != null ? canvas.scaleFactor : 1f;
        if (Screen.width == lastScreenWidth && Screen.height == lastScreenHeight && Mathf.Approximately(scale, lastScaleFactor)) return;
        ApplySidebarSizes(scale);
    }

    void ApplySidebarSizes(float scale)
    {
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
        lastScaleFactor = scale;
        Rect gameViewport = aspectFitter.GetViewportPixelRect();
        SetWidth(leftSidebar, gameViewport.x / scale);
        SetWidth(rightSidebar, (Screen.width - (gameViewport.x + gameViewport.width)) / scale);
    }

    private static void SetWidth(RectTransform sidebar, float width)
    {
        if (sidebar != null) sidebar.sizeDelta = new Vector2(width, sidebar.sizeDelta.y);
    }
}
