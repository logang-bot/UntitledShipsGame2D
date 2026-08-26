using UnityEngine;

[ExecuteAlways] // Runs in Edit mode too, so Game view previews the pillarbox without pressing Play
[RequireComponent(typeof(Camera))]
public class AspectRatioFitter : MonoBehaviour
{
    [Tooltip("Target width:height ratio, e.g. 9/16 for a 9:16 portrait game area")]
    public float targetAspectWidth = 9f;
    public float targetAspectHeight = 16f;

    private Camera cam;
    private int lastScreenWidth;
    private int lastScreenHeight;

    void Awake()
    {
        cam = GetComponent<Camera>();
        ApplyPillarbox();
    }

    void Update()
    {
        // Re-apply if the window is resized (PC windowed mode) or orientation
        // changes (mobile). Cheap check, not a per-frame recalculation.
        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
        {
            ApplyPillarbox();
        }
    }

    void ApplyPillarbox()
    {
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;

        float targetAspect = targetAspectWidth / targetAspectHeight;
        float windowAspect = (float)Screen.width / Screen.height;
        float scaleHeight = windowAspect / targetAspect;

        Rect rect = cam.rect;

        if (scaleHeight < 1.0f)
        {
            // Screen is narrower/taller than target - letterbox top/bottom
            rect.width = 1.0f;
            rect.height = scaleHeight;
            rect.x = 0f;
            rect.y = (1.0f - scaleHeight) / 2.0f;
        }
        else
        {
            // Screen is wider than target - pillarbox left/right (this is the
            // PC case: portrait game centered, extra space on both sides for HUD)
            float scaleWidth = 1.0f / scaleHeight;
            rect.width = scaleWidth;
            rect.height = 1.0f;
            rect.x = (1.0f - scaleWidth) / 2.0f;
            rect.y = 0f;
        }

        // Snap to whole pixels. Camera.rect drives the GPU viewport, which
        // Unity rounds to integer pixels internally, but HUDSidebarFitter
        // reads this same rect back as unrounded float pixels
        // (GetViewportPixelRect()) to size the HUD sidebars. At most screen
        // sizes the pillarbox math lands on a fractional pixel (e.g. at
        // 1920x1080 the boundary is 656.25px) - left unrounded, the two
        // independent roundings (GPU viewport vs. UI RectTransform) can
        // disagree by a sub-pixel sliver, visible as a thin seam at the
        // sidebar/viewport edge. Rounding here guarantees both read the
        // exact same integer boundary, with nothing left to diverge on.
        rect.x = Mathf.Round(rect.x * Screen.width) / Screen.width;
        rect.y = Mathf.Round(rect.y * Screen.height) / Screen.height;
        rect.width = Mathf.Round(rect.width * Screen.width) / Screen.width;
        rect.height = Mathf.Round(rect.height * Screen.height) / Screen.height;

        cam.rect = rect;
    }

    // Exposes the current gameplay viewport in screen pixels, useful for
    // positioning HUD elements precisely relative to the pillarbox edges.
    public Rect GetViewportPixelRect()
    {
        return new Rect(
            cam.rect.x * Screen.width,
            cam.rect.y * Screen.height,
            cam.rect.width * Screen.width,
            cam.rect.height * Screen.height
        );
    }
}