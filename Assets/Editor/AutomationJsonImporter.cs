using System;
using System.IO;
using System.Linq;
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
    public string componentType;
    public string primitive;
    public string scenePath;
    public string message;

    // Asset automation
    public string asset;
    public string assetPath;
    public bool loop = false;
    public bool playOnAwake = true;
    public float volume = 1f;

    public float[] position;
    public float[] rotation;
    public float[] scale;
    public float[] color;
    public float[] backgroundColor;

    public float orthographicSize = 5f;
    public int sortingOrder = 0;
    public bool setAsOnlyBuildScene = false;
}

[Serializable]
public sealed class AutomationAssetMapFile
{
    public AutomationAssetMapEntry[] assets;
}

[Serializable]
public sealed class AutomationAssetMapEntry
{
    public string key;
    public string path;
    public string kind;
}

public static class AutomationJsonImporter
{
    private const string DefaultRelativePath = "Automation/command.json";
    private const string AssetMapRelativePath = "Automation/asset-map.json";
    private const string WhiteSpritePath = "Assets/Generated/AutomationWhite.png";

    private static AutomationAssetMapFile cachedAssetMap;

    public static void RunFromDefaultFile()
    {
        cachedAssetMap = LoadAssetMap();

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
        AssetDatabase.Refresh();
    }

    private static AutomationAssetMapFile LoadAssetMap()
    {
        string path = Path.Combine(Application.dataPath, AssetMapRelativePath);

        if (!File.Exists(path))
        {
            Debug.Log("[AutomationAssets] No asset-map.json found; direct assetPath commands still work.");
            return new AutomationAssetMapFile { assets = Array.Empty<AutomationAssetMapEntry>() };
        }

        string json = File.ReadAllText(path);
        AutomationAssetMapFile map = JsonUtility.FromJson<AutomationAssetMapFile>(json);

        if (map == null || map.assets == null)
            return new AutomationAssetMapFile { assets = Array.Empty<AutomationAssetMapEntry>() };

        Debug.Log($"[AutomationAssets] Loaded {map.assets.Length} asset mapping(s).");
        return map;
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

            case "createScene":
                CreateScene(command, index);
                break;

            case "openScene":
                Require(command.scenePath, index, "scenePath");
                EditorSceneManager.OpenScene(command.scenePath, OpenSceneMode.Single);
                break;

            case "createEmpty":
                CreateEmpty(command);
                break;

            case "createCamera2D":
                CreateCamera2D(command);
                break;

            case "createSpriteRect":
                CreateSpriteRect(command);
                break;

            case "createPrimitive":
                CreatePrimitive(command, index);
                break;

            case "addComponent":
                AddComponent(command, index);
                break;

            case "spawnAsset":
                SpawnAsset(command, index);
                break;

            case "setSpriteAsset":
                SetSpriteAsset(command, index);
                break;

            case "setAudioAsset":
                SetAudioAsset(command, index);
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

    private static string ResolveAssetPath(AutomationCommand command, int index)
    {
        if (!string.IsNullOrWhiteSpace(command.assetPath))
        {
            if (!command.assetPath.StartsWith("Assets/", StringComparison.Ordinal))
                throw new InvalidOperationException(
                    $"Command #{index}: assetPath must start with 'Assets/': {command.assetPath}");

            return command.assetPath;
        }

        Require(command.asset, index, "asset or assetPath");

        AutomationAssetMapEntry match = cachedAssetMap?.assets?
            .FirstOrDefault(entry =>
                entry != null &&
                string.Equals(entry.key, command.asset, StringComparison.OrdinalIgnoreCase));

        if (match == null || string.IsNullOrWhiteSpace(match.path))
        {
            throw new InvalidOperationException(
                $"Command #{index}: asset key '{command.asset}' is not present in Assets/Automation/asset-map.json.");
        }

        return match.path;
    }

    private static void SpawnAsset(AutomationCommand command, int index)
    {
        string path = ResolveAssetPath(command, index);

        GameObject instance = null;

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab != null)
        {
            instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        }
        else
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null)
            {
                instance = new GameObject(
                    string.IsNullOrWhiteSpace(command.name) ? sprite.name : command.name);

                SpriteRenderer renderer = instance.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingOrder = command.sortingOrder;

                if (HasColor(command.color))
                    renderer.color = ToColor(command.color);
            }
        }

        if (instance == null)
        {
            throw new InvalidOperationException(
                $"Command #{index}: spawnAsset supports Prefab/GameObject or Sprite assets. Could not load: {path}");
        }

        if (!string.IsNullOrWhiteSpace(command.name))
            instance.name = command.name;

        ApplyTransform(instance.transform, command);

        Debug.Log($"[AutomationAssets] Spawned asset '{path}' as '{instance.name}'.");
    }

    private static void SetSpriteAsset(AutomationCommand command, int index)
    {
        Require(command.target, index, "target");
        string path = ResolveAssetPath(command, index);

        GameObject go = GameObject.Find(command.target);
        if (go == null)
            throw new InvalidOperationException(
                $"Command #{index}: target not found: {command.target}");

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
            throw new InvalidOperationException(
                $"Command #{index}: Sprite could not be loaded: {path}");

        SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
        if (renderer == null)
            renderer = go.AddComponent<SpriteRenderer>();

        renderer.sprite = sprite;
        renderer.sortingOrder = command.sortingOrder;

        if (HasColor(command.color))
            renderer.color = ToColor(command.color);

        Debug.Log($"[AutomationAssets] Assigned sprite '{path}' to '{go.name}'.");
    }

    private static void SetAudioAsset(AutomationCommand command, int index)
    {
        Require(command.target, index, "target");
        string path = ResolveAssetPath(command, index);

        GameObject go = GameObject.Find(command.target);
        if (go == null)
            throw new InvalidOperationException(
                $"Command #{index}: target not found: {command.target}");

        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        if (clip == null)
            throw new InvalidOperationException(
                $"Command #{index}: AudioClip could not be loaded: {path}");

        AudioSource source = go.GetComponent<AudioSource>();
        if (source == null)
            source = go.AddComponent<AudioSource>();

        source.clip = clip;
        source.loop = command.loop;
        source.playOnAwake = command.playOnAwake;
        source.volume = Mathf.Clamp01(command.volume);

        Debug.Log($"[AutomationAssets] Assigned audio '{path}' to '{go.name}'.");
    }

    private static void CreateScene(AutomationCommand command, int index)
    {
        Require(command.scenePath, index, "scenePath");

        string directory = Path.GetDirectoryName(command.scenePath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        if (!EditorSceneManager.SaveScene(scene, command.scenePath))
            throw new InvalidOperationException($"Could not save scene: {command.scenePath}");

        if (command.setAsOnlyBuildScene)
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(command.scenePath, true)
            };
        }

        Debug.Log($"[AutomationJson] Created scene: {command.scenePath}");
    }

    private static void CreateEmpty(AutomationCommand command)
    {
        GameObject go = new GameObject(
            string.IsNullOrWhiteSpace(command.name) ? "GameObject" : command.name);

        ApplyTransform(go.transform, command);
        Debug.Log($"[AutomationJson] Created empty object: {go.name}");
    }

    private static void CreateCamera2D(AutomationCommand command)
    {
        GameObject go = new GameObject(
            string.IsNullOrWhiteSpace(command.name) ? "Main Camera" : command.name);

        Camera camera = go.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = command.orthographicSize > 0f
            ? command.orthographicSize
            : 5f;
        camera.clearFlags = CameraClearFlags.SolidColor;

        if (HasColor(command.backgroundColor))
            camera.backgroundColor = ToColor(command.backgroundColor);
        else
            camera.backgroundColor = new Color(0.04f, 0.05f, 0.1f, 1f);

        go.tag = "MainCamera";

        ApplyTransform(go.transform, command);

        if (!HasVector3(command.position))
            go.transform.position = new Vector3(0f, 0f, -10f);

        Debug.Log($"[AutomationJson] Created 2D camera: {go.name}");
    }

    private static void CreateSpriteRect(AutomationCommand command)
    {
        Sprite sprite = EnsureWhiteSpriteAsset();

        GameObject go = new GameObject(
            string.IsNullOrWhiteSpace(command.name) ? "SpriteRect" : command.name);

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = HasColor(command.color)
            ? ToColor(command.color)
            : Color.white;
        renderer.sortingOrder = command.sortingOrder;

        ApplyTransform(go.transform, command);

        Debug.Log($"[AutomationJson] Created sprite rect: {go.name}");
    }

    private static Sprite EnsureWhiteSpriteAsset()
    {
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(WhiteSpritePath);
        if (existing != null)
            return existing;

        Directory.CreateDirectory(Path.GetDirectoryName(WhiteSpritePath));

        var texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        var pixels = Enumerable.Repeat(Color.white, 16 * 16).ToArray();
        texture.SetPixels(pixels);
        texture.Apply();

        File.WriteAllBytes(WhiteSpritePath, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(WhiteSpritePath, ImportAssetOptions.ForceSynchronousImport);

        TextureImporter importer = AssetImporter.GetAtPath(WhiteSpritePath) as TextureImporter;
        if (importer == null)
            throw new InvalidOperationException("Could not configure generated sprite importer.");

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 16f;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(WhiteSpritePath);
        if (sprite == null)
            throw new InvalidOperationException("Generated sprite could not be loaded.");

        return sprite;
    }

    private static void AddComponent(AutomationCommand command, int index)
    {
        Require(command.target, index, "target");
        Require(command.componentType, index, "componentType");

        GameObject go = GameObject.Find(command.target);
        if (go == null)
            throw new InvalidOperationException(
                $"Command #{index}: target not found: {command.target}");

        Type componentType = FindType(command.componentType);
        if (componentType == null || !typeof(Component).IsAssignableFrom(componentType))
            throw new InvalidOperationException(
                $"Command #{index}: component type not found: {command.componentType}");

        if (go.GetComponent(componentType) == null)
            go.AddComponent(componentType);

        Debug.Log($"[AutomationJson] Added component {componentType.Name} to {go.name}");
    }

    private static Type FindType(string typeName)
    {
        Type direct = Type.GetType(typeName);
        if (direct != null)
            return direct;

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type match = assembly.GetTypes().FirstOrDefault(
                type => type.FullName == typeName || type.Name == typeName);

            if (match != null)
                return match;
        }

        return null;
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
        Debug.Log($"[AutomationJson] Created {primitiveType}: {go.name}");
    }

    private static void SetTransform(AutomationCommand command, int index)
    {
        Require(command.target, index, "target");
        GameObject go = GameObject.Find(command.target);

        if (go == null)
            throw new InvalidOperationException(
                $"Command #{index}: target not found: {command.target}");

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

        UnityEngine.Object.DestroyImmediate(go);
        Debug.Log($"[AutomationJson] Deleted: {command.target}");
    }

    private static void SaveActiveScene()
    {
        Scene scene = SceneManager.GetActiveScene();

        if (!scene.IsValid())
            throw new InvalidOperationException("No valid active scene to save.");

        if (string.IsNullOrWhiteSpace(scene.path))
            throw new InvalidOperationException(
                "Active scene has no asset path.");

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

    private static bool HasColor(float[] values)
        => values != null && (values.Length == 3 || values.Length == 4);

    private static Vector3 ToVector3(float[] values)
        => new Vector3(values[0], values[1], values[2]);

    private static Color ToColor(float[] values)
        => new Color(
            values[0],
            values[1],
            values[2],
            values.Length >= 4 ? values[3] : 1f);

    private static void Require(string value, int index, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException(
                $"Command #{index} requires '{field}'.");
    }
}
