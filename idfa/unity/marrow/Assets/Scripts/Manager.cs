using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Marrow
{
    public class Manager<T> : MonoBehaviour
    where T : MonoBehaviour
    {
        private static T _Instance;
        public static T Instance
        {
            get
            {
                if (_Instance == null) _Instance = GameObject.FindObjectOfType<T>();
                return _Instance;
            }
        }
    }
}
