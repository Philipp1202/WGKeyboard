using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.IO;
using System;

namespace WordGestureKeyboard {
    public class FileHandler {
        Dictionary<string, List<Vector2>> locationWordPointsDict = null;
        Dictionary<string, List<Vector2>> normalizedWordPointsDict = null;
        public string layout;
        public List<string> layouts;
        public Dictionary<string, Tuple<List<float>, List<string>>> layoutCompositions;
        HashSet<string> wordsInLexicon = new HashSet<string>();
        public Dictionary<string, int> wordRanking = new Dictionary<string, int>();
        public bool isLoading = false;
        string pathToLexicon = "Packages/com.unibas.wgkeyboard/Assets/10000_english_words.txt";

        public FileHandler(string layout) {
            this.layout = layout;
            wordsInLexicon = GetAllWordsInLexicon().Item1;
            wordRanking = GetAllWordsInLexicon().Item2;
        }

        /// <summary>
        /// Loads the sokgraph (perfect graph for word) points that have been generated for a certain keyboard layout in a file called "sokgraph_'layout'.txt".
        /// Saves the information in the locationWordPointsDict and normalizedWordPointsDict.
        /// </summary>
        /// <param name="layout">Layout from which the sokgraphs should be loaded.</param>
        async public void LoadWordGraphs(string layout) {
            await Task.Run(() => {
                isLoading = true;
                locationWordPointsDict = new Dictionary<string, List<Vector2>>();
                normalizedWordPointsDict = new Dictionary<string, List<Vector2>>();

                string path = "Packages/com.unibas.wgkeyboard/Assets/sokgraph_" + layout + ".txt";

                StreamReader sr = new StreamReader(path);
                Debug.Log("HERE IS BEGINNING OF LOADGRAPHS");
                string line;
                string[] splits;
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

                    locationWordPointsDict.Add(splits[0], locationPoints);
                    normalizedWordPointsDict.Add(splits[0], normalizedPoints);

                    normalizedPoints = new List<Vector2>();
                    locationPoints = new List<Vector2>();
                }
                isLoading = false;
            });
        }

        /// <summary>
        /// Looks, if the new word to be added already exists in the dictionary. If yes, then this function does nothing, else it writes the word in the words txt-file,
        /// adds the sokgraph points to the corresponding files and also adds these to the normalizedWordPointsDict and locationWordPointsDict.
        /// </summary>
        /// <param name="newWord">New word that should be added to the dicitonary.</param>
        public void addNewWordToDict(string newWord, GraphPointsCalculator GPC) {
            if (newWord.Length != 0 && !newWord.Contains(" ")) {
                StreamReader sr = new StreamReader(pathToLexicon);
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
                    StreamWriter sw = File.AppendText(pathToLexicon);
                    sw.WriteLine(newWord);
                    sw.Close();

                    foreach (var l in layouts) {
                        Debug.Log(l);
                        List<Vector2> wordLocationPoints = GPC.getWordPoints(newWord, layoutCompositions[l]);
                        if (wordLocationPoints == null) {
                            Debug.Log("CANT BE WRITTEN IN: " + l);
                            continue;
                        }
                        List<Vector2> wordNormPoints = GPC.normalize(wordLocationPoints, 2);

                        string path = "Packages/com.unibas.wgkeyboard/Assets/sokgraph_" + l + ".txt";
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
                            normalizedWordPointsDict.Add(newWord, wordNormPoints);
                            locationWordPointsDict.Add(newWord, wordLocationPoints);
                        }
                    }
                    wordsInLexicon.Add(newWord);
                    wordRanking[newWord] = 20000;
                }
            }

            // Calculate graph of new word and add it to the dict/list
        }

        public void addKeyboardLettersToLexicon(GraphPointsCalculator GPC) {
            HashSet<string> allCharacters = new HashSet<string>();
            foreach (string layout in layouts) {
                foreach (string l in layoutCompositions[layout].Item2) {
                    foreach (char character in l.ToLower()) {
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
            StreamReader sr = new StreamReader(pathToLexicon);
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
            StreamWriter sw = File.AppendText(pathToLexicon);
            foreach (string s in allCharacters) {
                sw.WriteLine(s);
            }
            sw.Close();

            foreach (var l in layouts) {
                string path = "Packages/com.unibas.wgkeyboard/Assets/sokgraph_" + l + ".txt";
                sw = File.AppendText(path);
                foreach (string s in allCharacters) {
                    List<Vector2> wordLocationPoints = GPC.getWordPoints(s, layoutCompositions[l]);
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
        /// It stores the layout names in "layouts" and the order of the keys with the layout name in the dict "layoutCompositions".
        /// </summary>
        void loadLayoutsOld() {
            layouts = new List<string>();
            //layoutCompositions = new Dictionary<string, List<string>>();
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
                    //layoutCompositions.Add(l, k);
                    keys = new List<string>();
                    l = "";
                    continue;
                }

                keys.Add(line);
            }
        }

        /// <summary>
        /// Loads the layouts from the file in Assets/layouts.txt.
        /// It stores the layout names in "layouts" and the order of the characters for a line with the indent for every layout in the dictionary "layoutCompositions".
        /// </summary>
        public void LoadLayouts() {
            layouts = new List<string>();
            layoutCompositions = new Dictionary<string, Tuple<List<float>, List<string>>>();
            string path = "Packages/com.unibas.wgkeyboard/Assets/layouts.txt";
            StreamReader sr = new StreamReader(path);
            string line;
            string l = "";  // layoutname
            Tuple<List<float>, List<string>> composition;
            composition = new Tuple<List<float>, List<string>>(new List<float>(), new List<string>());
            int i = 0;
            while (true) {
                line = sr.ReadLine();
                if (i < 6) {    // skip first 6 lines, because they just explain the format to be used
                    i++;
                } else if (line == null) { // end of file reached
                    break;
                } else if (l == "") {
                    l = line;
                } else if (line == "-----") {
                    HashSet<char> allCharacters = new HashSet<char>();
                    bool isIllegalLayout = false;
                    foreach (string s in composition.Item2) {
                        foreach (char character in s.ToLower()) {
                            if (character.ToString() == " " || character.ToString() == "<") {
                                continue;
                            }
                            if (allCharacters.Contains(character)) {
                                isIllegalLayout = true;
                                Debug.Log("THIS ISSSSSSSSSSSSSSS THE IMPOSTER CHARACTER!!!!!!!!: " + character);
                                goto Illegal;
                            }
                            allCharacters.Add(character);
                        }
                    }
                    Illegal:
                    if (!isIllegalLayout) { // two or more times same character in layout
                        layouts.Add(l);
                        layoutCompositions.Add(l, composition);
                        Debug.Log("JUST ADDED: " + l);
                    } 
                    composition = new Tuple<List<float>, List<string>>(new List<float>(), new List<string>());
                    l = "";
                } else {
                    string[] splits = line.Split("$$");
                    float f;
                    if (float.TryParse(splits[1], out f)) { // true, if a float, false if not
                        composition.Item1.Add(f);
                    } else {
                        composition.Item1.Add(0);
                    }
                    composition.Item2.Add(splits[0]);
                    Debug.Log("line: " + splits[0] + " : padding: " + f);
                }
            }
        }

        /// <summary>
        /// Returns a tuple containing a hashset of all words in the lexicon and a dictionary with all the words and their rank (how frequently used in language)
        /// </summary>
        /// <returns>The words in a lexicon and their rank</returns>
        Tuple<HashSet<string>, Dictionary<string, int>> GetAllWordsInLexicon() {
            HashSet<string> words = new HashSet<string>();
            Dictionary<string, int> wordsWithRank = new Dictionary<string, int>();
            string line;
            StreamReader sr = new StreamReader(pathToLexicon);
            int rank = 0;
            while (true) {
                line = sr.ReadLine();
                if (line == null) {
                    break;
                }
                words.Add(line);
                wordsWithRank[line] = rank;
                rank++;
            }
            return new Tuple<HashSet<string>, Dictionary<string, int>>(words, wordsWithRank);
        }

        public List<string> getLayouts() {
            return layouts;
        }

        public Dictionary<string, Tuple<List<float>, List<string>>> GetLayoutCompositions() {
            return layoutCompositions;
        }

        public Dictionary<string, List<Vector2>> getLocationWordPointsDict() {
            return locationWordPointsDict;
        }

        public Dictionary<string, List<Vector2>> getNormalizedWordPointsDict() {
            return normalizedWordPointsDict;
        }
    }
}
