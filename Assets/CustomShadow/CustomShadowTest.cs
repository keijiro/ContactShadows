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
    CommandBuffer _command1, _command2;

    void OnDestroy()
    {
        if (_material != null)
        {
            if (Application.isPlaying)
                Destroy(_material);
            else
                DestroyImmediate(_material);
        }

        if (_command1 != null) _command1.Release();
        if (_command2 != null) _command2.Release();

        if (_maskRT != null) RenderTexture.ReleaseTemporary(_maskRT);
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

        // We require the camera depth texture.
        camera.depthTextureMode |= DepthTextureMode.Depth;

        // Lazy initialization of temp objects.
        if (_material == null)
        {
            _material = new Material(_shader);
            _material.hideFlags = HideFlags.DontSave;
        }

        if (_command1 == null)
        {
            _command1 = new CommandBuffer();
            _command2 = new CommandBuffer();
            _command1.name = "Contact Shadow Ray Tracing";
            _command2.name = "Contact Shadow Composite";
        }

        if (_maskRT != null &&
            (_maskRT.width != camera.pixelWidth ||
             _maskRT.height != camera.pixelHeight))
        {
            RenderTexture.ReleaseTemporary(_maskRT);
            _maskRT = null;
        }

        if (_maskRT == null)
            _maskRT = RenderTexture.GetTemporary(
                camera.pixelWidth, camera.pixelHeight,
                0, RenderTextureFormat.RHalf
            );


        // Firstly, update the shader parameters.
        _material.SetVector("_LightVector",
            transform.InverseTransformDirection(-_light.transform.forward) *
            _light.shadowBias / (_sampleCount - 1.5f)
        );

        _material.SetFloat("_RejectionDepth", _rejectionDepth);
        _material.SetInt("_SampleCount", _sampleCount);
        _material.SetFloat("_Convergence", 1.0f / 64);
        _material.SetInt("_FrameCount", Time.frameCount);

        _command1.Clear();

        var tempTexID = Shader.PropertyToID("_TempTex");
        var temp2TexID = Shader.PropertyToID("_Temp2Tex");
        var maskTexID = Shader.PropertyToID("_MaskTex");

        _command1.GetTemporaryRT(tempTexID, camera.pixelWidth, camera.pixelHeight, 0, FilterMode.Bilinear, RenderTextureFormat.RHalf);
        _command1.SetRenderTarget(tempTexID);
        _command1.DrawProcedural(Matrix4x4.identity, _material, 0, MeshTopology.Triangles, 3);

        _command1.GetTemporaryRT(temp2TexID, camera.pixelWidth, camera.pixelHeight, 0, FilterMode.Bilinear, RenderTextureFormat.RHalf);
        _command1.SetRenderTarget(temp2TexID);
        _command1.SetGlobalTexture(maskTexID, _maskRT);
        _command1.DrawProcedural(Matrix4x4.identity, _material, 1, MeshTopology.Triangles, 3);

        _command1.CopyTexture(temp2TexID, _maskRT);

        _command2.Clear();
        _command2.SetGlobalTexture(maskTexID, _maskRT);
        _command2.DrawProcedural(Matrix4x4.identity, _material, 2, MeshTopology.Triangles, 3);
    }
}
