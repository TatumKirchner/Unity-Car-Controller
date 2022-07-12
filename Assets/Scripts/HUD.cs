using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HUD : MonoBehaviour
{
    [SerializeField] private Text _speedText;
    [SerializeField] private CarController _carController;
    [SerializeField] private GameObject _controlsUiPanel;
    [SerializeField] private GameObject _pausePanel;
    [SerializeField] private Toggle _mphToggle;
    [SerializeField] private Toggle _kphToggle;
    [SerializeField] private GameObject _speedTextPanel;
    private string _speedType;

    private void Start()
    {
        _speedType = _carController._speedType == SpeedType.MPH ? " MPH" : " KPH";
        PlayerInput.OnPause += TogglePause;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void LateUpdate()
    {
        _speedText.text = Mathf.RoundToInt(_carController.CurrentSpeed).ToString() + _speedType;
    }

    public void UseMPHToggle(bool value)
    {
        if (value)
        {
            _speedTextPanel.SetActive(true);
            _carController._speedType = SpeedType.MPH;
            _speedType = " MPH";
            _kphToggle.isOn = !value;
        }
        else if (!_mphToggle.isOn && !_kphToggle.isOn)
        {
            _speedTextPanel.SetActive(false);
        }
    }

    public void UseKPHToggle(bool value)
    {
        if (value)
        {
            _speedTextPanel.SetActive(true);
            _carController._speedType = SpeedType.KPH;
            _speedType = " KPH";
            _mphToggle.isOn = !value;
        }
        else if (!_mphToggle.isOn && !_kphToggle.isOn)
        {
            _speedTextPanel.SetActive(false);
        }
    }

    public void ToggleSound(bool value)
    {
        if (value)
        {
            AudioListener.volume = 1f;
        }
        else
        {
            AudioListener.volume = 0f;
        }
    }

    public void ToggleControlsUi(bool value)
    {
        _controlsUiPanel.SetActive(value);
    }

    private void TogglePause(bool value)
    {
        _pausePanel.SetActive(value);

        if (value) 
        {
            EventSystem.current.SetSelectedGameObject(_mphToggle.gameObject);
            AudioListener.pause = true;
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            EventSystem.current.SetSelectedGameObject(null);
            AudioListener.pause = false;
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
}
