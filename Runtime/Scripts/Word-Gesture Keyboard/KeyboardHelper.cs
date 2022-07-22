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

        public float longestKeyboardLine;
        public float keyRadius;
        public float keyboardLength;
        public float keyboardWidth;
        public float[] backSpaceHitbox = new float[4]{0,0,0,0};
        public float[] spaceHitbox = new float[4]{0,0,0,0};
        public float sigma;
        public float keyboardScale = 1;
        float keyboardKeyWidth = 0.05f;

        public KeyboardHelper(Transform t, GameObject k, BoxCollider b, FileHandler f) {
            transform = t;
            key = k;
            boxCollider = b;
        }

        /// <summary>
        /// Generates the keys for the word-gesture keyboard for the given layout and puts it on the keyboard (also determines the size of the WGKeyboard).
        /// </summary>
        /// <param name="layoutComposition">A tuple that contains two lists, one with the lines of characters and one with the lines' indents of the layout for which the keyboard should be generated</param>
        public void CreateKeyboardOverlay(Tuple<List<float>, List<string>> layoutComposition) {
            List<string> keyList = layoutComposition.Item2;
            List<float> indentList = layoutComposition.Item1;
            int count = keyList.Count;
            longestKeyboardLine = 0;
            for (int i = 0; i < count; i++) {
                float lineLength = keyList[i].Length + Mathf.Abs(indentList[i]);
                if (keyList[i].Contains(" ")) {
                    lineLength += 7;    // because length of spacebar is 8 * normal keysize, that means 7 * keysize extra
                }
                if (keyList[i].Contains("<")) {
                    lineLength += 1;    // because length of backspace is 2 * normal keysize, that means 1 * keysize extra
                }
                if (lineLength > longestKeyboardLine) {
                    longestKeyboardLine = lineLength;
                }
            }
            sigma = 1 / longestKeyboardLine / 2;    // right now key radius after transformation of x to length 1 (but could be changed)
            keyRadius = 1 / longestKeyboardLine / 2;   // key radius after transformation of x to length 1
            keyboardLength = keyboardKeyWidth * longestKeyboardLine;
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
                float offset = indentList[count - y - 1] * l;
                float offsetSpecial = 0;
                int x = 0;
                foreach (var letter in s) {
                    GameObject specificKey = GameObject.Instantiate(key) as GameObject;
                    int scale = 1;
                    offsetSpecial = 0;
                    if (letter.ToString() == "<") {
                        scale = 2;
                        offsetSpecial = keyboardKeyWidth / 2;
                        Transform borderLeft = specificKey.transform.GetChild(2);
                        Transform borderRight = specificKey.transform.GetChild(3);
                        borderLeft.localScale = new Vector3(borderLeft.localScale.x / scale, borderLeft.localScale.y, borderLeft.localScale.z);
                        borderRight.localScale = new Vector3(borderRight.localScale.x / scale, borderRight.localScale.y, borderRight.localScale.z);
                    } else if (letter.ToString() == " ") {
                        scale = 8;
                        offsetSpecial = keyboardKeyWidth * 3.5f;
                        Transform borderLeft = specificKey.transform.GetChild(2);
                        Transform borderRight = specificKey.transform.GetChild(3);
                        borderLeft.localScale = new Vector3(borderLeft.localScale.x / scale, borderLeft.localScale.y, borderLeft.localScale.z);
                        borderRight.localScale = new Vector3(borderRight.localScale.x / scale, borderRight.localScale.y, borderRight.localScale.z);
                    }
                    specificKey.transform.position = new Vector3(transform.position.x - keyboardKeyWidth * (longestKeyboardLine / 2 - x) + offset + offsetSpecial + keyboardKeyWidth / 2, transform.position.y + 0.005f, transform.position.z - keyboardKeyWidth * (count / 2.0f - y - 1) - keyboardKeyWidth / 2);
                    specificKey.transform.Find("Canvas").Find("Text").GetComponent<Text>().text = letter.ToString();
                    specificKey.transform.localScale = new Vector3(l + keyboardKeyWidth * (scale - 1), l, specificKey.transform.localScale.z);
                    specificKey.transform.SetParent(this.transform);

                    offset += offsetSpecial * 2;    // to get all the keys in right position that are behind the special-sized keys

                    x += 1;
                }
                y -= 1;
            }
            transform.parent.localScale = new Vector3(keyboardScale, keyboardScale, keyboardScale);
            transform.parent.localRotation = tempRot;
            
            Debug.Log("KEYBOARD CREATION TIME: " + (Time.realtimeSinceStartup - startTime));
        }

        /// <summary>
        /// Finds the hitboxes for the space and backspace keys if they are present in the keyboard layout and assigns it to the fields "backSpaceHitbox" and "spaceHitbox".
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
                        backSpaceHitbox[0] = (xPos - offsetSpecial + xIndent) / longestKeyboardLine;
                        backSpaceHitbox[1] = (xPos + offsetSpecial + xIndent) / longestKeyboardLine;
                        backSpaceHitbox[2] = (yPos - 0.5f) / longestKeyboardLine;
                        backSpaceHitbox[3] = (yPos + 0.5f) / longestKeyboardLine;
                        xIndent += scale - 1;
                    } else if (layoutComposition.Item2[count - y - 1][x].ToString() == " ") {
                        spaceFound = true;
                        int scale = 8;
                        float offset = layoutComposition.Item1[count - y - 1];
                        float offsetSpecial = scale / 2;
                        float xPos = x + offset + offsetSpecial;
                        float yPos = y + 0.5f;
                        spaceHitbox[0] = (xPos - offsetSpecial + xIndent) / longestKeyboardLine;
                        spaceHitbox[1] = (xPos + offsetSpecial + xIndent) / longestKeyboardLine;
                        spaceHitbox[2] = (yPos - 0.5f) / longestKeyboardLine;
                        spaceHitbox[3] = (yPos + 0.5f) / longestKeyboardLine;
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