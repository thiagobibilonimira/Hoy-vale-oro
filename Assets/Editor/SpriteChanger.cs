#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class SpriteChanger
{
    static SpriteChanger()
    {
        // Automatically execute the change once when compilation completes
        EditorApplication.delayCall += () =>
        {
            ChangeSprite(true);
        };
    }

    [MenuItem("Tools/Cambiar Sprite Bidon a Objeto21")]
    public static void ChangeSpriteMenu()
    {
        ChangeSprite(false);
    }

    private static void ChangeSprite(bool silencioso)
    {
        string scenePath = "Assets/Scenes/nivel_1_ypf.unity";
        string spritePath = "Assets/Sprites/objetos/objeto21.png";

        if (EditorApplication.isPlaying || EditorApplication.isPaused)
        {
            return;
        }

        if (!System.IO.File.Exists(scenePath))
        {
            if (!silencioso) Debug.LogError("No se encontró la escena en: " + scenePath);
            return;
        }

        try
        {
            // Save active scene first
            string previousScenePath = EditorSceneManager.GetActiveScene().path;
            
            // Save open scenes only if there are modifications to prevent errors
            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.isDirty)
            {
                EditorSceneManager.SaveOpenScenes();
            }

            // Open target scene
            var scene = EditorSceneManager.OpenScene(scenePath);

            // Find the GameObject named "bidon(1)" (searching all root hierarchy recursively)
            GameObject targetGo = FindGameObjectInScene("bidon(1)");

            if (targetGo != null)
            {
                SpriteRenderer sr = targetGo.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    Sprite newSprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                    if (newSprite != null)
                    {
                        // Check if already changed to avoid redundant saves
                        if (sr.sprite != newSprite)
                        {
                            Undo.RecordObject(sr, "Cambiar Sprite a objeto21");
                            sr.sprite = newSprite;
                            
                            // Mark dirty and save
                            EditorSceneManager.MarkSceneDirty(scene);
                            EditorSceneManager.SaveScene(scene);
                            Debug.Log("Sprite de 'bidon(1)' cambiado exitosamente a 'objeto21' en nivel_1_ypf.");
                        }
                        else if (!silencioso)
                        {
                            Debug.Log("El sprite de 'bidon(1)' ya está configurado como 'objeto21'.");
                        }
                    }
                    else
                    {
                        Debug.LogError("No se pudo cargar el sprite en la ruta: " + spritePath);
                    }
                }
                else
                {
                    Debug.LogError("El GameObject 'bidon(1)' no tiene un componente SpriteRenderer.");
                }
            }
            else
            {
                if (!silencioso) Debug.LogError("No se encontró el GameObject 'bidon(1)' en la escena nivel_1_ypf.");
            }

            // Reopen previous scene if different
            if (previousScenePath != scenePath && !string.IsNullOrEmpty(previousScenePath))
            {
                EditorSceneManager.OpenScene(previousScenePath);
            }
        }
        catch (System.Exception ex)
        {
            if (!silencioso)
            {
                Debug.LogError("Excepción al cambiar sprite: " + ex.Message);
            }
        }
    }

    private static GameObject FindGameObjectInScene(string name)
    {
        GameObject[] rootObjs = EditorSceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var obj in rootObjs)
        {
            GameObject found = FindRecursive(obj, name);
            if (found != null) return found;
        }
        return null;
    }

    private static GameObject FindRecursive(GameObject current, string name)
    {
        if (current.name == name) return current;
        for (int i = 0; i < current.transform.childCount; i++)
        {
            GameObject found = FindRecursive(current.transform.GetChild(i).gameObject, name);
            if (found != null) return found;
        }
        return null;
    }
}
#endif
