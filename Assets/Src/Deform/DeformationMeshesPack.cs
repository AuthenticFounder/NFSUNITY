using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeformationMeshesPack : MonoBehaviour
{
    public MeshFilter[] meshFilters;
    public MeshCollider[] colliders;
    public float ImpactDamage = 3f;
    public float radiusDeformate = 0.7f;
    public float maxDeformation = 0.16f;
    public float minVelocity = 5f;
    private Vector3[][] originalVertices;
    private float nextTimeDeform = 0f;
    private Vector3 sumPosImpacts = Vector3.zero;
    private Vector3 sumVelocityImpacts = Vector3.zero;
    private int sumImpacts = 0;

    private Rigidbody rigidB;
    private Vector3 impactVelocity;

    void Start ()
    {
        rigidB = GetComponent<Rigidbody>();

        originalVertices = new Vector3[meshFilters.Length][];

        for(int i = 0; i < meshFilters.Length; i++)
        {
            originalVertices[i] = meshFilters[i].mesh.vertices;
            meshFilters[i].mesh.MarkDynamic();
        }
    }

    void FixedUpdate()
    {
        if (Time.time - nextTimeDeform >= 0.2f && sumImpacts > 0)
        {
            float invCount = 1f / sumImpacts;
            sumPosImpacts *= invCount; // same sumPosImpacts / sumImpacts
            sumVelocityImpacts *= invCount;
            //Vector3 impactVelocity = Vector3.zero;

            if (sumVelocityImpacts.sqrMagnitude > 1.5f)
            {
                impactVelocity = transform.TransformDirection(sumVelocityImpacts) * 0.5f;
            }
            impactVelocity = new Vector3(impactVelocity.x * impactVelocity.x, impactVelocity.y * impactVelocity.y, impactVelocity.z * impactVelocity.z);

            if (impactVelocity.sqrMagnitude > 0)
            {
                Vector3 contactPoint = transform.TransformPoint(sumPosImpacts);

                for (int i = 0; i < meshFilters.Length; i++)
                {
                    DeformationMesh(meshFilters[i].mesh, originalVertices[i], meshFilters[i].transform, contactPoint, impactVelocity, i);
                }
            }

            sumImpacts = 0;
            sumPosImpacts = Vector3.zero;
            sumVelocityImpacts = Vector3.zero;
            nextTimeDeform = Time.time + 0.2f * Random.Range(-0.4f, 0.4f);
        }
    }

    private void DeformationMesh(Mesh mesh, Vector3[] originalVerts, Transform localTransfrom, Vector3 contactPoint, Vector3 contactVelocity, int i)
    {
        Vector3[] vertices = mesh.vertices;
        bool isDeformate = false;

        for (int j = 0; j < vertices.Length; j++)
        {
            Vector3 localContactPoint = localTransfrom.InverseTransformPoint(contactPoint);
            Vector3 localContactForce = localTransfrom.InverseTransformDirection(contactVelocity);
            float distance = Vector3.Distance(localContactPoint, vertices[j]);

            if (distance <= radiusDeformate)
            {
                isDeformate = true;
                Vector3 damage = localContactForce * (radiusDeformate - distance) * ImpactDamage;
                vertices[j] += damage;
                Vector3 deformation = vertices[j] - originalVerts[j];
                vertices[j] = originalVerts[j] + deformation.normalized * maxDeformation;
                //vertices[j] = vertices[j] + deformation.normalized * maxDeformation;
            }
        }

        if (isDeformate)
        {
            mesh.vertices = vertices;
            //mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            if (colliders.Length<=0) return;

            if (colliders[i] != null)
            {
                colliders[i].sharedMesh = mesh;
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.relativeVelocity.magnitude > minVelocity)
        {
            int impactCount = 0;
            Vector3 impactPosition = Vector3.zero, impactVelocity = Vector3.zero;

            foreach (ContactPoint contacts in collision.contacts)
            {
                impactCount++;
                impactPosition += contacts.point;
                impactVelocity += collision.relativeVelocity;
            }

            if (impactCount > 0)
            {
                float invCount = 1f / impactCount;
                impactPosition *= invCount; //same impactPosition / impactCount
                impactVelocity *= invCount;
                sumPosImpacts += transform.InverseTransformPoint(impactPosition);
                sumVelocityImpacts += transform.InverseTransformDirection(impactVelocity);
                sumImpacts++;
            }

            //here you play crash sound
        }

    }
}
