using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

[ExecuteInEditMode]
public class BasicRayTracing : MonoBehaviour
{
    public RayTracingShader rayTracingShader = null;

    private uint cameraWidth = 0;
    private uint cameraHeight = 0;

    private RenderTexture rayTracingOutput = null;

    private RayTracingAccelerationStructure raytracingAccelerationStructure = null;

    private void CreateRaytracingAccelerationStructure()
    {
        if (raytracingAccelerationStructure == null)
        {
            RayTracingAccelerationStructure.RASSettings settings = new RayTracingAccelerationStructure.RASSettings();
            settings.rayTracingModeMask = RayTracingAccelerationStructure.RayTracingModeMask.Everything;
            settings.managementMode = RayTracingAccelerationStructure.ManagementMode.Automatic;
            settings.layerMask = 255;

            raytracingAccelerationStructure = new RayTracingAccelerationStructure(settings);
        }
    }

    private void ReleaseResources()
    {
        if (raytracingAccelerationStructure != null)
        {
            raytracingAccelerationStructure.Release();
            raytracingAccelerationStructure = null;
        }

        if (rayTracingOutput)
        {
            rayTracingOutput.Release();
            rayTracingOutput = null;
        }

        cameraWidth = 0;
        cameraHeight = 0;
    }

    private void CreateResources()
    {
        CreateRaytracingAccelerationStructure();

        if (cameraWidth != Camera.main.pixelWidth || cameraHeight != Camera.main.pixelHeight)
        {
            if (rayTracingOutput)
                rayTracingOutput.Release();

            rayTracingOutput = new RenderTexture(Camera.main.pixelWidth, Camera.main.pixelHeight, 0, RenderTextureFormat.ARGBHalf);
            rayTracingOutput.enableRandomWrite = true;
            rayTracingOutput.Create();

            cameraWidth = (uint)Camera.main.pixelWidth;
            cameraHeight = (uint)Camera.main.pixelHeight;
        }
    }

    void OnDestroy()
    {
        ReleaseResources();
    }

    void OnDisable()
    {
        ReleaseResources();
    }

    private void Update()
    {
        CreateResources();
    }

    [ImageEffectOpaque]
    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (!SystemInfo.supportsRayTracing || !rayTracingShader)
        {
            Debug.Log("The RayTracing API is not supported by this GPU or by the current graphics API.");
            Graphics.Blit(src, dest);
            return;
        }

        if (raytracingAccelerationStructure == null)
            return;

        CommandBuffer cmdBuffer = new CommandBuffer();

        cmdBuffer.BuildRayTracingAccelerationStructure(raytracingAccelerationStructure);

        cmdBuffer.SetRayTracingShaderPass(rayTracingShader, "Test");

        // Input
        cmdBuffer.SetRayTracingAccelerationStructure(rayTracingShader, Shader.PropertyToID("g_SceneAccelStruct"), raytracingAccelerationStructure);
        cmdBuffer.SetRayTracingMatrixParam(rayTracingShader, Shader.PropertyToID("g_InvViewMatrix"), Camera.main.cameraToWorldMatrix);
        cmdBuffer.SetRayTracingFloatParam(rayTracingShader, Shader.PropertyToID("g_Zoom"), Mathf.Tan(Mathf.Deg2Rad * Camera.main.fieldOfView * 0.5f));

        // Output
        cmdBuffer.SetRayTracingTextureParam(rayTracingShader, Shader.PropertyToID("g_Output"), rayTracingOutput);

        cmdBuffer.DispatchRays(rayTracingShader, "MainRayGenShader", cameraWidth, cameraHeight, 1);

        Graphics.ExecuteCommandBuffer(cmdBuffer);

        cmdBuffer.Release();

        Graphics.Blit(rayTracingOutput, dest);
    }
}
