using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput : MonoBehaviour
{
    private PlayerControls _playerControls;
    private CarController _carController;

    public float accel;
    public float handBrake;
    public float turn;

    private void Awake()
    {
        _playerControls = new PlayerControls();
        _playerControls.Player.Enable();

        //_playerControls.Player.Accelerate.performed += PlayerControls_Accelerate;
        //_playerControls.Player.HandBrake.performed += PlayerControls_HandBrake;
        //_playerControls.Player.Turn.performed += PlayerControls_Turn;

        _carController = GetComponent<CarController>();
    }

    private void Update()
    {
        accel = _playerControls.Player.Accelerate.ReadValue<float>();
        turn = _playerControls.Player.Turn.ReadValue<float>();
        handBrake = _playerControls.Player.HandBrake.ReadValue<float>();
    }

    private void FixedUpdate()
    {
        _carController.Move(turn, accel, accel, handBrake);
    }

    private void PlayerControls_Accelerate(InputAction.CallbackContext context)
    {
        accel = context.ReadValue<float>();
    }

    private void PlayerControls_HandBrake(InputAction.CallbackContext context)
    {
        handBrake = context.ReadValue<float>();
    }

    private void PlayerControls_Turn(InputAction.CallbackContext context)
    {
        turn = context.ReadValue<float>();
    }
}
