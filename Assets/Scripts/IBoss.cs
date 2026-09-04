using UnityEngine;

// Shared contract between MarauderBoss and HalcyonBoss (and future bosses)
// so LevelSequencer/PlayerController can drive whichever boss a level uses
// without knowing its concrete type. A deliberate, scoped exception to this
// project's "no interfaces" convention - see docs/architecture.md and
// docs/superpowers/specs/2026-09-04-halcyon-boss-design.md.
public interface IBoss
{
    void SetVisible(bool visible);
    void ApplyContactDamage(GameObject ship);
}
