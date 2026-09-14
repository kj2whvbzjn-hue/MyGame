using UnityEditor;

[InitializeOnLoad]
public static class ConfigureWebGLForPages
{
    static ConfigureWebGLForPages()
    {
        // GitHub Pages does not provide Unity's Brotli/Gzip Content-Encoding
        // headers, so build WebGL without compression.
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.decompressionFallback = false;
    }
}
