using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public sealed class CustomShadowTest : MonoBehaviour
{
    [SerializeField] Light _light;
    [SerializeField, Range(0, 5)] float _rejectionDepth = 0.5f;
    [SerializeField, Range(4, 32)] int _sampleCount = 16;
    [SerializeField, Range(0, 1)] float _convergenceSpeed = 0.2f;

    [SerializeField, HideInInspector] Shader _shader;
    [SerializeField, HideInInspector] NoiseTextureSet _noiseTextures;

    Material _material;
    RenderTexture _tempMaskRT, _prevMaskRT;
    CommandBuffer _command1, _command2;

    // MRT array; Reused between frames to avoid GC memory allocation.
    RenderTargetIdentifier[] _mrt = new RenderTargetIdentifier[2];

    void OnDestroy()
    {
        if (_material != null)
        {
            if (Application.isPlaying)
                Destroy(_material);
            else
                DestroyImmediate(_material);
        }

        if (_tempMaskRT != null) RenderTexture.ReleaseTemporary(_tempMaskRT);
        if (_prevMaskRT != null) RenderTexture.ReleaseTemporary(_prevMaskRT);

        if (_command1 != null) _command1.Release();
        if (_command2 != null) _command2.Release();
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

    Matrix4x4 _previousVP;

    void Update()
    {
        if (_light == null) return;

        var camera = GetComponent<Camera>();
        var scrWidth = camera.pixelWidth;
        var scrHeight = camera.pixelHeight;
        var maskFormat = RenderTextureFormat.R8;

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

        // Firstly, update the shader parameters.
        _material.SetVector("_LightVector",
            transform.InverseTransformDirection(-_light.transform.forward) *
            _light.shadowBias / (_sampleCount - 1.5f)
        );

        _material.SetFloat("_RejectionDepth", _rejectionDepth);
        _material.SetInt("_SampleCount", _sampleCount);
        _material.SetFloat("_Convergence", _convergenceSpeed * _convergenceSpeed);
        _material.SetInt("_FrameCount", Time.frameCount);

        // Screen coordinate to noise texture cooridinate scale
        var noiseTexture = _noiseTextures.GetTexture();
        var noiseScale = new Vector2(scrWidth, scrHeight) / noiseTexture.width;
        _material.SetVector("_NoiseScale", noiseScale);

        // Discard the temp mask (used in the previous frame) and recreate it.
        RenderTexture.ReleaseTemporary(_tempMaskRT);
        _tempMaskRT = RenderTexture.GetTemporary(scrWidth, scrHeight, 0, maskFormat);

        // Render the shadow mask within the first command buffer.
        _command1.Clear();
        _command1.SetGlobalTexture(Shader.PropertyToID("_ShadowMask"), BuiltinRenderTextureType.CurrentActive);
        _command1.SetGlobalTexture(Shader.PropertyToID("_NoiseTex"), noiseTexture);
        _command1.SetRenderTarget(_tempMaskRT);
        _command1.DrawProcedural(Matrix4x4.identity, _material, 0, MeshTopology.Triangles, 3);

        // Allocate a new mask RT.
        var newMaskRT = RenderTexture.GetTemporary(scrWidth, scrHeight, 0, maskFormat);

        // Apply the temporal filter and blend it to the screen-space shadow
        // mask within the second command buffer.
        _command2.Clear();
        _mrt[0] = BuiltinRenderTextureType.CurrentActive;
        _mrt[1] = newMaskRT;
        _command2.SetRenderTarget(_mrt, BuiltinRenderTextureType.CurrentActive);
        _command2.SetGlobalTexture(Shader.PropertyToID("_PrevMask"), _prevMaskRT);
        _command2.SetGlobalTexture(Shader.PropertyToID("_TempMask"), _tempMaskRT);

        var proj = GL.GetGPUProjectionMatrix(camera.nonJitteredProjectionMatrix, true);
        //_material.SetMatrix("_NonJitteredVP", proj * camera.worldToCameraMatrix);
        //_material.SetMatrix("_PreviousVP", _previousVP); //camera.previousViewProjectionMatrix);
        _command2.SetGlobalMatrix("_NonJitteredVP", proj * camera.worldToCameraMatrix);
        _command2.SetGlobalMatrix("_PreviousVP", _previousVP);//camera.previousViewProjectionMatrix);
        _previousVP = proj * camera.worldToCameraMatrix;

        _command2.DrawProcedural(Matrix4x4.identity, _material, 1, MeshTopology.Triangles, 3);

        // Update history.
        RenderTexture.ReleaseTemporary(_prevMaskRT);
        _prevMaskRT = newMaskRT;
    }
}
