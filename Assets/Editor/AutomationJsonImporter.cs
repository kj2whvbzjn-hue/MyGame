using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public sealed class AutomationCommandFile
{
    public AutomationCommand[] commands;
}

[Serializable]
public sealed class AutomationCommand
{
    public string type;
    public string name;
    public string target;
    public string primitive;
    public string scenePath;
    public string message;

    public float[] position;
    public float[] rotation;
    public float[] scale;
}

public static class AutomationJsonImporter
{
    private const string DefaultRelativePath = "Automation/command.json";

    public static void RunFromDefaultFile()
    {
        string path = Path.Combine(Application.dataPath, DefaultRelativePath);

        if (!File.Exists(path))
        {
            Debug.Log($"[AutomationJson] No command file found: {path}");
            return;
        }

        string json = File.ReadAllText(path);
        AutomationCommandFile file = JsonUtility.FromJson<AutomationCommandFile>(json);

        if (file == null || file.commands == null || file.commands.Length == 0)
        {
            Debug.Log("[AutomationJson] No commands to execute.");
            return;
        }

        Debug.Log($"[AutomationJson] Executing {file.commands.Length} command(s).");

        for (int i = 0; i < file.commands.Length; i++)
        {
            Execute(file.commands[i], i);
        }

        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
    }

    private static void Execute(AutomationCommand command, int index)
    {
        if (command == null || string.IsNullOrWhiteSpace(command.type))
            throw new InvalidOperationException($"Command #{index} has no type.");

        switch (command.type)
        {
            case "log":
                Debug.Log($"[AutomationJson] {command.message}");
                break;

            case "openScene":
                Require(command.scenePath, index, "scenePath");
                EditorSceneManager.OpenScene(command.scenePath, OpenSceneMode.Single);
                break;

            case "createPrimitive":
                CreatePrimitive(command, index);
                break;

            case "setTransform":
                SetTransform(command, index);
                break;

            case "deleteObject":
                DeleteObject(command, index);
                break;

            case "saveScene":
                SaveActiveScene();
                break;

            default:
                throw new InvalidOperationException(
                    $"Command #{index} has unsupported type: {command.type}");
        }
    }

    private static void CreatePrimitive(AutomationCommand command, int index)
    {
        Require(command.primitive, index, "primitive");

        if (!Enum.TryParse(command.primitive, true, out PrimitiveType primitiveType))
        {
            throw new InvalidOperationException(
                $"Command #{index}: unknown primitive '{command.primitive}'.");
        }

        GameObject go = GameObject.CreatePrimitive(primitiveType);
        go.name = string.IsNullOrWhiteSpace(command.name)
            ? command.primitive
            : command.name;

        ApplyTransform(go.transform, command);
        Undo.RegisterCreatedObjectUndo(go, "Automation create primitive");

        Debug.Log($"[AutomationJson] Created {primitiveType}: {go.name}");
    }

    private static void SetTransform(AutomationCommand command, int index)
    {
        Require(command.target, index, "target");
        GameObject go = GameObject.Find(command.target);

        if (go == null)
            throw new InvalidOperationException(
                $"Command #{index}: target not found: {command.target}");

        Undo.RecordObject(go.transform, "Automation set transform");
        ApplyTransform(go.transform, command);

        Debug.Log($"[AutomationJson] Updated transform: {go.name}");
    }

    private static void DeleteObject(AutomationCommand command, int index)
    {
        Require(command.target, index, "target");
        GameObject go = GameObject.Find(command.target);

        if (go == null)
        {
            Debug.LogWarning(
                $"[AutomationJson] Delete skipped; target not found: {command.target}");
            return;
        }

        Undo.DestroyObjectImmediate(go);
        Debug.Log($"[AutomationJson] Deleted: {command.target}");
    }

    private static void SaveActiveScene()
    {
        Scene scene = SceneManager.GetActiveScene();

        if (!scene.IsValid())
            throw new InvalidOperationException("No valid active scene to save.");

        if (string.IsNullOrWhiteSpace(scene.path))
            throw new InvalidOperationException(
                "Active scene has no asset path. Save/create the scene explicitly first.");

        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[AutomationJson] Saved scene: {scene.path}");
    }

    private static void ApplyTransform(Transform transform, AutomationCommand command)
    {
        if (HasVector3(command.position))
            transform.position = ToVector3(command.position);

        if (HasVector3(command.rotation))
            transform.eulerAngles = ToVector3(command.rotation);

        if (HasVector3(command.scale))
            transform.localScale = ToVector3(command.scale);
    }

    private static bool HasVector3(float[] values)
        => values != null && values.Length == 3;

    private static Vector3 ToVector3(float[] values)
        => new Vector3(values[0], values[1], values[2]);

    private static void Require(string value, int index, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException(
                $"Command #{index} requires '{field}'.");
    }
}
