using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class CarUI : MonoBehaviour {
    public Drivetrain car;
    [Header("Generic")]
    public Text speed;
    public Text gear;
    public Text rpm;
    [Header("Gameplay")]
    public Text rank;
    public Text lap;
    public Text lapTime;
    public Text lapTimeLast;
    public Text lapTimeBest;
    //public Text drift;
    //public Text driftDistance;

    // Use this for initialization
    void Start () {
		
	}

    void OnLevelWasLoaded()
    {
        DynamicGI.UpdateEnvironment();
    }

    // Update is called once per frame
    void Update() {
        if (car)
        {
            if (speed) speed.text = "Velocity: "+car.Speed.ToString("0") + " km/h";
            if (gear) gear.text = "Gear: "+car.Gear.ToString("0");
            if (rpm) rpm.text = "RPM: "+car.rpm.ToString("0");
        }
    }

}
