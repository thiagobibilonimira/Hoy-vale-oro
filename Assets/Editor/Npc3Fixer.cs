#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// Fixes npc3 which was accidentally parented to the Canvas (Screen Space Overlay),
/// making its world position land at (-930, -541) — way off screen.
/// This tool detaches it from the Canvas and positions it in world space
/// next to the other NPCs.
/// </summary>
public static class Npc3Fixer
{
    [MenuItem("Tools/Corregir posición de NPC3")]
    public static void CorregirNpc3()
    {
        string scenePath = "Assets/Scenes/nivel_1_ypf.unity";

        if (!System.IO.File.Exists(scenePath))
        {
            EditorUtility.DisplayDialog("Error", "No se encontró la escena 'nivel_1_ypf'.", "OK");
            return;
        }

        string escenaAnterior = EditorSceneManager.GetActiveScene().path;
        EditorSceneManager.SaveOpenScenes();

        Scene escena = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        // Find npc3
        GameObject npc3 = GameObject.Find("npc3");
        if (npc3 == null)
        {
            EditorUtility.DisplayDialog("Error", "No se encontró el objeto 'npc3' en la escena.", "OK");
            return;
        }

        Debug.Log($"[Npc3Fixer] npc3 encontrado. Parent actual: {(npc3.transform.parent != null ? npc3.transform.parent.name : "ninguno")}");
        Debug.Log($"[Npc3Fixer] Posición local actual: {npc3.transform.localPosition}");
        Debug.Log($"[Npc3Fixer] Posición world actual: {npc3.transform.position}");

        // Reference the other NPCs to align position
        GameObject vagabundo   = GameObject.Find("vagabundo");
        GameObject combativa   = GameObject.Find("combativa_0");

        float targetY = -1.68f;
        if (vagabundo != null) targetY = vagabundo.transform.position.y;

        // Place npc3 to the right of vagabundo, before the boundary wall (x = 32.04) and within camera bounds (x = 22.47 + 8.89 = 31.36)
        float targetX = 27f;

        // Detach from Canvas parent → becomes a root scene object
        npc3.transform.SetParent(null, false);

        // Set world position
        npc3.transform.position = new Vector3(targetX, targetY, 0f);

        // Also fix objeto9 position and parent (which was also accidentally parented to Canvas)
        GameObject objeto9 = GameObject.Find("objeto9");
        if (objeto9 != null)
        {
            objeto9.transform.SetParent(null, false);
            objeto9.transform.position = new Vector3(targetX - 0.4f, -2.18f, 0f);
            Debug.Log($"[Npc3Fixer] objeto9 reposicionado correctamente en: {objeto9.transform.position}");
        }

        // Ensure scale is correct (match vagabundo or use same small negative x for flip)
        if (vagabundo != null)
        {
            Vector3 vs = vagabundo.transform.localScale;
            // Keep npc3's Y/Z scale, just make sure magnitude is reasonable
            float absScale = Mathf.Abs(npc3.transform.localScale.y);
            if (absScale < 0.01f || absScale > 10f)
            {
                absScale = Mathf.Abs(vs.y);
                npc3.transform.localScale = new Vector3(-absScale, absScale, absScale);
            }
        }

        Debug.Log($"[Npc3Fixer] Nueva posición world: {npc3.transform.position}");
        Debug.Log($"[Npc3Fixer] Nueva escala: {npc3.transform.localScale}");

        EditorSceneManager.MarkSceneDirty(escena);
        bool ok = EditorSceneManager.SaveScene(escena, scenePath);

        if (ok)
        {
            EditorUtility.DisplayDialog("Listo",
                $"npc3 reposicionado correctamente:\n\n" +
                $"• Posición: ({targetX:F1}, {targetY:F2}, 0)\n" +
                $"• Ahora es un objeto raíz (sin Canvas como padre)\n\n" +
                "Abrí la escena para verificar que esté visible.", "OK");
        }
        else
        {
            Debug.LogError("[Npc3Fixer] Error al guardar la escena.");
        }

        if (!string.IsNullOrEmpty(escenaAnterior) && escenaAnterior != scenePath)
            EditorSceneManager.OpenScene(escenaAnterior);
        else
            EditorSceneManager.OpenScene(scenePath);
    }
}
#endif
