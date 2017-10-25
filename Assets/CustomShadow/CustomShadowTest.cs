using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class CustomShadowTest : MonoBehaviour
{
    [SerializeField] Light _light;
    [SerializeField, Range(0, 5)] float _rejectionDepth = 0.5f;
    [SerializeField, Range(4, 32)] int _sampleCount = 16;

    [SerializeField, HideInInspector] Shader _shader;

    Material _material;
    RenderTexture _maskRT;
    CommandBuffer _command1;
    CommandBuffer _command2;

    void OnDestroy()
    {
        if (_material != null)
        {
            if (Application.isPlaying)
                Destroy(_material);
            else
                DestroyImmediate(_material);
        }

        if (_maskRT != null)
        {
            if (Application.isPlaying)
                Destroy(_maskRT);
            else
                DestroyImmediate(_maskRT);
        }
    }

    void OnPreCull()
    {
        // Add the command buffer to the light before camera culling.
        if (_command1 != null && _light != null)
        {
            _light.AddCommandBuffer(LightEvent.AfterScreenspaceMask, _command1);
            _light.AddCommandBuffer(LightEvent.AfterScreenspaceMask, _command2);
        }
    }

    void OnPreRender()
    {
        // We can remove the command buffer before starting render with this
        // camera. Note: This is thought to be done in OnPostRender, but for
        // some reasons it crashes if we do it in OnPostRender. So, we do it in
        // OnPreRender instead. Actually I'm not sure why this works though!
        if (_command1 != null && _light != null)
        {
            _light.RemoveCommandBuffer(LightEvent.AfterScreenspaceMask, _command1);
            _light.RemoveCommandBuffer(LightEvent.AfterScreenspaceMask, _command2);
        }
    }

    void Update()
    {
        if (_light == null) return;

        var camera = GetComponent<Camera>();

        // Destroy the mask RT when the screen size was changed.
        if (_maskRT != null &&
            (_maskRT.width != camera.pixelWidth ||
             _maskRT.height != camera.pixelHeight))
        {
            RenderTexture.ReleaseTemporary(_maskRT);
            _command1.Release();
            _command2.Release();
            _maskRT = null;
            _command1 = _command2 = null;
        }

        // Lazy initialization of the material.
        if (_material == null)
        {
            _material = new Material(_shader);
            _material.hideFlags = HideFlags.DontSave;
        }

        // Lazy initialization of the mask buffer.
        if (_maskRT == null)
        {
            _maskRT = RenderTexture.GetTemporary(
                camera.pixelWidth, camera.pixelHeight, 0,
                RenderTextureFormat.RHalf
            );
        }

        // Lazy initialization of the command buffer.
        if (_command1 == null)
        {
            _command1 = new CommandBuffer();
            _command2 = new CommandBuffer();

            _command1.name = "Contact Shadow Ray Tracing";
            _command2.name = "Contact Shadow Composite";

            _command1.SetRenderTarget(_maskRT);
            _command1.DrawProcedural(Matrix4x4.identity, _material, 0, MeshTopology.Triangles, 3);

            _command2.SetGlobalTexture(Shader.PropertyToID("_MaskTex"), _maskRT);
            _command2.DrawProcedural(Matrix4x4.identity, _material, 1, MeshTopology.Triangles, 3);
        }

        // We require the camera depth texture.
        GetComponent<Camera>().depthTextureMode |= DepthTextureMode.Depth;

        // Shader parameters
        _material.SetVector("_LightVector",
            transform.InverseTransformDirection(-_light.transform.forward) *
            _light.shadowBias / (_sampleCount - 1.5f)
        );

        _material.SetFloat("_RejectionDepth", _rejectionDepth);
        _material.SetInt("_SampleCount", _sampleCount);
        _material.SetFloat("_Convergence", 1.0f / 64);
        _material.SetInt("_FrameCount", Time.frameCount);
    }
}
