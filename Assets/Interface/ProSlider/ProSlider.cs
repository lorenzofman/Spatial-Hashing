using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProSlider : MonoBehaviour
{
    [SerializeField] private string sliderLabel;
    [SerializeField] private TextMeshProUGUI value; 
    [SerializeField] private Slider slider;
    public Slider.SliderEvent onValueChanged;

    private void Start()
    {
        slider.onValueChanged.AddListener(OnValueChange);
    }

    private void OnValueChange(float sliderValue)
    {
        value.text = $"{sliderLabel}: {sliderValue}";
        onValueChanged.Invoke(sliderValue);
    }
}
