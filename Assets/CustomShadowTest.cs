using UnityEngine;

[ExecuteInEditMode]
public class CustomShadowTest : MonoBehaviour
{
    [SerializeField] Light _light;
    [SerializeField] Shader _shader;

    Material _material;

    void Update()
    {
        GetComponent<Camera>().depthTextureMode |= DepthTextureMode.Depth;
    }

    void OnDestroy()
    {
        if (_material != null)
            if (Application.isPlaying)
                Destroy(_material);
            else
                DestroyImmediate(_material);
    }

    void OnRenderImage(RenderTexture source, RenderTexture dest)
    {
        if (_material == null)
        {
            _material = new Material(_shader);
            _material.hideFlags = HideFlags.DontSave;
        }

        if (_light != null)
            _material.SetVector("_LightDirection", _light.transform.forward);

        Graphics.Blit(source, dest, _material, 0);
    }
}
