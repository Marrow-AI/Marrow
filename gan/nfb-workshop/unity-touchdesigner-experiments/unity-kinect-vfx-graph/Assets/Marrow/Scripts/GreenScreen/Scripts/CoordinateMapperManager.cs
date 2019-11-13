using UnityEngine;
using System.Collections;
using Windows.Kinect;
using System.Runtime.InteropServices;
using System;

public class CoordinateMapperManager : MonoBehaviour
{
    private KinectSensor m_pKinectSensor;
    private CoordinateMapper m_pCoordinateMapper;
    private MultiSourceFrameReader m_pMultiSourceFrameReader;
    private DepthSpacePoint[] m_pDepthCoordinates;

    private byte[] pColorBuffer;
    private byte[] pBodyIndexBuffer;

    private ushort[] pDepthBuffer;

    const int cDepthWidth = 512;
    const int cDepthHeight = 424;
    const int cColorWidth = 1920;
    const int cColorHeight = 1080;

    long frameCount = 0;

    double elapsedCounter = 0.0;
    double fps = 0.0;

    Texture2D m_pColorRGBX;

    bool nullFrame = false;

    void Awake()
    {
        pColorBuffer = new byte[cColorWidth * cColorHeight * 4];
        pBodyIndexBuffer = new byte[cDepthWidth * cDepthHeight];
        pDepthBuffer = new ushort[cDepthWidth * cDepthHeight];

        m_pColorRGBX = new Texture2D(cColorWidth, cColorHeight, TextureFormat.RGBA32, false);

        m_pDepthCoordinates = new DepthSpacePoint[cColorWidth * cColorHeight];

        InitializeDefaultSensor();
    }

    Rect fpsRect = new Rect(10, 10, 200, 30);
    Rect nullFrameRect = new Rect(10, 50, 200, 30);

    void OnGUI()
    {
        GUI.Box(fpsRect, "FPS: " + fps.ToString("0.00"));

        if (nullFrame)
        {
            GUI.Box(nullFrameRect, "NULL MSFR Frame");
        }
    }

    public Texture2D GetColorTexture()
    {
        return m_pColorRGBX;
    }

    public byte[] GetBodyIndexBuffer()
    {
        return pBodyIndexBuffer;
    }

    public DepthSpacePoint[] GetDepthCoordinates()
    {
        return m_pDepthCoordinates;
    }

    void InitializeDefaultSensor()
    {
        m_pKinectSensor = KinectSensor.GetDefault();

        if (m_pKinectSensor != null)
        {
            // Initialize the Kinect and get coordinate mapper and the frame reader
            m_pCoordinateMapper = m_pKinectSensor.CoordinateMapper;

            m_pKinectSensor.Open();
            if (m_pKinectSensor.IsOpen)
            {
                m_pMultiSourceFrameReader = m_pKinectSensor.OpenMultiSourceFrameReader(
                    FrameSourceTypes.Color | FrameSourceTypes.Depth | FrameSourceTypes.BodyIndex);
            }
        }

        if (m_pKinectSensor == null)
        {
            UnityEngine.Debug.LogError("No ready Kinect found!");
        }
    }

    void ProcessFrame()
    {
        var pDepthData = GCHandle.Alloc(pDepthBuffer, GCHandleType.Pinned);
        var pDepthCoordinatesData = GCHandle.Alloc(m_pDepthCoordinates, GCHandleType.Pinned);

        m_pCoordinateMapper.MapColorFrameToDepthSpaceUsingIntPtr(
            pDepthData.AddrOfPinnedObject(),
            (uint)pDepthBuffer.Length * sizeof(ushort),
            pDepthCoordinatesData.AddrOfPinnedObject(),
            (uint)m_pDepthCoordinates.Length);

        pDepthCoordinatesData.Free();
        pDepthData.Free();

        m_pColorRGBX.LoadRawTextureData(pColorBuffer);
        m_pColorRGBX.Apply();
    }

    void Update()
    {
        // Get FPS
        elapsedCounter += Time.deltaTime;
        if (elapsedCounter > 1.0)
        {
            fps = frameCount / elapsedCounter;
            frameCount = 0;
            elapsedCounter = 0.0;
        }

        if (m_pMultiSourceFrameReader == null)
        {
            return;
        }

        var pMultiSourceFrame = m_pMultiSourceFrameReader.AcquireLatestFrame();
        if (pMultiSourceFrame != null)
        {
            frameCount++;
            nullFrame = false;

            using (var pDepthFrame = pMultiSourceFrame.DepthFrameReference.AcquireFrame())
            {
                using (var pColorFrame = pMultiSourceFrame.ColorFrameReference.AcquireFrame())
                {
                    using (var pBodyIndexFrame = pMultiSourceFrame.BodyIndexFrameReference.AcquireFrame())
                    {
                        // Get Depth Frame Data.
                        if (pDepthFrame != null)
                        {
                            var pDepthData = GCHandle.Alloc(pDepthBuffer, GCHandleType.Pinned);
                            pDepthFrame.CopyFrameDataToIntPtr(pDepthData.AddrOfPinnedObject(), (uint)pDepthBuffer.Length * sizeof(ushort));
                            pDepthData.Free();
                        }

                        // Get Color Frame Data
                        if (pColorFrame != null)
                        {
                            var pColorData = GCHandle.Alloc(pColorBuffer, GCHandleType.Pinned);
                            pColorFrame.CopyConvertedFrameDataToIntPtr(pColorData.AddrOfPinnedObject(), (uint)pColorBuffer.Length, ColorImageFormat.Rgba);
                            pColorData.Free();
                        }

                        // Get BodyIndex Frame Data.
                        if (pBodyIndexFrame != null)
                        {
                            var pBodyIndexData = GCHandle.Alloc(pBodyIndexBuffer, GCHandleType.Pinned);
                            pBodyIndexFrame.CopyFrameDataToIntPtr(pBodyIndexData.AddrOfPinnedObject(), (uint)pBodyIndexBuffer.Length);
                            pBodyIndexData.Free();
                        }
                    }
                }
            }

            ProcessFrame();
        }
        else
        {
            nullFrame = true;
        }
    }

    void OnApplicationQuit()
    {
        pDepthBuffer = null;
        pColorBuffer = null;
        pBodyIndexBuffer = null;

        if (m_pDepthCoordinates != null)
        {
            m_pDepthCoordinates = null;
        }

        if (m_pMultiSourceFrameReader != null)
        {
            m_pMultiSourceFrameReader.Dispose();
            m_pMultiSourceFrameReader = null;
        }

        if (m_pKinectSensor != null)
        {
            m_pKinectSensor.Close();
            m_pKinectSensor = null;
        }
    }
}