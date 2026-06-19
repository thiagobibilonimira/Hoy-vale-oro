using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.Reflection;

[InitializeOnLoad]
public class BurstConfigurator : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    static BurstConfigurator()
    {
        DisableBurstForPlatform(BuildTarget.StandaloneOSX);
        DisableBurstForPlatform(BuildTarget.StandaloneWindows);
        DisableBurstForPlatform(BuildTarget.StandaloneWindows64);
        DisableBurstCommon();
    }

    public void OnPreprocessBuild(BuildReport report)
    {
        Debug.Log("PreprocessBuild: Ensuring Burst compilation is disabled to prevent macOS build crashes.");
        DisableBurstForPlatform(report.summary.platform);
        DisableBurstCommon();
    }

    private static void DisableBurstForPlatform(BuildTarget target)
    {
        try
        {
            var type = System.Type.GetType("Unity.Burst.Editor.BurstPlatformAotSettings, Unity.Burst.Editor");
            if (type != null)
            {
                var getSettingsMethod = type.GetMethod("GetOrCreateSettings", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                if (getSettingsMethod != null)
                {
                    var settings = getSettingsMethod.Invoke(null, new object[] { target });
                    if (settings != null)
                    {
                        var enableBurstField = type.GetField("EnableBurstCompilation", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                        if (enableBurstField != null)
                        {
                            enableBurstField.SetValue(settings, false);
                            var saveMethod = type.GetMethod("Save", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                            if (saveMethod != null)
                            {
                                saveMethod.Invoke(settings, new object[] { target });
                                Debug.Log($"Successfully disabled Burst compilation for {target}");
                            }
                        }
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Failed to disable Burst for target {target} via reflection: {e.Message}");
        }
    }

    private static void DisableBurstCommon()
    {
        try
        {
            var type = System.Type.GetType("Unity.Burst.Editor.BurstPlatformAotSettings, Unity.Burst.Editor");
            if (type != null)
            {
                var getSettingsMethod = type.GetMethod("GetOrCreateSettings", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                if (getSettingsMethod != null)
                {
                    var settings = getSettingsMethod.Invoke(null, new object[] { null });
                    if (settings != null)
                    {
                        var enableBurstField = type.GetField("EnableBurstCompilation", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                        if (enableBurstField != null)
                        {
                            enableBurstField.SetValue(settings, false);
                            var saveMethod = type.GetMethod("Save", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                            if (saveMethod != null)
                            {
                                saveMethod.Invoke(settings, new object[] { null });
                                Debug.Log("Successfully disabled Burst compilation globally (Common settings)");
                            }
                        }
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Failed to disable Burst globally via reflection: {e.Message}");
        }
    }
}
