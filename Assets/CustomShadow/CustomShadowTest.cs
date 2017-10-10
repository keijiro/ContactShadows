using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class CustomShadowTest : MonoBehaviour
{
    [SerializeField] Light _light;
    [SerializeField, Range(0, 5)] float _rejectionDepth = 0.5f;

    [SerializeField, HideInInspector] Shader _shader;

    Material _material;
    CommandBuffer _command;
    Light _boundLight;

    void OnEnable()
    {
        if (_material == null)
        {
            _material = new Material(_shader);
            _material.hideFlags = HideFlags.DontSave;
        }

        if (_command == null)
        {
            _command = new CommandBuffer();
            _command.name = "Contact Shadow";
            _command.DrawProcedural(Matrix4x4.identity, _material, 0, MeshTopology.Triangles, 3);
        }

        if (_light != null)
        {
            _light.AddCommandBuffer(LightEvent.AfterScreenspaceMask, _command);
            _boundLight = _light;
        }
    }

    void OnDisable()
    {
        if (_boundLight != null)
        {
            _boundLight.RemoveCommandBuffer(LightEvent.AfterScreenspaceMask, _command);
            _boundLight = null;
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
        GetComponent<Camera>().depthTextureMode |= DepthTextureMode.Depth;

        var lightDir = (_light != null) ? _light.transform.forward : Vector3.forward;
        _material.SetVector("_LightDirection", transform.InverseTransformDirection(-lightDir));

        _material.SetFloat("_RejectionDepth", _rejectionDepth);

        if (_boundLight != _light)
        {
            if (_boundLight != null)
                _boundLight.RemoveCommandBuffer(LightEvent.AfterScreenspaceMask, _command);

            if (_light != null)
                _light.AddCommandBuffer(LightEvent.AfterScreenspaceMask, _command);

            _boundLight = _light;
        }
    }
}
