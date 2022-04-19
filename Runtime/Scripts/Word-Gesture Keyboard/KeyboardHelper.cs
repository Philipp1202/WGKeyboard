using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WordGestureKeyboard {
    public class KeyboardHelper {

        GameObject Key;
        Transform transform;
        BoxCollider boxCollider;
        FileHandler FH;

        public float numKeysOnLongestLine;
        public float keyRadius;
        public float keyboardLength;
        public float keyboardWidth;
        public float[] backSpaceHitbox = new float[4];
        public float[] spaceHitbox = new float[4];
        public float delta;

        public KeyboardHelper(Transform t, GameObject K, BoxCollider b, FileHandler f) {
            transform = t;
            Key = K;
            boxCollider = b;
            FH = f;
        }

        /// <summary>
        /// Generates the keys for the word-gesture keyboard for the given layout and puts in on the keyboard.
        /// </summary>
        /// <param name="layout">Keyboard layout to be generated.</param>
        public void createKeyboardOverlay(string layout) { // implementation does not work, if first row is not the longest (has most characters) (first.length > allOther.length)
            List<string> keyList = FH.layoutKeys[layout];
            int count = keyList.Count;
            for (int i = 0; i < count; i++) {
                if (keyList[i].Length > numKeysOnLongestLine) {
                    numKeysOnLongestLine = keyList[i].Length;
                }
            }
            delta = 1 / numKeysOnLongestLine;
            keyRadius = 1 / numKeysOnLongestLine / 2;   // keyradius after transformation of x to length 1
            transform.localScale = new Vector3(0.05f * numKeysOnLongestLine, 0.05f * count, transform.localScale.z); // keyboard gets bigger if more keys on one line, but keys always have the same size
            keyboardLength = transform.localScale.x;
            keyboardWidth = transform.localScale.y;
            boxCollider.size = new Vector3(keyboardLength, 0.05f, keyboardWidth);
            //boxCollider.center = new Vector3(boxCollider.center.x, 0.03f, boxCollider.center.z);

            float startTime = Time.realtimeSinceStartup;
            Quaternion tempRot = transform.parent.localRotation;
            transform.parent.localRotation = new Quaternion(0, 0, 0, 0);    // set to 0,0,0,0, because otherwise could lead to wrong positions and rotations for keys, when attachd to keyboard
            float l = keyboardLength / (numKeysOnLongestLine + 1.5f);
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
                    GameObject specificKey = GameObject.Instantiate(Key) as GameObject;
                    int scale = 1;
                    if (letter.ToString() == "<") {
                        scale = 2;
                        o += keyRadius * keyboardLength;
                        float xPos = x / numKeysOnLongestLine + o / keyboardLength;
                        float yPos = y / numKeysOnLongestLine + keyRadius;
                        backSpaceHitbox[0] = xPos - scale * 1 / (numKeysOnLongestLine + 1.5f) / 2 + keyRadius;
                        backSpaceHitbox[1] = xPos + scale * 1 / (numKeysOnLongestLine + 1.5f) / 2 + keyRadius;
                        backSpaceHitbox[2] = yPos - keyRadius;
                        backSpaceHitbox[3] = yPos + keyRadius;
                    } else if (letter.ToString() == " ") {
                        scale = 8;
                        float xPos = x / numKeysOnLongestLine + o / keyboardLength;
                        float yPos = y / numKeysOnLongestLine + keyRadius;
                        spaceHitbox[0] = xPos - scale / 2 * l / keyboardLength + keyRadius;
                        spaceHitbox[1] = xPos + scale / 2 * l / keyboardLength + keyRadius;
                        spaceHitbox[2] = yPos - keyRadius;
                        spaceHitbox[3] = yPos + keyRadius;
                        Debug.Log("XPOS: " + xPos + ":" + o / keyboardLength + " : YPOS: " + yPos);
                        Debug.Log(spaceHitbox[0] + " " + spaceHitbox[1] + " " + spaceHitbox[2] + " " + spaceHitbox[3]);
                    }
                    specificKey.transform.position = new Vector3(transform.position.x + (-keyboardLength / numKeysOnLongestLine) * ((numKeysOnLongestLine) / 2 - x) + o + keyRadius * keyboardLength, transform.position.y + 0.005f, transform.position.z - (-keyboardWidth / count) * (-count / 2.0f + y + 1) - keyRadius * keyboardLength);
                    specificKey.transform.Find("Canvas").Find("Text").GetComponent<Text>().text = letter.ToString();
                    specificKey.transform.localScale = new Vector3(l * scale, l, specificKey.transform.localScale.z);


                    // for next 4 lines: didn't have a better idea but to make roation of transform.parent object to 0,0,0 (if it's not 0,0,0, then something strange happens, when setting this.transform as parent
                    specificKey.transform.SetParent(this.transform);


                    x += 1;
                }
                y -= 1;
            }
            transform.parent.localRotation = tempRot;
            Debug.Log("KEYBOARD CREATION TIME: " + (Time.realtimeSinceStartup - startTime));
        }

        /// <summary>
        /// Changes the color of the keyboard.
        /// </summary>
        /// <param name="mat">The color the keyboard should be changed to.</param>
        public void changeColorKeyboard(Material mat) {
            transform.GetComponent<MeshRenderer>().material = mat;
        }
    }
}