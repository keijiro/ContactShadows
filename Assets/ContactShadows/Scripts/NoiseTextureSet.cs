// Experimental implementation of contact shadows for Unity
// https://github.com/keijiro/ContactShadows

using UnityEngine;

namespace PostEffects
{
    // A custom scriptable object class that is used only for managing
    // references to the noise texture assets.
    public sealed class NoiseTextureSet : ScriptableObject
    {
        [SerializeField] Texture2D [] _textures;

        public Texture2D GetTexture()
        {
            return GetTexture(Time.frameCount);
        }

        public Texture2D GetTexture(int frameCount)
        {
            return _textures[frameCount % _textures.Length];
        }
    }
}
