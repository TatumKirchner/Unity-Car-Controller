using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class WheelEffects : MonoBehaviour
{
    public static Transform skidTrailsDetachedParent;

    [SerializeField] private Transform skidTrailPf;
    [SerializeField] private ParticleSystem skidParticles;
    [SerializeField] private float _skidTrailOffset = 0.1f;
    public bool IsSkidding { get; private set; }
    public bool IsPlayingAudio { get; private set; }

    private AudioSource _audioSource;
    private Transform _skidTrail;
    private WheelCollider _wheelCollider;

    private void Start()
    {
        if (skidParticles == null)
        {
            Debug.LogWarning("No smoke particle system found on car", gameObject);
        }
        else
        {
            skidParticles.Stop();
        }

        _wheelCollider = GetComponent<WheelCollider>();
        _audioSource = GetComponent<AudioSource>();
        IsPlayingAudio = false;

        if (skidTrailsDetachedParent == null)
            skidTrailsDetachedParent = new GameObject("SkidTrails - Detached").transform;
    }

    public void EmitTireSmoke()
    {
        skidParticles.transform.position = transform.position - transform.up * _wheelCollider.radius;
        skidParticles.Emit(1);
        if (!IsSkidding)
            StartCoroutine(StartSkidTrail());
    }

    public void PlayAudio()
    {
        _audioSource.Play();
        IsPlayingAudio = true;
    }

    public void StopAudio()
    {
        _audioSource.Stop();
        IsPlayingAudio = false;
    }

    public IEnumerator StartSkidTrail()
    {
        IsSkidding = true;
        _skidTrail = Instantiate(skidTrailPf);
        while (_skidTrail == null)
        {
            yield return null;
        }
        _skidTrail.parent = transform;
        _skidTrail.localPosition = -Vector3.up * (_wheelCollider.radius + _skidTrailOffset);
    }

    public void EndSkidTrail()
    {
        if (!IsSkidding)
            return;
        IsSkidding = false;
        _skidTrail.parent = skidTrailsDetachedParent;
        Destroy(_skidTrail.gameObject, 10f);
    }
}
