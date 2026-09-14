using UnityEditor;

[InitializeOnLoad]
public static class ConfigureWebGLForPages
{
    static ConfigureWebGLForPages()
    {
        PlayerSettings.WebGL.decompressionFallback = true;
    }
}
