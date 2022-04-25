using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WordGestureKeyboard {
    public class layoutKeyScript : MonoBehaviour {

        public Material whiteMat;
        public Material grayMat;
        // Start is called before the first frame update
        void Start() {

        }

        // Update is called once per frame
        void Update() {

        }

        public void chooseLayout(Transform t, bool b) {
            if (b) {
                transform.GetComponent<MeshRenderer>().material = grayMat;
                string layout = transform.GetChild(0).GetChild(0).GetComponent<Text>().text;
                transform.parent.parent.parent.Find("WGKeyboard").GetComponent<WGKMain>().changeLayout(layout);
            } else {
                transform.GetComponent<MeshRenderer>().material = whiteMat;
            }
        }
    }
}