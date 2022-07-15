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

        [Serializable]
        public class WordDeleteEvent : UnityEvent {
        }

        public MaterialHolder materials;
        public AudioSource wordInputSound;
        public GameObject key;
        public GameObject layoutKey;
        public string startingLayout;
        public TextInputEvent result;
        public WordDeleteEvent deleteEvent;
        public GameObject optionObjects;
        public GameObject chooseObjects;
        public GameObject optionsKey;
        public GameObject addKey;
        public GameObject layoutsObjects;
        public GameObject scaleObjects;
        public GameObject previewWord;

        List<GameObject> subOptionButtons = new List<GameObject>();

        BoxCollider boxCollider;
        Text keyboardText;
        LineRenderer LR;

        UserInputHandler UIH;
        GraphPointsCalculator GPC;
        FileHandler FH;
        KeyboardHelper KH;

        Material whiteMat;
        Material grayMat;
        Material keyboardMat;

        bool isWriting = false;
        string lastInputWord = "";
        Transform controller = null;

        bool notEnded = false;
        bool isOptionsOpen = false;
        bool isAddingNewWord = false;
        bool isChoosingLayout = false;
        bool isChangeSizeOpen = false;

        float startTime = 0;

        // Start is called before the first frame update
        void Start() {
            startTime = Time.realtimeSinceStartup;

            whiteMat = materials.whiteMat;
            grayMat = materials.grayMat;
            keyboardMat = materials.keyboardMat;

            subOptionButtons.Add(optionObjects);
            subOptionButtons.Add(addKey);
            subOptionButtons.Add(layoutsObjects);
            subOptionButtons.Add(scaleObjects);

            boxCollider = transform.parent.GetComponent<BoxCollider>();
            keyboardText = transform.parent.Find("Canvas").GetChild(0).GetComponent<Text>();
            LR = GetComponent<LineRenderer>();
            LR.numCapVertices = 5;
            LR.numCornerVertices = 5;

            if (startingLayout == "") { // if user didn't specify another layout, the standard qwertz layout will be used
                startingLayout = "qwertz";
            }

            FH = new FileHandler(startingLayout);
            FH.LoadLayouts();
            KH = new KeyboardHelper(this.transform, key, boxCollider, FH);
            KH.createKeyboardOverlay(startingLayout);
            UIH = new UserInputHandler(LR, this.transform);
            GPC = new GraphPointsCalculator();
            
            FH.LoadWordGraphs(startingLayout);
            KH.MakeSpaceAndBackspaceHitbox(FH.GetLayoutCompositions()[startingLayout]);

            UpdateObjectPositions();

            print("Time needed for startup: " + (Time.realtimeSinceStartup - startTime));
        }

        void Update() {
            if (isWriting) {
                for (int i = 0; i < 4; i++) {
                    chooseObjects.transform.GetChild(i).gameObject.SetActive(false);
                }
                if (!UIH.GetIsSamplingPoints()) {
                    Vector3 hitPoint = UIH.GetHitPoint(controller.position);
                    //print(hitPoint);
                    if (UIH.checkPoint(hitPoint)) {
                        UIH.samplePoints(hitPoint);
                    }
                    if(!GPC.isCalculatingPreview) {
                        List<Vector2> pointsList = UIH.getTransformedPoints2();
                        UnityEngine.Debug.Log("pointslist: " + pointsList.Count);
                        if (pointsList.Count != 0) {
                            GPC.calcBestWords(pointsList, 20, FH.getLocationWordPointsDict(), FH.getNormalizedWordPointsDict(), KH.delta, KH.keyRadius, FH.wordRanking, true);
                        }
                    }
                    previewWord.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<Text>().text = GPC.bestWord;
                }
            } else if (!UIH.GetIsSamplingPoints() && notEnded) {   // these two bools tell us, whether the calculation has ended and notEnded tells us, if the user input has been further processed
                List<Vector2> pointsList = UIH.getTransformedPoints();

                if (pointsList.Count != 0) {    // user pressed trigger button, but somehow it didn^t register any points
                    if (GPC.isBackSpaceOrSpace(pointsList, KH.backSpaceHitbox, KH.spaceHitbox) == -1) {
                        wordInputSound.Play();
                        if (isAddingNewWord) {
                            if (keyboardText.text != "") {
                                keyboardText.text = keyboardText.text.Substring(0, keyboardText.text.Length - 1);
                            }
                        } else {
                            deleteEvent.Invoke();
                        }

                        for (int i = 0; i < 4; i++) {
                            chooseObjects.transform.GetChild(i).gameObject.SetActive(false);
                        }
                    } else if (GPC.isBackSpaceOrSpace(pointsList, KH.backSpaceHitbox, KH.spaceHitbox) == 1) {
                        wordInputSound.Play();
                        result.Invoke(" ");
                        for (int i = 0; i < 4; i++) {
                            chooseObjects.transform.GetChild(i).gameObject.SetActive(false);
                        }
                    } else {
                        GPC.calcBestWords(pointsList, 20, FH.getLocationWordPointsDict(), FH.getNormalizedWordPointsDict(), KH.delta, KH.keyRadius, FH.wordRanking, false);
                    }
                    notEnded = false;
                }
            } else if (GPC.sortedDict != null) {
                for (int i = 0; i < 4; i++) {
                    chooseObjects.transform.GetChild(i).gameObject.SetActive(false);
                }
                int bestWordsDictLength = GPC.sortedDict.Count;
                if (bestWordsDictLength != 0) {
                    wordInputSound.Play();
                    previewWord.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<Text>().text = GPC.sortedDict[0];
                    if (isAddingNewWord) {  // putting text into textfield from keyboard
                        keyboardText.text += GPC.sortedDict[0];
                    } else {    // putting text into inputfield of query
                        for (int i = 0; i < bestWordsDictLength - 1; i++) {
                            if (i > 3) {    // maximum of 4 best words apart from top word
                                break;
                            }
                            chooseObjects.transform.GetChild(i).GetChild(0).GetChild(0).GetComponent<Text>().text = GPC.sortedDict[i + 1];
                            chooseObjects.transform.GetChild(i).gameObject.SetActive(true);
                        }
                        lastInputWord = GPC.sortedDict[0];
                        result.Invoke(GPC.sortedDict[0]);
                    }
                } else {
                    UnityEngine.Debug.Log("no word found");
                }
                GPC.sortedDict = null;
            }
        }

        /// <summary>
        /// Updates the positions of all objects of the WGKeyboard (especially needed if layout changes)
        /// </summary>
        void UpdateObjectPositions() {
            optionsKey.transform.localPosition = new Vector3(0.5f * KH.keyboardLength + optionsKey.transform.localScale.x * 0.5f + 0.005f, 0, transform.localScale.y * 0.5f - optionsKey.transform.localScale.z * 0.5f);
            optionObjects.transform.localPosition = new Vector3(0.5f * KH.keyboardLength + optionObjects.transform.localScale.x * 0.5f + 0.005f, optionObjects.transform.localPosition.y, optionsKey.transform.localPosition.z + (MathF.Sin((90 - 360 + optionObjects.transform.localEulerAngles.x) * Mathf.PI / 180)) * (optionObjects.transform.localScale.z * 3));//optionObjects.transform.localPosition.z + transform.localScale.y * 0.5f - optionsKey.transform.localScale.z * 0.5f);
            addKey.transform.localPosition = new Vector3(0.5f * KH.keyboardLength + addKey.transform.localScale.x * 0.5f + 0.005f, 0, -transform.localScale.y * 0.5f + addKey.transform.localScale.z * 0.5f);
            previewWord.transform.localPosition = new Vector3(0, previewWord.transform.localPosition.y, transform.localScale.y * 0.5f + previewWord.transform.localScale.z * 0.5f * 1.1f + 0.001f);
            chooseObjects.transform.localPosition = new Vector3(0, chooseObjects.transform.localPosition.y, previewWord.transform.localPosition.z + previewWord.transform.localScale.z * 0.5f + chooseObjects.transform.localScale.y * 0.35f);
            UnityEngine.Debug.Log("addPos: " + addKey.transform.localPosition + ", scalePos: " + addKey.transform.localScale + ", optsPos: " + optionsKey.transform.localPosition + ", optsScale" + optionsKey.transform.localScale);
            if (((addKey.transform.localPosition + (addKey.transform.localScale * 0.5f)) - (optionsKey.transform.localPosition - (optionsKey.transform.localScale * 0.5f))).z >= 0) {
                addKey.transform.localPosition = new Vector3(addKey.transform.localPosition.x, addKey.transform.localPosition.y, optionsKey.transform.localPosition.z - (optionsKey.transform.localScale.z * 0.5f) - (addKey.transform.localScale.z * 0.5f) - 0.02f);
            }
        }

        /// <summary>
        /// Used if user interacts with keyboard with the correct controller button.
        /// If they press said button, the WGKeyboard is set in "drawing mode" which means, it modifies the hitbox of the WGKeyboard itself and activates the character keys' hitboxes.
        /// </summary>
        /// <param name="t">Transform that interacts with WGKeyboard's hitbox</param>
        /// <param name="b">Boolean that indicates if the controller button is pressed (true) or released (false)</param>
        public void DrawWord(Transform t, bool b) {
            if (b) {
                isWriting = true;
                controller = t;
                boxCollider.center = new Vector3(boxCollider.center.x, 0.007f, boxCollider.center.z);
                boxCollider.size = new Vector3(boxCollider.size.x, 0.001f, boxCollider.size.z);
                foreach (Transform child in this.transform) {
                    child.GetComponent<BoxCollider>().isTrigger = true;
                }
                transform.GetComponent<MeshRenderer>().material = whiteMat;
            } else {
                isWriting = false;
                notEnded = true;
                boxCollider.center = new Vector3(boxCollider.center.x, 0.03f, boxCollider.center.z);
                boxCollider.size = new Vector3(boxCollider.size.x, 0.05f, boxCollider.size.z);
                foreach (Transform child in this.transform) {
                    child.GetComponent<BoxCollider>().isTrigger = false;
                }
                Vector3 center = boxCollider.center;
                Vector3 size = boxCollider.size;
                Vector3 posOnCollider = boxCollider.transform.InverseTransformPoint(controller.position) - center;
                if (!(Mathf.Abs(posOnCollider.x) < size.x / 2 && Mathf.Abs(posOnCollider.y) < size.y / 2 && Mathf.Abs(posOnCollider.z) < size.z / 2)) {
                    transform.GetComponent<MeshRenderer>().material = keyboardMat;
                }
                print("POS ON COLLIDER: " + posOnCollider + " : " + size);
            }
        }

        public void scalePlus(Transform t, bool b) {
            if (b && transform.parent.localScale.x < 2) {
                scaleObjects.transform.GetChild(0).GetComponent<MeshRenderer>().material = grayMat;
                transform.parent.localScale += new Vector3(0.05f, 0.05f, 0.05f);
                KH.keyboardScale += 0.05f;
            } else {
                scaleObjects.transform.GetChild(0).GetComponent<MeshRenderer>().material = whiteMat;
            }
        }

        public void scaleMinus(Transform t, bool b) {
            if (b && transform.parent.localScale.x > 0.5) {
                scaleObjects.transform.GetChild(1).GetComponent<MeshRenderer>().material = grayMat;
                transform.parent.localScale -= new Vector3(0.05f, 0.05f, 0.05f);
                KH.keyboardScale -= 0.05f;
            } else {
                scaleObjects.transform.GetChild(1).GetComponent<MeshRenderer>().material = whiteMat;
            }
        }

        public void changeSize(Transform t, bool b) {
            if (b) {
                if (!isChangeSizeOpen) {
                    optionObjects.transform.GetChild(0).GetComponent<MeshRenderer>().material = grayMat;
                    scaleObjects.SetActive(true);
                } else {
                    optionObjects.transform.GetChild(0).GetComponent<MeshRenderer>().material = whiteMat;
                    scaleObjects.SetActive(false);
                }
                isChangeSizeOpen = !isChangeSizeOpen;
            }
        }

        public void enterOptions(Transform t, bool b) {
            if (b) {
                if (!isOptionsOpen) {
                    optionsKey.GetComponent<MeshRenderer>().material = grayMat;
                    optionObjects.SetActive(true);
                    for (int i = 0; i < 4; i++) {
                        chooseObjects.transform.GetChild(i).gameObject.SetActive(false);
                    }
                } else {
                    optionsKey.GetComponent<MeshRenderer>().material = whiteMat;
                    optionObjects.SetActive(false);
                    foreach (GameObject g in subOptionButtons) {
                        g.SetActive(false);
                    }
                    for (int i = 0; i < 3; i++) { 
                        optionObjects.transform.GetChild(i).GetComponent<MeshRenderer>().material = whiteMat;
                    }
                    isAddingNewWord = false;
                    isChoosingLayout = false;
                    isChangeSizeOpen = false;
                }
                isOptionsOpen = !isOptionsOpen;
            }
        }

        public void enterAddWordMode(Transform t, bool b) {
            if (b) {
                isAddingNewWord = !isAddingNewWord;
                if (isAddingNewWord) {
                    optionObjects.transform.GetChild(2).GetComponent<MeshRenderer>().material = grayMat;
                    addKey.SetActive(true);
                    foreach (Transform child in this.transform) {
                        child.GetComponent<BoxCollider>().enabled = true;
                    }
                    lastInputWord = "";
                    keyboardText.text = "";

                    for (int i = 0; i < 4; i++) {
                        chooseObjects.transform.GetChild(i).gameObject.SetActive(false);
                    }
                } else {
                    optionObjects.transform.GetChild(2).GetComponent<MeshRenderer>().material = whiteMat;
                    addKey.SetActive(false);
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
                    optionObjects.transform.GetChild(1).GetComponent<MeshRenderer>().material = grayMat;
                    for (int i = 0; i < FH.layouts.Count; i++) {
                        GameObject Key = Instantiate(layoutKey, layoutsObjects.transform, false) as GameObject;
                        Key.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = FH.layouts[i];
                        //Key.transform.localPosition = new Vector3(Key.transform.localPosition.x, Key.transform.localPosition.y + Key.transform.localScale.y * 1.1f * i, Key.transform.localPosition.z);
                        Key.transform.localPosition = new Vector3(0, 0, Key.transform.localScale.y * 1.1f * i);
                        //Key.transform.SetParent(transform.parent.Find("Layouts"));
                        //Key.transform.localScale = new Vector3(1.4f, 0.7f, 0.05f);
                        //Key.transform.localPosition = new Vector3(1.571f, 0.575f + i, 1.273f);
                        //Key.transform.localRotation = new Quaternion(0, 0, 0, 0);
                        Key.SetActive(true);
                    }
                    layoutsObjects.SetActive(true);
                } else {
                    optionObjects.transform.GetChild(1).GetComponent<MeshRenderer>().material = whiteMat;
                    foreach (Transform child in layoutsObjects.transform) {
                        GameObject.Destroy(child.gameObject);
                    }
                    layoutsObjects.SetActive(false);
                }
            }
        }

        public void changeLayout(string layout) {
            foreach (Transform child in this.transform) {
                GameObject.Destroy(child.gameObject);
            }
            startingLayout = layout;
            FH.layout = layout;
            KH.createKeyboardOverlay(layout);
            FH.LoadWordGraphs(layout);
            //delayActivateLayoutButtons();
            KH.MakeSpaceAndBackspaceHitbox(FH.GetLayoutCompositions()[layout]);
            UpdateObjectPositions();
        }

        public void addNewWordToDict(Transform t, bool b) {
            if (b) {
                addKey.GetComponent<MeshRenderer>().material = grayMat;
                string newWord = keyboardText.text; // maybe needs to be changed, but maybe let it be with Text Object for adding a word -> wouldn't interfere with input in query
                FH.addNewWordToDict(newWord, GPC);
                keyboardText.text = "";
            } else {
                addKey.GetComponent<MeshRenderer>().material = whiteMat;
            }
        }

        public void hoverKeyboard(bool b) {
            if (b) {
                transform.GetComponent<MeshRenderer>().material = whiteMat;
            } else if (!b && !isWriting) {  // don't want to make keyboard write when interacting with in in sense of writing on it
                transform.GetComponent<MeshRenderer>().material = keyboardMat;
            }
        }

        public void changeWord(Text t, string word) {
            wordInputSound.Play();

            t.text = lastInputWord;
            lastInputWord = word;

            deleteEvent.Invoke();
            result.Invoke(word);
        }

        async public void delayActivateLayoutButtons() {
            layoutsObjects.SetActive(false);
            while (FH.isLoading) {
                await Task.Delay(1);
            }
            layoutsObjects.SetActive(true);
        }
    }
}