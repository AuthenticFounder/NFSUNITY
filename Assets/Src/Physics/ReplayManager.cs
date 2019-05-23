using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ReplayManager : MonoBehaviour
{
    [System.Serializable]
    public class Racer
    {
        public Transform transform;
        public Drivetrain car;
        public List<VehicleState> vehicleState = new List<VehicleState>();
    }

    [System.Serializable]
    public class VehicleState
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 velocity;
        public Vector3 angularVelocity;
        public float throttleInput;
        public float throttle;
        public float brake;
        public float clutch;
        public int gear;
        public float handbrake;
        public float steer;
        public VehicleState(Racer r)
        {
            position = r.transform.position;
            rotation = r.transform.rotation;
            velocity = r.transform.GetComponent<Rigidbody>().velocity;
            angularVelocity = r.transform.GetComponent<Rigidbody>().angularVelocity;
            //throttleInput = r.car.throttleInput;
            throttle = r.car.throttle;
            //brake = r.car.brake;
            //clutch = r.car.clutch;
            gear = r.car.gear;
            //handbrake = r.car.handbrake;
            //steer = r.car.steering;


            throttleInput = r.car.throttleInput;
            brake = r.car.GetComponent<CarController>().brake;
            handbrake = r.car.GetComponent<CarController>().handbrake;
            steer = r.car.GetComponent<CarController>().steering;
        }
    }

    [System.Serializable]
    public class LocalEnvironment
    {
        public Transform transform;
        public Environment environment;
        public CarController carController;
        public List<EnvState> envState = new List<EnvState>();
    }

    [System.Serializable]
    public class EnvState
    {
        public float time;
        public int currentCar;
        public EnvState(LocalEnvironment e)
        {
            if (e.environment) time = e.environment.time;
            //if (e.carController) currentCar = e.carController.CurrentSelection;
        }
    }

    public enum ReplayState { Recording, Playing, NONE }
    public static ReplayManager instance;

    [HideInInspector]
    public ReplayState replayState = ReplayState.NONE;

    [HideInInspector]
    public LocalEnvironment environment = new LocalEnvironment();

    [HideInInspector]
    public List<Racer> racers = new List<Racer>(new Racer[100]);

    [HideInInspector]
    public int CurrentFrame;

    [HideInInspector]
    public int TotalFrames;

    [HideInInspector]
    public float ReplayPercent;
    private int PlaybackSpeed = 1;

    void Awake()
    {
        instance = this;

        replayState = ReplayState.NONE;
    }

    void FixedUpdate()
    {
        switch (replayState)
        {
            case ReplayState.Recording:

                Record();

                break;

            case ReplayState.Playing:

                Playback();

                break;
        }
    }

    void Record()
    {
        environment.envState.Add(new EnvState(environment));
        for (int i = 0; i < racers.Count; i++)
        {
            //Record
            if (racers[i].car)
            {
                racers[i].vehicleState.Add(new VehicleState(racers[i]));
            }
        }
    }

    public void SetLastFrame()
    {
        for (int i = 0; i < racers.Count; i++)
        {
            if (CurrentFrame < racers[0].vehicleState.Count - 1 && CurrentFrame > 0)
            {
                SetVehicleStateFromReplayFrame(racers[i], racers[i].vehicleState.Count - 1, false);
                racers[i].car.GetComponent<Rigidbody>().isKinematic = true;
                racers[i].car.GetComponent<Rigidbody>().isKinematic = false;
            }
        }
    }

    void Playback()
    {
        CurrentFrame += 1 * PlaybackSpeed;

        if (CurrentFrame <= 2)
            AdjustPlaybackSpeed(1);

        for (int i = 0; i < racers.Count; i++)
        {
            if (CurrentFrame < racers[0].vehicleState.Count - 1 && CurrentFrame > 0)
            {
                //Playback Replay
                SetSceneStateFromReplayFrame(environment,CurrentFrame);
                SetVehicleStateFromReplayFrame(racers[i], CurrentFrame, PlaybackSpeed == 1);
            }
            else
            {
                //Restart Replay
                RestartReplay();
            }
        }
        //Get the replay percentage
        ReplayPercent = (float)CurrentFrame / (float)TotalFrames;
    }
    /*
    public void GetRacersAndStartRecording(RaceDriver[] allRacers)
    {
        // Environment
        //environment = new LocalEnvironment();
        //environment.sun = GameObject.FindObjectOfType<SunPosition>();

        //Get the racer's vehicle controllers and start recording the replay
        racers.RemoveRange(allRacers.Length, racers.Count - allRacers.Length);

        for (int i = 0; i < racers.Count; i++)
        {
            racers[i].transform = allRacers[i].car.gameObject.transform;
            racers[i].car = allRacers[i].car;
        }

        replayState = ReplayState.Recording;
        Debug.Log("ReplayManager ---> Received " + racers.Count + " racers!");
    }*/
    public void GetRacersAndStartRecording(Drivetrain[] allRacers)
    {
        // Environment
        environment = new LocalEnvironment();
        environment.environment = GameObject.FindObjectOfType<Environment>();
        //environment.carController = GameObject.FindObjectOfType<CarController>();
        //environment.carController = CarController.instance;

        //Get the racer's vehicle controllers and start recording the replay
        racers.RemoveRange(allRacers.Length, racers.Count - allRacers.Length);

        for (int i = 0; i < racers.Count; i++)
        {
            racers[i].transform = allRacers[i].gameObject.transform;
            racers[i].car = allRacers[i];
        }

        replayState = ReplayState.Recording;
        Debug.Log("ReplayManager ---> Received " + racers.Count + " racers!");
    }

    public void StopRecording()
    {
        replayState = ReplayState.NONE;
        TotalFrames = GetTotalFrames();
    }

    public void ViewReplay()
    {
        if (TotalFrames <= 0) return;
       // SmoothFollow.instance.spectatorMode = true;
        //CarController.instance.disableInput = true;
        //DisableInputs();        
        MenuUI.instance.menuMode = GameMenuMode.Replay;
        replayState = ReplayState.Playing;
        if(CurrentFrame<=2) ResetScene();
    }

    public void SetPlaybackSpeed(int s)
    {
        PlaybackSpeed = s;
        Time.timeScale = Mathf.Abs(s);
    }

    public void AdjustPlaybackSpeed(int s)
    {
        if (PlaybackSpeed == s)
        {
            PlaybackSpeed = 1; //reset PlaybackSpeed on ff & rw buttons
        }
        else
        {
            PlaybackSpeed = s;
        }

        //Reset timescale incase the replay is paused
        if(Time.timeScale < 1) Time.timeScale = 1.0f;
    }

    public void PauseReplay()
    {
        PlaybackSpeed = (PlaybackSpeed > 0 || PlaybackSpeed < 0) ? 0 : 1;
        Time.timeScale = PlaybackSpeed;
    }

    public void ExitReplay()
    {
        MenuUI.instance.menuMode = GameMenuMode.Paused;
        AdjustPlaybackSpeed(1);
    }

    private void RestartReplay()
    {
        //GameUI.instance.StartCoroutine("ScreenFadeOut", 0.3f);
        ResetScene();
        CurrentFrame = 0;
    }

    private int GetTotalFrames()
    {
        return racers[0].vehicleState.Count;
    }

    public void ResetScene()
    {
        Skidmarks[] skids = GameObject.FindObjectsOfType<Skidmarks>();
        /*
        Damage[] dam = GameObject.FindObjectsOfType<Damage>();
        //CarEffects[] fx = GameObject.FindObjectsOfType<CarEffects>();
        foreach (Damage d in dam)
        {
            d.Repair();
            //s.Clear();
        }

        foreach (Skidmarks s in skids)
        {
            s.Clear();
        }*/
    }

    private void SetSceneStateFromReplayFrame(LocalEnvironment e, int currentFrame)
    {
        if (e.environment)e.environment.time = e.envState[currentFrame].time;
        //if (e.carController) e.carController.CurrentSelection = e.envState[currentFrame].currentCar;
    }

    private void SetVehicleStateFromReplayFrame(Racer r, int currentFrame, bool normalSpeed)
    {
        Rigidbody rb = r.transform.GetComponent<Rigidbody>();
        Drivetrain car = rb.transform.GetComponent<Drivetrain>();
        if (normalSpeed)
        {
            rb.MovePosition(r.vehicleState[currentFrame].position);
            rb.MoveRotation(r.vehicleState[currentFrame].rotation);
        }
        else
        {
            rb.transform.position = r.vehicleState[currentFrame].position;
            rb.transform.rotation = r.vehicleState[currentFrame].rotation;
        }

        rb.velocity = r.vehicleState[currentFrame].velocity;
        rb.angularVelocity = r.vehicleState[currentFrame].angularVelocity;
        rb.isKinematic = !normalSpeed;
        //Handle input playback             
        if (car)
        {
            car.throttleInput = r.vehicleState[currentFrame].throttleInput;
            car.throttle = r.vehicleState[currentFrame].throttle;
            //car.brake = r.vehicleState[currentFrame].brake;
            //car.clutch = r.vehicleState[currentFrame].clutch;
            car.gear = r.vehicleState[currentFrame].gear;
            //car.handbrake = r.vehicleState[currentFrame].handbrake;
            //car.steering = r.vehicleState[currentFrame].steer;


            car.GetComponent<CarController>().brake = r.vehicleState[currentFrame].brake;
            car.GetComponent<CarController>().handbrake = r.vehicleState[currentFrame].handbrake;
            car.GetComponent<CarController>().steering = r.vehicleState[currentFrame].steer;
        }
    }        
}

