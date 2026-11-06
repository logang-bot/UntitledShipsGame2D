using System;
using UnityEngine;

// The three upgrade icons (ico_dmg, ico_crit, ico_multi), set once in the
// Inspector of whichever view draws cards or chips.
[Serializable]
public class UpgradeCardIcons
{
    public Sprite attackDamage;
    public Sprite critStrike;
    public Sprite multishot;

    public Sprite For(UpgradeCard card)
    {
        switch (card)
        {
            case UpgradeCard.AttackDamage: return attackDamage;
            case UpgradeCard.CritStrike: return critStrike;
            default: return multishot;
        }
    }
}
