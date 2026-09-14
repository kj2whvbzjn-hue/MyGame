using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class AutomationEntry
{
    // FAST: compile + JSON automation + C# automation tasks only.
    // No WebGL build and no GitHub Pages deployment.
    public static void ValidateOnly()
    {
        Debug.Log("[Automation] FAST validation started.");

        RunAutomation();

        Debug.Log("[Automation] FAST validation PASS.");
    }

    // TEST / RELEASE: automation followed by a WebGL build.
    public static void BuildWebGL()
    {
        Debug.Log("[Automation] BuildWebGL started.");

        RunAutomation();

        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.decompressionFallback = false;

        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled && File.Exists(scene.path))
            .Select(scene => scene.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            throw new InvalidOperationException(
                "No enabled build scenes were found in EditorBuildSettings.");
        }

        const string outputPath = "build/WebGL/WebGL";
        Directory.CreateDirectory(outputPath);

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };

        Debug.Log($"[Automation] Building WebGL to {outputPath}.");

        BuildReport report = BuildPipeline.BuildPlayer(options);

        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new Exception(
                $"WebGL build failed: {report.summary.result}, " +
                $"errors={report.summary.totalErrors}, " +
                $"warnings={report.summary.totalWarnings}");
        }

        Debug.Log(
            $"[Automation] WebGL build succeeded. " +
            $"size={report.summary.totalSize} bytes, " +
            $"time={report.summary.totalTime}");
    }

    private static void RunAutomation()
    {
        AutomationJsonImporter.RunFromDefaultFile();
        AutomationTaskRunner.RunAll();

        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
    }
}
