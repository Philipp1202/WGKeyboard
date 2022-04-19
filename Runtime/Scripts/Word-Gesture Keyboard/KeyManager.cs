using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WordGestureKeyboard {
    public class KeyManager : MonoBehaviour {

        public Material keyHoverMat;
        public Material normalMat;
        /*
        Transform parent;
        string letter;
        Text text;
        bool hoverEnter = true;
        public Material whiteMat;
        public Material grayMat;

        // Start is called before the first frame update
        void Start()
        {
            parent = transform.parent;
            //text = parent.parent.GetChild(1).GetChild(0).GetComponent<Text>();
            letter = this.transform.GetChild(0).GetChild(0).GetComponent<Text>().text;
        }

        // Update is called once per frame
        void Update()
        {
        
        }

        public void writeLetter() {
            if (parent.GetComponent<WGKTest>().modeArr[1] || parent.GetComponent<WGKTest>().modeArr[2]) {
                if (hoverEnter) {
                    parent.GetComponent<WGKTest>().writeWord(letter);
                    this.GetComponent<MeshRenderer>().material = grayMat;
                } else {
                    this.GetComponent<MeshRenderer>().material = whiteMat;
                }
                hoverEnter = !hoverEnter;
            }
        }*/

        public void IsHovered(bool b) {
            if (b) {
                transform.GetComponent<MeshRenderer>().material = keyHoverMat;
            } else {
                transform.GetComponent<MeshRenderer>().material = normalMat;
            }
        }

        public void setColorDefault() {
            transform.GetComponent<MeshRenderer>().material = normalMat;
        }
    }
}