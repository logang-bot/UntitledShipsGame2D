using NUnit.Framework;
using UnityEngine;

public class SfxCueTests
{
    private AudioClip a;
    private AudioClip b;

    [SetUp]
    public void SetUp()
    {
        a = AudioClip.Create("a", 441, 1, 44100, false);
        b = AudioClip.Create("b", 441, 1, 44100, false);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(a);
        Object.DestroyImmediate(b);
    }

    [Test]
    public void Pick_WithNoClips_IsNull()
    {
        Assert.IsNull(new SfxCue().Pick());
        Assert.IsNull(new SfxCue { clips = new AudioClip[0] }.Pick());
    }

    [Test]
    public void Pick_WithOneClip_IsThatClip()
    {
        Assert.AreSame(a, new SfxCue { clips = new[] { a } }.Pick());
    }

    [Test]
    public void Pick_WithVariants_IsAlwaysOneOfThem()
    {
        SfxCue cue = new SfxCue { clips = new[] { a, b } };
        for (int i = 0; i < 20; i++) CollectionAssert.Contains(cue.clips, cue.Pick());
    }

    [Test]
    public void Volume_DefaultsToFull()
    {
        Assert.AreEqual(1f, new SfxCue().volume);
    }
}
