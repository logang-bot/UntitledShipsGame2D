// One combo press's resulting shot: how hard it hits, how wide its hitbox
// is, and which sprite it wears.
public readonly struct AttackerComboShot
{
    public readonly float DamageMultiplier;
    public readonly float WidthMultiplier;
    public readonly PlayerShotKind Kind;

    public AttackerComboShot(float damageMultiplier, float widthMultiplier, PlayerShotKind kind)
    {
        DamageMultiplier = damageMultiplier;
        WidthMultiplier = widthMultiplier;
        Kind = kind;
    }
}
