using System.Collections;
using Nexus.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus
{
    public class Spinner : MonoBehaviour
    {
        public Vector3 degreesPerSecond = new Vector3(0f, 30f, 0f);

        void Update()
        {
            transform.Rotate(degreesPerSecond * Time.deltaTime, Space.Self);
        }
    }
}
