using NUnit.Framework;
using UnityEngine;

// Pure-logic tests for WardenArm.PickWeighted - the random draw is injected
// as a parameter (not read from Random.value internally), so these are
// deterministic, same testing shape as ShipCollisionUtilTests.
public class WardenArmTargetingTests
{
    private GameObject a, b, c;

    [SetUp]
    public void SetUp()
    {
        a = new GameObject("A");
        b = new GameObject("B");
        c = new GameObject("C");
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(a);
        Object.DestroyImmediate(b);
        Object.DestroyImmediate(c);
    }

    [Test]
    public void PickWeighted_NoTaunt_UniformDraw_PicksExpectedShip()
    {
        GameObject[] ships = { a, b, c };
        WardenArm.TauntBias bias = new WardenArm.TauntBias(null, false, 3f);

        // 3 equal-weight candidates (total=3): draw 0.5 -> target 1.5 -> falls in b's [1,2) slot.
        GameObject result = WardenArm.PickWeighted(ships, bias, 0.5f);

        Assert.AreSame(b, result);
    }

    [Test]
    public void PickWeighted_SkipsInactiveShips()
    {
        b.SetActive(false);
        GameObject[] ships = { a, b, c };
        WardenArm.TauntBias bias = new WardenArm.TauntBias(null, false, 3f);

        // Only a, c remain (total=2): draw 0.9 -> target 1.8 -> falls in c's [1,2) slot.
        GameObject result = WardenArm.PickWeighted(ships, bias, 0.9f);

        Assert.AreSame(c, result);
    }

    [Test]
    public void PickWeighted_TauntActive_WeightsTauntedShipHeavily()
    {
        GameObject[] ships = { a, b, c };
        // Under equal weights (total=3), draw 0.5 gives target 1.5 -> b.
        // Under taunt bias (a=3, b=1, c=1; total=5), same draw 0.5 gives target 2.5 -> a.
        WardenArm.TauntBias bias = new WardenArm.TauntBias(a, true, 3f);

        GameObject result = WardenArm.PickWeighted(ships, bias, 0.5f);

        Assert.AreSame(a, result);
    }

    [Test]
    public void PickWeighted_TauntWindowExpired_FallsBackToUniform()
    {
        GameObject[] ships = { a, b, c };
        WardenArm.TauntBias bias = new WardenArm.TauntBias(a, false, 3f); // Active=false

        GameObject result = WardenArm.PickWeighted(ships, bias, 0.5f);

        Assert.AreSame(b, result); // same as the no-taunt uniform case
    }

    [Test]
    public void PickWeighted_NoLivingShips_ReturnsNull()
    {
        a.SetActive(false); b.SetActive(false); c.SetActive(false);
        GameObject[] ships = { a, b, c };
        WardenArm.TauntBias bias = new WardenArm.TauntBias(null, false, 3f);

        Assert.IsNull(WardenArm.PickWeighted(ships, bias, 0.5f));
    }
}
