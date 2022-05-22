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

        public Text userInputText;  // remove, only for evaluation
        EvaluationManager evaluationManager; // remove, only for evaluation

        [Serializable]
        public class TextInputEvent : UnityEvent<string> {
        }

        public AudioSource wordInputSound;
        public GameObject Key;
        public Material whiteMat;
        public Material grayMat;
        public Material keyboardHoverMat;
        public Material keyboardMat;
        public GameObject layoutKey;
        public string layout;
        public TextInputEvent result;
        public GameObject optionObjects;
        public GameObject chooseObjects;
        public GameObject optionsKey;
        public GameObject addKey;
        public GameObject layoutsObjects;
        public GameObject scaleObjects;

        List<GameObject> subOptionButtons = new List<GameObject>();

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
        bool isChangeSizeOpen = false;

        string queryInput = "";
        string lastInputWord = "";

        float startTime = 0;

        // Start is called before the first frame update
        void Start() {
            evaluationManager = GameObject.Find("EvaluationPhraseHolder").GetComponent<EvaluationManager>();

            startTime = Time.realtimeSinceStartup;

            subOptionButtons.Add(optionObjects);
            subOptionButtons.Add(addKey);
            subOptionButtons.Add(layoutsObjects);
            subOptionButtons.Add(scaleObjects);

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
            FH.loadLayouts();
            KH = new KeyboardHelper(this.transform, Key, boxCollider, FH);
            KH.createKeyboardOverlay(layout);
            UIH = new UserInputHandler(LR, this.transform);
            GPC = new GraphPointsCalculator();
            
            //FH.addKeyboardLettersToLexicon(GPC);
            FH.loadWordGraphs(layout);
            KH.checkForSpaceAndBackspace(FH.getLayoutKeys(), layout);

            // maybe change next 4 lines
            //optionObjects = transform.parent.Find("OptionObjects").gameObject;
            //optionObjects.SetActive(false);
            //addNewWordKey = transform.parent.Find("Add").gameObject;
            //addNewWordKey.SetActive(false);

            //optionsKey = transform.parent.Find("Options");
            //optionObjects = transform.parent.Find("OptionObjects");
            //addKey = transform.parent.Find("Add");
            //layoutsObjects = transform.parent.Find("Layouts");

            updateOptionPositions();

            print("Time needed for startup: " + (Time.realtimeSinceStartup - startTime));

        }

        // Update is called once per frame
        /*
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
                    int count = lastInputWord.Length;
                    if (count != 0) {
                        for (int i = 0; i < count; i++) {
                            
                            text.text = text.text.Substring(0, text.text.Length - 1);
                            queryInput = queryInput.Substring(0, queryInput.Length - 1);
                            // TODO invoke backspace
                            if (queryInput == "") {
                                break;
                            }
                        }
                        lastInputWord = "";
                    } else {
                        text.text = text.text.Substring(0, text.text.Length - 1);
                        queryInput = queryInput.Substring(0, queryInput.Length - 1);
                        // TODO invoke backspace
                    }
                    for (int i = 0; i < 4; i++) {
                        chooseWord.transform.GetChild(i).gameObject.SetActive(false);
                    }
                    isLastSingleLetter = false;
                } else if (GPC.isBackSpaceOrSpace(pointsList, KH.backSpaceHitbox, KH.spaceHitbox) == 1) {
                    text.text += " ";
                    queryInput += " ";
                    // TODO: invoke inputtext with " "
                } else {
                    GPC.calcBestWords(pointsList, 20, FH.getLocationWordsPointsDict(), FH.getNormalizedWordsPointsDict(), KH.delta, KH.keyRadius);
                }
                print("TIME NEEDED: " + (Time.realtimeSinceStartup - startTime));
                notEnded = false;
            } else if (GPC.sortedDict != null) {
                for (int i = 0; i < 4; i++) {
                    chooseWord.transform.GetChild(i).gameObject.SetActive(false);
                }
                int bestWordsDictLength = GPC.sortedDict.Count;
                if (isAddingNewWord) {  // putting text into textfield from keyboard
                    text.text += GPC.sortedDict[bestWordsDictLength - 1];
                } else {    // putting text into inputfield of query
                    if (GPC.sortedDict[bestWordsDictLength - 1].Length == 1) { //single letter
                        if (isLastSingleLetter) {
                            result.Invoke(GPC.sortedDict[bestWordsDictLength - 1]);
                            text.text += GPC.sortedDict[bestWordsDictLength - 1];
                            queryInput += GPC.sortedDict[bestWordsDictLength - 1];
                        } else {
                            result.Invoke(" " + GPC.sortedDict[bestWordsDictLength - 1]);
                            text.text += " " + GPC.sortedDict[bestWordsDictLength - 1];
                            queryInput += " " + GPC.sortedDict[bestWordsDictLength - 1];
                        }
                        isLastSingleLetter = true;
                        lastInputWord = GPC.sortedDict[bestWordsDictLength - 1];
                    } else {    // not single letter but word
                        for (int i = 0; i < bestWordsDictLength - 1; i++) {
                            if (i > 3) {    // maximum of 4 best words apart from top word
                                break;
                            }
                            chooseWord.transform.GetChild(i).GetChild(0).GetChild(0).GetComponent<Text>().text = GPC.sortedDict[bestWordsDictLength - 2 - i];
                            chooseWord.transform.GetChild(i).gameObject.SetActive(true);
                        }
                        if (queryInput != "") { // not first word
                            result.Invoke(" ");
                            text.text += " ";
                            queryInput += " ";
                        }
                        result.Invoke(GPC.sortedDict[bestWordsDictLength - 1]);
                        text.text += GPC.sortedDict[bestWordsDictLength - 1];
                        queryInput += GPC.sortedDict[bestWordsDictLength - 1];
                        lastInputWord = GPC.sortedDict[bestWordsDictLength - 1];
                        isLastSingleLetter = false;
                    }
                }
                GPC.sortedDict = null;
            }
        }*/

        void Update() {
            if (isWriting) {
                if (!UIH.getIsSampling()) {
                    Vector3 hitPoint = UIH.getHitPoint(col.position, transform.forward);
                    //print(hitPoint);
                    if (hitPoint != new Vector3(1000, 1000, 1000)) {
                        UIH.samplePoints(hitPoint);
                    }
                }
            } else if (!UIH.getIsSampling() && notEnded) {   // these two bools tell us, whether the calculation has ended and notEnded tells us, if the user input has been further processed
                startTime = Time.realtimeSinceStartup;
                List<Vector2> pointsList = UIH.getTransformedPoints();

                if (pointsList.Count != 0) {    // user pressed trigger button, but somehow it didn^t register any points
                    boxCollider.center = new Vector3(boxCollider.center.x, 0.03f, boxCollider.center.z);
                    boxCollider.size = new Vector3(boxCollider.size.x, 0.05f, boxCollider.size.z);

                    if (GPC.isBackSpaceOrSpace(pointsList, KH.backSpaceHitbox, KH.spaceHitbox) == -1) {
                        wordInputSound.Play();
                        int count = lastInputWord.Length;
                        if (count > 1) {
                            for (int i = 0; i <= count; i++) {
                                text.text = text.text.Substring(0, text.text.Length - 1);
                                queryInput = queryInput.Substring(0, queryInput.Length - 1);

                                userInputText.text = userInputText.text.Substring(0, userInputText.text.Length - 1);
                                // TODO invoke backspace
                                if (queryInput == "") {
                                    break;
                                }
                            }
                            lastInputWord = "";
                        } else {
                            text.text = text.text.Substring(0, text.text.Length - 1);
                            queryInput = queryInput.Substring(0, queryInput.Length - 1);

                            userInputText.text = userInputText.text.Substring(0, userInputText.text.Length - 1);
                            // TODO invoke backspace
                        }
                        evaluationManager.nrBackspaces += 1;
                        for (int i = 0; i < 4; i++) {
                            chooseObjects.transform.GetChild(i).gameObject.SetActive(false);
                        }
                    } else if (GPC.isBackSpaceOrSpace(pointsList, KH.backSpaceHitbox, KH.spaceHitbox) == 1) {
                        wordInputSound.Play();
                        text.text += " ";
                        queryInput += " ";

                        userInputText.text += " ";
                        // TODO: invoke inputtext with " "
                    } else {
                        GPC.calcBestWords(pointsList, 20, FH.getLocationWordsPointsDict(), FH.getNormalizedWordsPointsDict(), KH.delta, KH.keyRadius);
                    }
                    //print("TIME NEEDED: " + (Time.realtimeSinceStartup - startTime));
                    notEnded = false;
                }
            } else if (GPC.sortedDict != null) {
                for (int i = 0; i < 4; i++) {
                    chooseObjects.transform.GetChild(i).gameObject.SetActive(false);
                }
                int bestWordsDictLength = GPC.sortedDict.Count;
                if (bestWordsDictLength != 0) {
                    wordInputSound.Play();
                    //print("THIS WAS THE MOST PROBABLE WORD: " + GPC.sortedDict[bestWordsDictLength - 1] +" YES IT IS INDEED");
                    if (isAddingNewWord) {  // putting text into textfield from keyboard
                        text.text += GPC.sortedDict[bestWordsDictLength - 1];
                        queryInput += GPC.sortedDict[bestWordsDictLength - 1];

                        userInputText.text += GPC.sortedDict[bestWordsDictLength - 1];
                    } else {    // putting text into inputfield of query
                        if (GPC.sortedDict[bestWordsDictLength - 1].Length == 1) { //single letter

                            result.Invoke(GPC.sortedDict[bestWordsDictLength - 1]);
                            text.text += GPC.sortedDict[bestWordsDictLength - 1];
                            queryInput += GPC.sortedDict[bestWordsDictLength - 1];
                            lastInputWord = GPC.sortedDict[bestWordsDictLength - 1];

                            userInputText.text += GPC.sortedDict[bestWordsDictLength - 1];
                        } else {    // not single letter but word
                            for (int i = 0; i < bestWordsDictLength - 1; i++) {
                                if (i > 3) {    // maximum of 4 best words apart from top word
                                    break;
                                }
                                chooseObjects.transform.GetChild(i).GetChild(0).GetChild(0).GetComponent<Text>().text = GPC.sortedDict[bestWordsDictLength - 2 - i];
                                chooseObjects.transform.GetChild(i).gameObject.SetActive(true);
                            }

                            result.Invoke(GPC.sortedDict[bestWordsDictLength - 1] + " ");
                            text.text += GPC.sortedDict[bestWordsDictLength - 1] + " ";
                            queryInput += GPC.sortedDict[bestWordsDictLength - 1] + " ";
                            lastInputWord = GPC.sortedDict[bestWordsDictLength - 1];

                            userInputText.text += GPC.sortedDict[bestWordsDictLength - 1] + " ";
                        }
                    }
                } else {
                    UnityEngine.Debug.Log("no word found");
                }
                GPC.sortedDict = null;
            }
        }
        
        void updateOptionPositions() {
            optionsKey.transform.localPosition = new Vector3(0.5f * KH.keyboardLength + optionsKey.transform.localScale.x * 0.5f + 0.005f, 0, transform.localScale.y * 0.5f - optionsKey.transform.localScale.z * 0.5f);
            optionObjects.transform.localPosition = new Vector3(0.5f * KH.keyboardLength + optionObjects.transform.localScale.x * 0.5f + 0.005f, optionObjects.transform.localPosition.y, optionsKey.transform.localPosition.z + (MathF.Sin((90 - 360 + optionObjects.transform.localEulerAngles.x) * Mathf.PI / 180)) * (optionObjects.transform.localScale.z * 3));//optionObjects.transform.localPosition.z + transform.localScale.y * 0.5f - optionsKey.transform.localScale.z * 0.5f);
            //print("beta: " + (MathF.Sin((90 - 360 + optionObjects.transform.localEulerAngles.x) * Mathf.PI / 180)) + " term " + (optionObjects.transform.localScale.z * 3));
            addKey.transform.localPosition = new Vector3(0.5f * KH.keyboardLength + addKey.transform.localScale.x * 0.5f + 0.005f, 0, -transform.localScale.y * 0.5f + addKey.transform.localScale.z);
            //layoutsObjects.transform.localPosition = new Vector3(0.5f * KH.keyboardLength + layoutsObjects.transform.localScale.x * 0.5f + 0.0075f + optionObjects.transform.localScale.x, optionObjects.transform.localPosition.y, optionObjects.transform.localPosition.z);
            chooseObjects.transform.localPosition = new Vector3(0, chooseObjects.transform.localPosition.y, transform.localScale.y * 0.5f + 0.035f);
            //scaleObjects.transform.localPosition = new Vector3(0.5f * KH.keyboardLength + layoutsObjects.transform.localScale.x * 0.5f + 0.0075f + optionObjects.transform.localScale.x, scaleObjects.transform.localPosition.y, scaleObjects.transform.localPosition.z);
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
                    lastInputWord = ""; // for now, maybe changes, if everything with the query inputfield works, and text field is for addWordMode only
                    text.text = "";

                    userInputText.text = "";
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
            this.layout = layout;
            FH.layout = layout;
            KH.createKeyboardOverlay(layout);
            FH.loadWordGraphs(layout);
            KH.checkForSpaceAndBackspace(FH.getLayoutKeys(), layout);
            updateOptionPositions();
        }

        public void drawWord(Transform t, bool b) {
            if (b) {
                isWriting = true;
                col = t;
                boxCollider.center = new Vector3(boxCollider.center.x, 0.01f, boxCollider.center.z);
                boxCollider.size = new Vector3(boxCollider.size.x, 0.001f, boxCollider.size.z);
                foreach (Transform child in this.transform) {
                    child.GetComponent<BoxCollider>().isTrigger = true;
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
                    child.GetComponent<BoxCollider>().isTrigger = false;
                    //child.GetComponent<BoxCollider>().enabled = false;
                    //child.GetComponent<KeyManager>().setColorDefault();
                }
            }
        }

        public void addNewWordToDict(Transform t, bool b) {
            if (b) {
                addKey.GetComponent<MeshRenderer>().material = grayMat;
                string newWord = text.text; // maybe needs to be changed, but maybe let it be with Text Object for adding a word -> wouldn't interfere with input in query
                FH.addNewWordToDict(newWord, GPC);
                text.text = "";

                userInputText.text = "";
            } else {
                addKey.GetComponent<MeshRenderer>().material = whiteMat;
            }
        }

        public void hoverKeyboard(bool b) {
            if (b) {
                transform.GetComponent<MeshRenderer>().material = keyboardHoverMat;
            } else if (!b && !isWriting) {  // don't want to make keyboard write when interacting with in in sense of writing on it
                transform.GetComponent<MeshRenderer>().material = keyboardMat;
            }
        }

        public void changeWord(Text t, string word) {
            wordInputSound.Play();
            int count = lastInputWord.Length;
            for (int i = 0; i <= count; i++) {
                text.text = text.text.Substring(0, text.text.Length - 1);
                userInputText.text = userInputText.text.Substring(0, userInputText.text.Length - 1);
                if (text.text == "") {
                    break;
                }
            }
            t.text = lastInputWord;

            result.Invoke(word + " ");
            text.text += word + " ";
            queryInput += word + " ";
            lastInputWord = word;

            userInputText.text += word + " ";
        }
    }
}