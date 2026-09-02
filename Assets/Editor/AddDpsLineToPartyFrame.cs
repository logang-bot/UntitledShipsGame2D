using TMPro;
using UnityEditor;
using UnityEngine;

// One-shot migration: adds the DPS stat line to PartyFrame.prefab and wires it
// to PartyFrameUI.dpsText, by duplicating the existing FireRateText line so the
// new one inherits its font, size, color and alignment exactly.
//
// Done through Unity's own prefab API rather than by hand-editing the prefab
// YAML, which would mean inventing fileIDs and a TMP component block by hand.
// Idempotent - running it twice is a no-op.
//
// Run headless with:
//   Unity.exe -batchmode -quit -projectPath <proj> \
//     -executeMethod AddDpsLineToPartyFrame.Run
//
// Delete this file once it has been run; it exists only to perform the edit.
public static class AddDpsLineToPartyFrame
{
    private const string PrefabPath = "Assets/Prefabs/PartyFrame.prefab";

    [MenuItem("Tools/Add DPS Line to PartyFrame")]
    public static void Run()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        if (root == null)
        {
            Debug.LogError($"[AddDpsLine] Could not load {PrefabPath}");
            EditorApplication.Exit(1);
            return;
        }

        try
        {
            PartyFrameUI ui = root.GetComponent<PartyFrameUI>();
            if (ui == null)
            {
                Debug.LogError("[AddDpsLine] PartyFrameUI component not found on the prefab root.");
                EditorApplication.Exit(1);
                return;
            }

            if (ui.dpsText != null)
            {
                Debug.Log("[AddDpsLine] dpsText is already wired - nothing to do.");
                return;
            }

            if (ui.fireRateText == null)
            {
                Debug.LogError("[AddDpsLine] fireRateText is not wired, so there is no line to copy.");
                EditorApplication.Exit(1);
                return;
            }

            GameObject source = ui.fireRateText.gameObject;
            GameObject copy = Object.Instantiate(source, source.transform.parent);
            copy.name = "DpsText";
            // Sit directly beneath Fire Rate: the two are read together, and
            // InfoColumn's VerticalLayoutGroup orders purely by sibling index.
            copy.transform.SetSiblingIndex(source.transform.GetSiblingIndex() + 1);

            TextMeshProUGUI label = copy.GetComponent<TextMeshProUGUI>();
            label.text = "DPS: 0.0"; // placeholder; PartyFrameUI.Update() overwrites every frame
            ui.dpsText = label;

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Debug.Log($"[AddDpsLine] Added '{copy.name}' at sibling index " +
                      $"{copy.transform.GetSiblingIndex()} under '{source.transform.parent.name}' and wired PartyFrameUI.dpsText.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
