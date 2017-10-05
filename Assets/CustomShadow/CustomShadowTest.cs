using UnityEngine;

[ExecuteInEditMode]
public class CustomShadowTest : MonoBehaviour
{
    [SerializeField] Light _light;
    [SerializeField, Range(0, 5)] float _rejectionDepth = 0.5f;
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
        {
            _material.SetVector(
                "_LightDirection",
                transform.InverseTransformDirection(-_light.transform.forward)
            );
        }

        _material.SetFloat("_RejectionDepth", _rejectionDepth);

        Graphics.Blit(source, dest, _material, 0);
    }
}
