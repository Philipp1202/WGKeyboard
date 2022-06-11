using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

namespace WordGestureKeyboard {
    public class EvaluationManager : MonoBehaviour {
        public AudioSource successSound;
        public Material grayMat;
        Text testPhrase;
        Text userPhrase;
        Text phraseNumber;
        GameObject startButton;
        Transform wpmText;
        GameObject wpmBackground;
        public int startingPosition;
        bool hasStarted = false;
        int position;
        int nrPhrase = 0;
        List<string> phrases;
        HashSet<string> wordList = new HashSet<string>();
        float wordsPerMinute;
        float gesturesPerMinute;
        float startTime;
        float phraseStartTime;
        float nrWords = 0;
        float nrWordsPhrase = 0;
        float nrCharacters = 0;
        float nrCharactersPhrase = 0;
        public int nrBackspaces = 0;
        // Start is called before the first frame update
        void Start() {
            position = startingPosition;
            phrases = getPhrases();
            testPhrase = transform.GetChild(0).Find("EvaluationPhrase").GetComponent<Text>();
            userPhrase = transform.GetChild(0).Find("UserInputPhrase").GetComponent<Text>();
            phraseNumber = transform.GetChild(0).Find("PhraseNumber").GetComponent<Text>();
            startButton = transform.Find("StartButton").gameObject;
            wpmText = transform.GetChild(0).Find("WPM");
            wpmBackground = transform.GetChild(0).Find("WPMBackground").gameObject;

            wpmText.gameObject.SetActive(false);
            wpmBackground.SetActive(false);

            string path = "Packages/com.unibas.wgkeyboard/Assets/10000_english_words.txt";
            StreamReader sr = new StreamReader(path);
            string line;
            while (true) {
                line = sr.ReadLine();
                wordList.Add(line);
                if (line == null) {
                    break;
                }
            }
            sr.Close();

            StreamWriter sw = File.AppendText(path);
            foreach (string phrase in phrases) {
                if (phrase == "" || phrase == null) {
                    continue;
                }
                string[] words = phrase.Split(' ');
                foreach (string word in words) {
                    if (!wordList.Contains(word)) {
                        sw.WriteLine(word);
                    }
                }
            }
            sw.Close();
        }

        // Update is called once per frame
        void Update() {
            if (hasStarted) {
                if (testPhrase.text == userPhrase.text || testPhrase.text + " " == userPhrase.text) {
                    successSound.Play();
                    if (nrBackspaces > 0) {
                        writeStatistics(position.ToString() + ": " + testPhrase.text + ": nr of Backspaces: " + nrBackspaces);
                        print(position.ToString() + ": " + testPhrase.text + ": nr of Backspaces: " + nrBackspaces);
                        nrBackspaces = 0;
                    }
                    wordsPerMinute = (nrCharactersPhrase / 5) / ((Time.realtimeSinceStartup - phraseStartTime) / 60);
                    gesturesPerMinute = nrWordsPhrase / ((Time.realtimeSinceStartup - phraseStartTime) / 60);
                    writeStatistics("PHRASENR: " + position + ", WPM: " + wordsPerMinute.ToString() + ", GMP " + gesturesPerMinute.ToString() + ", TIME: " + (Time.realtimeSinceStartup - phraseStartTime) + ", Nr of characters: " + nrCharactersPhrase + ", Nr of words: " + nrWordsPhrase);
                    phraseStartTime = Time.realtimeSinceStartup;
                    if (nrPhrase >= 3) {
                        testPhrase.text = "Thank you for participating!";
                        wordsPerMinute = (nrCharacters / 5) / ((Time.realtimeSinceStartup - startTime) / 60);
                        gesturesPerMinute = nrWords / ((Time.realtimeSinceStartup - startTime) / 60);
                        wpmBackground.SetActive(true);
                        wpmText.gameObject.SetActive(true);
                        wpmText.transform.GetComponent<Text>().text = "wpm: " + System.Math.Round(wordsPerMinute, 3).ToString() + "\ngpm: " + System.Math.Round(gesturesPerMinute, 3).ToString();
                        print("WPM: " + wordsPerMinute.ToString() + " GMP " + gesturesPerMinute.ToString());
                        print("TIME NEEDED: " + (Time.realtimeSinceStartup - startTime).ToString());
                        print("NUMBER OF CHARACTERS: " + nrCharacters);
                        print("NUMBER OF GESTURES: " + nrWords);
                        writeStatistics("Nr of characters: " + nrCharacters + " : Nr of words: " + nrWords + " : Time: " + (Time.realtimeSinceStartup - startTime).ToString());
                        hasStarted = false;
                    } else {
                        nrPhrase += 1;
                        position += 1;
                        if (position >= 500) {
                            position = 0;
                        }
                        testPhrase.text = phrases[position];
                        phraseNumber.text = (nrPhrase + 1).ToString() + " / 15";
                        userPhrase.text = "";
                        nrCharacters += phrases[position].Length;
                        nrCharactersPhrase = phrases[position].Length;
                        string[] temp = phrases[position].Split(' ');
                        nrWords += temp.Length;
                        nrWordsPhrase = temp.Length;
                    }
                }
            }
        }

        List<string> getPhrases() {

            string path = "Packages/com.unibas.wgkeyboard/Assets/evaluation_phrases.txt";
            StreamReader sr = new StreamReader(path);
            List<string> phrases = new List<string>();
            string line;
            while (true) {
                line = sr.ReadLine();
                phrases.Add(line);
                if (line == null) {
                    break;
                }
            }
            sr.Close();
            return phrases;
        }

        public void startEvaluation(Transform t, bool b) {
            if (!b) {
                hasStarted = true;
                testPhrase.text = phrases[position];
                userPhrase.text = "";
                startTime = Time.realtimeSinceStartup;
                phraseStartTime = Time.realtimeSinceStartup;
                startButton.SetActive(false);

                phraseNumber.text = "1 / 15";
                nrCharacters += phrases[position].Length;
                nrCharactersPhrase = phrases[position].Length;
                print(nrCharacters);
                string[] temp = phrases[position].Split(' ');
                nrWords += temp.Length;
                nrWordsPhrase = temp.Length;
                print(nrWords);
                nrBackspaces = 0;
            } else {
                startButton.GetComponent<MeshRenderer>().material = grayMat;
            }
        }

        void writeStatistics(string data) {
            string path = "Packages/com.unibas.wgkeyboard/Assets/Evaluation_Analytics.txt";

            StreamWriter sw = File.AppendText(path);
            sw.WriteLine(data);
            sw.Close();
        }
    }
}