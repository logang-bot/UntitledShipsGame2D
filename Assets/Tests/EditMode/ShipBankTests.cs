using NUnit.Framework;

public class ShipBankTests
{
    [Test]
    public void BankAngle_MovingRight_BanksNegative()
    {
        Assert.Less(ShipBank.BankAngle(1f, 3f, 35f), 0f);
    }

    [Test]
    public void BankAngle_MovingLeft_BanksPositive()
    {
        Assert.Greater(ShipBank.BankAngle(-1f, 3f, 35f), 0f);
    }

    [Test]
    public void BankAngle_Still_IsLevel()
    {
        Assert.AreEqual(0f, ShipBank.BankAngle(0f, 3f, 35f));
    }

    [Test]
    public void BankAngle_FasterThanFullBankSpeed_ClampsToMax()
    {
        Assert.AreEqual(-35f, ShipBank.BankAngle(10f, 3f, 35f));
        Assert.AreEqual(35f, ShipBank.BankAngle(-10f, 3f, 35f));
    }

    [Test]
    public void BankAngle_HalfOfFullBankSpeed_GivesHalfAngle()
    {
        Assert.AreEqual(-17.5f, ShipBank.BankAngle(1.5f, 3f, 35f), 0.0001f);
    }
}
