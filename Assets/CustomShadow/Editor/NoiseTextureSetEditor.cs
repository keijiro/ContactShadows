using UnityEngine;
using UnityEditor;

#if false

public static class NoiseTextureSetEditor
{
    [MenuItem("Assets/Create/Noise Texture Set")]
    static void CreateNoiseTextureSet()
    {
        var set = ScriptableObject.CreateInstance<NoiseTextureSet>();
        AssetDatabase.CreateAsset(set, "Assets/NoiseTextures.asset");
    }
}

#endif
