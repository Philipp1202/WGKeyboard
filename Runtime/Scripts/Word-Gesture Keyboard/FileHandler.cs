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
        /// Loads the points for the perfect graphs for the words that have been generated for a certain keyboard layout in a file called "graph_'layout'.txt".
        /// Saves the information in the locationWordPointsDict and normalizedWordPointsDict.
        /// </summary>
        /// <param name="layout">Layout from which the graphs should be loaded</param>
        async public void LoadWordGraphs(string layout) {
            await Task.Run(() => {
                locationWordPointsDict = new Dictionary<string, List<Vector2>>();
                normalizedWordPointsDict = new Dictionary<string, List<Vector2>>();

                string path = "Packages/com.unibas.wgkeyboard/Assets/Graph_Files/graph_" + layout + ".txt";

                StreamReader sr = new StreamReader(path);
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
        /// Checks if the word "newWord" already exists in the dictionary. If true, then this function does nothing, else it writes the word in the lexicon file and
        /// adds the graph points to the corresponding files. Additionally, it also adds these to the normalizedWordPointsDict and locationWordPointsDict, such that they can be immediately used with the active layout.
        /// </summary>
        /// <param name="newWord">New word that should be added to the dicitonary.</param>
        public void AddNewWordToDict(string newWord, GraphPointsCalculator GPC) {
            if (newWord.Length != 0 && !newWord.Contains(" ") && !wordsInLexicon.Contains(newWord)) {
                StreamWriter sw = File.AppendText(pathToLexicon);
                sw.WriteLine(newWord);
                sw.Close();

                foreach (var l in layouts) {
                    List<Vector2> wordLocationPoints = GPC.GetWordPoints(newWord, layoutCompositions[l]);
                    if (wordLocationPoints == null) {
                        continue;   // word cannot be written with given layout
                    }
                    List<Vector2> wordNormPoints = GPC.Normalize(wordLocationPoints, 2);

                    string path = "Packages/com.unibas.wgkeyboard/Assets/Graph_Files/graph_" + l + ".txt";
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
                        if (i < wordNormPoints.Count - 1) {
                            newLine += ",";
                        }
                    }
                    sw.WriteLine(newLine);
                    sw.Close();

                    if (l == this.layout) { // add word and points to these two dictionaries so that this word can be written as gesture with the active layout without reloading
                        normalizedWordPointsDict.Add(newWord, wordNormPoints);
                        locationWordPointsDict.Add(newWord, wordLocationPoints);
                    }
                }
                wordsInLexicon.Add(newWord);
                wordRanking[newWord] = 100;
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
                    foreach (string s in composition.Item2) {
                        foreach (char character in s.ToLower()) {
                            if (character.ToString() == " " || character.ToString() == "<") {
                                continue;
                            }
                            if (allCharacters.Contains(character)) {
                                goto Illegal;
                            }
                            allCharacters.Add(character);
                        }
                    }

                    float longestKeyboardLine = 0;
                    for (int j = 0; j < composition.Item2.Count; j++) {
                        float lineLength = composition.Item2[j].Length + Mathf.Abs(composition.Item1[j]);
                        if (composition.Item2[j].Contains(" ")) {
                            lineLength += 7;    // because length of spacebar is 8 * normal keysize, that means 7 * keysize extra
                        }
                        if (composition.Item2[j].Contains("<")) {
                            lineLength += 1;    // because length of backspace is 2 * normal keysize, that means 1 * keysize extra
                        }
                        if (lineLength > longestKeyboardLine) {
                            longestKeyboardLine = lineLength;
                        }
                    }
                    bool isWiderThanHigh = longestKeyboardLine >= composition.Item2.Count;
                    
                    if (File.Exists("Packages/com.unibas.wgkeyboard/Assets/Graph_Files/graph_" + l + ".txt") && isWiderThanHigh) { // graph file does not exist / layout is higher than wide
                        layouts.Add(l);
                        layoutCompositions.Add(l, composition);
                    }
                    Illegal:
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

        public List<string> GetLayouts() {
            return layouts;
        }

        public Dictionary<string, Tuple<List<float>, List<string>>> GetLayoutCompositions() {
            return layoutCompositions;
        }

        public Dictionary<string, List<Vector2>> GetLocationWordPointsDict() {
            return locationWordPointsDict;
        }

        public Dictionary<string, List<Vector2>> GetNormalizedWordPointsDict() {
            return normalizedWordPointsDict;
        }
    }
}
