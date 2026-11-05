using NUnit.Framework;

public class MenuEaseTests
{
    [TestCase(0f, 0f)]
    [TestCase(1f, 1f)]
    [TestCase(0.5f, 0.125f)]
    public void In_IsCubic(float t, float expected)
    {
        Assert.AreEqual(expected, MenuEase.In(t), 1e-5f);
    }

    [TestCase(0f, 0f)]
    [TestCase(1f, 1f)]
    [TestCase(0.5f, 0.875f)]
    public void Out_MirrorsIn(float t, float expected)
    {
        Assert.AreEqual(expected, MenuEase.Out(t), 1e-5f);
    }

    [Test]
    public void Out_IsAheadOfIn_MidTween()
    {
        Assert.Greater(MenuEase.Out(0.3f), MenuEase.In(0.3f));
    }
}
