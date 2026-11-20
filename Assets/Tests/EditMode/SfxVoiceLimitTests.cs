using NUnit.Framework;
using UnityEngine;

public class SfxVoiceLimitTests
{
    private const float ClipSeconds = 0.5f;
    private const float PastTheGap = SfxVoiceLimit.MinGapSeconds * 1.5f;
    private AudioClip clip;
    private AudioClip other;

    [SetUp]
    public void SetUp()
    {
        SfxVoiceLimit.Reset();
        clip = AudioClip.Create("clip", Mathf.RoundToInt(44100 * ClipSeconds), 1, 44100, false);
        other = AudioClip.Create("other", Mathf.RoundToInt(44100 * ClipSeconds), 1, 44100, false);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(clip);
        Object.DestroyImmediate(other);
    }

    [Test]
    public void TryClaim_SameFrameRepeat_IsRefused()
    {
        Assert.IsTrue(SfxVoiceLimit.TryClaim(clip, 1f));
        Assert.IsFalse(SfxVoiceLimit.TryClaim(clip, 1f));
    }

    [Test]
    public void TryClaim_AfterTheGap_IsAllowed()
    {
        SfxVoiceLimit.TryClaim(clip, 1f);
        Assert.IsTrue(SfxVoiceLimit.TryClaim(clip, 1f + PastTheGap));
    }

    [Test]
    public void TryClaim_OneMoreThanMaxVoices_IsRefused()
    {
        float t = 1f;
        for (int i = 0; i < SfxVoiceLimit.MaxVoices; i++, t += PastTheGap)
            Assert.IsTrue(SfxVoiceLimit.TryClaim(clip, t));
        Assert.IsFalse(SfxVoiceLimit.TryClaim(clip, t));
    }

    [Test]
    public void TryClaim_OnceTheFirstCopyEnds_IsAllowedAgain()
    {
        float t = 1f;
        for (int i = 0; i < SfxVoiceLimit.MaxVoices; i++, t += PastTheGap)
            SfxVoiceLimit.TryClaim(clip, t);
        Assert.IsTrue(SfxVoiceLimit.TryClaim(clip, 1f + ClipSeconds));
    }

    [Test]
    public void TryClaim_DifferentClips_AreIndependent()
    {
        SfxVoiceLimit.TryClaim(clip, 1f);
        Assert.IsTrue(SfxVoiceLimit.TryClaim(other, 1f));
    }

    [Test]
    public void TryClaim_NullClip_IsRefused()
    {
        Assert.IsFalse(SfxVoiceLimit.TryClaim(null, 1f));
    }

    [Test]
    public void Reset_ForgetsPlayingCopies()
    {
        SfxVoiceLimit.TryClaim(clip, 5f);
        SfxVoiceLimit.Reset();
        Assert.IsTrue(SfxVoiceLimit.TryClaim(clip, 0f));
    }
}
