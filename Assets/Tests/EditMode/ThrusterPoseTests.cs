using NUnit.Framework;
using UnityEngine;

public class ThrusterPoseTests
{
    [Test]
    public void From_AtCruiseWithNoFlicker_IsTheModelledPlume()
    {
        ThrusterPose pose = ThrusterPose.From(0f, 0f);

        Assert.AreEqual(1f, pose.length, 1e-5f);
        Assert.AreEqual(1f, pose.width, 1e-5f);
        Assert.AreEqual(1f, pose.coreLength, 1e-5f);
        Assert.AreEqual(0.32f, pose.outerOpacity, 1e-5f);
        Assert.AreEqual(0.18f, pose.coreOpacity, 1e-5f);
        Assert.AreEqual(1f, pose.glowFactor, 1e-5f);
    }

    [Test]
    public void From_AtFullBoost_IsLongerWiderAndBrighter()
    {
        ThrusterPose pose = ThrusterPose.From(0f, 1f);

        Assert.AreEqual(2.4f, pose.length, 1e-5f);
        Assert.AreEqual(1.18f, pose.width, 1e-5f);
        Assert.AreEqual(1.5f, pose.coreLength, 1e-5f);
        Assert.AreEqual(0.62f, pose.outerOpacity, 1e-5f);
        Assert.AreEqual(0.68f, pose.coreOpacity, 1e-5f);
        Assert.AreEqual(2.2f, pose.glowFactor, 1e-5f);
    }

    [Test]
    public void From_FlickerSwingsLengthMoreAtBoost()
    {
        float cruiseSwing = ThrusterPose.From(1f, 0f).length - ThrusterPose.From(0f, 0f).length;
        float boostSwing = ThrusterPose.From(1f, 1f).length - ThrusterPose.From(0f, 1f).length;

        Assert.AreEqual(0.14f, cruiseSwing, 1e-5f);
        Assert.AreEqual(0.32f, boostSwing, 1e-5f);
    }

    [Test]
    public void Flicker_StaysWithinTheTwoWavesAmplitude()
    {
        for (float t = 0f; t < 2f; t += 0.013f)
            Assert.LessOrEqual(Mathf.Abs(ThrusterPose.Flicker(t, 1.7f, 0.5f)), 0.8f + 1e-5f);
    }

    [Test]
    public void Flicker_DiffersBetweenEngineSeeds()
    {
        Assert.AreNotEqual(ThrusterPose.Flicker(0.4f, 0f, 0f), ThrusterPose.Flicker(0.4f, 1.7f, 0f));
    }
}
