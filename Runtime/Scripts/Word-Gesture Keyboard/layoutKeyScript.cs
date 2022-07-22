using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WordGestureKeyboard {
    public class LayoutKeyScript : MonoBehaviour {

        public MaterialHolder materials;
        Material whiteMat;
        Material grayMat;

        // Start is called before the first frame update
        void Start() {
            whiteMat = materials.whiteMat;
            grayMat = materials.grayMat;
        }

        /// <summary>
        /// It takes the text written on the key to which this script is attached and calls another function that changes the layout to the text written on the key.
        /// </summary>
        /// <param name="t">Transfrom (not further needed)</param>
        /// <param name="b">If true it changes the "change layout" button's color to gray and calls another function to change the layout, if false it changes the the "change layout" button's color to white</param>
        public void ChooseLayout(Transform t, bool b) {
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