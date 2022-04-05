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

public class WGKTest : MonoBehaviour {

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
    List<string> layouts;

    List<string> keyboardSet;
    int pointCalls = 0;
    bool lastDistShort = false;
    bool lastAngleDistShort = false;
    float calcTime = 0;
    bool sampleCalcReady = true;
    bool notEnded = false;
    bool isOptionsOpen = false;
    bool isAddingNewWord = false;

    float startTime = 0;
    float keyRadius;
    float keyboardLength;
    float keyboardWidth;
    float deltaNormal;
    float numKeysOnLongestLine;
    float delta;

    float[] backSpaceHitbox = new float[4];
    float[] spaceHitbox = new float[4];

    //public bool[] modeArr = new bool[3];
    bool modeChangeOn = true;
    bool pressedEnter = true;

    // Start is called before the first frame update
    void Start()
    {   

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
        layouts = new List<string>();
        loadLayouts();
        loadWordGraphs(layout);
        createKeyboardOverlay(layout);
        print("Time needed for startup: " + (Time.realtimeSinceStartup - startTime));
        optionObjects = transform.parent.Find("OptionObjects").gameObject;
        optionObjects.SetActive(false);
        addNewWordKey = transform.parent.Find("Add").gameObject;
        addNewWordKey.SetActive(false);

    }

    // Update is called once per frame
    void Update()
    {
        if (isWriting) {
            if (sampleCalcReady) {
                int layerMask = LayerMask.GetMask("WGKeyboard");
                RaycastHit hit;
                Vector3 hitPoint = new Vector3(0, 0, 0);
                if (Physics.Raycast(col.position, transform.forward, out hit, Mathf.Infinity, layerMask)) {
                    hitPoint = hit.point;
                    samplePoints(hitPoint);
                } else if (Physics.Raycast(col.position, -transform.forward, out hit, Mathf.Infinity, layerMask)) {
                    hitPoint = hit.point;
                    samplePoints(hitPoint);
                }
            }
        } else if (sampleCalcReady && notEnded) {   // these two bools tell us, whether the calculation has ended and notEnded tells us, if the user input has been further processed
            startTime = Time.realtimeSinceStartup;
            List<Vector2> pointsList = new List<Vector2>();
            Vector3 point;

            for (int i = 0; i < LR.positionCount; i++) {
                point = LR.GetPosition(i);
                Vector3 localTransformedPoint = transform.parent.InverseTransformPoint(point);
                pointsList.Add(new Vector2((localTransformedPoint[0] + keyboardLength / 2) / keyboardLength, (localTransformedPoint[2] + keyboardWidth / 2) / keyboardLength)); // adding magnitudes, such that lower left corner of "coordinate system" is at (0/0) and not middle point at (0/0)
            }
            print("POINTCOUNT: " + pointCount + " VS  POINTCALLS: " + pointCalls);
            pointCount = 0;
            LR.positionCount = 0;
            lastDistShort = false;

            boxCollider.center = new Vector3(boxCollider.center.x, 0.03f, boxCollider.center.z);
            boxCollider.size = new Vector3(boxCollider.size.x, 0.05f, boxCollider.size.z);

            if (isBackSpaceOrSpace(pointsList) == -1) {
                text.text = text.text.Substring(0, text.text.Length - 1);   // maybe look here if a word or just a single letter was written before (to know whether to delete one letter or a whole word)
            } else if (isBackSpaceOrSpace(pointsList) == 1) {
                text.text += " ";
            } else {
                calcBestWords(pointsList, 20);
            }
            print("TIME NEEDED: " + (Time.realtimeSinceStartup - startTime));
            pointCalls = 0;
            notEnded = false;
        }
    }

    void loadLayouts() {
        string path = "Packages/com.unibas.wgkeyboard/Assets/layouts.txt";
        StreamReader sr = new StreamReader(path);
        string line;
        while (true) {
            line = sr.ReadLine();
            if (line == null) { // end of file reached
                break;
            }
            layouts.Add(line);
        }
    }

    // loads the sokgraphs that have been generated for a certain keyboard layout in a file called "sokgraph_'layout'.txt"
    void loadWordGraphs(string layout) {
        locationWordsPointsDict = new Dictionary<string, List<Vector2>>();
        normalizedWordsPointsDict = new Dictionary<string, List<Vector2>>();

        string path = "Packages/com.unibas.wgkeyboard/Assets/sokgraph_" + layout + ".txt";

        StreamReader sr = new StreamReader(path);
        string line;
        string[] splits = new string[3];
        string[] points;
        List<Vector2> locationPoints = new List<Vector2>();
        List<Vector2> normalizedPoints = new List<Vector2>();
        while (true) {
            line = sr.ReadLine();
            if (line == null) { // end of file reached
                break;
            }
            splits = line.Split(":");
            points = splits[1].Split(","); // not normalized points
            int n = 0;
            float v1 = 0;
            float d;
            foreach (string p in points) {
                float.TryParse(p, out d);
                if ((n % 2) == 0) {
                    v1 = d;
                } else {
                    locationPoints.Add(new Vector2(v1, d));
                }
                n += 1;
            }

            points = splits[2].Split(",");  // normalized points
            n = 0;
            foreach (string p in points) {
                float.TryParse(p, out d);
                if ((n % 2) == 0) {
                    v1 = d;
                } else {
                    normalizedPoints.Add(new Vector2(v1, d));
                }
                n += 1;
            }

            locationWordsPointsDict.Add(splits[0], locationPoints);
            normalizedWordsPointsDict.Add(splits[0], normalizedPoints);

            normalizedPoints = new List<Vector2>();
            locationPoints = new List<Vector2>();
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
        for (int i = 0; i < layouts.Count; i++) {
            GameObject Key = Instantiate(layoutKey) as GameObject;
            Key.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = layouts[i];
            Key.transform.localPosition = new Vector3(Key.transform.localPosition.x, Key.transform.localPosition.y + Key.transform.localScale.y * i, Key.transform.localPosition.z);
            Key.transform.SetParent(transform.parent.Find("Layouts"));
            Key.SetActive(true);
        }
    }

    public void chooseLayout(Transform t, bool b) {
        if (b) {
            string layout = t.GetChild(0).GetChild(0).GetComponent<Text>().text;
            foreach (Transform child in this.transform) {
                GameObject.Destroy(child.gameObject);
            }
            changeLayout(layout);
        }
    }

    void changeLayout(string layout) {
        createKeyboardOverlay(layout);
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
        keyboardSet = new List<String>();
        if (layout.Equals("qwertz")) {
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
        }

        for (int i = 0; i < keyboardSet.Count; i++) {
            if (keyboardSet[i].Length > numKeysOnLongestLine) {
                numKeysOnLongestLine = keyboardSet[i].Length;
            }
        }
        delta = 1 / numKeysOnLongestLine;
        keyRadius = 1 / numKeysOnLongestLine / 2;

        transform.localScale = new Vector3(0.05f * numKeysOnLongestLine, 0.05f * keyboardSet.Count, transform.localScale.z); // keyboard gets bigger if more keys on one line, but keys always have the same size
        keyboardLength = transform.localScale.x;
        keyboardWidth = transform.localScale.y;
        boxCollider.size = new Vector3(keyboardLength, 0.05f, keyboardWidth);

        int y = keyboardSet.Count - 1;
        foreach (string s in keyboardSet) {
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

            if (keyboardSet.Count == 4) {   // keyboard without numbers in top row
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
                specificKey.transform.position = new Vector3(transform.position.x + (-keyboardLength / numKeysOnLongestLine)*((numKeysOnLongestLine)/2-x) + o + keyRadius * keyboardLength, transform.position.y + 0.005f, transform.position.z - (-keyboardWidth/keyboardSet.Count)*(-keyboardSet.Count/2.0f + y + 1) - keyRadius * keyboardLength);
                specificKey.transform.Find("Canvas").Find("Text").GetComponent<Text>().text = letter.ToString();
                specificKey.transform.localScale = new Vector3(keyboardLength / (numKeysOnLongestLine+1.5f) * scale, keyboardLength / (numKeysOnLongestLine + 1.5f), specificKey.transform.localScale.z);
                specificKey.transform.SetParent(this.transform);

                x += 1;
            }
            y -= 1;
        }
    }

    async void samplePoints(Vector3 hitPoint) { // I worked with async, because FPS dropped from 90 to (worst case observed) around 40. Can't use Linerenderer functions in async Task.Run(), therefore worked with some "unnecessary" variables
        sampleCalcReady = false;
        int posCount = LR.positionCount;
        bool setPoint = false;
        Vector3 startPoint = new Vector3(0,0,0);
        Vector3 middlePoint = new Vector3(0,0,0);
        if (posCount > 2) {
            startPoint = LR.GetPosition(pointCount - 2);
            middlePoint = LR.GetPosition(pointCount - 1);
        }
        await Task.Run(() => {
            float minAngle = 10;
            float minSegmentDist = 0.015f;
            if (hitPoint != new Vector3(0, 0)) { // check if point needs to get set or if it can be ignored
                pointCalls++;
                if (pointCount < 3) {
                    pointCount++;
                    setPoint = true;
                } else {
                    Vector3 lastPoint;
                    if (Vector3.Angle(middlePoint - startPoint, hitPoint - middlePoint) < minAngle) { // set Point if almost a straight line
                        if (lastDistShort) {
                            setPoint = true;
                            lastPoint = startPoint;
                        } else {
                            pointCount++;
                            setPoint = true;
                            lastPoint = middlePoint;
                        }
                        if ((hitPoint - lastPoint).magnitude < minSegmentDist) {
                            lastDistShort = true;
                        } else {
                            lastDistShort = false;
                        }
                    } else {
                        if (lastDistShort) {
                            setPoint = true;
                            lastPoint = startPoint;
                        } else {
                            pointCount++;
                            setPoint = true;
                            lastPoint = middlePoint;
                        }
                        if ((hitPoint - lastPoint).magnitude < minSegmentDist / 5) {
                            lastDistShort = true;
                        } else {
                            lastDistShort = false;
                        }
                    }
                }
            }
        });
        if (setPoint) {
            LR.positionCount = pointCount;
            //print("HERE MIGHT BE AN ERROR: " + LR.positionCount + " , " + pointCount);
            LR.SetPosition(pointCount - 1, hitPoint);
            
        }
        setPoint = false;
        sampleCalcReady = true;

    }

    async void calcBestWords(List<Vector2> userInputPoints, int steps) {
        IOrderedEnumerable<KeyValuePair<string, float>> sortedDict = null;
        await Task.Run(() => {    // need await, because I couln't access the text of Text in task.run(() => {});
            List<Vector2> inputPoints = getWordGraphStepPoint(userInputPoints, steps);
            for (int i = 0; i < inputPoints.Count; i++)
            {
 //               print(inputPoints[i]);
            }
            List<Vector2> normalizedInputPoints = normalize(inputPoints, 2);

            Dictionary<string, List<Vector2>>[] filteredWordsPoints = seKeyPosition(inputPoints, locationWordsPointsDict, normalizedWordsPointsDict, steps);
            Dictionary<string, float> normalizedCostList = normalizedPointsCost(filteredWordsPoints[1], normalizedInputPoints, steps);
            Dictionary<string, float> locationCostList = locationCosts(filteredWordsPoints[0], inputPoints, steps);

            List<string> wordList = new List<string>();
            foreach (var word in normalizedCostList) {   // look which word had a good cost in shape and location
                if (locationCostList.ContainsKey(word.Key)) {
                    if (isSingleLetter(userInputPoints)) {
                        if (word.Key.Length == 1) {
                            wordList.Add(word.Key);
                        }
                    } else {
                        wordList.Add(word.Key);
                    }
                }
            }

            List<float> tempShapeCosts = new List<float>();
            List<float> tempLocationCosts = new List<float>();
            foreach (string word in wordList)
            {
                float shapeCost = normalizedCostList[word];
                float locationCost = locationCostList[word];
                float shapeProb = 1 / (delta * Mathf.Sqrt(2 * Mathf.PI)) * Mathf.Exp((float)(-0.5 * Mathf.Pow(shapeCost / delta, 2)));
                float locationProb = 1 / (delta * Mathf.Sqrt(2 * Mathf.PI)) * Mathf.Exp((float)(-0.5 * Mathf.Pow(locationCost / delta, 2)));
                ///print("LOCCOST: " + locationCost);
                ///print("LOCPROB: " + locationProb);
                tempShapeCosts.Add(shapeProb);
                tempLocationCosts.Add(locationProb);
            }

            float sum = 0;
            float sum2 = 0;
            int numWords = wordList.Count;
            for (int i = 0; i < numWords; i++)
            {
                sum += tempShapeCosts[i];
                ///print("sum1= " + sum);
                sum2 += tempLocationCosts[i];
                ///print("sum2= " + sum2);
            }
            for (int i = 0; i < numWords; i++)
            {
                tempShapeCosts[i] /= sum;
                tempLocationCosts[i] /= sum2;
            }

            List<float> tempCosts2 = new List<float>();
            sum = 0;
            for (int i = 0; i < numWords; i++)
            {
                sum += tempShapeCosts[i] * tempLocationCosts[i];
                ///print("sum3= " + sum);
            }
            for (int i = 0; i < numWords; i++)
            {
                tempCosts2.Add(tempShapeCosts[i] * tempLocationCosts[i] / sum);
            }

            Dictionary<string, float> finalCosts = new Dictionary<string, float>();
            int q = 0;
            foreach (string word in wordList)
            {
                finalCosts.Add(word, tempCosts2[q]);
                q += 1;
            }

            sortedDict = from entry in finalCosts orderby entry.Value ascending select entry;

            foreach (var word in sortedDict)
            {
                print("RESULT: " + word.Key + " : " + word.Value);
            }
            print(sortedDict.Last().Key);
        });

        print(sortedDict.Last().Key);

        if (isAddingNewWord) {  // putting text into textfield from keyboard
            text.text += sortedDict.Last().Key;
        } else {    // putting text into inputfield of query
            result.Invoke(sortedDict.Last().Key);
            text.text += sortedDict.Last().Key; // remove
        }
    }

    public void writeWord(string word) {
        /*if (modeArr[0]) {
            // word = whole word calculated from gesture
        } else {
            // single letter
            text.text += word;
            print("DO I GET HERE?");
            result.Invoke(word);
        }*/
    }

    public void addNewWordToDict(Transform t, bool b) {
        if (b) {
            this.transform.parent.GetChild(5).GetComponent<MeshRenderer>().material = grayMat;
            string newWord = text.text; // maybe needs to be changed, but maybe let it be with Text Object for adding a word -> wouldn't interfere with input in query
            if (newWord.Length != 0) {
                string path = "Packages/com.unibas.wgkeyboard/Assets/10000_english_words.txt";
                StreamReader sr = new StreamReader(path);
                bool isIn = false;
                string line;
                while (true) {
                    line = sr.ReadLine();
                    if (line == newWord) {  //word already in lexicon
                        print("WORD ALREADY IN DICT");
                        isIn = true;
                        break;
                    }
                    if (line == null) {
                        break;
                    }
                }
                sr.Close();
                if (!isIn) {
                    StreamWriter sw = File.AppendText(path);
                    sw.WriteLine(newWord);
                    sw.Close();

                    List<Vector2> wordLocationPoints = getWordPoints(newWord);
                    List<Vector2> wordNormPoints = normalize(wordLocationPoints, 2);

                    path = "Packages/com.unibas.wgkeyboard/Assets/sokgraph_qwertz.txt";
                    sw = File.AppendText(path);
                    string newLine = newWord + ":";
                    for (int i = 0; i < wordLocationPoints.Count; i++) {
                        newLine += wordLocationPoints[i].x.ToString() + "," + wordLocationPoints[i].y.ToString();
                        if (i < wordLocationPoints.Count - 1) {
                            newLine += ",";
                        }
                    }
                    newLine += ":";
                    for (int i = 0; i < wordNormPoints.Count; i++) {
                        newLine += wordNormPoints[i].x.ToString() + "," + wordNormPoints[i].y.ToString();
                        if (i < wordLocationPoints.Count - 1) {
                            newLine += ",";
                        }
                    }
                    sw.WriteLine(newLine);
                    sw.Close();

                    normalizedWordsPointsDict.Add(newWord, wordNormPoints);
                    locationWordsPointsDict.Add(newWord, wordLocationPoints);
                }
                text.text = "";
            }
        }
        else {
            this.transform.parent.GetChild(5).GetComponent<MeshRenderer>().material = whiteMat;
        }

        pressedEnter = !pressedEnter;

        // Calculate graph of new word and add it to the dict/list
    }

    Dictionary<string, List<Vector2>>[] seKeyPosition(List<Vector2> input, Dictionary<string, List<Vector2>> locWordsPoints, Dictionary<string, List<Vector2>> normWordsPoints, int steps)
    {
        Dictionary<string, List<Vector2>> newLocWordsPoints = new Dictionary<string, List<Vector2>>();
        Dictionary<string, List<Vector2>> newNormWordsPoints = new Dictionary<string, List<Vector2>>();

        foreach (var word in locWordsPoints) {
            if ((input[0] - word.Value[0]).magnitude < keyRadius * 1.5f && (input[steps - 1] - word.Value[steps - 1]).magnitude < keyRadius * 1.5f) {
                newLocWordsPoints.Add(word.Key, word.Value);
                newNormWordsPoints.Add(word.Key, normWordsPoints[word.Key]);
            }
            if (word.Key == "police" || word.Key == "point")
            {
                print(word.Key + ": " + (input[0] - word.Value[0]).magnitude + " : " + (input[steps - 1] - word.Value[steps - 1]).magnitude + " :: " + keyRadius * 1.5f);
            }
        }
        //print("LENGTH: " + newLocWordsPoints.Count);
        return new Dictionary<string, List<Vector2>>[] { newLocWordsPoints, newNormWordsPoints };
    }

    Dictionary<string, float> locationCosts(Dictionary<string, List<Vector2>> locationWordsPointDict, List<Vector2> inputPoints, int steps) {
        Dictionary<string, float> costList = new Dictionary<string, float>();
        float cost;
        float d;
        float d2;
        
        float[] arrD = new float[steps];

        foreach (var word in locationWordsPointDict) {
            cost = 0;
            d = 0;
            d2 = 0;
            // !!!!!alpha to determine!!!!!
            int j;
            foreach (Vector2 p in word.Value) {
                for (j = 0; j < steps; j++) {
                    arrD[j] = (p - inputPoints[j]).magnitude;
                }
                d += Mathf.Max(arrD.Min() - keyRadius, 0);
                //print(word.Key + ": D = " + arrD.Min() + " - " + keyRadius);
            }

            
            foreach (Vector2 p in inputPoints) {
                j = 0;
                foreach (Vector2 wordPoint in word.Value) {
                    arrD[j] = (p - wordPoint).magnitude;
                    j += 1;
                }
                d2 += Mathf.Max(arrD.Min() - keyRadius, 0);
            }

            if (d == 0 && d2 == 0) {
                cost = 0;
            } else {
                int k = 0;
                foreach (Vector2 p in word.Value) {
                    cost += (p - inputPoints[k]).magnitude;
                    if (word.Key == "point") {
                        //print("locpointcost inside: " + cost + " iteration: " + k);
                    } 
                    k += 1;
                }
            }

            if (word.Key == "point") {
                print("locpointcost: " + cost);
            }
            //print("COOOOOOOOOST: " + word.Key + " : " + cost);
            if (cost < 2) {
                costList.Add(word.Key, cost);
//                print("COOOOOOOOOST: " + word.Key + " : " + cost);
            }
        }
        return costList;
    }

    Dictionary<string, float> normalizedPointsCost(Dictionary<string, List<Vector2>> normalizedWordsPointDict, List<Vector2> normalizedInputPoints, int steps) {
        Dictionary<string, float> normalizedCostList = new Dictionary<string, float>();
        int n;
        float cost;
        foreach (var word in normalizedWordsPointDict) {
            n = 0;
            cost = 0;
            foreach (Vector2 p in word.Value) {
                cost += (p - normalizedInputPoints[n]).magnitude;
                //print("NORMCOST: " + normalizedInputPoints[n] + " : " + p + "  ::  " + (p - normalizedInputPoints[n]).magnitude);
                n += 1;
            }
            cost /= steps;

            //print("COSTS: " + word.Key + " : " + cost);
            if (word.Key == "point") {
                print("normpointcost: " + cost + " : " + (deltaNormal));
            }
            if (cost < keyRadius * deltaNormal) {
                normalizedCostList.Add(word.Key, cost);
                ///print("NORMCOST: " + word.Key + " : " + cost);
            }
            
        }

        return normalizedCostList;
    }

    List<Vector2> getWordGraphStepPoint(List<Vector2> points, int steps) {
//        print("HOW MANY POITNS? " + points.Count);
        double length = getLengthByPoints(points);
        List<Vector2> stepPoints = new List<Vector2>();
        Vector2 currPos = points[0];

        if (length == 0) {    
            for (int i = 0; i < steps; i++) {
                stepPoints.Add(currPos);
            }
            return stepPoints;
        }
        
        double stepSize = length / (steps - 1);
        List<Vector2> distVecs = new List<Vector2>();
        for (int i = 0; i < points.Count -1; i++) {
            distVecs.Add(points[i+1] - points[i]);
        }
        int numSteps = 1;
        double currStep = stepSize;
        int currPosNum = 0;
        int currDistVecNum = 0;
        
        stepPoints.Add(currPos);
        
        while (numSteps < steps) {
            //if (currDistVecNum >= distVecs.Count) {
            //    print("ERROR IN DISTVECS: WANT: " + currDistVecNum + " : HAVE: " + distVecs.Count + " : steps: " + numSteps + " : NEEDED DIST: " + currStep);
            //}
            if (currDistVecNum >= distVecs.Count) {
                stepPoints.Add(currPos);  // adding the last point "manually", because sometimes it can happen (because the numbers can get very small), that there are some rounding errors and it would want to go on the next distance vector, that doesn't exist. The distance it wold have to go further is about 1*e^(-8), which can be ignored.
                numSteps += 1;
            } else {
                Vector2 distVec = distVecs[currDistVecNum];
                double distVecLength = Mathf.Sqrt(Mathf.Pow(distVec[0], 2) + Mathf.Pow(distVec[1], 2));
                if (currStep != stepSize) {
                    if (distVecLength - currStep > -0.0000001) {
                        numSteps += 1;
                        currPos = currPos + distVec / (float)distVecLength * (float)currStep;
                        distVecs[currDistVecNum] = points[currPosNum + 1] - currPos;
                        stepPoints.Add(currPos);
                        currStep = stepSize;
                    } else {
                        currStep -= distVecLength;
                        currDistVecNum += 1;
                        currPosNum += 1;
                        currPos = points[currPosNum];
                    }
                } else if ((int)(distVecLength / stepSize + 0.0000001) > 0) {
                    int numPointsOnLine = (int)(distVecLength / stepSize + 0.0000001);
                    numSteps += numPointsOnLine;
                    for (int i = 0; i < numPointsOnLine; i++) {
                        stepPoints.Add(currPos + (i + 1) * (distVec / (float)distVecLength * (float)stepSize));
                    }

                    if (distVecLength - numPointsOnLine * stepSize > 0.0000001) {
                        currStep -= (distVecLength - numPointsOnLine * stepSize);
                    }

                    currDistVecNum += 1;
                    currPosNum += 1;
                    currPos = points[currPosNum];
                } else {
                    currStep -= distVecLength;
                    currDistVecNum += 1;
                    currPosNum += 1;
                    currPos = points[currPosNum];
                }
            }
        }
        
        return stepPoints;
    }

    double getLengthByPoints(List<Vector2> points) {
        double dist = 0;
        Vector2 distVec;
        for (int i = 0; i < points.Count - 1; i++) {
            distVec = points[i] - points[i+1];
            dist += Mathf.Sqrt(Mathf.Pow(distVec[0], 2) + Mathf.Pow(distVec[1], 2));
        }
        return dist;
    }
    
    List<Vector2> getWordPoints(string word) {
        Dictionary<string, Vector2> letterPos = new Dictionary<string, Vector2>();
        for (int y = 0; y < keyboardSet.Count; y++) {
            float o = 0;
            if (y == 3) {   // change, is wrong
                o = 0.5f;
            } else if (y == 2) {
                o = 0.75f;
            } else if (y == 1) {
                o = 1.25f;
            } else if (y == 0) {
                o = 5.25f;
            }

            if (keyboardSet.Count == 4) {   // keyboard without numbers in top row
                o -= 0.5f;
            }

            for (int x = 0; x < keyboardSet[keyboardSet.Count - y - 1].Count(); x++) {
                letterPos.Add(keyboardSet[keyboardSet.Count - y - 1][x].ToString(), new Vector2((x + o + 0.5f) / numKeysOnLongestLine, (y + 0.5f) / numKeysOnLongestLine)); // position of all letters if one key had radius 1
                print(keyboardSet[keyboardSet.Count - y - 1][x].ToString() + " : " + new Vector2((x + o + 0.5f) / numKeysOnLongestLine, (y + 0.5f) / numKeysOnLongestLine));
            }
        }

        List<Vector2> points = new List<Vector2>();
        foreach (var letter in word) {
            points.Add(letterPos[letter.ToString()]);
        }

        List<Vector2> locationPoints = getWordGraphStepPoint(points, 20);
        return locationPoints;
    }

    List<Vector2> normalize(List<Vector2> letterPoints, int length) {
        List<float> x = getX(letterPoints);
        List<float> y = getY(letterPoints);
        
        float minx = x.Min();
        float maxx = x.Max();
        float miny = y.Min();
        float maxy = y.Max();

        float[] boundingBox = {minx, maxx, miny, maxy};
        float[] boundingBoxSize = {maxx - minx, maxy - miny};
        
        float s;
        if (Mathf.Max(boundingBoxSize[0], boundingBoxSize[1]) != 0) {
            s = length / Mathf.Max(boundingBoxSize[0], boundingBoxSize[1]);
//            print("S = " + s);
        }
        else {
            s = 1;
        }
        deltaNormal = s;
        
        Vector2 middlePoint = new Vector2((boundingBox[0] + boundingBox[1]) / 2, (boundingBox[2] + boundingBox[3]) / 2);
        
        List<Vector2> newPoints = new List<Vector2>();
        foreach (var point in letterPoints) {
            newPoints.Add((point - middlePoint) * s);
        }
        return newPoints;
    }

    bool isSingleLetter(List<Vector2> letterPoints) {
        List<float> x = getX(letterPoints);
        List<float> y = getY(letterPoints);

        float minx = x.Min();
        float maxx = x.Max();
        float miny = y.Min();
        float maxy = y.Max();

        //float[] boundingBox = { minx, maxx, miny, maxy };
        //float[] boundingBoxSize = { maxx - minx, maxy - miny };

        if (maxx - minx > keyRadius || maxy - miny > keyRadius) {
            print("MAX: " + (keyRadius));
            print("IS" + (maxx - minx));
            return false;
        }
        return true;
    }

    // return -1 if it is a backSpace, 1 for space and 0 if it's neither of them
    int isBackSpaceOrSpace(List<Vector2> letterPoints) {
        List<float> x = getX(letterPoints);
        List<float> y = getY(letterPoints);

        float minx = x.Min();
        float maxx = x.Max();
        float miny = y.Min();
        float maxy = y.Max();

        if (backSpaceHitbox[0] < minx && backSpaceHitbox[1] > maxx && backSpaceHitbox[2] < miny && backSpaceHitbox[3] > maxy) {
            return -1;
        }
        if (spaceHitbox[0] < minx && spaceHitbox[1] > maxx && spaceHitbox[2] < miny && spaceHitbox[3] > maxy){
            return 1;
        }
        return 0;
    }

    List<float> getX(List<Vector2> wordPoints) {
        List<float> xPoints = new List<float>();
        for (int i = 0; i < wordPoints.Count; i++) {
            xPoints.Add(wordPoints[i][0]);
        }
        return xPoints;
    }

    List<float> getY(List<Vector2> wordPoints) {
        List<float> yPoints = new List<float>();
        for (int i = 0; i < wordPoints.Count; i++) {
            yPoints.Add(wordPoints[i][1]);
        }
        return yPoints;
    }
}
//980