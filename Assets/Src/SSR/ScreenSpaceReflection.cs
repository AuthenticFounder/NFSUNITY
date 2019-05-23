/********************************************************************
 FileName: ScreenSpaceReflection.cs
 Description: 屏幕空间反射SSR，DitherRayMarching，延迟渲染下
 history: 30:8:2018 by puppet_master
 https://blog.csdn.net/puppet_master
*********************************************************************/
using UnityEngine;
[ExecuteInEditMode]
public class ScreenSpaceReflection : MonoBehaviour
{
    Material reflectionMaterial = null;
    Camera currentCamera = null;
    [Range(0, 1000.0f)]
    public float maxRayMarchingDistance = 500.0f;
    [Range(0, 1024)]
    public int maxRayMarchingStep = 64;
    [Range(0, 32)]
    public int maxRayMarchingBinarySearchCount = 8;
    [Range(1, 50)]
    public int rayMarchingStepSize = 2;
    [Range(0, 2.0f)]
    public float depthThickness = 0.01f;

    [Range(0, 3)]
    public int downSample = 1;

    [Range(0, 3)]
    public int samplerScale = 1;

    private Texture2D ditherMap = null;


    private void Awake()
    {
        var shader = Shader.Find("Reflection/ScreenSpaceReflection");
        reflectionMaterial = new Material(shader);
        currentCamera = GetComponent<Camera>();
        if (ditherMap == null)
            ditherMap = GenerateDitherMap();

        if (Application.isPlaying)
            currentCamera.depthTextureMode |= DepthTextureMode.Depth | DepthTextureMode.MotionVectors;
        else
            currentCamera.depthTextureMode = DepthTextureMode.Depth;
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (reflectionMaterial == null)
        {
            Graphics.Blit(source, destination);
            return;
        }

        var width = source.width >> downSample;
        var height = source.height >> downSample;
        var screenSize = new Vector4(1.0f / width, 1.0f / height, width, height);
        var clipToScreenMatrix = new Matrix4x4();
        // (clip * 0.5 + 0.5)变换到screenspace，*width或height，得到真正的像素位置
        clipToScreenMatrix.SetRow(0, new Vector4(width * 0.5f, 0, 0, width * 0.5f));
        clipToScreenMatrix.SetRow(1, new Vector4(0, height * 0.5f, 0, height * 0.5f));
        clipToScreenMatrix.SetRow(2, new Vector4(0, 0, 1.0f, 0));
        clipToScreenMatrix.SetRow(3, new Vector4(0, 0, 0, 1.0f));
        var projectionMatrix = GL.GetGPUProjectionMatrix(currentCamera.projectionMatrix, false);
        var viewToScreenMatrix = clipToScreenMatrix * projectionMatrix;
        reflectionMaterial.SetMatrix("_ViewToScreenMatrix", viewToScreenMatrix);
        reflectionMaterial.SetVector("_ScreenSize", screenSize);

        reflectionMaterial.SetMatrix("_InverseProjectionMatrix", currentCamera.projectionMatrix.inverse);
        reflectionMaterial.SetMatrix("_CameraProjectionMatrix", currentCamera.projectionMatrix);
        reflectionMaterial.SetMatrix("_WorldToCameraMatrix", currentCamera.worldToCameraMatrix);

        reflectionMaterial.SetFloat("_maxRayMarchingBinarySearchCount", maxRayMarchingBinarySearchCount);
        reflectionMaterial.SetFloat("_maxRayMarchingDistance", maxRayMarchingDistance);
        reflectionMaterial.SetFloat("_maxRayMarchingStep", maxRayMarchingStep);
        reflectionMaterial.SetFloat("_rayMarchingStepSize", rayMarchingStepSize);
        reflectionMaterial.SetFloat("_depthThickness", depthThickness);
        reflectionMaterial.SetTexture("_ditherMap", ditherMap);

        var reflectRT = RenderTexture.GetTemporary(width, height, 0, source.format);
        var tempBlurRT = RenderTexture.GetTemporary(width, height, 0, source.format);
        Graphics.Blit(source, reflectRT, reflectionMaterial, 0);

        //高斯模糊，两次模糊，横向纵向，使用pass1进行高斯模糊
        reflectionMaterial.SetVector("_offsets", new Vector4(0, samplerScale, 0, 0));
        Graphics.Blit(reflectRT, tempBlurRT, reflectionMaterial, 1);
        reflectionMaterial.SetVector("_offsets", new Vector4(samplerScale, 0, 0, 0));
        Graphics.Blit(tempBlurRT, reflectRT, reflectionMaterial, 1);

        //将反射贴图叠加到原图
        reflectionMaterial.SetTexture("_ReflectTex", reflectRT);
        Graphics.Blit(source, destination, reflectionMaterial, 2);

        RenderTexture.ReleaseTemporary(reflectRT);
        RenderTexture.ReleaseTemporary(tempBlurRT);
    }


    private Texture2D GenerateDitherMap()
    {
        int texSize = 4;
        var ditherMap = new Texture2D(texSize, texSize, TextureFormat.Alpha8, false, true);
        ditherMap.filterMode = FilterMode.Point;
        Color32[] colors = new Color32[texSize * texSize];

        colors[0] = GetDitherColor(0.0f);
        colors[1] = GetDitherColor(8.0f);
        colors[2] = GetDitherColor(2.0f);
        colors[3] = GetDitherColor(10.0f);

        colors[4] = GetDitherColor(12.0f);
        colors[5] = GetDitherColor(4.0f);
        colors[6] = GetDitherColor(14.0f);
        colors[7] = GetDitherColor(6.0f);

        colors[8] = GetDitherColor(3.0f);
        colors[9] = GetDitherColor(11.0f);
        colors[10] = GetDitherColor(1.0f);
        colors[11] = GetDitherColor(9.0f);

        colors[12] = GetDitherColor(15.0f);
        colors[13] = GetDitherColor(7.0f);
        colors[14] = GetDitherColor(13.0f);
        colors[15] = GetDitherColor(5.0f);

        ditherMap.SetPixels32(colors);
        ditherMap.Apply();
        return ditherMap;
    }

    private Color32 GetDitherColor(float value)
    {
        byte byteValue = (byte)(value / 16.0f * 255);
        return new Color32(byteValue, byteValue, byteValue, byteValue);
    }

}