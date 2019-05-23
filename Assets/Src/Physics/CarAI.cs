using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarAI : MonoBehaviour {

    [Range(5, 50)]
    public float waypointRange = 10;
    public float accelSensitivity = 1.5f;
    public float brakeSensitivity = 0.5f;
    [Range(0,1)]
    public float steerCutoff = 0.5f;
    public WaypointCourse waypointCourse;

    int currentWp;
    int currentWaypoint;

    float steering;
    float throttle;
    float brake;

    CarController carController;
    float reverseTime;
    string guiString;

	// Use this for initialization
	void Start () {
		
	}
    float distance;
    // Update is called once per frame
    void Update() {
        if (!waypointCourse) return;

        carController = GetComponent<CarController>();
        if (!carController) return;

        // Navigate AI
        float steer;
        float accel;

        //Vector3 currentPoint = waypointCourse.waypoints[currentWp].point;
        //Vector3 nextPoint = waypointCourse.waypoints[currentWp + 1 % waypointCourse.waypoints.Count].point;
        //Vector3 prevPoint = waypointCourse.waypoints[currentWp > 1 ? currentWp -1 : waypointCourse.waypoints.Count-1].point;

        //Vector3 nextPoint = waypointCourse.waypoints[currentWp + 1 % waypointCourse.waypoints.Count].point;
        Vector3 currentPoint = waypointCourse.waypoints[currentWp].point;
        Vector3 prevPoint = waypointCourse.waypoints[currentWp > 1 ? currentWp - 1 : waypointCourse.waypoints.Count - 1].point;
        Vector3 dir = (currentPoint - prevPoint);


        //Vector3 currentPointOnLine = currentPoint + ((prevPoint - currentPoint).normalized);
        //float currentDist = waypointCourse.waypoints[currentWp].distance;


        float distanceToWp = Vector3.Distance(carController.transform.position, currentPoint);
        Vector3 forward = carController.transform.TransformDirection(Vector3.forward);
        Vector3 toCurrentPoint = currentPoint - carController.transform.position;
        //Gizmos.DrawLine(carController.transform.position, toCurrentPoint);

        float speed = carController.GetComponent<Rigidbody>().velocity.magnitude * 3.6f;
        float lookForwardDistance = Mathf.Lerp(10,60,(speed) / 300);
        Vector3 loc = carController.transform.position;
        Vector3 predictedLoc = carController.transform.position + (carController.transform.forward * 5) + (lookForwardDistance * dir.normalized);
        Vector3 targetPoint = ClosestPointConstrained(prevPoint, currentPoint, predictedLoc);
        Vector3 NP = ClosestPointConstrained(prevPoint, currentPoint, loc);

        ////
        Debug.DrawLine(loc, targetPoint, Color.red);
        Debug.DrawLine(loc, currentPoint, Color.green);
        Debug.DrawLine(loc, NP, Color.magenta);
        //Vector3 currentActualPoint = Vector3.Lerp(currentPoint, prevPoint, distanceToWp);
        //Debug.DrawLine(carController.transform.position, currentPointOnLine, Color.magenta);
        //Debug.DrawLine(carController.transform.position, currentActualPoint, Color.magenta);

        float minRange = 30;
        float maxRange = 100;
        float rangeTrigger = Mathf.Clamp01(((GetComponent<Rigidbody>().velocity.magnitude * 3.6f) - minRange) / (maxRange - minRange));


        ////
        Vector3 relativeVector = carController.transform.InverseTransformPoint(targetPoint);
        //Vector3 relativeVector = carController.transform.InverseTransformPoint(currentPoint);
        float steerFactor = (relativeVector.x / relativeVector.magnitude);
        steer = Mathf.Clamp(steerFactor, -1, 1);

        ///
        float toCurrent = Vector3.Dot(forward, toCurrentPoint);
        float wpDistRatio = toCurrent / distanceToWp;
        float toCurrentDec = ((wpDistRatio * wpDistRatio) * Mathf.Abs(steerFactor));
        guiString = string.Format("wpDistRatio: {0} ", wpDistRatio.ToString("0.000"));
        guiString += string.Format("curve: {0} ", toCurrentDec.ToString("0.000"));

        /*
        if (distanceToWp < waypointRange)
        {
            if (currentWp == waypointCourse.waypoints.Count - 1)
                currentWp = 0;
            else
                currentWp++;
        }*/
        
        float distToNextPoint = Vector3.Dot(predictedLoc - prevPoint, dir.normalized);

        if (distToNextPoint > dir.magnitude)
        {
            if (currentWp == waypointCourse.waypoints.Count - 1)
                currentWp = 0;
            else
                currentWp++;
        }

        float minSpeed = 10;
        float maxSpeed = 20;

        float speedTrigger = Mathf.Clamp01(((GetComponent<Rigidbody>().velocity.magnitude * 3.6f) - minSpeed) / (maxSpeed - minSpeed));
        guiString += string.Format("speedTrigger: {0} ", speedTrigger.ToString("0.000"));
        
        accel = 1;// Mathf.Lerp(-1.0f, 1.0f, (wpDistRatio * accelSensitivity) - toCurrentDec);
        accel -= Mathf.Lerp(0, Mathf.Lerp(0.0f, 1.0f + brakeSensitivity, toCurrentDec * accelSensitivity), speedTrigger);

        accel -= Mathf.Lerp(0,Mathf.Abs(steer) * steerCutoff, speedTrigger);

        if (toCurrent < 0)
        {
            //accel = 1;
            //reverseTime = 5;
            //accel = Mathf.Lerp(-1,1,(Mathf.Abs(steer)));
            //steer = 1;
        }

        carController.aiDrive = true;
        carController.horizontalInput = Mathf.Clamp(steer,-1.0f, 1.0f);
        carController.verticalInput = Mathf.Clamp(accel, -1.0f, 1.0f);

        guiString += string.Format("ACC: {0} ", accel.ToString("0%"));
        guiString += string.Format("STR: {0} ", steer.ToString("0%"));
    }

    private void OnGUI()
    {
        //GUILayout.Label("AI: "+ guiString);
    }


    float GetScalarProjection(Vector3 p, Vector3 a, Vector3 b)
    {
        //Vector3 that points from a to p
        Vector3 ap = (p - a);
        //PVector that points from a to b
        Vector3 ab = (b - a);
        return Vector3.Dot(ap, ab.normalized);
    }
    Vector3 GetNormalPoint(Vector3 p, Vector3 a, Vector3 b)
    {
        //Using the dot product for scalar projection
        float sp = GetScalarProjection(p, a, b);
        Vector3 ab = (b - a);
        Vector3 normalPoint = a + (ab.normalized * sp);
        return normalPoint;
    }

    Vector3 SegmentPoint(Vector3 a, Vector3 b, Vector3 p)
    {
        // If a == b line segment is a point and will cause a divide by zero in the line segment test.
        // Instead return distance from a
        if (a == b)
            return Vector3.zero;

        // Line segment to point distance equation
        Vector3 ba = b - a;
        Vector3 pa = a - p;
        return (pa - ba * (Vector3.Dot(pa, ba) / Vector3.Dot(ba, ba)));
    }

    // a and b are points on the line. p is the point in question.
    Vector3 ClosestPoint(Vector3 a, Vector3 b, Vector3 p)
    {
        Vector3 ab = b - a;
        Vector3 ap = p - a;
        Vector3 ar = Vector3.Project(ap, ab);
        return a + ar;
    }

    // a and b are the endpoints of the line segment. p is the point in question.
    Vector3 ClosestPointConstrained(Vector3 a, Vector3 b, Vector3 p)
    {
        Vector3 ab = b - a;
        Vector3 ap = p - a;
        Vector3 ar = Vector3.Project(ap, ab);

        if (Vector3.Dot(ab, ar) < 0)
        {
            return a;
        }
        if (ar.sqrMagnitude > ab.sqrMagnitude)
        {
            return b;
        }
        return a + ar;
    }
}
