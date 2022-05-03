using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.IO;
using System;

namespace WordGestureKeyboard {
    public class FileHandler {
        Dictionary<string, List<Vector2>> locationWordsPointsDict = null;
        Dictionary<string, List<Vector2>> normalizedWordsPointsDict = null;
        public string layout;
        public List<string> layouts;
        HashSet<string> wordsInLexicon = new HashSet<string>();
        public Dictionary<string, Tuple<List<float>, List<string>>> layoutKeys;

        public FileHandler(string layout) {
            this.layout = layout;
            wordsInLexicon = getAllWordsInLexicon(layout);
        }

        /// <summary>
        /// Loads the sokgraph points that have been generated for a certain keyboard layout in a file called "sokgraph_'layout'.txt".
        /// Saves the information in the locationWordsPointsDict and normalizedWordsPointsDict.
        /// </summary>
        /// <param name="layout">Layout from which the sokgraphs should be loaded.</param>
        async public void loadWordGraphs(string layout) {
            await Task.Run(() => {
                locationWordsPointsDict = new Dictionary<string, List<Vector2>>();
                normalizedWordsPointsDict = new Dictionary<string, List<Vector2>>();

                string path = "Packages/com.unibas.wgkeyboard/Assets/sokgraph_" + layout + ".txt";

                StreamReader sr = new StreamReader(path);
                Debug.Log("HERE IS BEGINNING OF LOADGRAPHS");
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

                    //Debug.Log("SOMETHING TO TEST: " + splits[0]);
                    locationWordsPointsDict.Add(splits[0], locationPoints);
                    normalizedWordsPointsDict.Add(splits[0], normalizedPoints);

                    normalizedPoints = new List<Vector2>();
                    locationPoints = new List<Vector2>();
                }
            });
        }

        /// <summary>
        /// Looks, if the new word to be added already exists in the dictionary. If yes, then this function does nothing, else it writes the word in the words txt-file,
        /// adds the sokgraph points to the corresponding files and also adds these to the normalizedWordsPointsDict and locationWordsPointsDict.
        /// </summary>
        /// <param name="newWord">New word that should be added to the dicitonary.</param>
        public void addNewWordToDict(string newWord, GraphPointsCalculator GPC) {
            if (newWord.Length != 0 && !newWord.Contains(" ")) {
                string path = "Packages/com.unibas.wgkeyboard/Assets/10000_english_words.txt";
                StreamReader sr = new StreamReader(path);
                bool isIn = false;
                string line;

                float starttime = Time.realtimeSinceStartup;
                while (true) {
                    line = sr.ReadLine();
                    if (line == newWord) {  //word already in lexicon
                        isIn = true;
                        break;
                    }
                    if (line == null) {
                        break;
                    }
                }
                
                if (wordsInLexicon.Contains(newWord)) {
                    isIn = true;
                }

                sr.Close();
                if (!isIn) {    // word to be added to the lexicon is new and not already in it
                    StreamWriter sw = File.AppendText(path);
                    sw.WriteLine(newWord);
                    sw.Close();

                    foreach (var l in layouts) {
                        Debug.Log(l);
                        List<Vector2> wordLocationPoints = GPC.getWordPoints(newWord, layoutKeys[l]);
                        if (wordLocationPoints == null) {
                            Debug.Log("CANT BE WRITTEN IN: " + l);
                            continue;
                        }
                        List<Vector2> wordNormPoints = GPC.normalize(wordLocationPoints, 2);

                        path = "Packages/com.unibas.wgkeyboard/Assets/sokgraph_" + l + ".txt";
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

                        if (l == this.layout) {
                            normalizedWordsPointsDict.Add(newWord, wordNormPoints);
                            locationWordsPointsDict.Add(newWord, wordLocationPoints);
                        }
                    }
                }
            }

            // Calculate graph of new word and add it to the dict/list
        }

        public void addKeyboardLettersToLexicon(GraphPointsCalculator GPC) {
            HashSet<string> allCharacters = new HashSet<string>();
            foreach (string layout in layouts) {
                foreach (string l in layoutKeys[layout].Item2) {
                    foreach (char character in l) {
                        if (!allCharacters.Contains(character.ToString())) {
                            allCharacters.Add(character.ToString());
                        }
                    }
                }
            }
            HashSet<string> allCharactersCopy = new HashSet<string>();
            foreach (string s in allCharacters) {
                allCharactersCopy.Add(s);
            }
            string path = "Packages/com.unibas.wgkeyboard/Assets/10000_english_words.txt";
            StreamReader sr = new StreamReader(path);
            string line;
            while (true) {
                line = sr.ReadLine();
                foreach (string s in allCharactersCopy) {
                    if (line == s) {
                        allCharacters.Remove(s);
                    }
                }
                if (line == null) {
                    break;
                }
            }
            sr.Close();
            StreamWriter sw = File.AppendText(path);
            foreach (string s in allCharacters) {
                sw.WriteLine(s);
            }
            sw.Close();

            foreach (var l in layouts) {
                path = "Packages/com.unibas.wgkeyboard/Assets/sokgraph_" + l + ".txt";
                sw = File.AppendText(path);
                foreach (string s in allCharacters) {
                    List<Vector2> wordLocationPoints = GPC.getWordPoints(s, layoutKeys[l]);
                    List<Vector2> wordNormPoints = GPC.normalize(wordLocationPoints, 2);

                    string newLine = s + ":";
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
                }
                sw.Close();
            }
        }

        /// <summary>
        /// Loads the layouts from the file in Assets/layouts.txt.
        /// It stores the layout names in "layouts" and the order of the keys with the layout name in the dict "layoutKeys".
        /// </summary>
        void loadLayoutsOld() {
            layouts = new List<string>();
            //layoutKeys = new Dictionary<string, List<string>>();
            string path = "Packages/com.unibas.wgkeyboard/Assets/layouts.txt";
            StreamReader sr = new StreamReader(path);
            string line;
            string l = "";  // layoutname
            List<string> keys = new List<string>();
            while (true) {
                line = sr.ReadLine();
                if (line == null) { // end of file reached
                    break;
                } else if (l == "") {
                    l = line;
                    layouts.Add(line);
                    continue;
                } else if (line == "-----") {
                    List<string> k = keys;
                    //layoutKeys.Add(l, k);
                    keys = new List<string>();
                    l = "";
                    continue;
                }

                keys.Add(line);
            }
        }

        /// <summary>
        /// Loads the layouts from the file in Assets/layouts.txt.
        /// It stores the layout names in "layouts" and the order of the keys with the layout name in the dict "layoutKeys".
        /// </summary>
        public void loadLayouts() {
            layouts = new List<string>();
            layoutKeys = new Dictionary<string, Tuple<List<float>, List<string>>>();
            string path = "Packages/com.unibas.wgkeyboard/Assets/layouts.txt";
            StreamReader sr = new StreamReader(path);
            string line;
            string l = "";  // layoutname
            Tuple<List<float>, List<string>> keys;
            keys = new Tuple<List<float>, List<string>>(new List<float>(), new List<string>());
            int i = 0;
            while (true) {
                line = sr.ReadLine();
                if (i < 6) {    // skip first 6 lines, because they just explain the format to be used
                    i++;
                    continue;
                }
                if (line == null) { // end of file reached
                    break;
                } else if (l == "") {
                    l = line;
                    layouts.Add(line);
                    continue;
                } else if (line == "-----") {
                    HashSet<char> allCharacters = new HashSet<char>();
                    bool isIllegalLayout = false;
                    foreach (string s in keys.Item2) {
                        foreach (char character in s) {
                            if (character.ToString() == " " || character.ToString() == "<") {
                                continue;
                            }
                            if (allCharacters.Contains(character)) {
                                isIllegalLayout = true;
                                Debug.Log("THIS ISSSSSSSSSSSSSSS THE IMPOSTER CHARACTER!!!!!!!!: " + character);
                            }
                            allCharacters.Add(character);
                        }
                    }
                    if (!isIllegalLayout) { // two or more times same character in layout
                        layoutKeys.Add(l, keys);
                        Debug.Log("JUST ADDED: " + l);
                    } else {
                        layouts.Remove(l);
                    }
                    keys = new Tuple<List<float>, List<string>>(new List<float>(), new List<string>());
                    l = "";
                    continue;
                }
                string[] splits = line.Split("$$");
                float f = 0;
                if (float.TryParse(splits[1], out f)) { // true, if a float, false if not
                    keys.Item1.Add(f);
                } else {
                    keys.Item1.Add(0);
                }
                keys.Item2.Add(splits[0]);
                Debug.Log("line: " + splits[0] + " : padding: " + f);
            }
        }

        HashSet<string> getAllWordsInLexicon(string layout) {
            HashSet<string> words = new HashSet<string>();
            string line;
            string path = "Packages/com.unibas.wgkeyboard/Assets/sokgraph_" + layout + ".txt";
            StreamReader sr = new StreamReader(path);
            while (true) {
                line = sr.ReadLine();
                words.Add(line);
                if (line == null) {
                    break;
                }
            }
            return words;
        }

        public List<string> getLayouts() {
            return layouts;
        }

        public Dictionary<string, Tuple<List<float>, List<string>>> getLayoutKeys() {
            return layoutKeys;
        }

        public Dictionary<string, List<Vector2>> getLocationWordsPointsDict() {
            return locationWordsPointsDict;
        }

        public Dictionary<string, List<Vector2>> getNormalizedWordsPointsDict() {
            return normalizedWordsPointsDict;
        }
    }
}
