using UnityEngine;
using UnityEditor;

// This custom menu item is only used in the initial setup.
// You might not need this script, so it's disabled by default.

#if false

namespace PostEffects
{
    public static class NoiseTextureSetEditor
    {
        [MenuItem("Assets/Create/Noise Texture Set")]
        static void CreateNoiseTextureSet()
        {
            var set = ScriptableObject.CreateInstance<NoiseTextureSet>();
            AssetDatabase.CreateAsset(set, "Assets/NoiseTextures.asset");
        }
    }
}

#endif
