using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WordGestureKeyboard {
    public class KeyManager : MonoBehaviour {

        public MaterialHolder materials;
        Material keyHoverMat;
        Material normalMat;


        // Start is called before the first frame update
        void Start()
        {
            keyHoverMat = materials.keyHoverMat;
            normalMat = materials.keyMat;
        }

        /// <summary>
        /// Changes the color of a key object.
        /// </summary>
        /// <param name="b">If true, changes color to a gray tone, otherwise if false, it changes the color to a brighter tone</param>
        public void IsHovered(bool b) {
            if (b) {
                transform.GetComponent<MeshRenderer>().material = keyHoverMat;
            } else {
                transform.GetComponent<MeshRenderer>().material = normalMat;
            }
        }

        /// <summary>
        /// Changes the color of a key object to its default color.
        /// </summary>
        public void SetColorDefault() {
            transform.GetComponent<MeshRenderer>().material = normalMat;
        }
    }
}