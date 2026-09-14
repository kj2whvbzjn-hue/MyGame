using UnityEditor;

[InitializeOnLoad]
public static class ConfigureWebGLForPages
{
    static ConfigureWebGLForPages()
    {
        // GitHub Pages cannot set Unity's Brotli/Gzip Content-Encoding headers.
        // Build uncompressed WebGL files so they can be served directly.
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.decompressionFallback = false;
    }
}
