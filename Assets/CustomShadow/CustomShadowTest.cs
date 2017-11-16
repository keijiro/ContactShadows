using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public sealed class CustomShadowTest : MonoBehaviour
{
    #region Editable attributes

    [SerializeField] Light _light;
    [SerializeField, Range(0, 5)] float _rejectionDepth = 0.5f;
    [SerializeField, Range(4, 32)] int _sampleCount = 16;
    [SerializeField, Range(0, 1)] float _convergenceSpeed = 0.2f;

    #endregion

    #region Internal resources

    [SerializeField, HideInInspector] Shader _shader;
    [SerializeField, HideInInspector] NoiseTextureSet _noiseTextures;

    #endregion

    #region Temporary objects

    Material _material;
    RenderTexture _tempMaskRT, _prevMaskRT;
    CommandBuffer _command1, _command2;

    // We track the VP matrix without using Camera.previousViewProjectionMatrix
    // because it's unusable in OnPreCull.
    Matrix4x4 _previousVP;

    // MRT array; Reused between frames to avoid GC memory allocation.
    RenderTargetIdentifier[] _mrt = new RenderTargetIdentifier[2];

    #endregion

    #region Internal utility functions

    // Calculates the view-projection matrix.
    Matrix4x4 CalculateVPMatrix()
    {
        var cam = GetComponent<Camera>();
        var p = cam.nonJitteredProjectionMatrix;
        var v = cam.worldToCameraMatrix;
        return GL.GetGPUProjectionMatrix(p, true) * v;
    }

    #endregion

    #region MonoBehaviour implementation

    void OnDestroy()
    {
        // Release temporary objects.
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
        // Update and set the command buffers to the target light.
        UpdateCommandBuffer();

        if (_light != null &&_command1 != null)
        {
            _light.AddCommandBuffer(LightEvent.AfterScreenspaceMask, _command1);
            _light.AddCommandBuffer(LightEvent.AfterScreenspaceMask, _command2);
        }
    }

    void OnPreRender()
    {
        // We can remove the command buffer before starting render in this
        // camera. Actually this should be done in OnPostRender, but it crashes
        // for some reasons. So, we do this in OnPreRender instead.
        if (_light != null &&_command1 != null)
        {
            _light.RemoveCommandBuffer(LightEvent.AfterScreenspaceMask, _command1);
            _light.RemoveCommandBuffer(LightEvent.AfterScreenspaceMask, _command2);
        }
    }

    void Update()
    {
        // We require the camera depth texture.
        GetComponent<Camera>().depthTextureMode |= DepthTextureMode.Depth;
    }

    #endregion

    #region Command buffer management

    void UpdateCommandBuffer()
    {
        var camera = GetComponent<Camera>();
        var scrWidth = camera.pixelWidth;
        var scrHeight = camera.pixelHeight;
        var maskFormat = RenderTextureFormat.R8;

        // Clear existing command buffers.
        if (_command1 != null)
        {
            _command1.Clear();
            _command2.Clear();
        }

        // Do nothing if the target light is not set.
        if (_light == null) return;

        // Lazy initialization of temporary objects.
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

        // Update the common shader parameters.
        _material.SetFloat("_RejectionDepth", _rejectionDepth);
        _material.SetInt("_SampleCount", _sampleCount);
        _material.SetFloat("_Convergence", _convergenceSpeed * _convergenceSpeed);

        // Calculate the light vector in the view space.
        _material.SetVector("_LightVector",
            transform.InverseTransformDirection(-_light.transform.forward) *
            _light.shadowBias / (_sampleCount - 1.5f)
        );

        // Scale factor: Screen coordinate -> Noise texture cooridinate
        var noiseTexture = _noiseTextures.GetTexture();
        var noiseScale = new Vector2(scrWidth, scrHeight) / noiseTexture.width;
        _material.SetVector("_NoiseScale", noiseScale);

        // View-Projection matrix difference from the previous frame
        var currentVP = CalculateVPMatrix();
        _material.SetMatrix("_NonJitteredVP", currentVP);
        _material.SetMatrix("_PreviousVP", _previousVP);

        // Discard the temp mask (used in the previous frame) and recreate it.
        RenderTexture.ReleaseTemporary(_tempMaskRT);
        _tempMaskRT = RenderTexture.GetTemporary(scrWidth, scrHeight, 0, maskFormat);

        // Render the shadow mask within the first command buffer.
        _command1.SetGlobalTexture(Shader.PropertyToID("_ShadowMask"), BuiltinRenderTextureType.CurrentActive);
        _command1.SetGlobalTexture(Shader.PropertyToID("_NoiseTex"), noiseTexture);
        _command1.SetRenderTarget(_tempMaskRT);
        _command1.DrawProcedural(Matrix4x4.identity, _material, 0, MeshTopology.Triangles, 3);

        // Allocate a new mask RT.
        var newMaskRT = RenderTexture.GetTemporary(scrWidth, scrHeight, 0, maskFormat);

        // Apply the temporal filter and blend it to the screen-space shadow
        // mask within the second command buffer.
        _mrt[0] = BuiltinRenderTextureType.CurrentActive;
        _mrt[1] = newMaskRT;
        _command2.SetRenderTarget(_mrt, BuiltinRenderTextureType.CurrentActive);
        _command2.SetGlobalTexture(Shader.PropertyToID("_PrevMask"), _prevMaskRT);
        _command2.SetGlobalTexture(Shader.PropertyToID("_TempMask"), _tempMaskRT);
        _command2.DrawProcedural(Matrix4x4.identity, _material, 1, MeshTopology.Triangles, 3);

        // Update the history.
        RenderTexture.ReleaseTemporary(_prevMaskRT);
        _prevMaskRT = newMaskRT;
        _previousVP = currentVP;
    }

    #endregion
}
