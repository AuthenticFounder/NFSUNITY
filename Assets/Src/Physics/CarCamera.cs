using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CamSpring
{
    public bool enable;
    public float stiffness = 40000f;
    public float damping = 1000f;
    public float mass = 50f;
    private Vector3 velocity;
    private Vector3 position;

    public Vector3 GetNormalizedVector(Vector3 vector, float max)
    {
        return new Vector3(Mathf.Clamp(vector.x, -max, max), Mathf.Clamp(vector.y / max, -max, max), Mathf.Clamp(vector.z / max, -max, max));

    }
    public Vector3 GetPosition(Vector3 desiredPosition)
    {
        Vector3 stretch = (position - desiredPosition);
        Vector3 force = -stiffness * stretch - damping * velocity;
        Vector3 acc = force / mass;
        velocity += acc * Time.deltaTime;
        position += velocity * Time.deltaTime;
        return position;
    }
}

public class CarCamera : MonoBehaviour
{
    public CamSpring spring;
    public Transform target;
    public Vector3 lookPoint;
    public Vector3 positionOffest = new Vector3(0,1,5);
    public float damping = 10.0f;
    //get a vector pointing from camera towards the ball
    public bool smoothedFollow = false;
    public bool useFixedLookDirection = false;

    public Transform cameraLocationContainer;
    public bool trackMode = false;

    public static CarCamera instance;

    public List<Transform> cameraLocations;
    private Transform closestPosition = null;

    public bool lookBack;
    float fov, prevFov;

    private void Awake()
    {
        instance = this;

        if (!cameraLocationContainer) return;

        Transform[] cams = cameraLocationContainer.GetComponentsInChildren<Transform>();

        cameraLocations.Clear();
        foreach(Transform t in cams)
        {
            if(t != cameraLocationContainer)
                cameraLocations.Add(t);
        }

        prevFov = GetComponent<Camera>().fieldOfView;
    }

    private void LateUpdate()
    {
        if (!target) return;
        
        if (!trackMode)
        {
            fov = prevFov;
            if (useFixedLookDirection)
            {
                transform.position = target.TransformPoint(positionOffest);
                transform.rotation = target.localRotation;
                return;
            }
            if (lookBack)
            {
                transform.position = target.TransformPoint(positionOffest);
                transform.rotation = Quaternion.Euler(Vector3.up * 180) * target.localRotation;
                return;
            }

        }
        else
        {
            if (!target || cameraLocations.Count <= 0) return;
            transform.position = GetClosestCameraPosition().position;
            transform.LookAt(target.position + Vector3.up * 0.5f);
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        //Detect();

        if (!target || useFixedLookDirection || lookBack) return;

        if (!trackMode)
        {
            Vector3 lookToward = target.position - transform.position;

            //make it stay a fixed distance behind ball
            Vector3 newPos;
            newPos = target.position - lookToward.normalized * positionOffest.z;
            newPos.y = target.position.y + positionOffest.y;
            if (spring.enable)
            {
                newPos = spring.GetPosition(newPos);
            }

            if (!smoothedFollow)
            {
                transform.position = newPos;
            }
            else
            {
                transform.position += (newPos - transform.position) * Time.deltaTime * damping;
            }

            lookToward = (target.position + (lookPoint)) - transform.position;

            transform.forward = lookToward.normalized;

            GetComponent<Camera>().fieldOfView = prevFov;
        }
        else
        {
            if (target)
            {
                float dist = Vector3.Distance(target.position, transform.position);
                //We use unscaled delta time incase of paused replays
                float newFow = Mathf.Lerp(2, 40, 1 - Mathf.Clamp01(dist / 50));
                fov = Mathf.Lerp(fov, newFow, Time.deltaTime * 1f);
                GetComponent<Camera>().fieldOfView = fov;
            }
        }
    }


    //Return the closest cinematic camera positon and use it as our next "go to" position
    Transform GetClosestCameraPosition()
    {
        float closestDistanceSqr = Mathf.Infinity;

        foreach (Transform c in cameraLocations)
        {
            float distanceToTarget = (c.position - target.position).sqrMagnitude;

            if (distanceToTarget < closestDistanceSqr)
            {
                closestPosition = c;
                closestDistanceSqr = distanceToTarget;
            }
        }

        return closestPosition;
    }
}