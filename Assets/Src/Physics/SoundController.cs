using UnityEngine;
using System.Collections;

// Simple class to controll sounds of the car, based on engine throttle and RPM, and skid velocity.
[RequireComponent(typeof(Drivetrain))]
[RequireComponent(typeof(CarController))]
public class SoundController : MonoBehaviour
{

    [Range(0, 1)]
    public float engineStartPitchRate = 0.2f;
    [Range(0, 1)]
    public float enginePitchRate = 1.0f;
    [Range(0, 1)]
    public float engineVolume = 0.5f;
    public AudioClip engine;
    [Range(0, 1)]
    public float skidPitchRate = 0.8f;
    [Range(0, 1)]
    public float skidVolume = 0.5f;
    public AudioClip skid;

    AudioSource engineSource;
    AudioSource skidSource;

    CarController car;
    Drivetrain drivetrain;

    AudioSource CreateAudioSource(AudioClip clip)
    {
        GameObject go = new GameObject("audio");
        go.transform.parent = transform;
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.AddComponent(typeof(AudioSource));
        go.GetComponent<AudioSource>().clip = clip;
        go.GetComponent<AudioSource>().loop = true;
        go.GetComponent<AudioSource>().volume = 0;
        go.GetComponent<AudioSource>().spatialBlend = 1;
        go.GetComponent<AudioSource>().Play();
        return go.GetComponent<AudioSource>();
    }

    void Start()
    {
        engineSource = CreateAudioSource(engine);
        skidSource = CreateAudioSource(skid);
        car = GetComponent(typeof(CarController)) as CarController;
        drivetrain = GetComponent(typeof(Drivetrain)) as Drivetrain;
    }

    void LateUpdate()
    {
        if (engineSource)
        {
            engineSource.pitch = engineStartPitchRate + engineStartPitchRate * (drivetrain.rpm / drivetrain.maxRPM);
            engineSource.volume = (0.4f + 0.6f * drivetrain.throttle) * engineVolume;
        }
        if (skidSource)
        {
            skidSource.pitch = Mathf.Clamp01(skidPitchRate + Mathf.Abs(car.slipVelo) * 0.01f);
            skidSource.volume = Mathf.Clamp01(Mathf.Abs(car.slipVelo) * 0.1f - 0.3f) * skidVolume;
        }
    }
}
