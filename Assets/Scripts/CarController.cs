using System.Collections;
using System.Collections.Generic;
using UnityEngine;

internal enum CarDriveType
{
    FrontWheelDrive,
    RearWheelDrive,
    AllWheelDrive
}

public enum SpeedType
{
    MPH,
    KPH
}

[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
    [Header("Car Presets")]
    [SerializeField] private CarDriveType _carDriveType = CarDriveType.AllWheelDrive;
    public SpeedType _speedType = SpeedType.MPH;

    [Header("Car Components"), Space(7)]
    [SerializeField] private GameObject[] _wheelMeshes = new GameObject[4];
    public WheelCollider[] wheelColliders = new WheelCollider[4];
    [SerializeField] private WheelEffects[] _wheelEffects = new WheelEffects[4];
    [SerializeField] private MeshRenderer _brakeLightMeshRenderer;
    [SerializeField] private MeshRenderer _reverseLightMeshRenderer;

    [Header("Car Settings"), Space(7)]
    [Tooltip("The amount of offset to apply to the rigidbody center of mass.")]
    [SerializeField] private Vector3 _centerOfMassOffset;
    [Tooltip("How far the wheels can turn."), Range(20f, 35f)]
    [SerializeField] private float _maximumSteerAngle;
    [Tooltip("How much torque to add to the drive wheels when moving forward.")]
    [SerializeField] private float _fullTorqueOverAllWheels;
    [Tooltip("How much torque to add to the drive wheels in reverse.")]
    [SerializeField] private float _reverseTorque;
    [Tooltip("How much force should be used for the handbrake.")]
    [SerializeField] private float _maxHandbrakeTorque;
    [Tooltip("Will limit how fast the car can go.")]
    [SerializeField] private float _topSpeed = 200.0f;
    [Tooltip("The limit of the rev range.")]
    [SerializeField] private float _revRangeBoundary = 1f;
    [Tooltip("How much slip until wheel effects start playing."), Range(0.1f, 1f)]
    [SerializeField] private float _slipLimit;
    [Tooltip("How much force will be used to apply the brakes")]
    [SerializeField] private float _brakeTorque;
    [Tooltip("How quickly digital input reaches the max value.")]
    [SerializeField] private float _smoothInputSpeed = 0.2f;
    private static int NumberOfGears = 5;

    [Header("Steering Helpers"), Space(7)]
    [Tooltip("How much force will be applied to the wheels to prevent flipping. (A good value is around the spring value of the wheel collider.")]
    [SerializeField] private float _antiRollVal = 3500.0f;
    [Tooltip("How much down force to add to the car.")]
    [SerializeField] private float _downForce = 100.0f;
    [Tooltip("0 is pure physics, 1 the car will grip in the direction it's facing.")]
    [SerializeField, Range(0, 1)] private float _steerHelper;
    [Tooltip("0 is no traction control, 1 will try and prevent any slipping")]
    [SerializeField, Range(0, 1)] private float _tractionControl;


    private Quaternion[] _wheelMeshLocalRotations;
    private float _steerAngle;
    private int _gearNum;
    private float _gearFactor;
    private float _oldRotation;
    private float _currentTorque;
    private Rigidbody _rigidbody;
    private Vector2 _currentInputVector;
    private Vector2 _smoothInputVelocity;
    private int _emissionPropertyId;
    private float _currentMaxSteerAngle;

    public bool Skidding { get; private set; }
    public float BrakeInput { get; private set; }
    public float CurrentSteerAngle { get { return _steerAngle; } }
    public float CurrentSpeed { get { return _speedType == SpeedType.MPH ? _rigidbody.velocity.magnitude * 2.23693629f : _rigidbody.velocity.magnitude * 3.6f; } }
    public float MaxSpeed { get { return _topSpeed; } }
    public float Revs { get; private set; }
    public float AccelInput { get; private set; }

    private void Awake()
    {
        _wheelMeshLocalRotations = new Quaternion[4];
        for (int i = 0; i < 4; i++)
        {
            _wheelMeshLocalRotations[i] = _wheelMeshes[i].transform.localRotation;
        }
        

        _maxHandbrakeTorque = float.MaxValue;
        _rigidbody = GetComponent<Rigidbody>();
        _currentTorque = _fullTorqueOverAllWheels - (_tractionControl * _fullTorqueOverAllWheels);
        _rigidbody.centerOfMass += _centerOfMassOffset;
        _emissionPropertyId = Shader.PropertyToID("_EmissionColor");
    }

    private void GearChanging()
    {
        float f = Mathf.Abs(CurrentSpeed / MaxSpeed);
        float upGearLimit = (1 / (float)NumberOfGears) * (_gearNum + 1);
        float downGearLimit = (1 / (float)NumberOfGears) * _gearNum;

        if (_gearNum > 0 && f < downGearLimit)
            _gearNum--;

        if (f > upGearLimit && (_gearNum <(NumberOfGears - 1)))
            _gearNum++;
    }

    private static float CurveFactor(float factor)
    {
        return 1 - (1 - factor) * (1 - factor);
    }

    private static float UnclampedLerp(float from, float to, float value)
    {
        return (1.0f - value) * from + value * to;
    }

    private void CalculateGearFactor()
    {
        float f = (1 / (float)NumberOfGears);

        float targetGearFactor = Mathf.InverseLerp(f * _gearNum, f * (_gearNum + 1), Mathf.Abs(CurrentSpeed / MaxSpeed));
        _gearFactor = Mathf.Lerp(_gearFactor, targetGearFactor, Time.deltaTime * 5.0f);
    }

    private void CalculateRevs()
    {
        CalculateGearFactor();
        float gearNumFactor = _gearNum / (float)NumberOfGears;
        float revsRangeMin = UnclampedLerp(0f, _revRangeBoundary, CurveFactor(gearNumFactor));
        float revsRangeMax = UnclampedLerp(_revRangeBoundary, 1f, gearNumFactor);
        Revs = UnclampedLerp(revsRangeMin, revsRangeMax, _gearFactor);
    }

    public void Move(float steering, float accel, float footBrake, float handBrake)
    {
        Vector2 input = new Vector2(steering, accel);
        _currentInputVector = Vector2.SmoothDamp(_currentInputVector, input, ref _smoothInputVelocity, _smoothInputSpeed);
        accel = _currentInputVector.y;
        steering = _currentInputVector.x;

        for (int i = 0; i < 4; i++)
        {
            wheelColliders[i].GetWorldPose(out Vector3 position, out Quaternion quat);
            _wheelMeshes[i].transform.SetPositionAndRotation(position, quat);
        }

        steering = Mathf.Clamp(steering, -1, 1);
        AccelInput = accel = Mathf.Clamp(accel, 0, 1);
        BrakeInput = footBrake = -1 * Mathf.Clamp(footBrake, -1, 0);
        handBrake = Mathf.Clamp(handBrake, 0, 1);

        _steerAngle = steering * _currentMaxSteerAngle;
        wheelColliders[0].steerAngle = _steerAngle;
        wheelColliders[1].steerAngle = _steerAngle;

        SteerHelper();
        ApplyDrive(accel, footBrake);
        CapSpeed();

        if (handBrake > 0f)
        {
            float handBrakeTorque = handBrake * _maxHandbrakeTorque;
            wheelColliders[2].brakeTorque = handBrakeTorque;
            wheelColliders[3].brakeTorque = handBrakeTorque;
            TurnBrakeLightsOn();
        }
        else
        {
            wheelColliders[2].brakeTorque = 0f;
            wheelColliders[3].brakeTorque = 0f;
        }

        CalculateRevs();
        GearChanging();
        AddDownForce();
        CheckForWheelSpin();
        TractionControl();
        AntiRoll();
        SetSteerAngle();
    }

    private void CapSpeed()
    {
        float speed = _rigidbody.velocity.magnitude;
        switch (_speedType)
        {
            case SpeedType.MPH:
                speed *= 2.23693629f;
                if (speed > _topSpeed)
                    _rigidbody.velocity = (_topSpeed / 2.23693629f) * _rigidbody.velocity.normalized;
                break;

            case SpeedType.KPH:
                speed *= 3.6f;
                if (speed > _topSpeed)
                    _rigidbody.velocity = (_topSpeed / 3.6f) * _rigidbody.velocity.normalized;
                break;
        }
    }

    private void ApplyDrive(float accel, float footBrake)
    {
        float thrustTorque;
        switch (_carDriveType)
        {
            case CarDriveType.FrontWheelDrive:
                thrustTorque = accel * (_currentTorque / 2f);
                wheelColliders[0].motorTorque = wheelColliders[1].motorTorque = thrustTorque;
                break;
            case CarDriveType.RearWheelDrive:
                thrustTorque = accel * (_currentTorque / 2f);
                wheelColliders[2].motorTorque = wheelColliders[3].motorTorque = thrustTorque;
                break;
            case CarDriveType.AllWheelDrive:
                thrustTorque = accel * (_currentTorque / 4f);
                for (int i = 0; i < 4; i++)
                {
                    wheelColliders[i].motorTorque = thrustTorque;
                }
                break;
        }

        for (int i = 0; i < 4; i++)
        {
            if (CurrentSpeed > 5 && Vector3.Angle(transform.forward, _rigidbody.velocity) < 50f)
            {
                wheelColliders[i].brakeTorque = _brakeTorque * footBrake;
            } 
            else if (footBrake > 0)
            {
                wheelColliders[i].brakeTorque = 0f;
                wheelColliders[i].motorTorque = -_reverseTorque * footBrake;
            }
        }

        if (footBrake > 0)
        {
            if (CurrentSpeed > 5 && Vector3.Angle(transform.forward, _rigidbody.velocity) < 50f)
            {
                TurnBrakeLightsOn();
            }
            else
            {
                TurnBrakeLightsOff();
                TurnReverseLightsOn();
            }
        }
        else
        {
            TurnBrakeLightsOff();
            TurnReverseLightsOff();
        }

    }

    private void SteerHelper()
    {
        for (int i = 0; i < 4; i++)
        {
            wheelColliders[i].GetGroundHit(out WheelHit wheelHit);
            if (wheelHit.normal == Vector3.zero)
                return;
        }

        if (Mathf.Abs(_oldRotation - transform.eulerAngles.y) < 10f)
        {
            float turnAdjust = (transform.eulerAngles.y - _oldRotation) * _steerHelper;
            Quaternion velRotation = Quaternion.AngleAxis(turnAdjust, Vector3.up);
            _rigidbody.velocity = velRotation * _rigidbody.velocity;
        }

        _oldRotation = transform.eulerAngles.y;
    }

    private void AntiRoll()
    {
        float travelL = 1.0f;
        float travelR = 1.0f;
        bool groundedLf = wheelColliders[0].GetGroundHit(out WheelHit wheelHit);

        if (groundedLf)
            travelL = (-wheelColliders[0].transform.InverseTransformPoint(wheelHit.point).y - wheelColliders[0].radius) / wheelColliders[0].suspensionDistance;

        bool groundedRf = wheelColliders[1].GetGroundHit(out wheelHit);

        if (groundedRf)
            travelR = (-wheelColliders[1].transform.InverseTransformPoint(wheelHit.point).y - wheelColliders[1].radius) / wheelColliders[1].suspensionDistance;

        float antiRollForce = (travelL - travelR) * _antiRollVal;

        if (groundedLf)
            _rigidbody.AddForceAtPosition(wheelColliders[0].transform.up * -antiRollForce, wheelColliders[0].transform.position);

        if (groundedRf)
            _rigidbody.AddForceAtPosition(wheelColliders[1].transform.up * antiRollForce, wheelColliders[1].transform.position);

        bool groundedLr = wheelColliders[2].GetGroundHit(out wheelHit);

        if (groundedLr)
            travelL = (-wheelColliders[2].transform.InverseTransformPoint(wheelHit.point).y - wheelColliders[2].radius) / wheelColliders[2].suspensionDistance;

        bool groundedRr = wheelColliders[3].GetGroundHit(out wheelHit);

        if (groundedRr)
            travelR = (-wheelColliders[3].transform.InverseTransformPoint(wheelHit.point).y - wheelColliders[3].radius) / wheelColliders[3].suspensionDistance;

        antiRollForce = (travelL - travelR) * _antiRollVal;

        if (groundedLr)
            _rigidbody.AddForceAtPosition(wheelColliders[2].transform.up * -antiRollForce, wheelColliders[2].transform.position);

        if (groundedRr)
            _rigidbody.AddForceAtPosition(wheelColliders[3].transform.up * antiRollForce, wheelColliders[3].transform.position);
    }

    private void AddDownForce()
    {
        if (_downForce > 0)
            _rigidbody.AddForce(_downForce * _rigidbody.velocity.magnitude * -transform.up);
    }

    private void CheckForWheelSpin()
    {
        for (int i = 0; i < 4; i++)
        {
            wheelColliders[i].GetGroundHit(out WheelHit wheelHit);

            if (Mathf.Abs(wheelHit.forwardSlip) >= _slipLimit || Mathf.Abs(wheelHit.sidewaysSlip) >= _slipLimit)
            {
                _wheelEffects[i].EmitTireSmoke();

                if (!AnySkidSoundPlaying())
                {
                    _wheelEffects[i].PlayAudio();
                }
                continue;
            }

            if (_wheelEffects[i].IsPlayingAudio)
                _wheelEffects[i].StopAudio();

            _wheelEffects[i].EndSkidTrail();
        }
    }

    void TractionControl()
    {
        WheelHit wheelHit;
        switch (_carDriveType)
        {
            case CarDriveType.FrontWheelDrive:
                wheelColliders[0].GetGroundHit(out wheelHit);
                AdjustTorque(wheelHit.forwardSlip);

                wheelColliders[1].GetGroundHit(out wheelHit);
                AdjustTorque(wheelHit.forwardSlip);
                break;
            case CarDriveType.RearWheelDrive:
                wheelColliders[2].GetGroundHit(out wheelHit);
                AdjustTorque(wheelHit.forwardSlip);

                wheelColliders[3].GetGroundHit(out wheelHit);
                AdjustTorque(wheelHit.forwardSlip);
                break;
            case CarDriveType.AllWheelDrive:
                for (int i = 0; i < 4; i++)
                {
                    wheelColliders[i].GetGroundHit(out wheelHit);
                    AdjustTorque(wheelHit.forwardSlip);
                }
                break;
        }
    }

    private void AdjustTorque(float forwardSlip)
    {
        if (forwardSlip >= _slipLimit && _currentTorque >= 0)
        {
            _currentTorque -= 10 * _tractionControl;
        }
        else
        {
            _currentTorque += 10 * _tractionControl;
            if (_currentTorque > _fullTorqueOverAllWheels)
            {
                _currentTorque = _fullTorqueOverAllWheels;
            }
        }
    }

    private bool AnySkidSoundPlaying()
    {
        for (int i = 0; i < 4; i++)
        {
            if (_wheelEffects[i].IsPlayingAudio)
            {
                return true;
            }
        }
        return false;
    }

    private void TurnBrakeLightsOn()
    {
        _brakeLightMeshRenderer.material.SetColor(_emissionPropertyId, Color.white);

        if (!_brakeLightMeshRenderer.material.IsKeywordEnabled("_EMISSION"))
            _brakeLightMeshRenderer.material.EnableKeyword("_EMISSION");
    }

    private void TurnBrakeLightsOff()
    {
        _brakeLightMeshRenderer.material.SetColor(_emissionPropertyId, Color.black);
    }

    private void TurnReverseLightsOn()
    {
        _reverseLightMeshRenderer.material.SetColor(_emissionPropertyId, Color.white);

        if (!_reverseLightMeshRenderer.material.IsKeywordEnabled("_EMISSION"))
            _reverseLightMeshRenderer.material.EnableKeyword("_EMISSION");
    }

    private void TurnReverseLightsOff()
    {
        _reverseLightMeshRenderer.material.SetColor(_emissionPropertyId, Color.black);
    }

    private void SetSteerAngle()
    {
        if (CurrentSpeed < 25f)
        {
            _currentMaxSteerAngle = Mathf.MoveTowards(_currentMaxSteerAngle, _maximumSteerAngle, 0.5f);
        }
        else if (CurrentSpeed > 25f && CurrentSpeed < 60f)
        {
            _currentMaxSteerAngle = Mathf.MoveTowards(_currentMaxSteerAngle, _maximumSteerAngle / 1.5f, 0.5f);
        }
        else if (CurrentSpeed > 60)
        {
            _currentMaxSteerAngle = Mathf.MoveTowards(_currentMaxSteerAngle, _maximumSteerAngle / 2f, 0.5f);
        }
    }
}
