using UnityEngine;
using System.Collections;
using Windows.Kinect;

public class KinectManager : MonoBehaviour
{
    private KinectSensor _Sensor;
    private MultiSourceFrameReader _Reader;
    private Body[] _BodyData = null;
    private ushort[] _DepthData = null;
    private byte[] _BodyIndexData = null;
    private Windows.Kinect.Vector4 floorClipPlane;

    public bool isNewFrame { get; private set; }

    public KinectSensor GetSensor()
    {
        return _Sensor;
    }

    public Windows.Kinect.Vector4 GetFlootClipPlane()
    {
        return floorClipPlane;
    }

    public Body[] GetBodyData()
    {
        return _BodyData;
    }

    public ushort[] GetDepthData()
    {
        return _DepthData;
    }

    public byte[] GetBodyIndexData()
    {
        return _BodyIndexData;
    }

    void Awake()
    {
        _Sensor = KinectSensor.GetDefault();
        isNewFrame = false;

        if (_Sensor != null)
        {
            _Reader = _Sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Body | FrameSourceTypes.BodyIndex | FrameSourceTypes.Depth);

            if (!_Sensor.IsOpen)
            {
                _Sensor.Open();
            }
        }
    }
    void Update()
    {
        if (_Reader != null)
        {
            var frame = _Reader.AcquireLatestFrame();
            if (frame != null)
            {
                isNewFrame = true;

                // Update body data
                var bodyFrame = frame.BodyFrameReference.AcquireFrame();
                if (bodyFrame != null)
                {
                    floorClipPlane = bodyFrame.FloorClipPlane;
                    if (_BodyData == null)
                    {
                        _BodyData = new Body[_Sensor.BodyFrameSource.BodyCount];
                    }
                    bodyFrame.GetAndRefreshBodyData(_BodyData);
                    bodyFrame.Dispose();
                    bodyFrame = null;
                }

                // Update depth data
                var depthFrame = frame.DepthFrameReference.AcquireFrame();
                if (depthFrame != null)
                {
                    if (_DepthData == null)
                    {
                        _DepthData = new ushort[_Sensor.DepthFrameSource.FrameDescription.LengthInPixels];
                    }
                    depthFrame.CopyFrameDataToArray(_DepthData);
                    depthFrame.Dispose();
                    depthFrame = null;
                }

                // Update body index data
                var bodyIndexFrame = frame.BodyIndexFrameReference.AcquireFrame();
                if (bodyIndexFrame != null)
                {
                    if (_BodyIndexData == null)
                    {
                        _BodyIndexData = new byte[_Sensor.BodyIndexFrameSource.FrameDescription.LengthInPixels];
                    }
                    bodyIndexFrame.CopyFrameDataToArray(_BodyIndexData);
                    bodyIndexFrame.Dispose();
                    bodyIndexFrame = null;
                }
                frame = null;
            }
            else
                isNewFrame = false;
        }
    }

    void OnApplicationQuit()
    {
        if (_Reader != null)
        {
            _Reader.Dispose();
            _Reader = null;
        }

        if (_Sensor != null)
        {
            if (_Sensor.IsOpen)
            {
                _Sensor.Close();
            }

            _Sensor = null;
        }
    }
}