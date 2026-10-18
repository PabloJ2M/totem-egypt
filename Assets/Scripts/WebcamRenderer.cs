using System;
using UnityEngine;
using UnityEngine.Events;

public class WebcamRenderer : MonoBehaviour
{
    [Header("Camera")] [SerializeField] private string deviceName = "";
    [SerializeField] private int requestedWidth = 1280;
    [SerializeField] private int requestedHeight = 720;
    [SerializeField] private int requestedFps = 30;

    [Header("Output")] [SerializeField] private int outputSize = 512;
    [SerializeField] private bool mirrorHorizontally;

    [Header("Zoom")] [Range(0f, 1f)] [SerializeField]
    private float zoom;

    [SerializeField] private int minCropPixels = 128;

    public UnityEvent<Texture> onTextureReady;
    public UnityEvent<int> onDeviceChanged;

    private WebCamDevice[] _devices = Array.Empty<WebCamDevice>();
    private WebCamTexture _webcam;
    private RenderTexture _output;
    private int _currentIndex = -1;
    private bool _isReady;
    private bool _textureAnnounced;

    public RenderTexture OutputTexture => _output;
    public int CurrentDeviceIndex => _currentIndex;

    private void Awake()
    {
        RefreshDevices();
        _currentIndex = ResolveInitialIndex();
    }
    private void Start()
    {
        if (_currentIndex >= 0) StartDevice(_currentIndex);
        else Debug.LogWarning($"[{nameof(WebcamRenderer)}] No webcam detected.", this);
    }
    private void Update()
    {
        if (!_webcam) return;
        
        if (!_isReady) {
            if (_webcam.width <= 16) return;
            _isReady = true;
            
            if (!_output) CreateOutput();
            if (!_textureAnnounced) {
                _textureAnnounced = true;
                onTextureReady?.Invoke(_output);
            }
        }

        if (_webcam.didUpdateThisFrame) Render();
    }
    private void OnEnable()
    {
        if (_webcam && !_webcam.isPlaying)
            _webcam.Play();
    }
    private void OnDisable()
    {
        if (!_webcam && _webcam.isPlaying)
            _webcam.Stop();
    }
    private void OnDestroy()
    {
        StopAndDisposeWebcam();
        if (!_output) return;
        _output.Release();
        Destroy(_output);
    }

    private void StartWebcam()
    {
        var devices = WebCamTexture.devices;
        if (devices.Length == 0)
        {
            Debug.LogWarning($"[{nameof(WebcamRenderer)}] There is no webcam.", this);
            return;
        }

        var chosen = devices[0].name;
        if (!string.IsNullOrEmpty(deviceName))
        {
            foreach (var d in devices)
            {
                if (!d.name.Contains(deviceName)) continue;
                chosen = d.name;
                break;
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

        if (mirrorHorizontally)
        {
            scale.x = -sx;
            offset.x = ox + sx;
        }

        Graphics.Blit(_webcam, _output, scale, offset);
    }
    private void CreateOutput()
    {
        _output = new RenderTexture(outputSize, outputSize, 0, RenderTextureFormat.ARGB32)
        {
            name = "WebcamRT",
            filterMode = FilterMode.Bilinear
        };

        _output.Create();
    }

    public void RefreshDevices() => _devices = WebCamTexture.devices;
    public string[] GetDeviceNames()
    {
        var names = new string[_devices.Length];
        for (var i = 0; i < _devices.Length; i++) names[i] = _devices[i].name;
        return names;
    }
    private int ResolveInitialIndex()
    {
        if (_devices.Length == 0) return -1;
        if (string.IsNullOrEmpty(deviceName)) return 0;
        
        for (var i = 0; i < _devices.Length; i++)
            if (_devices[i].name.Contains(deviceName)) return i;
        
        return 0;
    }

    public void SetDevice(int index)
    {
        if (index < 0 || index >= _devices.Length) return;
        if (index == _currentIndex && _webcam && _webcam.isPlaying) return;
        StartDevice(index);
    }
    private void StartDevice(int index)
    {
        StopAndDisposeWebcam();

        _currentIndex = index;
        _isReady = false;

        _webcam = new WebCamTexture(_devices[index].name, requestedWidth, requestedHeight, requestedFps);
        _webcam.Play();

        onDeviceChanged?.Invoke(index);
    }
    private void StopAndDisposeWebcam()
    {
        if (!_webcam) return;
        if (_webcam.isPlaying) _webcam.Stop();
        Destroy(_webcam);
        _webcam = null;
    }
    
    public void SetZoom(float value) => zoom = Mathf.Clamp01(value);
    public void Flip(bool value) => mirrorHorizontally = value;
}