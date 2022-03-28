using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UI;
using System.IO;
using System;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;

public class WGKTest : MonoBehaviour {

    public GameObject Key;
    public Material whiteMat;
    public Material grayMat;
   
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

    LineRenderer LRDebug;

    BoxCollider boxCollider;
    Text text;
    List<string> keyboardSet;

    float startTime = 0;
    float keyRadius;
    float keyboardLength;
    float keyboardWidth;
    float deltaNormal;
    float numKeysOnLongestLine;
    float delta;

    public bool[] modeArr = new bool[3];
    bool modeChangeOn = true;
    bool pressedEnter = true;

    // Start is called before the first frame update
    void Start()
    {
        startTime = Time.realtimeSinceStartup;
        var go = new GameObject("TestLine", typeof(LineRenderer));
        LRDebug = go.GetComponent<LineRenderer>();
        LRDebug.numCapVertices = 4;
        LRDebug.numCornerVertices = 4;
        LRDebug.widthMultiplier = 0.01f;
        LRDebug.useWorldSpace = false;

        boxCollider = transform.parent.GetComponent<BoxCollider>();
        isWriting = false;
        text = transform.parent.GetChild(1).GetChild(0).GetComponent<Text>();

        LR = GetComponent<LineRenderer>();
        LR.numCapVertices = 5;
        LR.numCornerVertices = 5;

        keyboardLength = transform.localScale.x;
        keyboardWidth = transform.localScale.y;
        
        normalizedWordsPointsDict = new Dictionary<string, List<Vector2>>();
        locationWordsPointsDict = new Dictionary<string, List<Vector2>>();

        string path = "Packages/com.unibas.wgkeyboard/Assets/sokgraph_qwertz.txt";

        StreamReader sr = new StreamReader(path);
        string line;
        string[] splits = new string[3];
        List<Vector2> normalizedPoints = new List<Vector2>();
        List<Vector2> locationPoints = new List<Vector2>();
        while (true) {
            line = sr.ReadLine();
            if (line == null) { // end of file reached
                break;
            }
            splits = line.Split(":");
            
            string[] points = splits[1].Split(","); // not normalized points
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
            v1 = 0;
            foreach (string p in points) {
                float.TryParse(p, out d);
                if ((n % 2) == 0) {
                    v1 = d;
                } else {
                    normalizedPoints.Add(new Vector2(v1, d));
                }
                n += 1;
            }

//            print("NPOINTSCOUNT: " + normalizedPoints.Count);

            normalizedWordsPointsDict.Add(splits[0], normalizedPoints);
            locationWordsPointsDict.Add(splits[0], locationPoints);
//            print("COUTN: " + normalizedWordsPointsDict["a"].Count);
            normalizedPoints = new List<Vector2>();
            locationPoints = new List<Vector2>();
        }

        createKeyboardOverlay("qwertz");

        print("Time needed for startup: " + (Time.realtimeSinceStartup - startTime));
    }

    // Update is called once per frame
    void Update()
    { 
        if (isWriting) {
            int layerMask = LayerMask.GetMask("WGKeyboard");
            RaycastHit hit;
            if (Physics.Raycast(col.position, transform.forward, out hit, Mathf.Infinity, layerMask)) {
                if (pointCount >= LR.positionCount) {
                    LR.positionCount++;
                }

                LR.SetPosition(pointCount, hit.point);
                pointCount+=1;
            } else if (Physics.Raycast(col.position, -transform.forward, out hit, Mathf.Infinity, layerMask)) {
                if (pointCount >= LR.positionCount) {
                    LR.positionCount++;
                }
                LR.SetPosition(pointCount, hit.point);
                pointCount+=1;
            }
        }
    }
    /*
    public void hoverTest(bool b)
    {
        int layerMask = LayerMask.GetMask("WGKeyboard");
        RaycastHit hit;
        Vector3 point = new Vector3(0,0,0);
        if (Physics.Raycast(col.position, transform.forward, out hit, Mathf.Infinity, layerMask)) {
            point = hit.point;
        } else if (Physics.Raycast(col.position, -transform.forward, out hit, Mathf.Infinity, layerMask)) {
            point = hit.point;
        }

        if (point != new Vector3(0,0,0)) {
            point = transform.parent.InverseTransformPoint(point);
            Vector2 transformedPoint = new Vector2((point[0] + keyboardLength / 2) / keyboardLength, (point[2] + keyboardWidth / 2) / keyboardLength);
            int xPos = (int) (transformedPoint.x / keyboardLength * numKeysOnLongestLine);
            int yPos = (int) (transformedPoint.y / keyboardWidth * keyboardSet.Count);
            if (xPos < numKeysOnLongestLine) {
                text.text = keyboardSet[xPos][yPos].ToString();
            } else {
                text.text = "-";
            }
        }
    }*/

    public void enterWordGestureMode(bool b) {
        changeMode(0);
    }

    public void enterSingleInputMode(bool b) {
        changeMode(1);
    }

    public void enterAddNewWordMode(bool b) {
        changeMode(2);
    }

    void changeMode(int mode) {
        if (modeChangeOn) {
            print("MODE: " + mode);
            Transform parent = transform.parent;
            for (int i = 0; i < 3; i++) {
                if (i != mode) {
                    parent.GetChild(i + 2).GetComponent<MeshRenderer>().material = whiteMat;
                    modeArr[i] = false;
                } else {
                    parent.GetChild(i + 2).GetComponent<MeshRenderer>().material = grayMat;
                    modeArr[i] = true;
                }
            }
        }

        if (mode == 2) {
            this.transform.parent.GetChild(5).gameObject.SetActive(true);
        } else {
            this.transform.parent.GetChild(5).gameObject.SetActive(false);
        }
        modeChangeOn = !modeChangeOn;   // needed because objects that call this method on hover do not have rigidbody, therefore bool b is always false
    }

    public void drawWord(Transform t, bool b) {
        if (b && modeArr[0]) {
            isWriting = true;
            col = t;
            boxCollider.center = new Vector3(boxCollider.center.x, 0.01f, boxCollider.center.z);
            boxCollider.size = new Vector3(boxCollider.size.x, 0.001f, boxCollider.size.z);
        }
    }

    public void endWord(Transform t, bool b) {
        print("here1");
        if (!b && modeArr[0]) {
            print("here2");
            print("COUNT: " + LR.positionCount);
            startTime = Time.realtimeSinceStartup;
            List<Vector2> pointsList = new List<Vector2>();
            //List<Vector3> pointsListTest = new List<Vector3>();
            Vector3 point;
            
            for (int i = 0; i < LR.positionCount; i++) {
                point = LR.GetPosition(i);
                Vector3 localTransformedPoint = transform.parent.InverseTransformPoint(point);
                
                pointsList.Add(new Vector2((localTransformedPoint[0] + keyboardLength/2) / keyboardLength, (localTransformedPoint[2] + keyboardWidth/2) / keyboardLength)); // adding magnitudes, such that lower left corner of "coordinate system" is at (0/0) and not middle point at (0/0)
                //pointsListTest.Add(pointsList[i]);
                //print("PREPOINT: " + localTransformedPoint);
                //print("POINT: " + pointsList[i]);
            }
            pointCount = 0;
            LR.positionCount = 0;
            isWriting = false;

            /*
            LRDebug.positionCount = pointsListTest.Count;
            for (int i = 0; i < pointsListTest.Count; i++) {
                LRDebug.SetPosition(i, pointsListTest[i]);
            }
            LRDebug.transform.Rotate(-transform.localRotation.eulerAngles.z, -transform.localRotation.eulerAngles.y, -transform.localRotation.eulerAngles.x + 90);
            */

            boxCollider.center = new Vector3(boxCollider.center.x, 0.03f, boxCollider.center.z);
            boxCollider.size = new Vector3(boxCollider.size.x, 0.05f, boxCollider.size.z);
            calcBestWords(pointsList, 20);

//////            Task.Run(() => { calcBestWords(pointsList, 20);});
            print("TIME NEEDED: " + (Time.realtimeSinceStartup-startTime));
        }
    }

    void createKeyboardOverlay(string layout) {
        keyboardSet = new List<String>();
        if (layout.Equals("qwertz")) {
            keyboardSet.Add("qwertzuiop");
            keyboardSet.Add("asdfghjkl");
            keyboardSet.Add("yxcvbnm");
        }

        for (int i = 0; i < keyboardSet.Count; i++) {
            if (keyboardSet[i].Length > numKeysOnLongestLine) {
                numKeysOnLongestLine = keyboardSet[i].Length;
            }
        }
        delta = 1 / numKeysOnLongestLine;   // is also the width of a key
        keyRadius = 1 / numKeysOnLongestLine / 2;

        int y = keyboardSet.Count - 1;
        foreach (string s in keyboardSet) {
            float o = 0;
            int x = 0;
            if (y == 1) {
                o = 0.025f * keyboardLength;
            } else if (y == 0) {
                o = 0.075f * keyboardLength;
            }
            foreach (var letter in s) {
                GameObject specificKey = Instantiate(Key) as GameObject;
                specificKey.transform.position = new Vector3(transform.position.x + (-keyboardLength / 10)*(4.5f-x) + o, transform.position.y + 0.005f, transform.position.z - (-keyboardWidth/3)*(y-1));
                specificKey.transform.Find("Canvas").Find("Text").GetComponent<Text>().text = letter.ToString();
                specificKey.transform.SetParent(this.transform);
                specificKey.transform.localScale -= (new Vector3(specificKey.transform.localScale.x, specificKey.transform.localScale.y, specificKey.transform.localScale.z)) * 0.5f;

                x += 1;
            }
            y -= 1;
        }
    }

    async void calcBestWords(List<Vector2> userInputPoints, int steps) {
        IOrderedEnumerable<KeyValuePair<string, float>> sortedDict = null;
        await Task.Run(() =>    // need await, because I couln't access the text of Text in task.run(() => {});
        {
            List<Vector2> inputPoints = getWordGraphStepPoint(userInputPoints, steps);
            for (int i = 0; i < inputPoints.Count; i++)
            {
                print(inputPoints[i]);
            }
            List<Vector2> normalizedInputPoints = normalize(inputPoints, 2);

            Dictionary<string, List<Vector2>>[] filteredWordsPoints = seKeyPosition(inputPoints, locationWordsPointsDict, normalizedWordsPointsDict, steps);
            Dictionary<string, float> normalizedCostList = normalizedPointsCost(filteredWordsPoints[1], normalizedInputPoints, steps);
            Dictionary<string, float> locationCostList = locationCosts(filteredWordsPoints[0], inputPoints, steps);

            List<string> wordList = new List<string>();
            foreach (var word in normalizedCostList)
            {   // look which word had a good cost in shape and location
                if (locationCostList.ContainsKey(word.Key))
                {
                    wordList.Add(word.Key);
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

        text.text = sortedDict.Last().Key;
        print(sortedDict.Last().Key);
    }

    public void writeWord(string word) {
        if (modeArr[0]) {
            // word = whole word calculated from gesture
        } else {
            // single letter
            text.text += word;
        }
    }

    public void addNewWordToDict() {
        if (pressedEnter) {
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
                }
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
        print("LENGTH: " + newLocWordsPoints.Count);
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
            Vector2 distVec = distVecs[currDistVecNum];
            double distVecLength = Mathf.Sqrt(Mathf.Pow(distVec[0], 2) + Mathf.Pow(distVec[1], 2));
            if (currStep != stepSize) {
                if (distVecLength - currStep > -0.0000001) {
                    numSteps += 1;
                    currPos = currPos + distVec / (float)distVecLength * (float)currStep;
                    distVecs[currDistVecNum] = points[currPosNum + 1] - currPos;
                    stepPoints.Add(currPos);
                    currStep = stepSize;
                }
                else {
                    currStep -= distVecLength;
                    currDistVecNum += 1;
                    currPosNum += 1;
                    currPos = points[currPosNum];
                }
            } else if ((int)(distVecLength / stepSize + 0.0000001) > 0) {
                int numPointsOnLine = (int)(distVecLength / stepSize + 0.0000001);
                numSteps += numPointsOnLine;
                for (int i = 0; i < numPointsOnLine; i++) {
                    stepPoints.Add(currPos + (i+1) * (distVec / (float)distVecLength * (float)stepSize));
                }
                
                if (distVecLength - numPointsOnLine * stepSize > 0.0000001) {
                    currStep -= (distVecLength - numPointsOnLine * stepSize);
                }
                    
                currDistVecNum += 1;
                currPosNum += 1;
                currPos = points[currPosNum];
            }
                    
            else {
                currStep -= distVecLength;
                currDistVecNum += 1;
                currPosNum += 1;
                currPos = points[currPosNum];
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
    /*
    List<Vector2> getWordPoints() {
        Dictionary<string, Vector2> letterPos = new Dictionary<string, List<Vector2>>();
        for (int y = 0; y < keyboardSet.Count; y++) {
            float x = 0;
            if (y == 0) {
                x = 0;
            } else if (y == 1) {
                x = 0.25f;
            } else {
                x = 0.75f;
            }

            foreach (char letter in keyboardSet[y]) {
                letterPos[letter.ToString()] = new Vector2(x, 2-y);
            }
        }

    }*/

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

