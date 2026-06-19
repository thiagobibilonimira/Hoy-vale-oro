#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build.Reporting;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

public static class WebGLBuilder
{
    [MenuItem("Tools/Compilar WebGL para GitHub Pages")]
    public static void Build()
    {
        string buildPath = "/Users/franciscofafian/Documents/GitHub/Hoy-vale-oro/docs";

        // Create docs directory if it does not exist
        if (!Directory.Exists(buildPath))
        {
            Directory.CreateDirectory(buildPath);
        }

        // Get enabled scenes from Build Settings
        List<string> activeScenes = new List<string>();
        foreach (var scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled)
            {
                activeScenes.Add(scene.path);
            }
        }

        if (activeScenes.Count == 0)
        {
            EditorUtility.DisplayDialog("Error", "No hay escenas habilitadas en la configuración de compilación (Build Settings).", "OK");
            return;
        }

        Debug.Log($"[WebGLBuilder] Iniciando compilación WebGL de {activeScenes.Count} escenas en: {buildPath}");

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = activeScenes.ToArray();
        buildPlayerOptions.locationPathName = buildPath;
        buildPlayerOptions.target = BuildTarget.WebGL;
        buildPlayerOptions.options = BuildOptions.None;

        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log("[WebGLBuilder] Compilación WebGL exitosa.");
            EditorUtility.DisplayDialog("Compilación Completada", 
                $"El juego se compiló con éxito para WebGL en:\n{buildPath}\n\nAhora ve a GitHub Desktop, verás la carpeta 'docs' lista para hacer Commit y Push.", "OK");
        }
        else if (summary.result == BuildResult.Failed)
        {
            Debug.LogError($"[WebGLBuilder] La compilación falló con {summary.totalErrors} errores.");
            EditorUtility.DisplayDialog("Error de Compilación", "La compilación para WebGL falló. Revisa la consola de Unity para más detalles.", "OK");
        }
    }
}
#endif
