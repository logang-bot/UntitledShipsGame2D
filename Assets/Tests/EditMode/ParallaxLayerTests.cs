using NUnit.Framework;

public class ParallaxLayerTests
{
    private const float TileHeight = 10f;

    [TestCase(0f, 0f)]
    [TestCase(3.5f, 3.5f)]
    [TestCase(10f, 0f)]
    [TestCase(27f, 7f)]
    public void WrappedOffset_StaysWithinOneTile(float scrolled, float expected)
    {
        Assert.AreEqual(expected, ParallaxLayer.WrappedOffset(scrolled, TileHeight), 1e-4f);
    }
}
