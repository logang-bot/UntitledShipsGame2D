using UnityEngine;

[ExecuteAlways] // Runs in Edit mode too, so Game view previews sidebar sizing without pressing Play
public class HUDSidebarFitter : MonoBehaviour
{
    public AspectRatioFitter aspectFitter; // drag the Main Camera here
    public RectTransform leftSidebar;
    public RectTransform rightSidebar;

    private int lastScreenWidth;
    private int lastScreenHeight;

    void Update()
    {
        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
        {
            ApplySidebarSizes();
        }
    }

    void ApplySidebarSizes()
    {
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;

        Rect gameViewport = aspectFitter.GetViewportPixelRect();

        float leftBarWidth = gameViewport.x;
        float rightBarWidth = Screen.width - (gameViewport.x + gameViewport.width);

        if (leftSidebar != null)
        {
            leftSidebar.sizeDelta = new Vector2(leftBarWidth, leftSidebar.sizeDelta.y);
        }

        if (rightSidebar != null)
        {
            rightSidebar.sizeDelta = new Vector2(rightBarWidth, rightSidebar.sizeDelta.y);
        }
    }
}