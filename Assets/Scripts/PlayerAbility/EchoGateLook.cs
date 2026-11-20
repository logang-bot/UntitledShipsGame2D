using UnityEngine;

// How the Echo Gate is drawn: the Combat FX frames when they're in
// ShipCombatFx, otherwise a plain bar in the Support's color. Also the
// little spark where a shot splits.
public class EchoGateLook
{
    private const float ArtSpanPx = 461f;
    private const float SparkSeconds = 0.12f;
    private const int SparkSortingOrder = -3;

    private readonly PlayerAbility support;
    private readonly SpriteRenderer bar;

    public EchoGateLook(PlayerAbility support, SpriteRenderer bar)
    {
        this.support = support;
        this.bar = bar;
    }

    private ShipCombatFx Fx => support.Fx;
    private SupportEchoGateSettings Settings => support.supportPowers;
    private bool HasArt => Fx != null && Fx.HasEchoGateArt;

    public void Show(ZoneFrame frame, float scale)
    {
        if (HasArt) ShowArt(Fx.echoGateFrames[(int)frame], scale);
        else ShowPlaceholder(frame, scale);
    }

    public void ShowShatter()
    {
        if (HasArt && Fx.echoGateShatter != null) ShowArt(Fx.echoGateShatter, 1f);
        else bar.color = WithAlpha(support.TintColor, 0.25f);
    }

    public void Hide() => bar.enabled = false;

    public void PlaySpark(Vector3 at)
    {
        if (Fx == null || Fx.echoSplitSpark == null) return;
        SpriteRenderer spark = FxSprites.CreateChild(null, "EchoSplitSpark", SparkSortingOrder);
        spark.sprite = Fx.echoSplitSpark;
        spark.transform.position = at;
        Object.Destroy(spark.gameObject, SparkSeconds);
    }

    /// <summary>The frames are drawn with ArtSpanPx between the end nodes, at FxSprites.PixelsPerUnit.</summary>
    private void ShowArt(Sprite sprite, float scale)
    {
        float fit = Settings.width / (ArtSpanPx / FxSprites.PixelsPerUnit);
        bar.sprite = sprite;
        bar.color = NeonPalette.Get(PaletteColor.White, 1f);
        bar.transform.localScale = new Vector3(fit * scale, fit, 1f);
    }

    private void ShowPlaceholder(ZoneFrame frame, float scale)
    {
        bar.sprite = FxSprites.WhiteUnitSprite;
        bar.color = WithAlpha(support.TintColor, PlaceholderAlpha(frame));
        bar.transform.localScale = new Vector3(Settings.width * scale, Settings.thickness, 1f);
    }

    private static float PlaceholderAlpha(ZoneFrame frame) =>
        frame == ZoneFrame.Ending ? 0.3f : frame == ZoneFrame.Pulse ? 0.9f : 0.7f;

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }
}
