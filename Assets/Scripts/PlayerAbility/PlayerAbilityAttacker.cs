using UnityEngine;

public class PlayerAbilityAttacker
{
    private readonly PlayerAbility ability;

    public PlayerAbilityAttacker(PlayerAbility ability)
    {
        this.ability = ability;
    }

    public void Trigger()
    {
        float damage = ability.PlayerController.fireDamage * ability.bigShotDamageMultiplier;
        ability.PlayerController.FireBigShot(ability.bigShotWidthMultiplier, damage);
        ability.PlayerController.AddRecoil(Vector2.down * ability.recoilForce);
    }
}
