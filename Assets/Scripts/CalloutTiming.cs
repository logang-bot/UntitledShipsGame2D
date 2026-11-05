// A callout's fade in, hold and fade out, in seconds.
public struct CalloutTiming
{
    public float fadeIn;
    public float hold;
    public float fadeOut;

    public CalloutTiming(float fadeIn, float hold, float fadeOut)
    {
        this.fadeIn = fadeIn;
        this.hold = hold;
        this.fadeOut = fadeOut;
    }
}
