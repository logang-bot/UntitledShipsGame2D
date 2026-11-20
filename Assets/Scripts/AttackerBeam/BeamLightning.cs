using UnityEngine;

// The Nova Beam's lightning: jagged LineRenderer arcs crawling up both edges
// of the beam plus short branches crackling off its sides, redrawn every few
// frames so they flicker. Each line's point count is set on the prefab.
public class BeamLightning : MonoBehaviour
{
    private const int AbilityEffectSortingOrder = -1;

    public LineRenderer[] edgeArcs;
    public LineRenderer[] branches;
    public float redrawInterval = 0.05f;
    public float edgeJitter = 0.07f;
    public float branchJitter = 0.05f;
    /// <summary>Chance a branch shows on a given redraw, so they pop in and out.</summary>
    [Range(0f, 1f)] public float branchChance = 0.6f;
    public PaletteSwatch arcColor = new PaletteSwatch(PaletteColor.BeamArc);
    /// <summary>HDR multiplier on the line material, so the bloom catches the arcs.</summary>
    public float arcBrightness = 2.5f;
    /// <summary>Each line fades toward its far end.</summary>
    [Range(0f, 1f)] public float tipAlpha = 0.35f;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private MaterialPropertyBlock arcBlock;
    private float width;
    private float length;
    private float nextRedraw;

    void Awake()
    {
        arcBlock = new MaterialPropertyBlock();
        arcBlock.SetColor(BaseColorId, arcColor.Resolve() * arcBrightness);
        foreach (LineRenderer line in edgeArcs) Paint(line);
        foreach (LineRenderer line in branches) Paint(line);
    }

    public void Fit(float beamWidth, float beamLength)
    {
        width = beamWidth;
        length = beamLength;
    }

    void Update()
    {
        if (Time.time < nextRedraw) return;
        nextRedraw = Time.time + redrawInterval;
        for (int i = 0; i < edgeArcs.Length; i++) DrawEdgeArc(edgeArcs[i], i % 2 == 0 ? -1f : 1f);
        foreach (LineRenderer branch in branches) DrawBranch(branch);
    }

    private void Paint(LineRenderer line)
    {
        line.useWorldSpace = false;
        line.sortingOrder = AbilityEffectSortingOrder;
        line.startColor = NeonPalette.Get(PaletteColor.White);
        line.endColor = NeonPalette.Get(PaletteColor.White, tipAlpha);
        line.SetPropertyBlock(arcBlock);
    }

    private void DrawEdgeArc(LineRenderer line, float side)
    {
        Vector3 bottom = new Vector3(side * width * 0.5f, 0f, 0f);
        DrawJagged(line, new JaggedSpan(bottom, bottom + Vector3.up * length), edgeJitter);
    }

    private void DrawBranch(LineRenderer line)
    {
        line.enabled = Random.value < branchChance;
        if (!line.enabled) return;
        float side = Random.value < 0.5f ? -1f : 1f;
        Vector3 root = new Vector3(side * width * 0.5f, Random.Range(0.1f, length), 0f);
        Vector3 tip = root + new Vector3(side * Random.Range(0.15f, 0.5f), Random.Range(-0.15f, 0.45f), 0f);
        DrawJagged(line, new JaggedSpan(root, tip), branchJitter);
    }

    /// <summary>
    /// Spaces the line's points evenly from one end to the other and pushes
    /// every inner point sideways by a random amount up to jitter.
    /// </summary>
    private static void DrawJagged(LineRenderer line, JaggedSpan span, float jitter)
    {
        int last = line.positionCount - 1;
        Vector3 side = Vector3.Cross(span.to - span.from, Vector3.forward).normalized;
        for (int i = 0; i <= last; i++)
        {
            float offset = i == 0 || i == last ? 0f : Random.Range(-jitter, jitter);
            line.SetPosition(i, Vector3.Lerp(span.from, span.to, (float)i / last) + side * offset);
        }
    }
}
