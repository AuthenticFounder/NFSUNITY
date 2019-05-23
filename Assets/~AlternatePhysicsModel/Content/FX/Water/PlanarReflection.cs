using UnityEngine;
using System.Collections;

// This is in fact just the Water script from Pro Standard Assets,
// just with refraction stuff removed.

[ExecuteInEditMode] // Make mirror live-update even when not in play mode
public class PlanarReflection : MonoBehaviour
{
    public enum Axis
    {
        right,
        up,
        forward
    }
    public Axis reflectionAxis = Axis.up;
    public bool m_DisablePixelLights = true;
    public int m_TextureSize = 512;
    public float m_ClipPlaneOffset = 0.00f;
    public bool alignWithCam = true;

    public LayerMask m_ReflectLayers = -1;

    private Hashtable m_ReflectionCameras = new Hashtable(); // Camera -> Camera table

    private RenderTexture m_ReflectionTexture = null;
    private int m_OldReflectionTextureSize = 0;

    private static bool s_InsideRendering = false;

    // This is called when it's known that the object will be rendered by some
    // camera. We render reflections and do other updates here.
    // Because the script executes in edit mode, reflections for the scene view
    // camera will just work!
    private Renderer[] renderers;
    public void OnWillRenderObject()
    {
        renderers = transform.GetComponentsInChildren<Renderer>();

        foreach (Renderer r in renderers)
        {
            Render(r);
        }

    }
    /*
    public bool testFloor;
    RaycastHit hit;
    private float floorDistance;
    private float internalClipPlaneOffset;

    private void RayTestFloor()
    {
        Ray downRay = new Ray(Camera.current.transform.position, -Vector3.up);

        internalClipPlaneOffset = m_ClipPlaneOffset;

        if (testFloor)
        {
            internalClipPlaneOffset = (Camera.current.transform.localPosition.y) + m_ClipPlaneOffset;

            if (Physics.Raycast(downRay, out hit)) {
                floorDistance = hit.distance;
                internalClipPlaneOffset -= floorDistance;
            }
        }

    }*/

    float[] m_ClipPoints;

     // Collect the height info
    public void PopulateClipPoints(Transform tr)
    {
        Mesh m = tr.GetComponent<MeshFilter>().mesh;
        Vector3[] verts = m.vertices;
        m_ClipPoints = new float[m.vertices.Length];
        for(int i=0; i<m.vertices.Length; i++)
            m_ClipPoints[i] = m.vertices[i].y + m_ClipPlaneOffset;
    }
    public void Render(Renderer rend)
    {
        //var rend = GetComponent<Renderer>();
        if (!enabled || !rend || !rend.sharedMaterial || !rend.enabled)
            return;

        Camera cam = Camera.current;
        if (!cam)
            return;

        // Safeguard from recursive reflections.        
        if (s_InsideRendering)
            return;
        s_InsideRendering = true;

        Camera reflectionCamera;
        CreateMirrorObjects(cam, out reflectionCamera);

        // find out the reflection plane: position and normal in world space
        Vector3 pos = transform.position; // modifyed

        //Reflection normal
        Vector3 normal = Vector3.zero;

        if (reflectionAxis == Axis.up)
            normal = Vector3.up;
        else if (reflectionAxis == Axis.right)
            normal = Vector3.right;
        else if (reflectionAxis == Axis.forward)
            normal = Vector3.forward;

        // Optionally disable pixel lights for reflection
        int oldPixelLightCount = QualitySettings.pixelLightCount;
        if (m_DisablePixelLights)
            QualitySettings.pixelLightCount = 0;

        UpdateCameraModes(cam, reflectionCamera);

        // Render reflection
        // Reflect camera around reflection plane   
        Vector3 camNormal = (cam.transform.TransformPoint(normal) - cam.transform.position).normalized;

        Vector3 n = normal;// - cam.transform.up;
		    //float d = Vector3.Dot (alignWithCam ? camNormal : normal, pos) - m_ClipPlaneOffset;
        float d = Vector3.Dot(n, pos) - m_ClipPlaneOffset;
        Vector4 reflectionPlane = new Vector4 (n.x, -n.y, n.z, d);

		    Matrix4x4 reflection = Matrix4x4.zero;
		    CalculateReflectionMatrix (ref reflection, reflectionPlane);
		    Vector3 oldpos = cam.transform.position;
		    Vector3 newpos = reflection.MultiplyPoint( oldpos );
		    reflectionCamera.worldToCameraMatrix = cam.worldToCameraMatrix * reflection;

		    // Setup oblique projection matrix so that near plane is our reflection
		    // plane. This way we clip everything below/above it for free.
		    Vector4 clipPlane = CameraSpacePlane( reflectionCamera, pos, normal, 1.0f );
		    //Matrix4x4 projection = cam.projectionMatrix;
		    //Matrix4x4 projection = cam.CalculateObliqueMatrix(clipPlane);
		    //reflectionCamera.projectionMatrix = projection;

		    reflectionCamera.cullingMask = ~(1<<4) & m_ReflectLayers.value; // never render water layer
		    reflectionCamera.targetTexture = m_ReflectionTexture;
		    GL.SetRevertBackfacing (true);
		    reflectionCamera.transform.position = newpos;
		    Vector3 euler = cam.transform.eulerAngles;
		    reflectionCamera.transform.eulerAngles = new Vector3(0, euler.y, euler.z);
		    reflectionCamera.Render();
		    reflectionCamera.transform.position = oldpos;
		    GL.SetRevertBackfacing (false);

		    Material[] materials = rend.sharedMaterials;
		    foreach( Material mat in materials ) {
			    if( mat.HasProperty("_ReflectionTex") )
				    mat.SetTexture( "_ReflectionTex", m_ReflectionTexture );
		    }

		    // Restore pixel light count
		    if( m_DisablePixelLights )
			    QualitySettings.pixelLightCount = oldPixelLightCount;

		    s_InsideRendering = false;
	}


	// Cleanup all the objects we possibly have created
	void OnDisable()
	{
		if( m_ReflectionTexture ) {
			DestroyImmediate( m_ReflectionTexture );
			m_ReflectionTexture = null;
		}
		foreach( DictionaryEntry kvp in m_ReflectionCameras )
			DestroyImmediate( ((Camera)kvp.Value).gameObject );
		m_ReflectionCameras.Clear();
	}


	private void UpdateCameraModes( Camera src, Camera dest )
	{
		if( dest == null )
			return;
		// set camera to clear the same way as current camera
		dest.clearFlags = src.clearFlags;
		dest.backgroundColor = src.backgroundColor;        
		if( src.clearFlags == CameraClearFlags.Skybox )
		{
			Skybox sky = src.GetComponent(typeof(Skybox)) as Skybox;
			Skybox mysky = dest.GetComponent(typeof(Skybox)) as Skybox;
			if( !sky || !sky.material )
			{
				mysky.enabled = false;
			}
			else
			{
				mysky.enabled = true;
				mysky.material = sky.material;
			}
		}
		// update other values to match current camera.
		// even if we are supplying custom camera&projection matrices,
		// some of values are used elsewhere (e.g. skybox uses far plane)
		dest.farClipPlane = src.farClipPlane;
		dest.nearClipPlane = src.nearClipPlane;
		dest.orthographic = src.orthographic;
		dest.fieldOfView = src.fieldOfView;
		dest.aspect = src.aspect;
		dest.orthographicSize = src.orthographicSize;
	}

	// On-demand create any objects we need
	private void CreateMirrorObjects( Camera currentCamera, out Camera reflectionCamera )
	{
		reflectionCamera = null;

		// Reflection render texture
		if( !m_ReflectionTexture || m_OldReflectionTextureSize != m_TextureSize )
		{
			if( m_ReflectionTexture )
				DestroyImmediate( m_ReflectionTexture );
			m_ReflectionTexture = new RenderTexture( m_TextureSize, m_TextureSize, 16 );
			m_ReflectionTexture.name = "__MirrorReflection" + GetInstanceID();
			m_ReflectionTexture.isPowerOfTwo = true;
			m_ReflectionTexture.hideFlags = HideFlags.DontSave;
            m_ReflectionTexture.useMipMap = true;
            m_OldReflectionTextureSize = m_TextureSize;
		}

		// Camera for reflection
		reflectionCamera = m_ReflectionCameras[currentCamera] as Camera;
		if( !reflectionCamera ) // catch both not-in-dictionary and in-dictionary-but-deleted-GO
		{
			GameObject go = new GameObject( "Mirror Refl Camera id" + GetInstanceID() + " for " + currentCamera.GetInstanceID(), typeof(Camera), typeof(Skybox) );
			reflectionCamera = go.GetComponent<Camera>();
			reflectionCamera.enabled = false;
			reflectionCamera.transform.position = transform.position;
			reflectionCamera.transform.rotation = transform.rotation;
			reflectionCamera.gameObject.AddComponent<FlareLayer>();
			go.hideFlags = HideFlags.HideAndDontSave;
			m_ReflectionCameras[currentCamera] = reflectionCamera;
		}        
	}

	// Extended sign: returns -1, 0 or 1 based on sign of a
	private static float sgn(float a)
	{
		if (a > 0.0f) return 1.0f;
		if (a < 0.0f) return -1.0f;
		return 0.0f;
	}

	// Given position/normal of the plane, calculates plane in camera space.
	private Vector4 CameraSpacePlane (Camera cam, Vector3 pos, Vector3 normal, float sideSign)
	{
		Vector3 offsetPos = pos + normal * m_ClipPlaneOffset;
		Matrix4x4 m = cam.worldToCameraMatrix;
		Vector3 cpos = m.MultiplyPoint( offsetPos );
		Vector3 cnormal = m.MultiplyVector( normal ).normalized * sideSign;
		return new Vector4( cnormal.x, cnormal.y, cnormal.z, -Vector3.Dot(cpos,cnormal) );
	}

	// Calculates reflection matrix around the given plane
	private static void CalculateReflectionMatrix (ref Matrix4x4 reflectionMat, Vector4 plane)
	{
		reflectionMat.m00 = (1F - 2F*plane[0]*plane[0]);
		reflectionMat.m01 = (   - 2F*plane[0]*plane[1]);
		reflectionMat.m02 = (   - 2F*plane[0]*plane[2]);
		reflectionMat.m03 = (   - 2F*plane[3]*plane[0]);

		reflectionMat.m10 = (   - 2F*plane[1]*plane[0]);
		reflectionMat.m11 = (1F - 2F*plane[1]*plane[1]);
		reflectionMat.m12 = (   - 2F*plane[1]*plane[2]);
		reflectionMat.m13 = (   - 2F*plane[3]*plane[1]);

		reflectionMat.m20 = (   - 2F*plane[2]*plane[0]);
		reflectionMat.m21 = (   - 2F*plane[2]*plane[1]);
		reflectionMat.m22 = (1F - 2F*plane[2]*plane[2]);
		reflectionMat.m23 = (   - 2F*plane[3]*plane[2]);

		reflectionMat.m30 = 0F;
		reflectionMat.m31 = 0F;
		reflectionMat.m32 = 0F;
		reflectionMat.m33 = 1F;
	}
}