using UnityEngine;

[ExecuteInEditMode]
public sealed class GrassRenderer : MonoBehaviour
{
    [SerializeField] Mesh _sourceMesh;
    [SerializeField] float _extent = 1;
    [SerializeField] int _instanceCount = 100;
    [SerializeField] Color _color = Color.white;
    [SerializeField] float _scale = 1;

    [SerializeField, HideInInspector] Shader _shader;

    ComputeBuffer _drawArgsBuffer;

    Material _material;
    MaterialPropertyBlock _sheet;

    void OnValidate()
    {
        _instanceCount = Mathf.Max(1, _instanceCount);
    }

    void OnDisable()
    {
        // Release the compute buffers here not in OnDestroy because that's
        // too late to avoid compute buffer leakage warnings.
        if (_drawArgsBuffer != null)
        {
            _drawArgsBuffer.Release();
            _drawArgsBuffer = null;
        }
    }

    void OnDestroy()
    {
        if (_material)
        {
            if (Application.isPlaying)
                Destroy(_material);
            else
                DestroyImmediate(_material);
        }
    }

    void Update()
    {
        if (_sourceMesh == null) return;

        // Lazy initialization.
        if (_drawArgsBuffer == null)
            _drawArgsBuffer = new ComputeBuffer(
                1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);

        if (_material == null) _material = new Material(_shader);
        if (_sheet == null) _sheet = new MaterialPropertyBlock();

        // Bounding box (not accurate but enough for culling roughly)
        var bounds = new Bounds(
            transform.position,
            new Vector3(_extent, _sourceMesh.bounds.size.magnitude, _extent)
        );

        // Shader properties
        _sheet.SetColor("_Color", _color);
        _sheet.SetFloat("_Scale", _scale);
        _sheet.SetFloat("_Extent", _extent);
        _sheet.SetFloat("_Time", Application.isPlaying ? Time.time : 0);
        _sheet.SetMatrix("_LocalToWorld", transform.localToWorldMatrix);
        _sheet.SetMatrix("_WorldToLocal", transform.worldToLocalMatrix);

        // Instanced indirect drawing.
        _drawArgsBuffer.SetData(new uint[5] {
            _sourceMesh.GetIndexCount(0), (uint)_instanceCount, 0, 0, 0 });

        Graphics.DrawMeshInstancedIndirect(
            _sourceMesh, 0, _material, bounds, _drawArgsBuffer, 0, _sheet
        );
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0, 1, 1, 0.3f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(_extent, 0, _extent) * 2);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(_extent, 0, _extent) * 2);
    }
}
