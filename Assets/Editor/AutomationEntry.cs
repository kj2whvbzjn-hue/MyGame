using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class AutomationEntry
{
    // GameCI buildMethod entry point.
    public static void BuildWebGL()
    {
        Debug.Log("[Automation] BuildWebGL started.");

        // 1. JSON-driven operations.
        AutomationJsonImporter.RunFromDefaultFile();

        // 2. Optional C# tasks for operations that are not convenient in JSON.
        AutomationTaskRunner.RunAll();

        // 3. Persist editor changes before the build.
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();

        // GitHub Pages friendly WebGL output.
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
}
