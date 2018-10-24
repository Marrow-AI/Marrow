using System;
using UnityEngine;


public class HxSimpleRotate : MonoBehaviour
{
      public Vector3 RotateSpeed;
      private void Update()
      {

         transform.Rotate(RotateSpeed * Time.deltaTime, Space.Self);
      }
}

