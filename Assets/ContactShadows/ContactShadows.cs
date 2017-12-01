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
        [SerializeField] bool _downsample = false;

        #endregion

        #region Internal resources

        [SerializeField, HideInInspector] Shader _shader;
        [SerializeField, HideInInspector] NoiseTextureSet _noiseTextures;

        #endregion

        #region Temporary objects

        Material _material;
        RenderTexture _prevMaskRT1, _prevMaskRT2;
        CommandBuffer _command1, _command2;

        // We track the VP matrix without using previousViewProjectionMatrix
        // because it's not available for use in OnPreCull.
        Matrix4x4 _previousVP = Matrix4x4.identity;

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

            if (_prevMaskRT1 != null) RenderTexture.ReleaseTemporary(_prevMaskRT1);
            if (_prevMaskRT2 != null) RenderTexture.ReleaseTemporary(_prevMaskRT2);

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
                _command1.Clear();
                _command2.Clear();
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
        Vector2Int GetScreenSize()
        {
            var cam = Camera.current;
            var div = _downsample ? 2 : 1;
            return new Vector2Int(cam.pixelWidth / div, cam.pixelHeight / div);
        }

        // Update the temporary objects for the current frame.
        void UpdateTempObjects()
        {
            if (_prevMaskRT2 != null)
            {
                RenderTexture.ReleaseTemporary(_prevMaskRT2);
                _prevMaskRT2 = null;
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
            else
            {
                _command1.Clear();
                _command2.Clear();
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

            // "Reproject into the previous view" matrix
            _material.SetMatrix("_Reprojection", _previousVP * transform.localToWorldMatrix);
            _previousVP = CalculateVPMatrix();
        }

        // Build the command buffer for the current frame.
        void BuildCommandBuffer()
        {
            var maskSize = GetScreenSize();
            var maskFormat = RenderTextureFormat.R8;

            // Do raytracing and output to the unfiltered mask RT.
            var unfilteredMaskID = Shader.PropertyToID("_UnfilteredMask");
            _command1.SetGlobalTexture(Shader.PropertyToID("_ShadowMask"), BuiltinRenderTextureType.CurrentActive);
            _command1.GetTemporaryRT(unfilteredMaskID, maskSize.x, maskSize.y, 0, FilterMode.Point, maskFormat);
            _command1.SetRenderTarget(unfilteredMaskID);
            _command1.DrawProcedural(Matrix4x4.identity, _material, 0, MeshTopology.Triangles, 3);

            // Apply the temporal filter and output to the temporary shadow mask RT.
            var tempMaskRT = RenderTexture.GetTemporary(maskSize.x, maskSize.y, 0, maskFormat);
            _command1.SetGlobalTexture(Shader.PropertyToID("_PrevMask"), _prevMaskRT1);
            _command1.SetRenderTarget(tempMaskRT);
            _command1.DrawProcedural(Matrix4x4.identity, _material, 1 + (Time.frameCount & 1), MeshTopology.Triangles, 3);

            // Composite with the shadow buffer within the second command buffer.
            _command2.SetGlobalTexture(Shader.PropertyToID("_TempMask"), tempMaskRT);
            _command2.DrawProcedural(Matrix4x4.identity, _material, 3, MeshTopology.Triangles, 3);

            // Update the filter history.
            _prevMaskRT2 = _prevMaskRT1;
            _prevMaskRT1 = tempMaskRT;
        }

        #endregion
    }
}
