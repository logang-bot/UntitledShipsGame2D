using NUnit.Framework;

public class ThrustBoostTests
{
    [Test]
    public void Step_RisesFasterThanItFalls()
    {
        ThrustBoost rising = new ThrustBoost();
        rising.Step(1f, 0.1f);

        ThrustBoost falling = new ThrustBoost();
        for (int i = 0; i < 200; i++) falling.Step(1f, 0.1f);
        falling.Step(0f, 0.1f);

        Assert.Greater(rising.Current, 1f - falling.Current);
    }

    [Test]
    public void Step_IsFrameRateIndependent()
    {
        ThrustBoost oneStep = new ThrustBoost();
        oneStep.Step(1f, 0.1f);

        ThrustBoost tenSteps = new ThrustBoost();
        for (int i = 0; i < 10; i++) tenSteps.Step(1f, 0.01f);

        Assert.AreEqual(oneStep.Current, tenSteps.Current, 1e-4f);
    }

    [Test]
    public void Step_CapsAHitchAtOneTenthOfASecond()
    {
        ThrustBoost hitch = new ThrustBoost();
        hitch.Step(1f, 2f);

        ThrustBoost capped = new ThrustBoost();
        capped.Step(1f, 0.1f);

        Assert.AreEqual(capped.Current, hitch.Current, 1e-6f);
    }

    [Test]
    public void Step_SettlesOnTheTarget()
    {
        ThrustBoost boost = new ThrustBoost();
        for (int i = 0; i < 200; i++) boost.Step(1f, 0.05f);

        Assert.AreEqual(1f, boost.Current, 1e-4f);
    }
}
