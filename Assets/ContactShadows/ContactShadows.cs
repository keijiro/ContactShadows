// Experimental implementation of contact shadows for Unity
// https://github.com/keijiro/ContactShadows

using UnityEngine;
using UnityEngine.Rendering;

namespace PostEffects
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    public sealed class ContactShadows : MonoBehaviour
    {
        #region Editable attributes

        [SerializeField] Light _light;
        [SerializeField, Range(0, 5)] float _rejectionDepth = 0.5f;
        [SerializeField, Range(4, 32)] int _sampleCount = 16;
        [SerializeField, Range(0, 1)] float _temporalFilter = 0.5f;

        #endregion

        #region Internal resources

        [SerializeField, HideInInspector] Shader _shader;
        [SerializeField, HideInInspector] NoiseTextureSet _noiseTextures;

        #endregion

        #region Temporary objects

        Material _material;
        RenderTexture _tempMaskRT, _prevMaskRT;
        CommandBuffer _command1, _command2;

        // We track the VP matrix without using previousViewProjectionMatrix
        // because it's not available for use in OnPreCull.
        Matrix4x4 _previousVP;

        // MRT array; Reused between frames to avoid GC memory allocation.
        RenderTargetIdentifier[] _mrt = new RenderTargetIdentifier[2];

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
            // Update the temporary objects and build the command buffers for
            // the target light.

            UpdateTempObjects();

            if (_light != null)
            {
                BuildCommandBuffer();
                _light.AddCommandBuffer(LightEvent.AfterScreenspaceMask, _command1);
                _light.AddCommandBuffer(LightEvent.AfterScreenspaceMask, _command2);
            }
        }

        void OnPreRender()
        {
            // We can remove the command buffer before starting render in this
            // camera. Actually this should be done in OnPostRender, but it
            // crashes for some reasons. So, we do this in OnPreRender instead.

            if (_light != null)
            {
                _light.RemoveCommandBuffer(LightEvent.AfterScreenspaceMask, _command1);
                _light.RemoveCommandBuffer(LightEvent.AfterScreenspaceMask, _command2);
                // TODO: clear command buffer here?
            }
        }

        void Update()
        {
            // We require the camera depth texture.
            GetComponent<Camera>().depthTextureMode |= DepthTextureMode.Depth;
        }

        #endregion

        #region Internal methods

        // Calculates the view-projection matrix for GPU use.
        static Matrix4x4 CalculateVPMatrix()
        {
            var cam = Camera.current;
            var p = cam.nonJitteredProjectionMatrix;
            var v = cam.worldToCameraMatrix;
            return GL.GetGPUProjectionMatrix(p, true) * v;
        }

        // Get the screen dimensions.
        static Vector2Int GetScreenSize()
        {
            var cam = Camera.current;
            return new Vector2Int(cam.pixelWidth, cam.pixelHeight);
        }

        // Update the temporary objects for the current frame.
        void UpdateTempObjects()
        {
            // Clear existing command buffers.
            if (_command1 != null)
            {
                _command1.Clear();
                _command2.Clear();
            }

            // Discard the temp mask RT (used in the previous frame).
            if (_tempMaskRT != null)
            {
                RenderTexture.ReleaseTemporary(_tempMaskRT);
                _tempMaskRT = null;
            }

            // Do nothing below if the target light is not set.
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
                _command2.name = "Contact Shadow Temporal Filter";
            }

            // Update the common shader parameters.
            _material.SetFloat("_RejectionDepth", _rejectionDepth);
            _material.SetInt("_SampleCount", _sampleCount);

            var convergence = Mathf.Pow(1 - _temporalFilter, 2);
            _material.SetFloat("_Convergence", convergence);

            // Calculate the light vector in the view space.
            _material.SetVector("_LightVector",
                transform.InverseTransformDirection(-_light.transform.forward) *
                _light.shadowBias / (_sampleCount - 1.5f)
            );

            // Noise texture and its scale factor
            var noiseTexture = _noiseTextures.GetTexture();
            var noiseScale = (Vector2)GetScreenSize() / noiseTexture.width;
            _material.SetVector("_NoiseScale", noiseScale);
            _material.SetTexture("_NoiseTex", noiseTexture);

            // View-Projection matrix difference from the previous frame
            var currentVP = CalculateVPMatrix();
            _material.SetMatrix("_NonJitteredVP", currentVP);
            _material.SetMatrix("_PreviousVP", _previousVP);
            _previousVP = currentVP;
        }

        // Build the command buffer for the current frame.
        void BuildCommandBuffer()
        {
            var maskSize = GetScreenSize();
            var maskFormat = RenderTextureFormat.R8;

            // Allocate a temporary shadow mask RT (shared between command buffers).
            _tempMaskRT = RenderTexture.GetTemporary(maskSize.x, maskSize.y, 0, maskFormat);

            // Render the shadow mask within the first command buffer.
            _command1.SetGlobalTexture(Shader.PropertyToID("_ShadowMask"), BuiltinRenderTextureType.CurrentActive);
            _command1.SetRenderTarget(_tempMaskRT);
            _command1.DrawProcedural(Matrix4x4.identity, _material, 0, MeshTopology.Triangles, 3);

            if (_temporalFilter == 0)
            {
                // Simply blit the result to the shadow map texture.
                _command2.Blit(_tempMaskRT, BuiltinRenderTextureType.CurrentActive);
            }
            else
            {
                // Allocate a new shadow mask RT.
                var newMaskRT = RenderTexture.GetTemporary(maskSize.x, maskSize.y, 0, maskFormat);

                // Apply the temporal filter and blend it to the screen-space shadow
                // mask within the second command buffer.
                _mrt[0] = BuiltinRenderTextureType.CurrentActive;
                _mrt[1] = newMaskRT;
                _command2.SetRenderTarget(_mrt, BuiltinRenderTextureType.CurrentActive);
                _command2.SetGlobalTexture(Shader.PropertyToID("_PrevMask"), _prevMaskRT);
                _command2.SetGlobalTexture(Shader.PropertyToID("_TempMask"), _tempMaskRT);
                _command2.DrawProcedural(Matrix4x4.identity, _material, 1, MeshTopology.Triangles, 3);

                // Update the filter history.
                if (_prevMaskRT != null) RenderTexture.ReleaseTemporary(_prevMaskRT);
                _prevMaskRT = newMaskRT;
            }
        }

        #endregion
    }
}
