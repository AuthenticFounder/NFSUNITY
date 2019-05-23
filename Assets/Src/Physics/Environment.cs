using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class Surface
{
    public string name = "Unity_Tag";
    [Range(0.1f,1.0f)]
    public float friction = 1.0f;
    public Skidmarks skid;
    public GameObject emitter;
    public AudioClip clip;
    public bool onSruface;

}
[ExecuteInEditMode]
public class Environment : MonoBehaviour {

    [Header("Sun")]
    public Transform sun;
    [Range(0, 2)]
    public float minIntensity = 0.1f;
    [Range(0, 2)]
    public float maxIntensity = 1f;
    public float minPoint = 0.5f;
    [Range(0,360)]
    public float headingAngle = 0;
    [Range(0, 360)]
    public float pitchAngle = 0;
    [Range(0, 24)]
    public float time;
    [Range(0, 24)]
    public float lightsOnTime = 21;
    [Range(0, 24)]
    public float lightsOffTime = 9;
    [Range(-12, 12)]
    public float offsetTime = 0;
    public float timeSpeed = 0.1f;
    public float updateInterval = 1;
    [Header("Surface")]
    public List<Surface> surfaces;

    private bool playMode;
    private float timeFormat = 24;
    //[HideInInspector]

    private float updateSpeed;
    public bool sceneLightsOn;
    private float sunIntensity;

    private Material skyMat;

    public static Environment instance;

    private static List<Wheel> wheelContainer;
    bool surfacesInitialized;

    void Start ()
    {
        skyMat = RenderSettings.skybox;
    }

    private void Awake()
    {
        instance = this;
    }

    void Update()
    {
        #if UNITY_EDITOR
            playMode = EditorApplication.isPlaying;
        #else
            playMode = Application.isPlaying;
        #endif
        UpdateLighting();
        UpdateSurfaces();
    }

    void UpdateLighting()
    {
        if (playMode)
        {
            time += Time.deltaTime * timeSpeed;
            if (time > timeFormat) time = 0;
            //GetTrackLights();
            ToggleLights();
        }
        //
        updateSpeed += Time.deltaTime;
        if (updateSpeed > updateInterval)
        {
            if (!sun) return;
            //Vector3 elipse = new Vector3(Mathf.Sin((time / timeFormat) * 360 * Mathf.Deg2Rad) * 360, 0, Mathf.Cos((time / timeFormat) * 360 * Mathf.Deg2Rad) * 360);
            sun.transform.localRotation = Quaternion.Euler(0, headingAngle, -pitchAngle) * Quaternion.Euler(((offsetTime) / timeFormat) * 360, 0, 0) * Quaternion.Euler((time / timeFormat) * 360, 0, 0);
            //sun.transform.localRotation = Quaternion.Euler(0, headingAngle, -pitchAngle) * Quaternion.Euler((time / timeFormat) * 360, 0, 0);
            //if (sky.update) sky.Update(sun.GetComponent<Light>());
            updateSpeed = 0;
            
            Light l = sun.GetComponent<Light>();
            if (!l) return;
            /*
            if (!sceneLightsOn)
            {
                sunIntensity -= Time.deltaTime / 0.15f;
            }
            else
            {
                sunIntensity += Time.deltaTime / 0.15f;
            }

            sunIntensity = Mathf.Clamp01((sunIntensity));
            l.intensity = sunIntensity;
            */
            float tRange = 1 - minPoint;
            float dot = Mathf.Clamp01((Vector3.Dot(l.transform.forward, Vector3.down) - minPoint) / tRange);
            float i = ((maxIntensity - minIntensity) * dot) + minIntensity;
            l.intensity = i;

            float minAmbientPoint = -1;
            tRange = 1 - minAmbientPoint;
            dot = Mathf.Clamp01((Vector3.Dot(l.transform.forward, Vector3.down) - minAmbientPoint) / tRange);
            i = ((1 - 0.2f) * dot) + 0.2f;
            RenderSettings.ambientIntensity = i;

            //l.color = nightDayColor.Evaluate(dot);
            RenderSettings.ambientLight = l.color;

            //RenderSettings.ambientLight *= (i / maxIntensity);
            //i *= 2;
            //if (i > 1) i = 1;
            //RenderSettings.ambientIntensity = i;
            //skyMat.SetFloat("_AtmosphereThickness", i);
        }
    }

    void ToggleLights()
    {
        sceneLightsOn = ((time) > (lightsOnTime) || (time) < (lightsOffTime));

        GameObject[] sceneLights = GameObject.FindGameObjectsWithTag("SceneLights");
        foreach (GameObject g in sceneLights)
        {
            Light l = g.GetComponent<Light>();
            if (!l) return;
            l.enabled = sceneLightsOn;
        }
    }

    void SetupSurfaces()
    {
        if(!surfacesInitialized)
        {
            Wheel[] wheels = GameObject.FindObjectsOfType<Wheel>();
            wheelContainer.Clear();
            foreach (Wheel w in wheels)
            {
                wheelContainer.Add(w);
            }
            surfacesInitialized = true;
        }
    }

    void UpdateSurfaces()
    {
        Wheel[] wheels = GameObject.FindObjectsOfType<Wheel>();
        foreach (Wheel w in wheels)
        {
            if (!w) return;

            foreach(Surface s in surfaces)
            {
                Collider hitCol = w.Hit.collider;
                if (!hitCol) return;

                //if (s.name != hitCol.transform.tag) return;

                s.onSruface = hitCol.CompareTag(s.name);
                if (!s.onSruface) return;

                if(s.onSruface)
                {
                    w.grip = s.friction;
                }
                if (s.skid)
                    w.Skid = s.skid;
            }
        }

    }
}
