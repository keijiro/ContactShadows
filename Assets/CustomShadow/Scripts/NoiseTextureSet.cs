using UnityEngine;

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
