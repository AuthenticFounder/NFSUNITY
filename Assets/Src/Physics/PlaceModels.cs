using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum RotationType
{
    Steering,
    RPM,
    Speed
}

public enum RotationAxis
{
    Right,
    Up,
    Forward
}

public enum CameraMode
{
    Follow,
    Fixed,
    Cockpit
}

[System.Serializable]
public class WheelModels
{
    public List<GameObject> model;
    public GameObject speedModel;
    public GameObject hub;
}

[System.Serializable]
public class CameraRig
{
    public CameraMode cameraMode;
    public Vector3 positionOffest;

    public void ApplyCamera()
    {
        CarCamera carCamera = GameObject.FindObjectOfType<CarCamera>();
        if (!carCamera) return;
        carCamera.useFixedLookDirection = cameraMode == CameraMode.Fixed || cameraMode == CameraMode.Cockpit;
        carCamera.positionOffest = positionOffest;        
    }
}

[System.Serializable]
public class Rotatable
{
    public string name;
    public Transform transform;
    public RotationType type;
    public RotationAxis axis;
    public float range = 1;
    public float minAngle = 0;
    public float maxAngle = 180;
    public float fraction;
    public bool flip;
    [HideInInspector]
    public Quaternion savedRotation;
    public bool smooth;
    public float dampFactor = 0.1f;
    float smoothFraction;

    public Rotatable()
    {
        Stamp();
    }

    public void Stamp()
    {
        if (transform) savedRotation = transform.localRotation;
    }

    public Vector3 GetAxis()
    {
        if (axis == RotationAxis.Forward)
            return Vector3.forward;
        else if (axis == RotationAxis.Up)
            return Vector3.up;
        else
            return Vector3.right;
    }

    public void Update(CarController car)
    {
        if (!transform) return;

        if (!car) return;

        float value = 0;
        if (type == RotationType.Steering)
        {
            value = (car.steering * 0.5f) + 0.5f;
        }
        else if (type == RotationType.RPM)
        {
            value = car.GetComponent<Drivetrain>().rpm;
        }
        else if (type == RotationType.Speed)
        {
            value = car.GetComponent<Rigidbody>().velocity.magnitude * 3.6f;
        }

        if (type == RotationType.Steering)
            fraction = Mathf.Lerp(-range * 0.5f, range * 0.5f, value) * -1;
        else
            fraction = Mathf.Lerp(minAngle, maxAngle, value / range);

        if (smooth)
        {
            smoothFraction = Mathf.Lerp(smoothFraction, fraction, Time.deltaTime / dampFactor);
            fraction = smoothFraction;
        }

        transform.localRotation = savedRotation * Quaternion.Euler((fraction * (flip ? -1 : 1)) * GetAxis());
    }
}

public class PlaceModels : MonoBehaviour {

    [Header("Wheels")]
    public WheelModels wheelFL;
    public WheelModels wheelFR;
    public WheelModels wheelRL;
    public WheelModels wheelRR;
    public float heightOffset = 0.1f;
    [Header("Cameras")]
    public List<CameraRig> cameras;
    [Header("Cockpit")]
    public Transform cockpitContainer;
    public List<Transform> cockpit;
    public List<Rotatable> rotatable;
    CarController carController;

    public bool placeWheels;
    bool wheelsAssigned;

    public bool showCockpit;
    bool cockpitChanged = true;
    int curCamRig;
    bool cameraChanged;

    // Use this for initialization
    void Start()
    {
        carController = GetComponent<CarController>();
        PositionWheels();

        foreach (Rotatable r in rotatable)
        {
            r.Stamp();
        }
        if (cockpitContainer)
        {
            cockpit.Clear();

            Transform[] transforms = cockpitContainer.GetComponentsInChildren<Transform>();

            foreach (Transform t in transforms)
            {
                if (t != cockpitContainer)
                {
                    cockpit.Add(t);
                }
            }
        }
    }

    void PositionWheels()
    {
        if (!carController) return;
        if (placeWheels)
        {
            GetAlignment(carController.wheels[0].transform.gameObject, wheelFL.model);
            GetAlignment(carController.wheels[1].transform.gameObject, wheelFR.model);
            GetAlignment(carController.wheels[2].transform.gameObject, wheelRL.model);
            GetAlignment(carController.wheels[3].transform.gameObject, wheelRR.model);
            placeWheels = false;
        }
    }

    void AssignWheels()
    {
        if (!carController) return;
        if (!wheelsAssigned)
        {
            /*
            for(int i=0; i<carController.wheels.Length; i++)
            {
                Wheel w = carController.wheels[i];
                if (!w) return;
            }*/
            if (!wheelFL.speedModel) carController.wheels[0].modelSpeed = null;
            if (!wheelFR.speedModel) carController.wheels[1].modelSpeed = null;
            if (!wheelRL.speedModel) carController.wheels[2].modelSpeed = null;
            if (!wheelRR.speedModel) carController.wheels[3].modelSpeed = null;
            if (!wheelFL.hub) carController.wheels[0].modelHub = null;
            if (!wheelFR.hub) carController.wheels[1].modelHub = null;
            if (!wheelRL.hub) carController.wheels[2].modelHub = null;
            if (!wheelRR.hub) carController.wheels[3].modelHub = null;
            AssignMesh(wheelFL.model, carController.wheels[0].model);
            AssignMesh(wheelFR.model, carController.wheels[1].model);
            AssignMesh(wheelRL.model, carController.wheels[2].model);
            AssignMesh(wheelRR.model, carController.wheels[3].model);
            AssignMesh(wheelFL.hub, carController.wheels[0].modelHub);
            AssignMesh(wheelFR.hub, carController.wheels[1].modelHub);
            AssignMesh(wheelRL.hub, carController.wheels[2].modelHub);
            AssignMesh(wheelRR.hub, carController.wheels[3].modelHub);
            AssignMesh(wheelFL.speedModel, carController.wheels[0].modelSpeed);
            AssignMesh(wheelFR.speedModel, carController.wheels[1].modelSpeed);
            AssignMesh(wheelRL.speedModel, carController.wheels[2].modelSpeed);
            AssignMesh(wheelRR.speedModel, carController.wheels[3].modelSpeed);
            wheelsAssigned = true;

        }
    }

    void GetAlignment(GameObject a, GameObject b)
    {
        b.transform.localPosition = a.transform.localPosition;
    }

    void GetAlignment(GameObject a, List<GameObject> b)
    {
        if (b.Count <= 0) return;
        a.transform.localPosition = b[b.Count-1].transform.localPosition; // Apply the last position
        /*
        for (int i = 0; i < b.Count; i++)
        {
            //b[i].transform.localPosition = a.transform.localPosition;
            a.transform.localPosition = b[0].transform.localPosition;
        }*/
    }

    void AssignMesh(GameObject a, GameObject b)
    {
        if (a == null || b == null)
        {
            return;
        }
        a.transform.parent = b.transform;
        a.transform.localPosition = Vector3.zero;
    }

    void AssignMesh(List<GameObject> a, GameObject b)
    {
        if (a == null || b == null)
        {
            return;
        }

        for (int i = 0; i < a.Count; i++)
        {
            a[i].transform.parent = b.transform;
            a[i].transform.localPosition = Vector3.zero;
        }
    }

    void UpdateRotatables()
    {
        foreach(Rotatable r in rotatable)
        {
            r.Update(carController);
        }
    }


    void Cockpit()
    {
        Transform[] transforms = transform.GetComponentsInChildren<Transform>();

        //if (Input.GetKeyDown(KeyCode.V))
        //{
        //    showCockpit = !showCockpit;
        //    cockpitChanged = true;
        //}
        if (cockpitChanged)
        {
            for (int i = 0; i < transforms.Length; i++)
            {
                MeshRenderer mr = transforms[i].GetComponent<MeshRenderer>();
                if (mr) mr.shadowCastingMode = showCockpit ? UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly : UnityEngine.Rendering.ShadowCastingMode.On;
            }

            for (int i = 0; i < cockpit.Count; i++)
            {
                if (cockpit[i]) cockpit[i].gameObject.SetActive(showCockpit);
                MeshRenderer mr = cockpit[i].GetComponent<MeshRenderer>();
                if (mr) mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

            }
            cockpitChanged = false;
        }
    }

    public void NextCam()
    {
        if (cameras.Count <= 0) return;

        curCamRig++;
        if (curCamRig > cameras.Count-1)
            curCamRig = 0;
        if (curCamRig < 0)
            curCamRig = cameras.Count-1;
        cameraChanged = true;
    }

    void Cameras()
    {
        if (cameras.Count <= 0) return;
        showCockpit = (cameras[curCamRig].cameraMode == CameraMode.Cockpit);
        if (cameraChanged)
        {
            //if(cameras[curCamRig])
            cameras[curCamRig].ApplyCamera();
            //cameras[curCamRig].positionOffest;
            /*
            foreach (CameraRig cr in cameras)
            {
                //cr
            }*/
            cockpitChanged = true;
            cameraChanged = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        AssignWheels();
        Cameras();
        Cockpit();
        UpdateRotatables();
    }
}
