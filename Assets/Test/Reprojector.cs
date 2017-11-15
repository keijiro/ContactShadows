using UnityEngine;
using System.Collections;

class Reprojector : MonoBehaviour
{
    [SerializeField] float _interval = 1;

    [SerializeField, HideInInspector] Shader _shader;

    Material _material;
    RenderTexture _history;

    void DiscardHistory()
    {
        if (_history != null)
        {
            RenderTexture.ReleaseTemporary(_history);
            _history = null;
        }
    }

    IEnumerator Start()
    {
        while (true)
        {
            DiscardHistory();
            yield return new WaitForSeconds(_interval);
        }
    }

    void Destroy()
    {
        DiscardHistory();

        if (_material != null) Destroy(_material);
    }

    void Update()
    {
        GetComponent<Camera>().depthTextureMode =
            DepthTextureMode.Depth | DepthTextureMode.MotionVectors;
    }

    void OnRenderImage(RenderTexture source, RenderTexture dest)
    {
        var size = new Vector2Int(source.width, source.height);

        if (_material == null)
            _material = new Material(_shader);

        if (_history == null)
        {
            _history = RenderTexture.GetTemporary(size.x, size.y, 0);
            Graphics.Blit(source, _history);
        }

        var camera = GetComponent<Camera>();
        var proj = GL.GetGPUProjectionMatrix(camera.nonJitteredProjectionMatrix, true);
        _material.SetMatrix("_NonJitteredVP", proj * camera.worldToCameraMatrix);
        _material.SetMatrix("_PreviousVP", camera.previousViewProjectionMatrix);

        var next = RenderTexture.GetTemporary(size.x, size.y, 0);

        Graphics.Blit(_history, next, _material, 0);
        Graphics.Blit(next, dest);

        RenderTexture.ReleaseTemporary(_history);
        _history = next;
    }
}
