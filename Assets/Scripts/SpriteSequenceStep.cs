using UnityEngine;

// One frame of a SpriteSequence. A hold step lasts the sequence's
// holdSeconds instead of its own duration (an ability's sustain frame).
[System.Serializable]
public struct SpriteSequenceStep
{
    public Sprite sprite;
    public float duration;
    public bool hold;
}
