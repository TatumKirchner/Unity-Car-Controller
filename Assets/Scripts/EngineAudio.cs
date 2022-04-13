using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class EngineAudio : MonoBehaviour
{
    public enum EngineAudioOptions
    {
        Simple,
        FourChannel
    }
    [Header("Style")]
    public EngineAudioOptions engineSoundStyle = EngineAudioOptions.FourChannel;
    [Header("Clips"), Space(10)]
    public AudioClip lowAccelClip;
    public AudioClip lowDecelClip;
    public AudioClip highAccelClip;
    public AudioClip highDecelClip;
    [Header("Sound Settings"), Space(10)]
    public float pitchMultiplier = 1f;
    [SerializeField] private float _lowPitchMin = 1f;
    [SerializeField] private float _lowPitchMax = 6f;
    [SerializeField] private float _highPitchMultiplier = 0.25f;
    [SerializeField] private float _maxRolloffDistance = 500f;
    [SerializeField] private float _dopplerLevel = 1f;
    [SerializeField] private bool _useDoppler = true;
    [Header("Main Camera"), Space(10)]
    [SerializeField] private Transform _mainCameraTransform;

    private AudioSource _lowAccel;
    private AudioSource _lowDecel;
    private AudioSource _highAccel;
    private AudioSource _highDecel;
    private bool _soundStarted;
    private CarController _carController;

    private void StartSound()
    {
        _carController = GetComponent<CarController>();

        _highAccel = SetupEngineAudioSource(highAccelClip);

        if (engineSoundStyle == EngineAudioOptions.FourChannel)
        {
            _lowAccel = SetupEngineAudioSource(lowAccelClip);
            _lowDecel = SetupEngineAudioSource(lowDecelClip);
            _highDecel = SetupEngineAudioSource(highDecelClip);
        }

        _soundStarted = true;
    }

    private void StopSound()
    {
        foreach (AudioSource source in GetComponents<AudioSource>())
        {
            Destroy(source);
        }

        _soundStarted = false;
    }

    private void Update()
    {
        float camDist = (_mainCameraTransform.position - transform.position).sqrMagnitude;

        if (_soundStarted && camDist > _maxRolloffDistance * _maxRolloffDistance)
            StopSound();

        if (!_soundStarted && camDist < _maxRolloffDistance * _maxRolloffDistance)
            StartSound();

        if (_soundStarted)
        {
            float pitch = UnclampedLerp(_lowPitchMin, _lowPitchMax, _carController.Revs);
            pitch = Mathf.Min(_lowPitchMax, pitch);

            if (engineSoundStyle == EngineAudioOptions.Simple)
            {
                _highAccel.pitch = pitch * pitchMultiplier * _highPitchMultiplier;
                _highAccel.dopplerLevel = _useDoppler ? _dopplerLevel : 0;
                _highAccel.volume = 1;
            }
            else
            {
                _lowAccel.pitch = pitch * pitchMultiplier;
                _lowDecel.pitch = pitch * pitchMultiplier;
                _highAccel.pitch = pitch * _highPitchMultiplier * pitchMultiplier;
                _highDecel.pitch = pitch * _highPitchMultiplier * pitchMultiplier;

                float accFade = Mathf.Abs(_carController.AccelInput);
                float decFade = 1 - accFade;

                float highFade = Mathf.InverseLerp(0.2f, 0.8f, _carController.Revs);
                float lowFade = 1 - highFade;

                highFade = 1 - ((1 - highFade) * (1 - highFade));
                lowFade = 1 - ((1 - lowFade) * (1 - lowFade));
                accFade = 1 - ((1 - accFade) * (1 - accFade));
                decFade = 1 - ((1 - decFade) * (1 - decFade));

                _lowAccel.volume = lowFade * accFade;
                _lowDecel.volume = lowFade * decFade;
                _highAccel.volume = highFade * accFade;
                _highDecel.volume = highFade * decFade;

                _highAccel.dopplerLevel = _useDoppler ? _dopplerLevel : 0;
                _lowAccel.dopplerLevel = _useDoppler ? _dopplerLevel : 0;
                _highDecel.dopplerLevel = _useDoppler ? _dopplerLevel : 0;
                _lowDecel.dopplerLevel = _useDoppler ? _dopplerLevel : 0;
            }
        }
    }

    private AudioSource SetupEngineAudioSource(AudioClip clip)
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = 0;
        source.loop = true;
        source.time = Random.Range(0f, clip.length);
        source.Play();
        source.minDistance = 5f;
        source.maxDistance = _maxRolloffDistance;
        source.dopplerLevel = 0;
        return source;
    }

    private static float UnclampedLerp(float from, float to, float value)
    {
        return (1.0f - value) * from + value * to;
    }
}
