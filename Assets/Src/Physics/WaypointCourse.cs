using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class Waypoint
{
    public Transform transform;
    public Vector3 point;
    public float distance;
}

[ExecuteInEditMode]
public class WaypointCourse : MonoBehaviour {

    public List<Waypoint> waypoints;
    public float courseLength;
    int index;
    bool courseHasChanged;

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		if(!Application.isPlaying)
        {
            Transform[] wpContainer = GetComponentsInChildren<Transform>();
            courseLength = 0;
            if (wpContainer.Length>0)
            {
                waypoints.Clear();
                index = 0;
                foreach(Transform t in wpContainer)
                {
                    if(t != transform)
                    {
                        t.name = "WP" + index.ToString();
                        Waypoint w = new Waypoint();
                        w.transform = t;
                        w.point = t.position;
                        waypoints.Add(w);
                        index++;
                        courseHasChanged = true;
                    }
                }
            }

            if (courseHasChanged)
            {
                CalcDistance();
                courseHasChanged = false;
            }
        }
	}

    private void OnDrawGizmos()
    {

        if (waypoints.Count > 0)
        {

            Gizmos.color = Color.green;

            foreach (Waypoint w in waypoints)
                Gizmos.DrawSphere(w.point, 1f);

            Gizmos.color = Color.yellow;

            for (int a = 0; a < waypoints.Count; a++)
            {
                if(a < waypoints.Count - 1)
                    Gizmos.DrawLine(waypoints[a].point, waypoints[a + 1].point);
                else
                    Gizmos.DrawLine(waypoints[a].point, waypoints[0].point);
            }
        }

        /*
        if (waypoints.Count <= 0) return;
        int maxWp = waypoints.Count;
        courseLength = 0;
        //for (int i = 0; i< maxWp; i++)
        //{
            Waypoint wp = waypoints[i];

            // Rule works with closed courses for now
            //int i2 = (i>0) ? i - 1 : maxWp - 1;

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(wp.point, 0.3f);
            wp.distance = Vector3.Distance(wp.point, waypoints[i2].point);

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(wp.point, waypoints[i2].point);
            courseLength += wp.distance;
            /*
            if (i > 0)
            {
                wp.distance = Vector3.Distance(wp.point, waypoints[i - 1].point);
                Gizmos.DrawLine(wp.point, waypoints[i - 1].point);
            }
            else
            {
                wp.distance = Vector3.Distance(wp.point, waypoints[maxWp - 1].point);
                Gizmos.DrawLine(wp.point, waypoints[maxWp - 1].point);
            }*/
        //}*/
    }
    void CalcDistance()
    {
        if (waypoints.Count <= 0) return;

        int maxWp = waypoints.Count;
        courseLength = 0;
        for (int i = 0; i< maxWp; i++)
        {
            Waypoint wp = waypoints[i];
            int i2 = (i>0) ? i - 1 : maxWp - 1;
            wp.distance = Vector3.Distance(wp.point, waypoints[i2].point);
            courseLength += wp.distance;
        }
    }
}
