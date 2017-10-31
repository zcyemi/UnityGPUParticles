using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;



public class GpuInstancingParticles : MonoBehaviour
{
    private const int count = 1024;

    public Mesh mesh;
    public Material mat;
    public Camera m_targetCamera;
    public MeshRenderer m_maskrender;

    [Header("Colider")]
    public BoxCollider2D m_Collider;
    public Vector3 m_CollidersData;

    

    
    [Range(1, 20)]
    public int m_physicalUpdate = 10;
    private float m_timedelta = 0;
    private Vector4 m_generatePosOri = Vector4.zero;

    [Header("RT")]
    [Range(0.25f, 1.0f)]
    public float m_rtSize = 1.0f;
    private RenderTexture m_particleRT;

    public bool m_drawGizmos = false;
    public Rect m_generateRect = new Rect(0, 1080, 1920, 1080);
    [Range(100f, -100f)]
    public float m_bottomOffset = -10f;
    

    

    private CommandBuffer m_commandBuffer;
    private uint[] m_bufferArgs;
    private ComputeBuffer m_bufferWithArgs;
    private Vector4[] m_bufferPosition;
    private Vector4[] m_bufferParams;

    private ComputeBuffer m_cbufferPosition;
    private ComputeBuffer m_cbufferParams;
    public ComputeShader m_particleComputeShader;
    private int m_particleKernelId;


    void Start()
    {
        m_bufferArgs = new uint[5] { mesh.GetIndexCount(0), count, 0, 0, 0 };
        m_bufferWithArgs = new ComputeBuffer(1, sizeof(uint) * 5, ComputeBufferType.IndirectArguments);
        m_bufferWithArgs.SetData(m_bufferArgs);

        m_particleRT = new RenderTexture((int)(Screen.width * m_rtSize), (int)(Screen.height * m_rtSize), 0, RenderTextureFormat.ARGB32);

        if (m_maskrender != null)
        {
            m_maskrender.material.SetTexture("_ParticleRT", m_particleRT);
        }

        m_cbufferParams = new ComputeBuffer(count, sizeof(float) * 4);
        m_cbufferPosition = new ComputeBuffer(count, sizeof(float) * 4);

        m_bufferParams = new Vector4[count];
        m_bufferPosition = new Vector4[count];


        for (int i = 0; i < count; i++)
        {
            Vector3 startpos = GeneratePos();
            m_bufferPosition[i].x = startpos.x;                 //posx
            m_bufferPosition[i].y = startpos.y;                 //posy
            m_bufferPosition[i].z = startpos.z;                 //posz
            m_bufferPosition[i].w = m_targetCamera.transform.position.y - m_bottomOffset;  //bottom pos

            m_bufferParams[i].x = 0.01f;                        //size x
            m_bufferParams[i].y = Random.Range(0.4f, 0.7f);     //size y
            m_bufferParams[i].z = Random.Range(4f, 6f);         //speed
            m_bufferParams[i].w = Time.time;                    //starttime
        }

        m_cbufferPosition.SetData(m_bufferPosition);
        m_cbufferParams.SetData(m_bufferParams);

        mat.SetBuffer("_cbufferPosition", m_cbufferPosition);
        mat.SetBuffer("_cbufferParams", m_cbufferParams);

        m_timedelta = Time.time;

        m_particleKernelId = m_particleComputeShader.FindKernel("CSParticleKernel");
        m_particleComputeShader.SetBuffer(m_particleKernelId, "_cbufferPosition", m_cbufferPosition);
        m_particleComputeShader.SetBuffer(m_particleKernelId, "_cbufferParams", m_cbufferParams);

        m_particleComputeShader.SetVector("_GenRect", new Vector4(m_generateRect.x, m_generateRect.y, m_generateRect.width, m_generateRect.height));

        ProcessCollider();
    }

    private void ProcessCollider()
    {
        m_CollidersData = m_Collider.transform.position;
        m_CollidersData.z = m_Collider.transform.lossyScale.x * 0.5f;
        m_CollidersData.y += m_Collider.transform.lossyScale.y * 0.5f;

        m_particleComputeShader.SetVector("_collider", m_CollidersData);
    }


    private void Update()
    {
        float t = 1.0f / m_physicalUpdate;
        float curtime = Time.time;

        if (curtime - m_timedelta > t)
        {
            m_timedelta = curtime;

            ProcessCollider();

            m_particleComputeShader.SetFloat("_Time", curtime);
            Vector4 cpos = m_targetCamera.transform.position;
            cpos.w = m_targetCamera.transform.position.y - m_bottomOffset;
            m_particleComputeShader.SetVector("_CameraPos", cpos);
            m_particleComputeShader.Dispatch(m_particleKernelId, count / 64, 1, 1);

            if (m_commandBuffer == null)
            {
                Debug.Log("Generate Command Buffer");

                m_commandBuffer = new CommandBuffer();
                m_commandBuffer.name = "GPUInstancingParticles";


                var cameraevent = CameraEvent.BeforeForwardOpaque;

                m_commandBuffer.Clear();

                m_commandBuffer.ClearRenderTarget(true, true,new Color32(0,0,0,0));
                m_commandBuffer.DrawMeshInstancedIndirect(mesh, 0, mat, 0, m_bufferWithArgs);

                m_commandBuffer.Blit(new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget), m_particleRT);
                m_targetCamera.RemoveCommandBuffers(cameraevent);
                m_targetCamera.AddCommandBuffer(cameraevent, m_commandBuffer);
            }
        }
    }

    private void OnDisable()
    {
        if (m_particleRT != null)
            m_particleRT.Release();

        if (m_cbufferParams != null) m_cbufferParams.Release();
        if (m_cbufferPosition != null) m_cbufferPosition.Release();

        if (m_bufferWithArgs != null) m_bufferWithArgs.Release();
    }

    private void ReGenPos(ref Vector4 vec)
    {
        vec.x = m_generatePosOri.x + m_generateRect.width * (Random.value - 0.5f);
        vec.y = m_generatePosOri.y + m_generateRect.height * Random.value;

        vec.w = m_generatePosOri.z;
    }


    private Vector3 GeneratePos()
    {
        Vector3 vec = m_targetCamera.transform.position;
        vec.x += m_generateRect.x + m_generateRect.width * (Random.value - 0.5f);
        vec.y += m_generateRect.y + m_generateRect.height * Random.value;
        vec.z = 0;

        return vec;
    }


    private void OnDrawGizmos()
    {
        if (m_targetCamera == null) return;

        Vector2 tarpos = m_targetCamera.transform.position;
        float x1 = tarpos.x - m_generateRect.width * 0.5f + m_generateRect.x;
        float x2 = tarpos.x + m_generateRect.width * 0.5f + m_generateRect.x;

        float y1 = tarpos.y + m_generateRect.y;
        float y2 = tarpos.y + m_generateRect.height + m_generateRect.y;

        Gizmos.DrawLine(new Vector3(x1, y1, 0), new Vector3(x2, y1, 0));
        Gizmos.DrawLine(new Vector3(x1, y2, 0), new Vector3(x2, y2, 0));
        Gizmos.DrawLine(new Vector3(x1, y1, 0), new Vector3(x1, y2, 0));
        Gizmos.DrawLine(new Vector3(x2, y1, 0), new Vector3(x2, y2, 0));


        Vector3 bline = m_targetCamera.transform.position;
        bline.z = 0;
        bline.y -= m_bottomOffset;

        Gizmos.DrawLine(bline - new Vector3(m_generateRect.width * 0.5f, 0, 0), bline + new Vector3(m_generateRect.width * 0.5f, 0, 0));
    }
}
