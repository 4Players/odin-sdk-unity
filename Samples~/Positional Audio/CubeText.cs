using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OdinNative.Unity.Samples
{
    /// <summary>
    /// Will move the gameobject to look towards the camera.
    /// </summary>
    [ExecuteInEditMode]
    public class CubeText : MonoBehaviour
    {
        void Start()
        {
            gameObject.transform.rotation = Camera.main.transform.rotation;
        }
    }
}
