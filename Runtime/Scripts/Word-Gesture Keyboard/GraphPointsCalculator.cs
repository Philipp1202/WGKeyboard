using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;  // for Max(), Min(), ...
using System.Threading.Tasks;
using System;

namespace WordGestureKeyboard {
    public class GraphPointsCalculator {

        //public float keyRadius;
        public float deltaNormal;
        public bool isSampling = false;
        //public IOrderedEnumerable<KeyValuePair<string, float>> sortedDict = null;
        public List<string> sortedDict = null;
        public string bestWord = "";
        public bool isCalculatingPreview = false;
        //public float numKeysOnLongestLine;
        /*
        public GraphPointsCalculator(float n) {
            numKeysOnLongestLine = n;
        }*/

        /// <summary>
        /// Calculates the costs for every word given in "locationWordsPointDict" with respect to the "inputpoints". This is the cost of the not normalized points more or less according to SHARK2.
        /// </summary>
        /// <param name="locationWordsPointDict">Dict with all the non normalized graph points for all the words in the dictionary.</param>
        /// <param name="inputPoints">Points to compare all the words in the dictionary to. (userinput)</param>
        /// <param name="steps">Number of steps used for sampling the points.</param>
        /// <param name="keyRadius">Radius of a key of the keyboard.</param>
        Dictionary<string, float> locationCosts(Dictionary<string, List<Vector2>> locationWordsPointDict, List<Vector2> inputPoints, int steps, float keyRadius) {
            Dictionary<string, float> costList = new Dictionary<string, float>();
            float cost;
            float d;
            float d2;

            float[] arrD = new float[steps];
            float[] alpha = new float[steps];
            for (int i = 0; i < steps; i++) {
                alpha[i] = Mathf.Abs((1f / steps) * (steps / 2f) - (1f / steps) * i);
                alpha[i] += 10;
                //Debug.Log("PREALHA: " + alpha[i]);
            }
            float sum = alpha.Sum();
            //Debug.Log(sum);
            for (int i = 0; i < steps; i++) {
                alpha[i] = alpha[i] / sum;
                //Debug.Log("ALPHA: " + alpha[i]);
            }

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
                        cost += (p - inputPoints[k]).magnitude * alpha[k];
                        if (word.Key == "point") {
                            //print("locpointcost inside: " + cost + " iteration: " + k);
                        }
                        k += 1;
                    }
                }
                if (word.Key == "pink" || word.Key == "pork") {
                   //Debug.Log("LAST LOCATIONTEST WITH ALPHA: " + word.Key + ", " + cost);
                }

                //Debug.Log("locpointcost: " + cost);
                //print("COOOOOOOOOST: " + word.Key + " : " + cost);
                if (cost < keyRadius * 2) {
                    costList.Add(word.Key, cost);
                }
            }
            return costList;
        }

        /// <summary>
        /// Calculates the costs for every word given in "normalizedWordsPointDict" with respect to the "inputpoints". This is the cost of the normalized points more or less according to SHARK2.
        /// </summary>
        /// <param name="normalizedWordsPointDict">Dict with all the normalized graph points for all the words in the dictionary.</param>
        /// <param name="normalizedInputPoints">Normalized points to compare all the words in the dictionary to. (userinput)</param>
        /// <param name="steps">Number of steps used for sampling the points.</param>
        /// <param name="keyRadius">Radius of a key of the keyboard.</param>
        Dictionary<string, float> normalizedPointsCost(Dictionary<string, List<Vector2>> normalizedWordsPointDict, List<Vector2> normalizedInputPoints, int steps, float keyRadius) {
            Dictionary<string, float> normalizedCostList = new Dictionary<string, float>();
            int n;
            float cost;
            //Debug.Log("ATLEAST I AM HERE " + normalizedWordsPointDict.Count);
            foreach (var word in normalizedWordsPointDict) {
                n = 0;
                cost = 0;
                foreach (Vector2 p in word.Value) {
                    cost += (p - normalizedInputPoints[n]).magnitude;
                    //print("NORMCOST: " + normalizedInputPoints[n] + " : " + p + "  ::  " + (p - normalizedInputPoints[n]).magnitude);
                    n += 1;
                }
                cost /= steps;
                //Debug.Log("normpointcost: " + cost + " : " + (deltaNormal));
                //print("COSTS: " + word.Key + " : " + cost);
                if (word.Key == "point") {
                    Debug.Log("normpointcost: " + cost + " : " + (deltaNormal));
                }
                if (cost < keyRadius * deltaNormal) {
                    normalizedCostList.Add(word.Key, cost);
                    ///print("NORMCOST: " + word.Key + " : " + cost);
                }

            }

            return normalizedCostList;
        }

        /// <summary>
        /// Returns the x coordinates for all the given points.
        /// </summary>
        /// <param name="wordPoints">List of points.</param>
        public static List<float> getX(List<Vector2> wordPoints) {
            List<float> xPoints = new List<float>();
            for (int i = 0; i < wordPoints.Count; i++) {
                xPoints.Add(wordPoints[i][0]);
            }
            return xPoints;
        }

        /// <summary>
        /// Returns the y coordinates for all the given points.
        /// </summary>
        /// <param name="wordPoints">List of points.</param>
        public static List<float> getY(List<Vector2> wordPoints) {
            List<float> yPoints = new List<float>();
            for (int i = 0; i < wordPoints.Count; i++) {
                yPoints.Add(wordPoints[i][1]);
            }
            return yPoints;
        }

        /// <summary>
        /// Normalizes the given points to the given length.
        /// </summary>
        /// <param name="letterPoints">List of points.</param>
        /// <param name="length">Length, to that the spanned rectangle of the given points should be normalized to.</param>
        public List<Vector2> normalize(List<Vector2> letterPoints, int length) {
            List<float> x = getX(letterPoints);
            List<float> y = getY(letterPoints);

            float minx = x.Min();
            float maxx = x.Max();
            float miny = y.Min();
            float maxy = y.Max();

            float[] boundingBox = { minx, maxx, miny, maxy };
            float[] boundingBoxSize = { maxx - minx, maxy - miny };

            float s;
            if (Mathf.Max(boundingBoxSize[0], boundingBoxSize[1]) != 0) {
                s = length / Mathf.Max(boundingBoxSize[0], boundingBoxSize[1]);
                //            print("S = " + s);
            } else {
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

        /// <summary>
        /// Calculates, which word is most likely to be the one, the user intended to write.
        /// </summary>
        /// <param name="userInputPoints">List of points which a "best word" should be found for.</param>
        /// <param name="steps">Number of points of user input.</param>
        /// <param name="locationWordsPointsDict">Dict of words and their corresponding sokgraph points.</param>
        /// <param name="normalizedWordsPointsDict">Dict of words and their corresponding normalized sokgraph points.</param>
        /// <param name="delta">Parameter for algorithm, to determine if words should be discarded early or not. If smaller, more words get dicarded early on.</param>
        /// <param name="keyRadius">Radius of a key of the keyboard.</param>
        async public void calcBestWords(List<Vector2> userInputPoints, int steps, Dictionary<string, List<Vector2>> locationWordsPointsDict, Dictionary<string, List<Vector2>> normalizedWordsPointsDict, float delta, float keyRadius, Dictionary<string, int> wordRanking, bool isWriting) {
            if (isWriting) {
                isCalculatingPreview = true;
            }
            sortedDict = null;
            //Debug.Log("IIIIII AMMMMMMM HEREEREREREEEE 1");
            await Task.Run(() => {    // need await, because I couln't access the text of Text in task.run(() => {});
                //Debug.Log("IIIIII AMMMMMMM HEREEREREREEEE 2");
                List<Vector2> inputPoints = getWordGraphStepPoint(userInputPoints, steps);
                for (int i = 0; i < inputPoints.Count; i++) {
                    //               print(inputPoints[i]);
                }
                List<Vector2> normalizedInputPoints = normalize(inputPoints, 2);

                Dictionary<string, List<Vector2>>[] filteredWordsPoints = seKeyPosition(inputPoints, locationWordsPointsDict, normalizedWordsPointsDict, steps, keyRadius);
                Dictionary<string, float> normalizedCostList = normalizedPointsCost(filteredWordsPoints[1], normalizedInputPoints, steps, keyRadius);
                Dictionary<string, float> locationCostList = locationCosts(filteredWordsPoints[0], inputPoints, steps, keyRadius);

                List<string> wordList = new List<string>();
                foreach (var word in normalizedCostList) {   // look which word had a good cost in shape and location
                    if (locationCostList.ContainsKey(word.Key)) {
                        if (isSingleLetter(userInputPoints, keyRadius)) {
                            if (word.Key.Length == 1) {
                                wordList.Add(word.Key);
                            }
                        } else {
                            wordList.Add(word.Key);
                        }
                    }
                }

                if (wordList.Count > 0) {
                    List<float> tempShapeCosts = new List<float>();
                    List<float> tempLocationCosts = new List<float>();
                    foreach (string word in wordList) {
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
                    for (int i = 0; i < numWords; i++) {
                        sum += tempShapeCosts[i];
                        ///print("sum1= " + sum);
                        sum2 += tempLocationCosts[i];
                        ///print("sum2= " + sum2);
                    }
                    for (int i = 0; i < numWords; i++) {
                        tempShapeCosts[i] /= sum;
                        tempLocationCosts[i] /= sum2;
                    }

                    List<float> tempCosts2 = new List<float>();
                    sum = 0;
                    for (int i = 0; i < numWords; i++) {
                        sum += tempShapeCosts[i] * tempLocationCosts[i];
                        ///print("sum3= " + sum);
                    }
                    for (int i = 0; i < numWords; i++) {
                        tempCosts2.Add(tempShapeCosts[i] * tempLocationCosts[i] / sum);
                    }

                    Dictionary<string, float> finalCosts = new Dictionary<string, float>();
                    int q = 0;
                    foreach (string word in wordList) {
                        finalCosts.Add(word, tempCosts2[q]);
                        q += 1;
                    }
                    foreach (var k in finalCosts.Keys) {
                        Debug.Log(k + ": " + finalCosts[k]);
                    }

                    IOrderedEnumerable<KeyValuePair<string, float>> sorted = null;
                    sorted = from entry in finalCosts orderby entry.Value descending select entry;

                    List<KeyValuePair<string, float>> betterSortedDict;
                    List<string> sortedList;

                    betterSortedDict = sorted.ToList();
                    sortedList = sorted.Select(pair => pair.Key).ToList();  // only take words into a list, not their probabilities
                    float highestValue = betterSortedDict[0].Value;
                    Dictionary<string, int> bestMatches = new Dictionary<string, int>();
                    foreach (var pair in betterSortedDict) {
                        if (pair.Value == highestValue) {
                            bestMatches[pair.Key] = wordRanking[pair.Key];
                        }
                    }
                    IOrderedEnumerable<KeyValuePair<string, int>> sortedBestMatches = null;
                    sortedBestMatches = from entry in bestMatches orderby entry.Value ascending select entry;

                    int p = 0;
                    foreach (var pair in sortedBestMatches) {
                        sortedList[p] = pair.Key;
                        p++;
                    }
                    if (!isWriting) {
                        sortedDict = sortedList;
                        foreach (var k in sortedDict) {
                            Debug.Log(k);
                        }
                    } else {
                        bestWord = sortedList[0];
                        isCalculatingPreview = false;
                    }
                } else if (isWriting) {
                    bestWord = "";
                    isCalculatingPreview = false;
                }
            });
        }

        /// <summary>
        /// Looks, if for the given points it's very likely, that the input is meant to be single letter and not a word
        /// </summary>
        /// <param name="letterPoints">List of points.</param>
        /// <param name="keyRadius">Radîus of a key of the keyboard.</param>
        public bool isSingleLetter(List<Vector2> letterPoints, float keyRadius) {
            List<float> x = getX(letterPoints);
            List<float> y = getY(letterPoints);

            float minx = x.Min();
            float maxx = x.Max();
            float miny = y.Min();
            float maxy = y.Max();

            //float[] boundingBox = { minx, maxx, miny, maxy };
            //float[] boundingBoxSize = { maxx - minx, maxy - miny };

            if (maxx - minx > keyRadius || maxy - miny > keyRadius) {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Looks, if for the given points, the input meant is a backspace, space or nothing of these two.
        /// Returns -1 for a backspace, 1 for space and 0 if it's neither of them.
        /// </summary>
        /// <param name="letterPoints">List of points.</param>
        /// <param name="backSpaceHitbox">Hitbox of the backspace key.</param>
        /// <param name="spaceHitbox">Hitbox of the space key.</param>
        public int isBackSpaceOrSpace(List<Vector2> letterPoints, float[] backSpaceHitbox, float[] spaceHitbox) {
            if (letterPoints.Count == 0) {  // user wanted to write a word, but no point has been set
                return 0;
            }
            List<float> x = getX(letterPoints);
            List<float> y = getY(letterPoints);

            float minx = x.Min();
            float maxx = x.Max();
            float miny = y.Min();
            float maxy = y.Max();

            if (backSpaceHitbox[0] < minx && backSpaceHitbox[1] > maxx && backSpaceHitbox[2] < miny && backSpaceHitbox[3] > maxy) {
                return -1;
            }
            if (spaceHitbox[0] < minx && spaceHitbox[1] > maxx && spaceHitbox[2] < miny && spaceHitbox[3] > maxy) {
                return 1;
            }
            return 0;
        }

        /// <summary>
        /// Returns the added up distance of all the distances between two adjacent points.
        /// </summary>
        /// <param name="points">List of points.</param>
        public double getLengthByPoints(List<Vector2> points) {
            double dist = 0;
            Vector2 distVec;
            for (int i = 0; i < points.Count - 1; i++) {
                distVec = points[i] - points[i + 1];
                dist += Mathf.Sqrt(Mathf.Pow(distVec[0], 2) + Mathf.Pow(distVec[1], 2));
            }
            return dist;
        }

        /// <summary>
        /// Returns a list of points for a given word. These points correspond to the sokgraph points of this given word.
        /// </summary>
        /// <param name="word">Word from which to determine the sokgraph points.</param>
        /// <param name="l">Layout to be considered.</param>
        /// <param name="layoutKeys">Dict with all layouts and its related order of keys.</param>
        /// <param name="numKeyOnLongestLine">Highest number of keys in one line (on the keyboard).</param>
        public List<Vector2> getWordPoints(string word, Tuple<List<float>, List<string>> layoutInfo) {
            Dictionary<string, Vector2> letterPos = new Dictionary<string, Vector2>();
            List<float> paddingList = layoutInfo.Item1;
            List<string> charList = layoutInfo.Item2;
            int count = charList.Count;

            HashSet<char> allCharacters = new HashSet<char>();
            foreach (string line in charList) {
                foreach (char character in line) {
                    if (character.ToString() == " " || character.ToString() == "<") {
                        continue;
                    }
                    if (!allCharacters.Contains(character)) {
                        allCharacters.Add(character);
                    }
                }
            }
            foreach (char c in word) {
                if (!allCharacters.Contains(c)) {
                    return null;    // word cannot be written with current layout
                }
            }

            float numKeysOnLongestLine = 0;
            for (int i = 0; i < count; i++) {
                float lineLength = charList[i].Length + Mathf.Abs(paddingList[i]);
                if (charList[i].Contains(" ")) {
                    lineLength += 7;    // because length of spacebar is 8 * normal keysize, that means 7 * keysize extra
                }
                if (charList[i].Contains("<")) {
                    lineLength += 1;    // because length of backspace is 2 * normal keysize, that means 1 * keysize extra
                }
                if (lineLength > numKeysOnLongestLine) {
                    numKeysOnLongestLine = lineLength;
                }
            }

            for (int y = 0; y < count; y++) {
                float offset = paddingList[count - 1 - y];
                for (int x = 0; x < charList[count - y - 1].Count(); x++) {
                    if (charList[count - y - 1][x].ToString() == " ") {
                        offset += 7;
                        continue;
                    }
                    if (charList[count - y - 1][x].ToString() == "<") {
                        offset += 1;
                        continue;
                    }
                    letterPos.Add(charList[count - y - 1][x].ToString(), new Vector2((x + offset + 0.5f) / numKeysOnLongestLine, (y + 0.5f) / numKeysOnLongestLine)); // position of all letters if one key had radius 1
                }
            }

            List<Vector2> points = new List<Vector2>();
            foreach (char letter in word) {
                points.Add(letterPos[letter.ToString()]);
            }

            List<Vector2> locationPoints = getWordGraphStepPoint(points, 20);
            return locationPoints;
        }

        /// <summary>
        /// Returns a List of (steps + 1) points, that lie in equal distance to eachother on the graph the input points span.
        /// </summary>
        /// <param name="points">List of points.</param>
        /// <param name="steps">Number steps used to sample points.</param>
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
            for (int i = 0; i < points.Count - 1; i++) {
                distVecs.Add(points[i + 1] - points[i]);
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

        /// <summary>
        /// Returns a Dict with the words and their sokgraph points (normalized and not), but only the ones, where the starting and ending points lie close to the starting and ending points of the input.
        /// If the distance between starting and starting, and ending and ending points is too big, the word gets discarded and not returned in the Dict.
        /// </summary>
        /// <param name="input">List of points.</param>
        /// <param name="locWordsPoints">Dict with words and related sokgraph points.</param>
        /// <param name="normWordsPoints">Dict with words and related normalized sokgraph points.</param>
        /// <param name="steps">Number steps used to sample points.</param>
        /// <param name="keyRadius">Radius of a key of the keyboard.</param>
        // TODO: change name, because it doesnt calc any points but the new wordlists where words got discarded
        Dictionary<string, List<Vector2>>[] seKeyPosition(List<Vector2> input, Dictionary<string, List<Vector2>> locWordsPoints, Dictionary<string, List<Vector2>> normWordsPoints, int steps, float keyRadius) {
            Dictionary<string, List<Vector2>> newLocWordsPoints = new Dictionary<string, List<Vector2>>();
            Dictionary<string, List<Vector2>> newNormWordsPoints = new Dictionary<string, List<Vector2>>();
            //Debug.Log("HOW MANY WORDS I CAN CHOOSE FROM?: " + locWordsPoints.Count);
            foreach (var word in locWordsPoints) {
                if (word.Key == "pololoop") {
                    //Debug.Log("TEST FOR SEPOSITION: " + input[0] + " : " + word.Value[0] + input[steps - 1] + " : " + word.Value[steps - 1]);
                }
                if ((input[0] - word.Value[0]).magnitude < keyRadius * 2 && (input[steps - 1] - word.Value[steps - 1]).magnitude < keyRadius * 2) {
                    newLocWordsPoints.Add(word.Key, word.Value);
                    newNormWordsPoints.Add(word.Key, normWordsPoints[word.Key]);
                    //Debug.Log("POSSIBLE WORDS: " + word.Key);
                }
            }
            //print("LENGTH: " + newLocWordsPoints.Count);
            return new Dictionary<string, List<Vector2>>[] { newLocWordsPoints, newNormWordsPoints };
        }

        //public IOrderedEnumerable<KeyValuePair<string, float>> getSortedDisc() {
        //    return sortedDict;
        //}

        public List<string> getSortedDisc() {
            return sortedDict;
        }
    }
}
