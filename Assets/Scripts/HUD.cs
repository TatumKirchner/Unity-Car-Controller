using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HUD : MonoBehaviour
{
    [SerializeField] private Text _speedText;
    [SerializeField] private CarController _carController;

    private void Update()
    {
        string speedType = _carController._speedType == SpeedType.MPH ? " MPH" : " KPH";
        _speedText.text = Mathf.RoundToInt(_carController.CurrentSpeed).ToString() + speedType;
    }
}
