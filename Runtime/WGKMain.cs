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

public class WGKMain : MonoBehaviour {

    [Serializable]
    public class TextInputEvent : UnityEvent<string> {
    }

    public GameObject Key;
    public Material whiteMat;
    public Material grayMat;
    public TextInputEvent result;
    public string layout;
    public GameObject layoutKey;
    GameObject optionObjects;
    GameObject addNewWordKey;

    UserInputHandler UIH;
    GraphPointsCalculator GPC;
    FileHandler FH;


    BoxCollider boxCollider;
    Text text;
    LineRenderer LR;

    int pointCount = 0;
    Dictionary<string, List<Vector2>> normalizedWordsPointsDict;
    Dictionary<string, List<Vector2>> locationWordsPointsDict;
    Dictionary<string, List<Vector2>> seNormalizedWordsPointsDict;  // has only words in it considering starting and ending key positions
    Dictionary<string, List<Vector2>> seLocationWordsPointsDict;
    List<Vector2> normalizedPoints;
    List<Vector2> locationPoints;
    bool isWriting;
    Transform col = null;
    Dictionary<string, Vector2> letterPos;

    //List<string> keyboardSet;
    int pointCalls = 0;
    bool lastDistShort = false;
    bool lastAngleDistShort = false;
    float calcTime = 0;
    //bool sampleCalcReady = true;
    bool notEnded = false;
    bool isOptionsOpen = false;
    bool isAddingNewWord = false;
    bool isChoosingLayout = false;

    float startTime = 0;
    float keyRadius;
    float keyboardLength;
    float keyboardWidth;
    float deltaNormal;
    int numKeysOnLongestLine;
    float delta;

    float[] backSpaceHitbox = new float[4];
    float[] spaceHitbox = new float[4];

    //public bool[] modeArr = new bool[3];
    bool modeChangeOn = true;
    bool pressedEnter = true;

    // Start is called before the first frame update
    void Start() {

        startTime = Time.realtimeSinceStartup;

        boxCollider = transform.parent.GetComponent<BoxCollider>();
        text = transform.parent.GetChild(1).GetChild(0).GetComponent<Text>();
        //layoutKey = transform.parent.Find("Layouts").GetChild(0).gameObject;
        //layoutKey.SetActive(false);

        LR = GetComponent<LineRenderer>();
        LR.numCapVertices = 5;
        LR.numCornerVertices = 5;

        isWriting = false;

        if (layout == "") { // if user didn't specify another layout, the standard qwertz layout will be used
            layout = "qwertz";
        }
        //loadLayouts();
        //loadWordGraphs(layout);
        createKeyboardOverlay(layout);
        print("Time needed for startup: " + (Time.realtimeSinceStartup - startTime));
        optionObjects = transform.parent.Find("OptionObjects").gameObject;
        optionObjects.SetActive(false);
        addNewWordKey = transform.parent.Find("Add").gameObject;
        addNewWordKey.SetActive(false);

        UIH = new UserInputHandler(LR, this.transform);
        GPC = new GraphPointsCalculator(numKeysOnLongestLine);
    }

    // Update is called once per frame
    void Update() {
        if (isWriting) {
            if (!GPC.isSampling) {
                Vector3 hitPoint = UIH.getHitPoint(col.position, transform.forward);
                if (hitPoint != new Vector3(1000, 1000, 1000)) {
                    UIH.samplePoints(hitPoint);
                }
            }
        } else if (!GPC.isSampling && notEnded) {   // these two bools tell us, whether the calculation has ended and notEnded tells us, if the user input has been further processed
            startTime = Time.realtimeSinceStartup;
            List<Vector2> pointsList = UIH.getTransformedPoints();

            boxCollider.center = new Vector3(boxCollider.center.x, 0.03f, boxCollider.center.z);
            boxCollider.size = new Vector3(boxCollider.size.x, 0.05f, boxCollider.size.z);

            if (GPC.isBackSpaceOrSpace(pointsList, backSpaceHitbox, spaceHitbox) == -1) {
                text.text = text.text.Substring(0, text.text.Length - 1);   // maybe look here if a word or just a single letter was written before (to know whether to delete one letter or a whole word)
            } else if (GPC.isBackSpaceOrSpace(pointsList, backSpaceHitbox, spaceHitbox) == 1) {
                text.text += " ";
            } else {
                GPC.calcBestWords(pointsList, 20, FH.getLocationWordsPointsDict(), FH.getNormalizedWordsPointsDict(), delta);
            }
            print("TIME NEEDED: " + (Time.realtimeSinceStartup - startTime));
            pointCalls = 0;
            notEnded = false;
        } else if (GPC.sortedDict != null){
            if (isAddingNewWord) {  // putting text into textfield from keyboard
                text.text += GPC.sortedDict.Last().Key;
            } else {    // putting text into inputfield of query
                result.Invoke(GPC.sortedDict.Last().Key);
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
            } else {
                transform.parent.Find("OptionObjects").GetChild(2).GetComponent<MeshRenderer>().material = whiteMat;
                addNewWordKey.SetActive(false);
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
        createKeyboardOverlay(layout);
        FH.loadWordGraphs(layout);
    }

    public void drawWord(Transform t, bool b) {
        if (b) {
            isWriting = true;
            col = t;
            boxCollider.center = new Vector3(boxCollider.center.x, 0.01f, boxCollider.center.z);
            boxCollider.size = new Vector3(boxCollider.size.x, 0.001f, boxCollider.size.z);
        }
    }

    public void endWord(Transform t, bool b) {
        if (!b) {
            notEnded = true;
            isWriting = false;
        }
    }

    void createKeyboardOverlay(string layout) { // implementation does not work, if first row is not the longest (has most characters) (first.length > allOther.length)
        //keyboardSet = new List<String>();
        print("LAYOUT: " + layout);
        /*if (layout.Equals("qwertz")) {
            keyboardSet.Add("1234567890_-");
            keyboardSet.Add("qwertzuiopü");
            keyboardSet.Add("asdfghjklöä");
            keyboardSet.Add("yxcvbnm<");
            keyboardSet.Add(" ");
        } else if (layout.Equals("qwerty")) {
            keyboardSet.Add("1234567890_-");
            keyboardSet.Add("qwertyuiop");
            keyboardSet.Add("asdfghjkl");
            keyboardSet.Add("zxcvbnm<");
            keyboardSet.Add(" ");
        }*/
        List<String> keyList = FH.layoutKeys[layout];
        int count = keyList.Count;
        print(count);
        for (int i = 0; i < count; i++) {
            if (keyList[i].Length > numKeysOnLongestLine) {
                numKeysOnLongestLine = keyList[i].Length;
            }
            print(keyList[i]);
        }
        delta = 1 / numKeysOnLongestLine;
        keyRadius = 1 / numKeysOnLongestLine / 2;
        GPC.keyRadius = keyRadius;

        transform.localScale = new Vector3(0.05f * numKeysOnLongestLine, 0.05f * count, transform.localScale.z); // keyboard gets bigger if more keys on one line, but keys always have the same size
        keyboardLength = transform.localScale.x;
        keyboardWidth = transform.localScale.y;
        boxCollider.size = new Vector3(keyboardLength, 0.05f, keyboardWidth);
        //boxCollider.center = new Vector3(boxCollider.center.x, 0.03f, boxCollider.center.z);

        int y = count - 1;
        foreach (string s in keyList) {
            float o = 0;
            int x = 0;
            if (y == 3) {   // change, is wrong
                o = keyRadius * keyboardLength;
            } else if (y == 2) {
                o = 1.5f * keyRadius * keyboardLength;
            } else if (y == 1) {
                o = 2.5f * keyRadius * keyboardLength;
            } else if (y == 0) {
                o = 11 * keyRadius * keyboardLength;
            }

            if (count == 4) {   // keyboard without numbers in top row
                o -= keyRadius * keyboardLength;
            }


            foreach (var letter in s) {
                GameObject specificKey = Instantiate(Key) as GameObject;
                int scale = 1;
                if (letter.ToString() == "<") {
                    scale = 2;
                    o += keyRadius * keyboardLength;
                    float xPos = x / numKeysOnLongestLine + keyRadius;
                    float yPos = y / numKeysOnLongestLine + keyRadius;
                    backSpaceHitbox[0] = xPos - scale * keyRadius + o / keyboardLength;
                    backSpaceHitbox[1] = xPos + scale * keyRadius + o / keyboardLength;
                    backSpaceHitbox[2] = yPos - keyRadius;
                    backSpaceHitbox[3] = yPos + keyRadius;
                } else if (letter.ToString() == " ") {
                    scale = 8;
                    float xPos = x / numKeysOnLongestLine + keyRadius;
                    float yPos = y / numKeysOnLongestLine + keyRadius;
                    spaceHitbox[0] = xPos - scale * keyRadius + o / keyboardLength;
                    spaceHitbox[1] = xPos + scale * keyRadius + o / keyboardLength;
                    spaceHitbox[2] = yPos - keyRadius;
                    spaceHitbox[3] = yPos + keyRadius;
                }
                specificKey.transform.position = new Vector3(transform.position.x + (-keyboardLength / numKeysOnLongestLine) * ((numKeysOnLongestLine) / 2 - x) + o + keyRadius * keyboardLength, transform.position.y + 0.005f, transform.position.z - (-keyboardWidth / count) * (-count / 2.0f + y + 1) - keyRadius * keyboardLength);
                specificKey.transform.Find("Canvas").Find("Text").GetComponent<Text>().text = letter.ToString();
                specificKey.transform.localScale = new Vector3(keyboardLength / (numKeysOnLongestLine + 1.5f) * scale, keyboardLength / (numKeysOnLongestLine + 1.5f), specificKey.transform.localScale.z);

                // for next 4 lines: didn't have a better idea but to make roation of transform.parent object to 0,0,0 (if it's not 0,0,0, then something strange happens, when setting this.transform as parent
                Quaternion tempRot = transform.parent.localRotation;
                transform.parent.localRotation = new Quaternion(0, 0, 0, 0);
                specificKey.transform.SetParent(this.transform);
                transform.parent.localRotation = tempRot;

                x += 1;
            }
            y -= 1;
        }
    }

    public void addNewWordToDict(Transform t, bool b) {
        if (b) {
            this.transform.parent.GetChild(5).GetComponent<MeshRenderer>().material = grayMat;
            string newWord = text.text; // maybe needs to be changed, but maybe let it be with Text Object for adding a word -> wouldn't interfere with input in query
            FH.addNewWordToDict(newWord, GPC);
            text.text = "";
        } else {
            this.transform.parent.GetChild(5).GetComponent<MeshRenderer>().material = whiteMat;
        }
    }
}