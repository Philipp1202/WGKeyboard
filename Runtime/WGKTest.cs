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

    public GameObject UpperLeftCorner;
    public GameObject UpperRightCorner;
    public GameObject LowerRightCorner;
    public GameObject Key;
    Vector3 ULCPos;
    Vector3 URCPos;
    Vector3 LRCPos;
    Vector3 vec1;
    Vector3 vec2;
    Vector3 forward;
    Vector3 keyboardNorm;
    LineRenderer LR;
    int pointCount = 0;
    Dictionary<string, List<Vector2>> normalizedWordsPointsDict;
    Dictionary<string, List<Vector2>> locationWordsPointsDict;
    List<Vector2> normalizedPoints;
    List<Vector2> locationPoints;
    bool isWriting;
    Transform col = null;

    LineRenderer LRDebug;

    float startTime = 0;
    float keyLength;
    float deltaNormal;
    float deltaLocaton;
    float numKeysOnLongestLine = 1;
    float delta;



    // Start is called before the first frame update
    void Start()
    {
        var go = new GameObject("TestLine", typeof(LineRenderer));
        LRDebug = go.GetComponent<LineRenderer>();
        LRDebug.numCapVertices = 4;
        LRDebug.numCornerVertices = 4;
        LRDebug.widthMultiplier = 0.01f;
        LRDebug.useWorldSpace = false;



        isWriting = false;

        LR = GetComponent<LineRenderer>();
        LR.numCapVertices = 5;
        LR.numCornerVertices = 5;

        ULCPos = UpperLeftCorner.transform.position;
        URCPos = UpperRightCorner.transform.position;
        LRCPos = LowerRightCorner.transform.position;
        print(ULCPos);
        print(URCPos);
        print(LRCPos);
        vec1 = URCPos - ULCPos;
        vec2 = URCPos - LRCPos;
        forward = transform.forward;
        keyLength = vec1.magnitude / 10; // key width
        print("LENGTH OF KEYBOARD: " + vec1.magnitude);
        keyboardNorm.x = -(vec1.y*vec2.z - vec1.z*vec2.y);
        keyboardNorm.y = -(vec1.z*vec2.x - vec1.x*vec2.z);
        keyboardNorm.z = -(vec1.x*vec2.y - vec1.y*vec2.x);
        //LR.SetPosition(0, transform.position);
        //LR.SetPosition(1, transform.forward*10+transform.position);
        print(keyboardNorm);
        //Debug.DrawRay(transform.position, forward, Color.green);


        normalizedWordsPointsDict = new Dictionary<string, List<Vector2>>();
        locationWordsPointsDict = new Dictionary<string, List<Vector2>>();

        string path = "Packages/com.unibas.wgkeyboard/Runtime/sokgraph_qwertz.txt";
        //string[] lines = System.IO.File.ReadAllLines(path);
        //print(lines[Random.Range(0,lines.Length)]);

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

            print("NPOINTSCOUNT: " + normalizedPoints.Count);

            normalizedWordsPointsDict.Add(splits[0], normalizedPoints);
            locationWordsPointsDict.Add(splits[0], locationPoints);
            print("COUTN: " + normalizedWordsPointsDict["a"].Count);
            normalizedPoints = new List<Vector2>();
            locationPoints = new List<Vector2>();
        }

        print(normalizedWordsPointsDict["a"]);
        createKeyboardOverlay("qwertz");

        
    }

    // Update is called once per frame
    void Update()
    { 
        ULCPos = UpperLeftCorner.transform.position;
        URCPos = UpperRightCorner.transform.position;
        LRCPos = LowerRightCorner.transform.position;
        vec1 = URCPos - ULCPos;
        vec2 = URCPos - LRCPos;
        forward = transform.forward;

        if (isWriting) {
            //LR.SetPosition(pointCount, col.transform.position);
            //pointCount+=1;
            //print("SOMETHING SEEMS TO WORK");
            int layerMask = LayerMask.GetMask("WGKeyboard");
            RaycastHit hit;
            if (Physics.Raycast(col.position, transform.forward, out hit, Mathf.Infinity, layerMask))
            {
                //Debug.Log("Point of contact: "+hit.point);
                if (pointCount >= LR.positionCount) {
                    LR.positionCount++;
                }

                LR.SetPosition(pointCount, hit.point);
                pointCount+=1;
            } else if (Physics.Raycast(col.position, -transform.forward, out hit, Mathf.Infinity, layerMask))
            {
                //Debug.Log("Point of contact: "+hit.point);
                if (pointCount >= LR.positionCount) {
                    LR.positionCount++;
                }
                LR.SetPosition(pointCount, hit.point);
                pointCount+=1;
            }
        }
    }

    public void drawWord(Transform t, bool b) {
        if (b) {
            isWriting = true;
            col = t;
        }
    }

    public void endWord(Transform t, bool b) {
        if (!b) {
            startTime = Time.realtimeSinceStartup;
            List<Vector2> pointsList = new List<Vector2>();
            List<Vector3> pointsListTest = new List<Vector3>();
            Vector3 point;
            float halfLength = vec1.magnitude / 2;
            float halfWidth = vec2.magnitude / 2;
            float length = vec1.magnitude;
            float width = vec2.magnitude;
            //float s = 2 / vec1.magnitude;
            Vector3 v1 = new Vector3(0,0,0);
            Vector3 v2 = new Vector3(0,0,0);
            Vector3 v3 = new Vector3(0,0,0);
            for (int i = 0; i < LR.positionCount; i++) {
                point = LR.GetPosition(i);
                //print("THIS IS A LINERENDERER POINT: " + point);
                Transform parentTransform = transform.parent;
                
//                point = Quaternion.Euler(-parentTransform.localRotation.eulerAngles.x, -parentTransform.localRotation.eulerAngles.y, -parentTransform.localRotation.eulerAngles.z) * point;
//                point -= parentTransform.position;        
                ///point[1] = 0.5f;
                //print("THIS IS A transformed POINT: " + point);
                //LR.SetPosition(i, point);

                point -= parentTransform.position;
                //point += 0.5f * vec1 + 0.5f * vec2;
                Vector3 vec1Norm = vec1.normalized;
                Vector3 vec2Norm = vec2.normalized;
                int xPos = 0;   // needed to determine, whether to take u, v or w for x and y (because it's order can change)
                int yPos = 0;

                bool vec1Taken = false;
                bool vec2Taken = false;
                bool vec3Taken = false;

                if (vec1Norm.x != 0) {
                    v1 = vec1Norm;
                    vec1Taken = true;
                    xPos = 0;
                } else if (vec2Norm.x != 0) {
                    v1 = vec2Norm;
                    vec2Taken = true;
                    yPos = 0;
                } else {
                    v1 = forward;
                    vec3Taken = true;
                }

                if (vec1Norm.y != 0 && !vec1Taken) {
                    v2 = vec1Norm;
                    vec1Taken = true;
                    xPos = 1;
                } else if (vec2Norm.y != 0 && !vec2Taken) {
                    v2 = vec2Norm;
                    vec2Taken = true;
                    yPos = 1;
                } else {
                    v2 = forward;
                    vec3Taken = true;
                }

                if (!vec1Taken) {
                    v3 = vec1Norm;
                    xPos = 2;
                } else if (!vec2Taken) {
                    v3 = vec2Norm;
                    yPos = 2;
                } else {
                    v3 = forward;
                }

                
                float m = v1.y / v1.x;
                v1.y = 0;
                v2.y = v2.y - v2.x * m;
                v3.y = v3.y - v3.x * m;
                point.y = point.y - point.x * m;

                m = v1.z / v1.x;
                v1.z = 0;
                v2.z = v2.z - v2.x * m;
                v3.z = v3.z - v3.x * m;
                point.z = point.z - point.x * m;

                m = v2.z / v2.y;
                v2.z = 0;
                v3.z = v3.z - v3.y * m;
                point.z = point.z - point.y * m;

                float w = point.z / v3.z;
                float v = (point.y - v3.y * w) / v2.y;
                float u = (point.x - v3.x * w - v2.x * v) / v1.x;

                float[] res = new float[3];
                res[0] = u;
                res[1] = v;
                res[2] = w;
                for (int j = 0; j < 3; j++) {
                    print("THIS IS UWV: " + res[j]);
                }


                /*
                float z = vec1Norm.y / vec1Norm.x;
                vec1Norm.y -= vec1Norm.x * z;
                vec2Norm.y -= vec2Norm.x * z;
                point.y -= point.x * z;

                z = vec1Norm.z / vec1Norm.x;
                vec1Norm.z -= vec1Norm.x * z;
                vec2Norm.z -= vec2Norm.x * z;
                point.z -= point.x * z;

                float t2 = 0;
                if (vec2Norm.y != 0) {
                    t2 = point.y / vec2Norm.y;
                }

                float t3 = 0;
                if (vec2Norm.z != 0) {
                    t3 = point.z / vec2Norm.z;
                }
                //print(t2 + " ?= " + t3);
                //print(point.y + " AND " + point.z);

                t2 = Mathf.Max(t2, t3);

                float s = (point.x - vec2Norm.x * t2) / vec1Norm.x;
                */


                pointsList.Add(new Vector2((res[xPos] + halfLength) / length, (res[yPos] + halfWidth) / length)); // adding magnitudes, such that lower left corner of "coordinate system" is at (0/0) and not middle point at (0/0)
                pointsListTest.Add(pointsList[i]);
                print("CALCx: " + Mathf.Abs(res[xPos]) + "halflen" + halfLength + "len" + length);
                print("CALCy: " + Mathf.Abs(res[yPos]) + "halflen" + halfLength + "len" + length);
                print("POINT: " + pointsList[i]);

                //print("POINT IS ON: " + s + " : " + t2);
                //print("PPPPPPOINT= " + point.x + ", " + point.y + ", " + point.z);
                //print("VEC1: " + vec1Norm); 
                //print("VEC2: " + vec2Norm);

            }
            pointCount = 0;
            LR.positionCount = 0;
            isWriting = false;

            LRDebug.positionCount = pointsListTest.Count;
            for (int i = 0; i < pointsListTest.Count; i++) {
                LRDebug.SetPosition(i, pointsListTest[i]);
            }
            LRDebug.transform.Rotate(-transform.localRotation.eulerAngles.z, -transform.localRotation.eulerAngles.y, -transform.localRotation.eulerAngles.x + 90);

            Task.Run(() => { calcBestWords(pointsList, 20);});
            //print("WORKED UNTIIL HERE :D");
            print("TIME NEEDED: " + (Time.realtimeSinceStartup-startTime));
        }
    }

    /*void OnTriggerEnter(Collider other) {
        isWriting = true;
        col = other;
    }*/
    /*
    void OnTriggerExit(Collider other) {
        t = Time.realtimeSinceStartup;
        List<Vector2> pointsList = new List<Vector2>();
        List<Vector3> pointsListTest = new List<Vector3>();
        Vector3 point;
        float halfLength = vec1.magnitude / 2;
        float halfWidth = vec2.magnitude / 2;
        float length = vec1.magnitude;
        float width = vec2.magnitude;
        //float s = 2 / vec1.magnitude;
        for (int i = 0; i < LR.positionCount; i++) {
            point = LR.GetPosition(i);
            //print("THIS IS A LINERENDERER POINT: " + point);
            point -= transform.position;
            point = Quaternion.Euler(-transform.localRotation.eulerAngles.z, -transform.localRotation.eulerAngles.y, -transform.localRotation.eulerAngles.x + 90) * point;
            //print("THIS IS A transformed POINT: " + point);
            //LR.SetPosition(i, point);
            pointsList.Add(new Vector2((point[0] + halfLength) / length + 0.05f, (point[2] + halfWidth) / length + 0.05f)); // adding magnitudes, such that lower left corner of "coordinate system" is at (0/0) and not middle point at (0/0)
            pointsListTest.Add(point);

        }
        pointCount = 0;
        LR.positionCount = 0;
        isWriting = false;

        LRDebug.positionCount = pointsListTest.Count;
        for (int i = 0; i < pointsListTest.Count; i++) {
            LRDebug.SetPosition(i, pointsListTest[i]);
        }

        calcBestWords(pointsList, 20);
        //print("WORKED UNTIIL HERE :D");
        print("TIME NEEDED: " + (Time.realtimeSinceStartup-t));
    }*/

    void createKeyboardOverlay(string layout) {
        List<string> keyboardSet = new List<String>();
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

        int y = keyboardSet.Count - 1;
        foreach (string s in keyboardSet) {
            float t = 0;
            int x = 0;
            if (y == 1) {
                t = 0.025f * vec1.magnitude;
            } else if (y == 0) {
                t = 0.075f * vec1.magnitude;
            }
            foreach (var letter in s) {
                GameObject specificKey = Instantiate(Key) as GameObject;
                print("VEC1.x " + vec2);
                specificKey.transform.position = new Vector3(transform.position.x + (-vec1.x / 10)*(4.5f-x) + t, transform.position.y + 0.005f, transform.position.z - (-vec2.z/3)*(y-1));
                specificKey.transform.Find("Canvas").Find("Text").GetComponent<Text>().text = letter.ToString();
                specificKey.transform.SetParent(this.transform);
                specificKey.transform.localScale -= (new Vector3(specificKey.transform.localScale.x, specificKey.transform.localScale.y, specificKey.transform.localScale.z)) * 0.5f;

                x += 1;
            }
            y -= 1;
        }
    }

    async void calcBestWords(List<Vector2> userInputPoints, int steps) {
        List<Vector2> inputPoints = getWordGraphStepPoint(userInputPoints, steps);
        for (int i = 0; i < inputPoints.Count; i++) {
            print(inputPoints[i]);
        }
        List<Vector2> normalizedInputPoints = normalize(inputPoints, 2);

        //List<Vector2> inputPoints = new List<Vector2>();
        //foreach (var point in inputPoints2) {
            //inputPoints.Add(point * 10 / vec1.magnitude); // change 10 to whatever number of keys the "longest" line has
        //}
        Dictionary<string, float> normalizedCostList = normalizedPointsCost(normalizedWordsPointsDict, normalizedInputPoints, steps);

        /*
        Dictionary<string, float> normalizedCostListSorted = new Dictionary<string, float>();
        for (int i = 0; i < normalizedCostList.Count; i++) {
            KeyValuePair<string, float> lowestCostPair = new KeyValuePair<string, float>("", 99999999999);
            foreach (var entry in normalizedCostList) {
                if (entry.Value < lowestCostPair.Value) {
                    lowestCostPair = entry;
                }
            }
            normalizedCostList.Remove(lowestCostPair.Key);
            normalizedCostListSorted.Add(lowestCostPair.Key, lowestCostPair.Value);
        }
        */
        /*foreach (var entry in normalizedCostListSorted) {
            print(entry.Key + "  :  " + entry.Value.ToString());
        }*/
        //print(normalizedCostList["hello"]);

        Dictionary<string, float> costList = locationCosts(locationWordsPointsDict, inputPoints, steps);
        
        /*
        foreach (var entry in costList) {
            System.Diagnostics.Debug.WriteLine(entry.Key + "  :  " + entry.Value.ToString());
            System.Diagnostics.Debug.Flush();
        }*/
        
        List<string> wordList = new List<string>();
        foreach (var word in normalizedCostList) {   // look which word had a good cost in shape and location
            if (costList.ContainsKey(word.Key)) {
                wordList.Add(word.Key);
            }
        }
        //print("WORDLISTLENGTH: " + wordList.Count);

        List<float> tempShapeCosts = new List<float>();
        List<float> tempLocationCosts = new List<float>();
        foreach (string word in wordList) {
            float shapeCost = normalizedCostList[word];
            float locationCost = costList[word];
            float shapeProb = 1/(delta*Mathf.Sqrt(2*Mathf.PI)) * Mathf.Exp((float)(-0.5 * Mathf.Pow(shapeCost / delta, 2)));
            float locationProb = 1/(delta*Mathf.Sqrt(2*Mathf.PI)) * Mathf.Exp((float)(-0.5 * Mathf.Pow(locationCost / delta, 2)));
            ///print("LOCCOST: " + locationCost);
            ///print("LOCPROB: " + locationProb);
            tempShapeCosts.Add(shapeProb);
            tempLocationCosts.Add(locationProb);
        }

        float sum = 0;
        float sum2 = 0;
        for (int i = 0; i < tempShapeCosts.Count; i++) {
            sum += tempShapeCosts[i];
            ///print("sum1= " + sum);
            sum2 += tempLocationCosts[i];
            ///print("sum2= " + sum2);
        }
        for (int i = 0; i < tempShapeCosts.Count; i++) {
            tempShapeCosts[i] /= sum;
            tempLocationCosts[i] /= sum2;
        }

        List<float> tempCosts2 = new List<float>();
        sum = 0;
        for (int i = 0; i < tempShapeCosts.Count; i++) {
            sum += tempShapeCosts[i] * tempLocationCosts[i];
            ///print("sum3= " + sum);
        }
        for (int i = 0; i < tempShapeCosts.Count; i++) {
            tempCosts2.Add(tempShapeCosts[i] * tempLocationCosts[i] / sum);
        }

        Dictionary<string, float> finalCosts = new Dictionary<string, float>();
        int q = 0;
        foreach (string word in wordList) {
            finalCosts.Add(word, tempCosts2[q]);
            q += 1;
        }

        var sortedDict = from entry in finalCosts orderby entry.Value ascending select entry;

        foreach (var word in sortedDict) {
            print("RESULT: " + word.Key + " : " + word.Value);
        }
        
        ///return "hello";
    }

    Dictionary<string, float> locationCosts(Dictionary<string, List<Vector2>> locationWordsPointDict, List<Vector2> inputPoints, int steps) {
        Dictionary<string, float> costList = new Dictionary<string, float>();
        int i;
        float cost;
        float d;
        float d2;
        
        float[] arrD = new float[steps];
        float keyRadius = 0.05f; // length of keyboard is set to 1

        foreach (var word in locationWordsPointDict) {
            i = 0;
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
            //int d = 1;
            //int d2 = 3;
            //float cost = 0;

            if (d == 0 && d2 == 0) {
                cost = 0;
            } else {
                int k = 0;
                foreach (Vector2 p in word.Value) {
                    cost += (p - inputPoints[k]).magnitude;
                    if (word.Key == "point") {
                        print("locpointcost inside: " + cost + " iteration: " + k);
                    } 
                    k += 1;
                }
            }

            if (word.Key == "point") {
                print("locpointcost: " + cost);
            }
            ///print("COOOOOOOOOST: " + word.Key + " : " + cost);
            if (cost < 2) {
                costList.Add(word.Key, cost);
//                print("COOOOOOOOOST: " + word.Key + " : " + cost);
            }
            deltaLocaton = 1;
        }
        return costList;
    }

    Dictionary<string, float> normalizedPointsCost(Dictionary<string, List<Vector2>> normalizedWordsPointDict, List<Vector2> normalizedInputPoints, int steps) {
        Dictionary<string, float> normalizedCostList = new Dictionary<string, float>();
        float keyRadius = 0.05f;
        int n;
        float cost;
        foreach (var word in normalizedWordsPointDict) {
            n = 0;
            cost = 0;
            foreach (Vector2 p in word.Value) {
                cost += (p - normalizedInputPoints[n]).magnitude;
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
            //print("numsteps = " + numSteps);
            //print("distvecnum = " + currDistVecNum + " all: " + distVecs.Count);
            Vector2 distVec = distVecs[currDistVecNum];
            double distVecLength = Mathf.Sqrt(Mathf.Pow(distVec[0], 2) + Mathf.Pow(distVec[1], 2));
            if (currStep != stepSize) {
                if (distVecLength - currStep > -0.00001) {
                    numSteps += 1;
                    currPos = currPos + distVec * (float)distVecLength * (float)currStep;
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
            } else if ((int)(distVecLength / stepSize + 0.00001) > 0) {
                int numPointsOnLine = (int)(distVecLength / stepSize + 0.00001);
                numSteps += numPointsOnLine;
                for (int i = 0; i < numPointsOnLine; i++) {
                    stepPoints.Add(currPos + (i+1) * (distVec / (float)distVecLength * (float)stepSize));
                }
                
                if (distVecLength - numPointsOnLine * stepSize > 0.00001) {
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

