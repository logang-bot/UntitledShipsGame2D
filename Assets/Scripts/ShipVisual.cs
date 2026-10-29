using UnityEngine;

// Shows the ship's role as its 3D model (docs/art-roadmap.md's "Source
// designs"): instantiates roleVisuals[role] under visualRoot, which holds
// the model's orientation, scale and z offset. Start, not Awake, for the
// same reason as PlayerRoleComponent - a spawned ship's role is assigned
// right after Instantiate. The hit flash tints every model renderer through
// ShipMesh.shader's _FlashColor.
public class ShipVisual : MonoBehaviour
{
    public GameObject[] roleVisuals; // indexed by (int)PlayerRole
    public Transform visualRoot;

    private static readonly int FlashColorId = Shader.PropertyToID("_FlashColor");
    private Renderer[] renderers = new Renderer[0];
    private MaterialPropertyBlock block;

    void Start()
    {
        PlayerRole role = GetComponent<PlayerRoleComponent>().role;
        GameObject model = Instantiate(roleVisuals[(int)role], visualRoot);
        renderers = model.GetComponentsInChildren<Renderer>();
        block = new MaterialPropertyBlock();
    }

    public void SetFlash(Color color) => ApplyFlash(color);

    public void ClearFlash() => ApplyFlash(Color.clear);

    private void ApplyFlash(Color color)
    {
        if (block == null) return;
        block.SetColor(FlashColorId, color);
        foreach (Renderer modelRenderer in renderers) modelRenderer.SetPropertyBlock(block);
    }
}
