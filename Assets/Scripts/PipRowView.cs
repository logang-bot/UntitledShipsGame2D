using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// A row of square pips on a party frame: HP (14 px white pips, amber when
// low) or shield (9 px blue segments, a fixed width per point so a 20-point
// Tank shield reads longer than a 3-point Medic one). The pip count follows
// the ship's max, which varies by role and is only applied once the ship's
// role is set up, so pips are cloned from a template whenever it changes.
public class PipRowView : MonoBehaviour
{
    public Image pipTemplate;
    public Sprite filledSprite;
    public Sprite emptySprite;
    public PaletteSwatch filledColor = new PaletteSwatch(PaletteColor.Text);
    public PaletteSwatch lowColor = new PaletteSwatch(PaletteColor.Notice);
    public PaletteSwatch emptyColor = new PaletteSwatch(PaletteColor.Slate);

    private readonly List<Image> pips = new List<Image>();
    private int shown = -1;

    public void Show(int filled, int max, bool low)
    {
        if (max != shown) Resize(max);
        for (int i = 0; i < max; i++) Paint(pips[i], i < filled, low);
    }

    private void Resize(int max)
    {
        pipTemplate.gameObject.SetActive(false);
        while (pips.Count < max) pips.Add(Spawn());
        for (int i = 0; i < pips.Count; i++) pips[i].gameObject.SetActive(i < max);
        shown = max;
    }

    private Image Spawn()
    {
        Image pip = Instantiate(pipTemplate, transform);
        pip.gameObject.SetActive(true);
        return pip;
    }

    private void Paint(Image pip, bool filled, bool low)
    {
        pip.sprite = filled ? filledSprite : emptySprite;
        pip.color = filled ? (low ? lowColor : filledColor) : emptyColor;
    }
}
