using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

namespace WordGestureKeyboard {
    public class KeyboardHelper {

        GameObject key;
        Transform transform;
        BoxCollider boxCollider;
        FileHandler FH;

        public float numKeysOnLongestLine;
        public float keyRadius;
        public float keyboardLength;
        public float keyboardWidth;
        public float[] backSpaceHitbox = new float[4]{0,0,0,0};
        public float[] spaceHitbox = new float[4]{0,0,0,0};
        public float delta;
        public float keyboardScale = 1;
        float keyboardKeyWidth = 0.05f;

        public KeyboardHelper(Transform t, GameObject k, BoxCollider b, FileHandler f) {
            transform = t;
            key = k;
            boxCollider = b;
            FH = f;
        }

        /// <summary>
        /// Generates the keys for the word-gesture keyboard for the given layout and puts in on the keyboard.
        /// </summary>
        /// <param name="layout">Keyboard layout to be generated.</param>
        public void createKeyboardOverlayOld(string layout) { // implementation does not work, if first row is not the longest (has most characters) (first.length > allOther.length)
            List<string> keyList = FH.layoutCompositions[layout].Item2;
            List<float> paddingList = FH.layoutCompositions[layout].Item1;
            int count = keyList.Count;
            numKeysOnLongestLine = 0;
            for (int i = 0; i < count; i++) {
                if (keyList[i].Length > numKeysOnLongestLine) {
                    numKeysOnLongestLine = keyList[i].Length;
                }
            }
            delta = 1 / numKeysOnLongestLine;
            keyRadius = 1 / numKeysOnLongestLine / 2;   // keyradius after transformation of x to length 1
            transform.localScale = new Vector3(0.05f * numKeysOnLongestLine * keyboardScale, 0.05f * count * keyboardScale, transform.localScale.z); // keyboard gets bigger if more keys on one line, but keys always have the same size
            keyboardLength = transform.localScale.x;
            keyboardWidth = transform.localScale.y;
            boxCollider.size = new Vector3(keyboardLength, 0.05f, keyboardWidth);
            //boxCollider.center = new Vector3(boxCollider.center.x, 0.03f, boxCollider.center.z);

            float startTime = Time.realtimeSinceStartup;
            Quaternion tempRot = transform.parent.localRotation;
            transform.parent.localRotation = new Quaternion(0, 0, 0, 0);    // set to 0,0,0,0, because otherwise could lead to wrong positions and rotations for keys, when attached to keyboard
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
                    GameObject specificKey = GameObject.Instantiate(key) as GameObject;
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
        /// Generates the keys for the word-gesture keyboard for the given layout and puts in on the keyboard.
        /// </summary>
        /// <param name="layout">Keyboard layout to be generated.</param>
        public void createKeyboardOverlay(string layout) {
            List<string> keyList = FH.layoutCompositions[layout].Item2;
            List<float> indentList = FH.layoutCompositions[layout].Item1;
            int count = keyList.Count;
            numKeysOnLongestLine = 0;
            for (int i = 0; i < count; i++) {
                float lineLength = keyList[i].Length + Mathf.Abs(indentList[i]);
                if (keyList[i].Contains(" ")) {
                    lineLength += 7;    // because length of spacebar is 8 * normal keysize, that means 7 * keysize extra
                }
                if (keyList[i].Contains("<")) {
                    lineLength += 1;    // because length of backspace is 2 * normal keysize, that means 1 * keysize extra
                }
                if (lineLength > numKeysOnLongestLine) {
                    numKeysOnLongestLine = lineLength;
                }
            }
            Debug.Log("numkeysonlongestline: " + numKeysOnLongestLine);
            delta = 1 / numKeysOnLongestLine;
            keyRadius = 1 / numKeysOnLongestLine / 2;   // keyradius after transformation of x to length 1
            keyboardLength = keyboardKeyWidth * numKeysOnLongestLine;
            keyboardWidth = keyboardKeyWidth * count;
            transform.localScale = new Vector3(keyboardLength, keyboardWidth, transform.localScale.z); // keyboard gets bigger if more keys on one line, but keys always have the same size
            boxCollider.size = new Vector3(keyboardLength, 0.05f, keyboardWidth);

            float startTime = Time.realtimeSinceStartup;
            Quaternion tempRot = transform.parent.localRotation;
            transform.parent.localRotation = new Quaternion(0, 0, 0, 0);    // set to 0,0,0,0, because otherwise could lead to wrong positions and rotations for keys, when attached to keyboard
            transform.parent.localScale = new Vector3(1, 1, 1);
            float l = keyboardKeyWidth / 1.1f;
            int y = count - 1;
            foreach (string s in keyList) {
                float offset = indentList[count - y - 1] * keyboardKeyWidth;
                float offsetSpecial = 0;
                int x = 0;
                foreach (var letter in s) {
                    GameObject specificKey = GameObject.Instantiate(key) as GameObject;
                    int scale = 1;
                    offsetSpecial = 0;
                    if (letter.ToString() == "<") {
                        scale = 2;
                        offsetSpecial = keyRadius * keyboardLength;
                        Transform borderLeft = specificKey.transform.GetChild(2);
                        Transform borderRight = specificKey.transform.GetChild(3);
                        borderLeft.localScale = new Vector3(borderLeft.localScale.x / scale, borderLeft.localScale.y, borderLeft.localScale.z);
                        borderRight.localScale = new Vector3(borderRight.localScale.x / scale, borderRight.localScale.y, borderRight.localScale.z);
                    } else if (letter.ToString() == " ") {
                        scale = 8;
                        offsetSpecial = keyRadius * keyboardLength * 8 - keyRadius * keyboardLength;
                        Transform borderLeft = specificKey.transform.GetChild(2);
                        Transform borderRight = specificKey.transform.GetChild(3);
                        borderLeft.localScale = new Vector3(borderLeft.localScale.x / scale, borderLeft.localScale.y, borderLeft.localScale.z);
                        borderRight.localScale = new Vector3(borderRight.localScale.x / scale, borderRight.localScale.y, borderRight.localScale.z);
                    }
                    specificKey.transform.position = new Vector3(transform.position.x + (-keyboardKeyWidth) * ((numKeysOnLongestLine) / 2 - x) + offset + offsetSpecial + keyboardKeyWidth / 2, transform.position.y + 0.005f, transform.position.z + (-keyboardKeyWidth) * (count / 2.0f - y - 1) - keyboardKeyWidth / 2);
                    specificKey.transform.Find("Canvas").Find("Text").GetComponent<Text>().text = letter.ToString();
                    specificKey.transform.localScale = new Vector3(l + keyboardKeyWidth * (scale - 1), l, specificKey.transform.localScale.z);
                    offset += offsetSpecial * 2;    // to get all the keys in right position that are behind the special-sized keys

                    
                    specificKey.transform.SetParent(this.transform);

                    x += 1;
                }
                y -= 1;
            }
            transform.parent.localScale = new Vector3(keyboardScale, keyboardScale, keyboardScale);
            transform.parent.localRotation = tempRot;
            
            Debug.Log("KEYBOARD CREATION TIME: " + (Time.realtimeSinceStartup - startTime));
        }

        /// <summary>
        /// Finds the hitboxes for the space and backspace keys if they are present in the keyboard layout.
        /// </summary>
        /// <param name="layoutComposition">The composition of the layout for which the space and backspace hitboxes have to be found.</param>
        public void MakeSpaceAndBackspaceHitbox(Tuple<List<float>, List<string>> layoutComposition) {
            bool backspaceFound = false;
            bool spaceFound = false;
            int xIndent;    // needed if space and backspace are on the same line, have to consider that they have a bigger size than 1
            int count = layoutComposition.Item2.Count;
            for (int y = 0; y < count; y++) {
                xIndent = 0;
                for (int x = 0; x < layoutComposition.Item2[count - y - 1].Length; x++) {
                    if (layoutComposition.Item2[count - y - 1][x].ToString() == "<") {
                        backspaceFound = true;
                        int scale = 2;
                        float offset = layoutComposition.Item1[count - y - 1];
                        float offsetSpecial = scale / 2;
                        float xPos = x + offset + offsetSpecial;
                        float yPos = y + 0.5f;
                        backSpaceHitbox[0] = (xPos - offsetSpecial + xIndent) / numKeysOnLongestLine;
                        backSpaceHitbox[1] = (xPos + offsetSpecial + xIndent) / numKeysOnLongestLine;
                        backSpaceHitbox[2] = (yPos - 0.5f) / numKeysOnLongestLine;
                        backSpaceHitbox[3] = (yPos + 0.5f) / numKeysOnLongestLine;
                        xIndent += scale - 1;
                    } else if (layoutComposition.Item2[count - y - 1][x].ToString() == " ") {
                        spaceFound = true;
                        int scale = 8;
                        float offset = layoutComposition.Item1[count - y - 1];
                        float offsetSpecial = scale / 2;
                        float xPos = x + offset + offsetSpecial;
                        float yPos = y + 0.5f;
                        spaceHitbox[0] = (xPos - offsetSpecial + xIndent) / numKeysOnLongestLine;
                        spaceHitbox[1] = (xPos + offsetSpecial + xIndent) / numKeysOnLongestLine;
                        spaceHitbox[2] = (yPos - 0.5f) / numKeysOnLongestLine;
                        spaceHitbox[3] = (yPos + 0.5f) / numKeysOnLongestLine;
                        xIndent += scale - 1;
                    }
                }
            }

            if (!backspaceFound) {
                backSpaceHitbox[0] = 0;
                backSpaceHitbox[1] = 0;
                backSpaceHitbox[2] = 0;
                backSpaceHitbox[3] = 0;
            }

            if (!spaceFound) {
                spaceHitbox[0] = 0;
                spaceHitbox[1] = 0;
                spaceHitbox[2] = 0;
                spaceHitbox[3] = 0;
            }
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