using System.Collections.Generic;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_Dropdown))]
public class WebcamDropdown : MonoBehaviour
{
    [SerializeField] private WebcamRenderer webcamRenderer;
    private TMP_Dropdown _dropdown;

    private void Awake() => _dropdown = GetComponent<TMP_Dropdown>();
    private void Start() => RefreshList();
    private void OnEnable()
    {
        _dropdown.onValueChanged.AddListener(OnDropdownChanged);
        webcamRenderer.onDeviceChanged.AddListener(OnRendererDeviceChanged);
    }
    private void OnDisable()
    {
        _dropdown.onValueChanged.RemoveListener(OnDropdownChanged);
        webcamRenderer.onDeviceChanged.RemoveListener(OnRendererDeviceChanged);
    }

    public void RefreshList()
    {
        _dropdown.ClearOptions();
        _dropdown.AddOptions(new List<string>(webcamRenderer.GetDeviceNames()));
        
        _dropdown.SetValueWithoutNotify(Mathf.Max(0, webcamRenderer.CurrentDeviceIndex));
        _dropdown.RefreshShownValue();
    }

    private void OnDropdownChanged(int index)
    {
        webcamRenderer.SetDevice(index);
    }
    private void OnRendererDeviceChanged(int index)
    {
        _dropdown.SetValueWithoutNotify(index);
        _dropdown.RefreshShownValue();
    }
}