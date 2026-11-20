using UnityEngine;

// One role's cut-in card content: the pilot's drawn frames (Assets/Art/CutIns/<Role>/,
// in CutInFrame order), the voice line played on the press
// (Assets/Audio/Voice/<Role>/, optional) and the card's kicker and title. A
// role without an entry on CutInCardView shows no card.
[System.Serializable]
public class CutInRoleArt
{
    public PlayerRole role;
    public Sprite[] frames;
    public AudioClip voice;
    public string kicker;
    public string title;
}
