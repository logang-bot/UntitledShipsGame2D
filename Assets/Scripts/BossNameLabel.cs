using TMPro;
using UnityEngine;

// A text that names the level's boss once it spawns - the overlays'
// "MARAUDER · RAID · 01" kicker and "MARAUDER DEFEATED" title. `format`
// gets {0} = boss name, {1} = fight kind. Bound by LevelSetup's onSpawned.
public class BossNameLabel : MonoBehaviour
{
    public TMP_Text label;
    public string format = "{0}";
    public bool tintToBoss;

    public void Bind(GameObject bossObject)
    {
        Level level = BossEvents.LevelOf(bossObject.GetComponent<IBoss>() as MonoBehaviour);
        label.text = string.Format(format, LevelCardText.BossName(level), BossPalette.Kind(level));
        if (tintToBoss) label.color = NeonPalette.Get(BossPalette.For(level).main);
    }
}
