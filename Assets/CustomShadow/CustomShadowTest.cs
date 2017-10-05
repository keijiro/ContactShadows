using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class CustomShadowTest : MonoBehaviour
{
    [SerializeField] Light _light;
    [SerializeField, Range(0, 5)] float _rejectionDepth = 0.5f;
    [SerializeField] Shader _shader;

    Material _material;
    CommandBuffer _command;
    Camera _camera;


    void OnEnable()
    {
        _camera = GetComponent<Camera>();

        if (_material == null)
        {
            _material = new Material(_shader);
            _material.hideFlags = HideFlags.DontSave;
        }

        if (_light)
        {
            _command = new CommandBuffer();
            _command.name = "Contact Shadow";
            _command.DrawProcedural(Matrix4x4.identity, _material, 0, MeshTopology.Triangles, 3);

            _light.AddCommandBuffer(LightEvent.AfterScreenspaceMask, _command);
        }
    }

    void OnDisable()
    {
        if (_command != null)
        {
            _camera.RemoveCommandBuffer(CameraEvent.BeforeImageEffects, _command);
            _command.Dispose();
            _command = null;
        }
    }

    void OnDestroy()
    {
        if (_material != null)
        {
            if (Application.isPlaying)
                Destroy(_material);
            else
                DestroyImmediate(_material);
        }
    }

    void Update()
    {
        _camera.depthTextureMode |= DepthTextureMode.Depth;

        var lightDir = (_light != null) ? _light.transform.forward : Vector3.forward;
        _material.SetVector("_LightDirection", transform.InverseTransformDirection(-lightDir));

        _material.SetFloat("_RejectionDepth", _rejectionDepth);
    }
}
