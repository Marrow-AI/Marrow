using UnityEngine;
using System.Collections;
using Windows.Kinect;
using System;


public class BodyIndexSourceManager : MonoBehaviour
{

    /// <summary>
    /// Collection of colors to be used to display the BodyIndexFrame data.
    /// </summary>
    private static readonly Color[] BodyColor =
        {
            Color.red,
            Color.grey,
            Color.blue,
            Color.yellow,
            Color.white,
            Color.magenta,
        };

    /// <summary>
    /// Active Kinect sensor
    /// </summary>
    private KinectSensor kinectSensor = null;

    /// <summary>
    /// Reader for body index frames
    /// </summary>
    private BodyIndexFrameReader bodyIndexFrameReader = null;

    private int BodyFrameWidth;
    private Texture2D _Texture;
    private byte[] _Data;

    public Texture2D GetBodyIndexTexture()
    {
        return _Texture;
    }

    void Start()
    {
        kinectSensor = KinectSensor.GetDefault();

        if (kinectSensor != null)
        {
            bodyIndexFrameReader = kinectSensor.BodyIndexFrameSource.OpenReader();

            var frameDesc = kinectSensor.BodyIndexFrameSource.FrameDescription;
            BodyFrameWidth = frameDesc.Width;
            //Debug.Log("frameDesc.BytesPerPixel = " + frameDesc.BytesPerPixel + " frameDesc.LengthInPixels = " + frameDesc.LengthInPixels);
            //Debug.Log("frameDesc.Width = " + frameDesc.Width + " frameDesc.Height = " + frameDesc.Height);

            //frameDesc.Width = 512 , frameDesc.Height = 424
            _Texture = new Texture2D(frameDesc.Width, frameDesc.Height, TextureFormat.RGBA32, false);
            _Data = new byte[frameDesc.BytesPerPixel * frameDesc.LengthInPixels];
            if (!kinectSensor.IsOpen)
            {
                kinectSensor.Open();
            }
        }
    }

    void Update()
    {
        if (bodyIndexFrameReader != null)
        {
            var frame = bodyIndexFrameReader.AcquireLatestFrame();
            if (frame != null)
            {
                frame.CopyFrameDataToArray(_Data);
                SetTextureFromData();
                _Texture.Apply();
                frame.Dispose();
                frame = null;
            }
        }
    }
    void SetTextureFromData()
    {
        for (int i = 0; i < _Data.Length; i++)
        {
            if (_Data[i] < BodyColor.Length)
            {
                try
                {
                    _Texture.SetPixel(i % BodyFrameWidth, i / BodyFrameWidth, BodyColor[_Data[i]]);
                }
                catch
                {
                    break;
                }
            }
            else
            {
                _Texture.SetPixel(i % BodyFrameWidth, i / BodyFrameWidth, Color.clear);
            }
        }
    }
}