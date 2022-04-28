using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResetCar : MonoBehaviour
{
    private Vector3 _startPosition;
    private Quaternion _startRotation;
    private Rigidbody _rigidbody;

    private void Start()
    {
        _startPosition = transform.position;
        _startRotation = transform.rotation;
        _rigidbody = GetComponent<Rigidbody>();
    }

    public void Respawn()
    {
        _rigidbody.velocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
        _rigidbody.MovePosition(_startPosition);
        _rigidbody.MoveRotation(_startRotation);
    }
}
