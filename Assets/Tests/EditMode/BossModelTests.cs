using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

// Builds a tiny stand-in for a boss model: a root with one hidden part
// that renders with a material named "boss_glow". BossModel indexes lazily,
// so no Awake call is needed.
public class BossModelTests
{
    private readonly List<Object> created = new List<Object>();

    private BossModel CreateModel()
    {
        GameObject root = new GameObject("TestBossModel");
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Quad);
        part.name = "boss_part";
        part.transform.SetParent(root.transform);
        part.SetActive(false);
        Material glow = new Material(Shader.Find("UntitledShips/ShipMesh")) { name = "boss_glow" };
        part.GetComponent<Renderer>().sharedMaterial = glow;
        created.Add(root);
        created.Add(glow);
        return root.AddComponent<BossModel>();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (Object item in created)
            if (item != null) Object.DestroyImmediate(item);
        created.Clear();
    }

    [Test]
    public void Node_FindsInactivePartsByName()
    {
        BossModel model = CreateModel();

        Assert.AreEqual("boss_part", model.Node("boss_part").name);
    }

    [Test]
    public void Show_TurnsAPartOnAndOff()
    {
        BossModel model = CreateModel();

        model.Show("boss_part", true);
        Assert.IsTrue(model.Node("boss_part").gameObject.activeSelf);
        model.Show("boss_part", false);
        Assert.IsFalse(model.Node("boss_part").gameObject.activeSelf);
    }

    [Test]
    public void Glow_SetsScaledEmissionOnRenderersUsingTheMaterial()
    {
        BossModel model = CreateModel();
        Renderer part = model.Node("boss_part").GetComponent<Renderer>();

        model.Glow("boss_glow", PaletteColor.White, 2f);

        MaterialPropertyBlock block = new MaterialPropertyBlock();
        part.GetPropertyBlock(block);
        Assert.AreEqual(2f * BossModel.GlowScale, block.GetColor("_EmissionColor").r, 0.001f);
    }

    [Test]
    public void ResetGlow_RestoresTheMaterialsEmission()
    {
        BossModel model = CreateModel();
        Renderer part = model.Node("boss_part").GetComponent<Renderer>();
        model.Glow("boss_glow", PaletteColor.White, 2f);

        model.ResetGlow("boss_glow");

        Assert.AreEqual(Emission(model.Node("boss_part").GetComponent<Renderer>()), Emission(part.sharedMaterial), 0.001f);
    }

    [Test]
    public void ResetGlow_KeepsAHitFlash()
    {
        BossModel model = CreateModel();
        BossHitFlash flash = model.gameObject.AddComponent<BossHitFlash>();
        flash.model = model;
        flash.node = "boss_part";
        flash.Flash();

        model.ResetGlow("boss_glow");

        Assert.Greater(BlockColor(model.Node("boss_part").GetComponent<Renderer>(), "_FlashColor").a, 0f);
    }

    [Test]
    public void HitFlash_Flash_SetsTheFlashColorOnTheNodesRenderers()
    {
        BossModel model = CreateModel();
        BossHitFlash flash = model.gameObject.AddComponent<BossHitFlash>();
        flash.model = model;
        flash.node = "boss_part";

        flash.Flash();

        Assert.IsTrue(flash.IsFlashing);
        Assert.AreEqual(flash.flashColor.alpha, BlockColor(model.Node("boss_part").GetComponent<Renderer>(), "_FlashColor").a, 0.001f);
    }

    private static float Emission(Renderer part) => BlockColor(part, "_EmissionColor").r;

    private static float Emission(Material material) => material.GetColor("_EmissionColor").r;

    private static Color BlockColor(Renderer part, string property)
    {
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        part.GetPropertyBlock(block);
        return block.GetColor(property);
    }
}
