using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterBuoyancy : MonoBehaviour {
    
    public float damper = 0.1f;
    public float waterDensity = 1000;

    private Rigidbody[] allRigidbodys;
	
	void Update ()
    {
        allRigidbodys = GameObject.FindObjectsOfType<Rigidbody>();
        foreach (Rigidbody rb in allRigidbodys)
        {
            Vector3 wp = rb.transform.TransformPoint(Vector3.zero);
            float waterLevel = transform.localPosition.y;
            if (wp.y < waterLevel)
            {
                float k = (waterLevel - wp.y) / 0.5f;
                if (k > 1)
                {
                    k = 1f;
                }
                else if (k < 0)
                {
                    k = 0f;
                }

                var velocity = rb.GetPointVelocity(wp);
                var localDampingForce = -velocity * damper * rb.mass;

                float volume = rb.mass / 1000;
                float archimedesForceMagnitude = waterDensity * Mathf.Abs(Physics.gravity.y) * volume;
                Vector3 localArchimedesForce = new Vector3(0, archimedesForceMagnitude, 0);
                Vector3 force = localDampingForce + Mathf.Sqrt(k) * localArchimedesForce;
                rb.AddForceAtPosition(force, wp);
            }
        }
    }
}
