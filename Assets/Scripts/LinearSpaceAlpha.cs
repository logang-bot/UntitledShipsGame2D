using UnityEngine;

// The project renders in Linear color space, so UI alpha blends in linear
// light, while the designs' alphas assume sRGB blending (a browser). Converts
// a design alpha into the alpha that looks the same in game. Without it a
// design's 86% black backdrop only darkens white to about 41% on screen.
public static class LinearSpaceAlpha
{
    private const float Gamma = 2.2f;

    /// <summary>
    /// Alpha for a black overlay. In sRGB, black at alpha a leaves c·(1−a);
    /// in linear that is (c·(1−a))^γ = c_lin·(1−a)^γ, so the linear alpha is
    /// 1 − (1−a)^γ. Identity when the project is in Gamma space.
    /// </summary>
    public static float ForBlackOverlay(float designAlpha)
    {
        if (QualitySettings.activeColorSpace != ColorSpace.Linear) return designAlpha;
        return 1f - Mathf.Pow(1f - designAlpha, Gamma);
    }
}
