using System.Collections.Generic;
using NUnit.Framework;

public class SquadLineupTests
{
    [TearDown]
    public void ResetCarriers()
    {
        CoOpRoster.Players = null;
        PartyRoleAssignment.HumanRole = null;
    }

    [Test]
    public void FromLocks_MapsEachLockedRoleToItsPlayerNumber()
    {
        int?[] lineup = SquadLineup.FromLocks(new PlayerRole?[] { PlayerRole.Tank, null, PlayerRole.Support });
        Assert.AreEqual(new int?[] { null, 1, null, 3 }, lineup);
    }

    [Test]
    public void Current_SinglePlayer_IsP1OnTheirRole()
    {
        PartyRoleAssignment.HumanRole = PlayerRole.Medic;
        Assert.AreEqual(new int?[] { null, null, 1, null }, SquadLineup.Current());
    }

    [Test]
    public void Current_CoOpRoster_UsesRosterRoles()
    {
        CoOpRoster.Players = new List<JoinedPlayer> { new JoinedPlayer { role = PlayerRole.Attacker }, new JoinedPlayer { role = PlayerRole.Tank } };
        Assert.AreEqual(new int?[] { 1, 2, null, null }, SquadLineup.Current());
    }
}
