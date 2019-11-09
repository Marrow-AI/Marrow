using UnityEngine;
using System.Collections;
using Windows.Kinect;

public class CoordinateMapperView : MonoBehaviour
{
	public GameObject CoordinateMapperManager;

    private CoordinateMapperManager _CoordinateMapperManager;

	private ComputeBuffer depthBuffer;
	private ComputeBuffer bodyIndexBuffer;

	DepthSpacePoint[] depthPoints;
	byte[] bodyIndexPoints;
	
	void Start ()
	{
		ReleaseBuffers ();
		
		if (CoordinateMapperManager == null)
		{
			return;
		}
		
		_CoordinateMapperManager = CoordinateMapperManager.GetComponent<CoordinateMapperManager>();
		
		Texture2D renderTexture = _CoordinateMapperManager.GetColorTexture();
		if (renderTexture != null)
		{
			gameObject.GetComponent<Renderer>().material.SetTexture("_MainTex", renderTexture);
		}

		depthPoints = _CoordinateMapperManager.GetDepthCoordinates ();
		if (depthPoints != null)
		{
			depthBuffer = new ComputeBuffer(depthPoints.Length, sizeof(float) * 2);
			gameObject.GetComponent<Renderer>().material.SetBuffer("depthCoordinates", depthBuffer);
		}

		bodyIndexPoints = _CoordinateMapperManager.GetBodyIndexBuffer ();
		if (bodyIndexPoints != null)
		{
			bodyIndexBuffer = new ComputeBuffer(bodyIndexPoints.Length, sizeof(float));
			gameObject.GetComponent<Renderer>().material.SetBuffer ("bodyIndexBuffer", bodyIndexBuffer);
		}
	}

	void Update()
	{
		//TODO: fix perf on this call.
		depthBuffer.SetData(depthPoints);

		// ComputeBuffers do not accept bytes, so we need to convert to float.
		float[] buffer = new float[512 * 424];
		for (int i = 0; i < bodyIndexPoints.Length; i++)
		{
			buffer[i] = (float)bodyIndexPoints[i];
		}
		bodyIndexBuffer.SetData(buffer);
		buffer = null;
	}
	
	private void ReleaseBuffers() 
	{
		if (depthBuffer != null) depthBuffer.Release();
		depthBuffer = null;

		if (bodyIndexBuffer != null) bodyIndexBuffer.Release();
		bodyIndexBuffer = null;

		depthPoints = null;
		bodyIndexPoints = null;
	}
	
	void OnDisable() 
	{
		ReleaseBuffers ();
	}
}
