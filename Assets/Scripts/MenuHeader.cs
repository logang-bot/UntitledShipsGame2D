using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The top bar shared by screens 02-05: the NEON FLEET label and the five
// step crumbs (MAIN, MODE, JOIN, ROLE, LEVEL). The current step is bright
// with a cyan underline, earlier steps are dimmed, later ones darker still.
[ExecuteAlways]
public class MenuHeader : MonoBehaviour
{
    [Range(0, 4)] public int stepIndex;
    public TMP_Text[] crumbs;
    public Image[] underlines;

    void OnEnable() => Apply();

    void OnValidate() => Apply();

    public void Apply()
    {
        if (crumbs == null) return;
        for (int i = 0; i < crumbs.Length; i++)
            ApplyCrumb(i);
    }

    private void ApplyCrumb(int i)
    {
        if (crumbs[i] != null) crumbs[i].color = NeonPalette.Get(CrumbToken(i));
        if (underlines == null || i >= underlines.Length || underlines[i] == null) return;
        underlines[i].color = NeonPalette.Get(PaletteColor.Tank, i == stepIndex ? 1f : 0f);
    }

    private PaletteColor CrumbToken(int i)
    {
        if (i == stepIndex) return PaletteColor.Text;
        return i < stepIndex ? PaletteColor.Text4 : PaletteColor.Muted;
    }
}
