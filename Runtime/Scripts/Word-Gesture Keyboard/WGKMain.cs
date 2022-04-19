using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

using UnityEngine.UI;
using System.IO;
using System;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;

namespace WordGestureKeyboard {
    public class WGKMain : MonoBehaviour {

        [Serializable]
        public class TextInputEvent : UnityEvent<string> {
        }

        public GameObject Key;
        public Material whiteMat;
        public Material grayMat;
        public Material keyboardHoverMat;
        public GameObject layoutKey;
        public string layout;
        public TextInputEvent result;

        GameObject optionObjects;
        GameObject addNewWordKey;

        BoxCollider boxCollider;
        Text text;
        LineRenderer LR;

        UserInputHandler UIH;
        GraphPointsCalculator GPC;
        FileHandler FH;
        KeyboardHelper KH;

        bool isWriting;
        Transform col = null;

        bool notEnded = false;
        bool isOptionsOpen = false;
        bool isAddingNewWord = false;
        bool isChoosingLayout = false;
        bool isLastSingleLetter = false;

        string queryInput;

        float startTime = 0;

        // Start is called before the first frame update
        void Start() {

            startTime = Time.realtimeSinceStartup;

            boxCollider = transform.parent.GetComponent<BoxCollider>();
            text = transform.parent.GetChild(1).GetChild(0).GetComponent<Text>();
            LR = GetComponent<LineRenderer>();
            LR.numCapVertices = 5;
            LR.numCornerVertices = 5;

            isWriting = false;

            if (layout == "") { // if user didn't specify another layout, the standard qwertz layout will be used
                layout = "qwertz";
            }
            //loadLayouts();
            //loadWordGraphs(layout);
            FH = new FileHandler(layout);
            KH = new KeyboardHelper(this.transform, Key, boxCollider, FH);
            KH.createKeyboardOverlay(layout);
            UIH = new UserInputHandler(LR, this.transform);
            GPC = new GraphPointsCalculator(KH.numKeysOnLongestLine);

            // maybe change next 4 lines
            optionObjects = transform.parent.Find("OptionObjects").gameObject;
            optionObjects.SetActive(false);
            addNewWordKey = transform.parent.Find("Add").gameObject;
            addNewWordKey.SetActive(false);

            print("Time needed for startup: " + (Time.realtimeSinceStartup - startTime));

        }

        // Update is called once per frame
        void Update() {
            if (isWriting) {
                if (!UIH.getIsSampling()) {
                    Vector3 hitPoint = UIH.getHitPoint(col.position, transform.forward);
                    print(hitPoint);
                    if (hitPoint != new Vector3(1000, 1000, 1000)) {
                        UIH.samplePoints(hitPoint);
                    }
                }
            } else if (!UIH.getIsSampling() && notEnded) {   // these two bools tell us, whether the calculation has ended and notEnded tells us, if the user input has been further processed
                startTime = Time.realtimeSinceStartup;
                List<Vector2> pointsList = UIH.getTransformedPoints();

                boxCollider.center = new Vector3(boxCollider.center.x, 0.03f, boxCollider.center.z);
                boxCollider.size = new Vector3(boxCollider.size.x, 0.05f, boxCollider.size.z);

                if (GPC.isBackSpaceOrSpace(pointsList, KH.backSpaceHitbox, KH.spaceHitbox) == -1) {
                    text.text = text.text.Substring(0, text.text.Length - 1);   // maybe look here if a word or just a single letter was written before (to know whether to delete one letter or a whole word)
                    // invoke backspace
                } else if (GPC.isBackSpaceOrSpace(pointsList, KH.backSpaceHitbox, KH.spaceHitbox) == 1) {
                    text.text += " ";
                    // invoke inputtext with " "
                } else {
                    GPC.calcBestWords(pointsList, 20, FH.getLocationWordsPointsDict(), FH.getNormalizedWordsPointsDict(), KH.delta, KH.keyRadius);
                }
                print("TIME NEEDED: " + (Time.realtimeSinceStartup - startTime));
                notEnded = false;
            } else if (GPC.sortedDict != null) {
                if (isAddingNewWord) {  // putting text into textfield from keyboard
                    text.text += GPC.sortedDict.Last().Key;
                } else {    // putting text into inputfield of query
                    result.Invoke(GPC.sortedDict.Last().Key);
                    if (GPC.sortedDict.Last().Key.Length == 1) { //single letter
                        if (isLastSingleLetter) {
                            result.Invoke(GPC.sortedDict.Last().Key);
                        } else {
                            result.Invoke(" " + GPC.sortedDict.Last().Key);
                        }
                        isLastSingleLetter = true;
                    } else {
                        isLastSingleLetter = false;
                    }
                    
                    text.text = GPC.sortedDict.Last().Key; // remove
                }
                GPC.sortedDict = null;
            }
        }

        public void enterOptions(Transform t, bool b) {
            if (b) {
                if (!isOptionsOpen) {
                    transform.parent.Find("Options").GetComponent<MeshRenderer>().material = grayMat;
                    optionObjects.SetActive(true);
                } else {
                    transform.parent.Find("Options").GetComponent<MeshRenderer>().material = whiteMat;
                    optionObjects.SetActive(false);
                }
                isOptionsOpen = !isOptionsOpen;
            }
        }

        public void enterAddWordMode(Transform t, bool b) {
            if (b) {
                isAddingNewWord = !isAddingNewWord;
                if (isAddingNewWord) {
                    transform.parent.Find("OptionObjects").GetChild(2).GetComponent<MeshRenderer>().material = grayMat;
                    addNewWordKey.SetActive(true);
                    foreach (Transform child in this.transform) {
                        child.GetComponent<BoxCollider>().enabled = true;
                    }
                } else {
                    transform.parent.Find("OptionObjects").GetChild(2).GetComponent<MeshRenderer>().material = whiteMat;
                    addNewWordKey.SetActive(false);
                    foreach (Transform child in this.transform) {
                        child.GetComponent<BoxCollider>().enabled = false;
                    }
                }
            }
        }

        public void enterLayoutChoose(Transform t, bool b) {
            if (b) {
                isChoosingLayout = !isChoosingLayout;
                if (isChoosingLayout) {
                    transform.parent.Find("OptionObjects").GetChild(1).GetComponent<MeshRenderer>().material = grayMat;
                    for (int i = 0; i < FH.layouts.Count; i++) {
                        GameObject Key = Instantiate(layoutKey, transform.parent.Find("Layouts"), false) as GameObject;
                        Key.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = FH.layouts[i];
                        Key.transform.localPosition = new Vector3(Key.transform.localPosition.x, Key.transform.localPosition.y + Key.transform.localScale.y * 1.1f * i, Key.transform.localPosition.z);
                        //Key.transform.SetParent(transform.parent.Find("Layouts"));
                        //Key.transform.localScale = new Vector3(1.4f, 0.7f, 0.05f);
                        //Key.transform.localPosition = new Vector3(1.571f, 0.575f + i, 1.273f);
                        //Key.transform.localRotation = new Quaternion(0, 0, 0, 0);
                        Key.SetActive(true);
                    }
                } else {
                    transform.parent.Find("OptionObjects").GetChild(1).GetComponent<MeshRenderer>().material = whiteMat;
                    foreach (Transform child in transform.parent.Find("Layouts")) {
                        GameObject.Destroy(child.gameObject);
                    }
                }
            }
        }

        public void changeLayout(string layout) {
            foreach (Transform child in this.transform) {
                GameObject.Destroy(child.gameObject);
            }
            this.layout = layout;
            KH.createKeyboardOverlay(layout);
            FH.loadWordGraphs(layout);
        }

        public void drawWord(Transform t, bool b) {
            if (b) {
                isWriting = true;
                col = t;
                boxCollider.center = new Vector3(boxCollider.center.x, 0.01f, boxCollider.center.z);
                boxCollider.size = new Vector3(boxCollider.size.x, 0.001f, boxCollider.size.z);
                foreach (Transform child in this.transform) {
                    child.GetComponent<BoxCollider>().enabled = true;
                }
                transform.GetComponent<MeshRenderer>().material = keyboardHoverMat;
            }
        }

        public void endWord(Transform t, bool b) {
            if (!b) {
                notEnded = true;
                isWriting = false;
                foreach (Transform child in this.transform) {
                    child.GetComponent<BoxCollider>().enabled = false;
                    child.GetComponent<KeyManager>().setColorDefault();
                }
            }
        }

        public void addNewWordToDict(Transform t, bool b) {
            if (b) {
                this.transform.parent.GetChild(5).GetComponent<MeshRenderer>().material = grayMat;
                string newWord = text.text; // maybe needs to be changed, but maybe let it be with Text Object for adding a word -> wouldn't interfere with input in query
                FH.addNewWordToDict(newWord, GPC, KH.numKeysOnLongestLine);
                text.text = "";
            } else {
                this.transform.parent.GetChild(5).GetComponent<MeshRenderer>().material = whiteMat;
            }
        }

        public void hoverKeyboard(bool b) {
            if (b) {
                transform.GetComponent<MeshRenderer>().material = keyboardHoverMat;
            } else if (!b && !isWriting) {  // don't want to make keyboard write when interacting with in in sense of writing on it
                transform.GetComponent<MeshRenderer>().material = whiteMat;
            }
        }
    }
}