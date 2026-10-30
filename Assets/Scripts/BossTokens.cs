// One boss's color family as palette tokens (design: HUD Boss Panel): its
// signature color, a dim stroke, a dark panel fill, and its glow alpha.
// Tokens rather than Colors so NeonFrameLooks can be built from them.
public struct BossTokens
{
    public PaletteColor main;
    public PaletteColor dim;
    public PaletteColor bg;
    public float glowAlpha;
}
