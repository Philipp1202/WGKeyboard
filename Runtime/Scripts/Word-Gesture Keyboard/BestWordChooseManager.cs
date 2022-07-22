using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WordGestureKeyboard {
    public class BestWordChooseManager : MonoBehaviour {

        public MaterialHolder materials;
        Material whiteMat;
        Material grayMat;

        // Start is called before the first frame update
        void Start() {
            Debug.Log("ERROR HERE?: " + gameObject.name);
            whiteMat = materials.whiteMat;
            grayMat = materials.grayMat;
        }

        /// <summary>
        /// Calls another function to swap the written word with the word written on the key to which this script is attached and vice versa.
        /// </summary>
        /// <param name="b">If true it calls a function and swaps the word of the text field with the word on the key to which this script is attached and changes the color, if false it only changes the color</param>
        public void ChooseWord(bool b) {
            if (b) {
                transform.parent.parent.Find("WGKeyboard").GetComponent<WGKMain>().ChangeWord(transform.GetChild(0).GetChild(0).GetComponent<Text>());
                transform.GetComponent<MeshRenderer>().material = grayMat;
            } else {
                transform.GetComponent<MeshRenderer>().material = whiteMat;
            }
        }
    }
}
