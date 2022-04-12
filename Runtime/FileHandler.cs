using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.IO;

public class FileHandler {
    Dictionary<string, List<Vector2>> locationWordsPointsDict = null;
    Dictionary<string, List<Vector2>> normalizedWordsPointsDict = null;
    string layout;
    public List<string> layouts;
    public Dictionary<string, List<string>> layoutKeys;

    public FileHandler(string layout) {
        this.layout = layout;
        loadLayouts();
        loadWordGraphs(layout);
    }
    // loads the sokgraphs that have been generated for a certain keyboard layout in a file called "sokgraph_'layout'.txt"
    async public void loadWordGraphs(string layout) {
        await Task.Run(() => {
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
        });
    }

    public void addNewWordToDict(string newWord, GraphPointsCalculator GPC) {
        if (newWord.Length != 0) {
            string path = "Packages/com.unibas.wgkeyboard/Assets/10000_english_words.txt";
            StreamReader sr = new StreamReader(path);
            bool isIn = false;
            string line;
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
            sr.Close();
            if (!isIn) {
                StreamWriter sw = File.AppendText(path);
                sw.WriteLine(newWord);
                sw.Close();

                foreach (var l in layouts) {
                    List<Vector2> wordLocationPoints = GPC.getWordPoints(newWord, l, layoutKeys);
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

    void loadLayouts() {
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
                layoutKeys.Add(l, k);
                keys = new List<string>();
                l = "";
                continue;
            }

            keys.Add(line);
        }
    }

    public List<string> getLayouts() {
        return layouts;
    }

    public Dictionary<string, List<string>> getLayoutKeys() {
        return layoutKeys;
    }

    public Dictionary<string, List<Vector2>> getLocationWordsPointsDict() {
        return locationWordsPointsDict;
    }

    public Dictionary<string, List<Vector2>> getNormalizedWordsPointsDict() {
        return normalizedWordsPointsDict;
    }
}
