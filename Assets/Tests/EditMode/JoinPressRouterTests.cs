using NUnit.Framework;

public class JoinPressRouterTests
{
    [TestCase(JoinPressKind.Other)]
    [TestCase(JoinPressKind.Continue)]
    public void NewDevice_AnyNonBackPress_Joins(JoinPressKind kind)
    {
        Assert.AreEqual(JoinPressAction.Join, JoinPressRouter.Route(kind, false, true));
    }

    [Test]
    public void JoinedDevice_Back_Leaves()
    {
        Assert.AreEqual(JoinPressAction.Leave, JoinPressRouter.Route(JoinPressKind.Back, true, true));
    }

    [Test]
    public void NobodyJoined_Back_GoesBack()
    {
        Assert.AreEqual(JoinPressAction.Back, JoinPressRouter.Route(JoinPressKind.Back, false, false));
    }

    [Test]
    public void NewDevice_BackWhileOthersJoined_DoesNothing()
    {
        Assert.AreEqual(JoinPressAction.None, JoinPressRouter.Route(JoinPressKind.Back, false, true));
    }

    [Test]
    public void JoinedDevice_Continue_Continues()
    {
        Assert.AreEqual(JoinPressAction.Continue, JoinPressRouter.Route(JoinPressKind.Continue, true, true));
    }

    [Test]
    public void JoinedDevice_OtherPress_DoesNothing()
    {
        Assert.AreEqual(JoinPressAction.None, JoinPressRouter.Route(JoinPressKind.Other, true, true));
    }
}
