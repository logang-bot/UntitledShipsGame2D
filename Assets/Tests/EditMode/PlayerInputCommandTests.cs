using NUnit.Framework;
using UnityEngine;

public class PlayerInputCommandTests
{
    [Test]
    public void ToJson_FromJson_RoundTrip_PreservesMoveDirection()
    {
        var command = new PlayerInputCommand { moveDirection = new Vector2(0.5f, -1f) };

        PlayerInputCommand result = PlayerInputCommand.FromJson(command.ToJson());

        Assert.AreEqual(command.moveDirection, result.moveDirection);
    }

    [Test]
    public void ToJson_FromJson_RoundTrip_PreservesFiringFlag()
    {
        var command = new PlayerInputCommand { firing = true };

        PlayerInputCommand result = PlayerInputCommand.FromJson(command.ToJson());

        Assert.IsTrue(result.firing);
    }

    [Test]
    public void ToJson_FromJson_RoundTrip_PreservesAbilityAndComboFlags()
    {
        var command = new PlayerInputCommand
        {
            abilityPressed = true,
            combo1Pressed = true,
            combo3Pressed = true
        };

        PlayerInputCommand result = PlayerInputCommand.FromJson(command.ToJson());

        Assert.IsTrue(result.abilityPressed);
        Assert.IsTrue(result.combo1Pressed);
        Assert.IsFalse(result.combo2Pressed);
        Assert.IsTrue(result.combo3Pressed);
    }
}
