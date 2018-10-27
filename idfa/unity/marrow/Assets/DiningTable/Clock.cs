using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Clock : MonoBehaviour
{

    //public Transform hour;
    //public Transform minute;
    //public Transform second;

	public TMPro.TextMeshPro timeTitle;

    private float hourAngle;
    private float minuteAngle;
    private float secondAngle;

    private float offset = -90f;

    void Start()
    {
        hourAngle = 360f / 12f;
        minuteAngle = 360f / 60f;
        secondAngle = 360f / 60f;
    }

    void Update()
    {
        System.DateTime currTime = System.DateTime.Now;
        //int _h = currTime.Hour;
        //int _m = currTime.Minute;
        //int _s = currTime.Second;

		//hour.localEulerAngles = Vector3.right * (offset - _h * hourAngle);
		//minute.localEulerAngles = Vector3.right * (offset - _m * minuteAngle);
		//second.localEulerAngles = Vector3.right * (offset - _s * secondAngle);

		timeTitle.text = currTime.ToString("HH") + ":" + currTime.ToString("mm") + "\n" + currTime.ToString("MMM") + " " + currTime.ToString("dd");
    }
}
