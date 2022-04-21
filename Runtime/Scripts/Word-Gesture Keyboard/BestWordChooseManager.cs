using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WordGestureKeyboard {
    public class BestWordChooseManager : MonoBehaviour {
        public Material whiteMat;
        public Material grayMat;

        public void chooseWord(bool b) {
            if (b) {
                transform.parent.parent.Find("WGKeyboard").GetComponent<WGKMain>().changeWord(transform.GetChild(0).GetChild(0).GetComponent<Text>(), transform.GetChild(0).GetChild(0).GetComponent<Text>().text);
                transform.GetComponent<MeshRenderer>().material = grayMat;
            } else if (!b) {
                transform.GetComponent<MeshRenderer>().material = whiteMat;
            }
            //swapWord(word);
        }
        /*
        public void swapWord(string word) {
            transform.GetChild(0).GetChild(0).GetComponent<Text>().text = word;
        }*/
    }
}
