using UnityEngine;
using UnityEngine.Events;

public class WebcamRenderer : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private string deviceName = "";
    [SerializeField] private int requestedWidth = 1280;
    [SerializeField] private int requestedHeight = 720;
    [SerializeField] private int requestedFps = 30;

    [Header("Output")]
    [SerializeField] private int outputSize = 512;
    [SerializeField] private bool mirrorHorizontally;

    [Header("Zoom")]
    [Range(0f, 1f)] [SerializeField] private float zoom;
    [SerializeField] private int minCropPixels = 128;
    
    [SerializeField] private UnityEvent<Texture> onTextureReady;

    private WebCamTexture _webcam;
    private RenderTexture _output;
    private bool _isReady;
    
    // public RenderTexture OutputTexture => _output;

    private void Start() => StartWebcam();
    private void Update()
    {
        if (!_webcam) return;
        
        if (!_isReady) {
            if (_webcam.width <= 16) return;
            CreateOutput();
            _isReady = true;
            onTextureReady?.Invoke(_output);
        }
        
        if (_webcam.didUpdateThisFrame)
            Render();
    }
    private void OnDisable()
    {
        if (!_webcam && _webcam.isPlaying)
            _webcam.Stop();
    }
    private void OnDestroy()
    {
        if (_webcam != null) Destroy(_webcam);
        if (_output == null) return;
        _output.Release();
        Destroy(_output);
    }

    private void StartWebcam()
    {
        var devices = WebCamTexture.devices;
        if (devices.Length == 0) {
            Debug.LogWarning($"[{nameof(WebcamRenderer)}] There is no webcam.", this);
            return;
        }
        
        var chosen = devices[0].name;
        if (!string.IsNullOrEmpty(deviceName)) {
            foreach (var d in devices) {
                if (!d.name.Contains(deviceName)) continue;
                chosen = d.name; break;
            }
        }

        _webcam = new WebCamTexture(chosen, requestedWidth, requestedHeight, requestedFps);
        _webcam.Play();
    }
    private void Render()
    {
        var w = _webcam.width;
        var h = _webcam.height;
        
        var maxSide = Mathf.Min(w, h);
        var minSide = Mathf.Min(minCropPixels, maxSide);
        var cropSide = Mathf.Lerp(maxSide, minSide, zoom);
        
        var sx = cropSide / w;
        var sy = cropSide / h;

        var ox = (1f - sx) * 0.5f;
        var oy = (1f - sy) * 0.5f;

        var scale = new Vector2(sx, sy);
        var offset = new Vector2(ox, oy);
        
        if (mirrorHorizontally) {
            scale.x = -sx;
            offset.x = ox + sx;
        }
        
        Graphics.Blit(_webcam, _output, scale, offset);
    }
    private void CreateOutput()
    {
        _output = new RenderTexture(outputSize, outputSize, 0, RenderTextureFormat.ARGB32) {
            name = "WebcamRT",
            filterMode = FilterMode.Bilinear
        };
        
        _output.Create();
    }
    
    public void SetZoom(float value) => zoom = Mathf.Clamp01(value);
}
