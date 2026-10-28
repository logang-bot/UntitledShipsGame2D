using NUnit.Framework;

public class LevelSetupTests
{
    private static BossEntry[] Entries()
    {
        return new[]
        {
            new BossEntry { level = Level.Level1 },
            new BossEntry { level = Level.Level2 },
            new BossEntry { level = Level.Level3 },
        };
    }

    [TestCase(Level.Level1)]
    [TestCase(Level.Level2)]
    [TestCase(Level.Level3)]
    public void PickEntry_ReturnsTheEntryForThatLevel(Level level)
    {
        Assert.AreEqual(level, LevelSetup.PickEntry(Entries(), level).level);
    }

    [Test]
    public void PickEntry_MissingLevel_ReturnsNull()
    {
        BossEntry[] onlyLevel1 = { new BossEntry { level = Level.Level1 } };
        Assert.IsNull(LevelSetup.PickEntry(onlyLevel1, Level.Level3));
    }
}
