using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

using UnityEngine.UI;
using System.IO;
using System;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;

namespace WordGestureKeyboard {
    public class WGKMain : MonoBehaviour {

        [Serializable]
        public class TextInputEvent : UnityEvent<string> {
        }

        [Serializable]
        public class WordDeleteEvent : UnityEvent {
        }

        public MaterialHolder materials;
        public AudioSource wordInputSound;
        public GameObject key;
        public GameObject layoutKey;
        public string startingLayout;
        public TextInputEvent result;
        public WordDeleteEvent deleteEvent;
        public GameObject optionObjects;
        public GameObject chooseObjects;
        public GameObject optionsKey;
        public GameObject addKey;
        public GameObject layoutsObjects;
        public GameObject scaleObjects;
        public GameObject previewWord;

        List<GameObject> subOptionButtons = new List<GameObject>();

        BoxCollider boxCollider;
        Text keyboardText;
        LineRenderer LR;

        UserInputHandler UIH;
        GraphPointsCalculator GPC;
        FileHandler FH;
        KeyboardHelper KH;

        Material whiteMat;
        Material grayMat;
        Material keyboardMat;

        bool isWriting = false;
        string lastInputWord = "";
        Transform controller = null;

        bool notEnded = false;
        bool isOptionsOpen = false;
        bool isAddingNewWord = false;
        bool isChoosingLayout = false;
        bool isChangeSizeOpen = false;
        bool generatedLayoutKeys = false;

        float startTime = 0;

        // Start is called before the first frame update
        void Start() {
            startTime = Time.realtimeSinceStartup;

            whiteMat = materials.whiteMat;
            grayMat = materials.grayMat;
            keyboardMat = materials.keyboardMat;

            subOptionButtons.Add(optionObjects);
            subOptionButtons.Add(addKey);
            subOptionButtons.Add(layoutsObjects);
            subOptionButtons.Add(scaleObjects);

            boxCollider = transform.parent.GetComponent<BoxCollider>();
            keyboardText = transform.parent.Find("Canvas").GetChild(0).GetComponent<Text>();
            LR = GetComponent<LineRenderer>();
            LR.numCapVertices = 5;
            LR.numCornerVertices = 5;

            if (startingLayout == "") { // if user didn't specify another layout, the standard qwertz layout will be used
                startingLayout = "qwertz";
            }

            FH = new FileHandler(startingLayout);
            FH.LoadLayouts();
            KH = new KeyboardHelper(this.transform, key, boxCollider, FH);
            KH.CreateKeyboardOverlay(FH.GetLayoutCompositions()[startingLayout]);
            UIH = new UserInputHandler(LR, this.transform);
            GPC = new GraphPointsCalculator();
            
            FH.LoadWordGraphs(startingLayout);
            KH.MakeSpaceAndBackspaceHitbox(FH.GetLayoutCompositions()[startingLayout]);

            UpdateObjectPositions();

            print("Time needed for startup: " + (Time.realtimeSinceStartup - startTime));
        }

        void Update() {
            if (isWriting) {
                if (!UIH.GetIsSamplingPoints()) {
                    Vector3 hitPoint = UIH.GetHitPoint(controller.position);
                    if (UIH.CheckPoint(hitPoint)) {
                        UIH.SamplePoints(hitPoint);
                    }
                }
                if(!GPC.isCalculatingPreview) {
                    previewWord.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<Text>().text = GPC.bestWord;
                    List<Vector2> pointsList = UIH.GetTransformedPoints(false);
                    if (pointsList.Count != 0) {
                        if (GPC.IsBackSpaceOrSpace(pointsList, KH.backSpaceHitbox, KH.spaceHitbox) == 0) {
                            GPC.CalcBestWords(pointsList, FH.GetLocationWordPointsDict(), FH.GetNormalizedWordPointsDict(), KH.sigma, KH.keyRadius, FH.wordRanking, true);
                            //Task.Run(async () => await GPC.calcBestWords(pointsList, FH.GetLocationWordPointsDict(), FH.GetNormalizedWordPointsDict(), KH.sigma, KH.keyRadius, FH.wordRanking, true));
                        } else {
                            GPC.bestWord = "";
                            previewWord.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<Text>().text = "";
                        }
                    }
                }
            } else if (!UIH.GetIsSamplingPoints() && notEnded) {   // these two bools tell us, whether the calculation has ended and notEnded tells us, if the user input has been further processed
                List<Vector2> pointsList = UIH.GetTransformedPoints(true);
                if (pointsList.Count != 0) {    // user pressed trigger button, but somehow it didn't register any points
                    if (GPC.IsBackSpaceOrSpace(pointsList, KH.backSpaceHitbox, KH.spaceHitbox) == -1) {
                        wordInputSound.Play();
                        if (isAddingNewWord) {
                            if (keyboardText.text != "") {
                                keyboardText.text = keyboardText.text.Substring(0, keyboardText.text.Length - 1);
                            }
                        } else {
                            deleteEvent.Invoke();
                        }
                        GPC.bestWord = "";
                        previewWord.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<Text>().text = "";
                        SetChooseObjectsFalse();
                    } else if (GPC.IsBackSpaceOrSpace(pointsList, KH.backSpaceHitbox, KH.spaceHitbox) == 1) {
                        if (!isAddingNewWord) {
                            wordInputSound.Play();
                            result.Invoke(" ");
                            SetChooseObjectsFalse();
                        }
                        GPC.bestWord = "";
                        previewWord.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<Text>().text = "";
                    } else {
                        GPC.CalcBestWords(pointsList, FH.GetLocationWordPointsDict(), FH.GetNormalizedWordPointsDict(), KH.sigma, KH.keyRadius, FH.wordRanking, false);
                    }
                    notEnded = false;
                }
            } else if (GPC.sortedDict != null && GPC.sortedDict.Count != 0) {
                SetChooseObjectsFalse();
                string word = GPC.sortedDict[0];
                previewWord.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<Text>().text = word;
                if (word != "") {
                    wordInputSound.Play();
                    if (isAddingNewWord) {  // putting text into textfield from keyboard
                        keyboardText.text += GPC.sortedDict[0];
                    } else {    // putting text into inputfield of query
                        for (int i = 0; i < GPC.sortedDict.Count - 1; i++) {
                            if (i > 3) {    // maximum of 4 best words apart from top word
                                break;
                            }
                            chooseObjects.transform.GetChild(i).GetChild(0).GetChild(0).GetComponent<Text>().text = GPC.sortedDict[i + 1];
                            chooseObjects.transform.GetChild(i).gameObject.SetActive(true);
                        }
                        lastInputWord = GPC.sortedDict[0];
                        result.Invoke(GPC.sortedDict[0]);
                    }
                } else {
                    UnityEngine.Debug.Log("no word found");
                }
                GPC.sortedDict = null;
            }
        }

        /// <summary>
        /// Updates the positions of all objects of the WGKeyboard (especially needed if layout changes)
        /// </summary>
        void UpdateObjectPositions() {
            optionsKey.transform.localPosition = new Vector3(0.5f * KH.keyboardLength + optionsKey.transform.localScale.x * 0.5f + 0.005f, 0, transform.localScale.y * 0.5f - optionsKey.transform.localScale.z * 0.5f);
            optionObjects.transform.localPosition = new Vector3(0.5f * KH.keyboardLength + optionObjects.transform.localScale.x * 0.5f + 0.005f, optionObjects.transform.localPosition.y, optionsKey.transform.localPosition.z + (MathF.Sin((90 - 360 + optionObjects.transform.localEulerAngles.x) * Mathf.PI / 180)) * (optionObjects.transform.localScale.z * 3));//optionObjects.transform.localPosition.z + transform.localScale.y * 0.5f - optionsKey.transform.localScale.z * 0.5f);
            addKey.transform.localPosition = new Vector3(0.5f * KH.keyboardLength + addKey.transform.localScale.x * 0.5f + 0.005f, 0, -transform.localScale.y * 0.5f + addKey.transform.localScale.z * 0.5f);
            previewWord.transform.localPosition = new Vector3(0, previewWord.transform.localPosition.y, transform.localScale.y * 0.5f + previewWord.transform.localScale.z * 0.5f * 1.1f + 0.001f);
            chooseObjects.transform.localPosition = new Vector3(0, chooseObjects.transform.localPosition.y, previewWord.transform.localPosition.z + previewWord.transform.localScale.z * 0.5f + chooseObjects.transform.localScale.y * 0.35f);
            UnityEngine.Debug.Log("addPos: " + addKey.transform.localPosition + ", scalePos: " + addKey.transform.localScale + ", optsPos: " + optionsKey.transform.localPosition + ", optsScale" + optionsKey.transform.localScale);
            if (((addKey.transform.localPosition + (addKey.transform.localScale * 0.5f)) - (optionsKey.transform.localPosition - (optionsKey.transform.localScale * 0.5f))).z >= 0) {
                addKey.transform.localPosition = new Vector3(addKey.transform.localPosition.x, addKey.transform.localPosition.y, optionsKey.transform.localPosition.z - (optionsKey.transform.localScale.z * 0.5f) - (addKey.transform.localScale.z * 0.5f) - 0.02f);
            }
        }

        /// <summary>
        /// Used if user interacts with keyboard with the correct controller button.
        /// If they press said button, the WGKeyboard is set in "drawing mode" which means, it modifies the hitbox of the WGKeyboard itself and activates the character keys' hitboxes.
        /// </summary>
        /// <param name="t">Transform that interacts with WGKeyboard's hitbox</param>
        /// <param name="b">Boolean that indicates if the controller button is pressed (true) or released (false)</param>
        public void DrawWord(Transform t, bool b) {
            if (b) {
                SetChooseObjectsFalse();
                isWriting = true;
                controller = t;
                boxCollider.center = new Vector3(boxCollider.center.x, 0.007f, boxCollider.center.z);
                boxCollider.size = new Vector3(boxCollider.size.x, 0.001f, boxCollider.size.z);
                foreach (Transform child in this.transform) {
                    child.GetComponent<BoxCollider>().isTrigger = true;
                }
                transform.GetComponent<MeshRenderer>().material = whiteMat;
            } else {
                isWriting = false;
                notEnded = true;
                boxCollider.center = new Vector3(boxCollider.center.x, 0.03f, boxCollider.center.z);
                boxCollider.size = new Vector3(boxCollider.size.x, 0.05f, boxCollider.size.z);
                foreach (Transform child in this.transform) {
                    child.GetComponent<BoxCollider>().isTrigger = false;
                }
                Vector3 center = boxCollider.center;
                Vector3 size = boxCollider.size;
                Vector3 posOnCollider = boxCollider.transform.InverseTransformPoint(controller.position) - center;
                if (!(Mathf.Abs(posOnCollider.x) < size.x / 2 && Mathf.Abs(posOnCollider.y) < size.y / 2 && Mathf.Abs(posOnCollider.z) < size.z / 2)) {
                    transform.GetComponent<MeshRenderer>().material = keyboardMat;
                }
                print("POS ON COLLIDER: " + posOnCollider + " : " + size);
            }
        }

        /// <summary>
        /// Makes the WGKeyboard larger if "b" is true.
        /// </summary>
        /// <param name="t">Transform (not further needed)</param>
        /// <param name="b">True if WGKeyboard should get larger, otherwise false</param>
        public void ScalePlus(Transform t, bool b) {
            if (b && transform.parent.localScale.x < 2) {
                scaleObjects.transform.GetChild(0).GetComponent<MeshRenderer>().material = grayMat;
                transform.parent.localScale += new Vector3(0.05f, 0.05f, 0.05f);
                KH.keyboardScale += 0.05f;
            } else {
                scaleObjects.transform.GetChild(0).GetComponent<MeshRenderer>().material = whiteMat;
            }
        }

        /// <summary>
        /// Makes the WGKeyboard smaller if "b" is true.
        /// </summary>
        /// <param name="t">Transform (not further needed)</param>
        /// <param name="b">True if WGKeyboard should get smaller, otherwise false</param>
        public void ScaleMinus(Transform t, bool b) {
            if (b && transform.parent.localScale.x > 0.5) {
                scaleObjects.transform.GetChild(1).GetComponent<MeshRenderer>().material = grayMat;
                transform.parent.localScale -= new Vector3(0.05f, 0.05f, 0.05f);
                KH.keyboardScale -= 0.05f;
            } else {
                scaleObjects.transform.GetChild(1).GetComponent<MeshRenderer>().material = whiteMat;
            }
        }

        /// <summary>
        /// Every second time it is called with "b" being true, it expands the scale button by a "+" and a "-" button.
        /// Every other second time, it is called with "b" being true, it collapses the scale button and sets the "+" and "-" buttons' visibility to false.
        /// </summary>
        /// <param name="t">Transform (not futher needed)</param>
        /// <param name="b">If false, does not do anything, if true it either expands or collapses the "+" and "-" buttons</param>
        public void ChangeSize(Transform t, bool b) {
            if (b) {
                if (!isChangeSizeOpen) {
                    optionObjects.transform.GetChild(0).GetComponent<MeshRenderer>().material = grayMat;
                    scaleObjects.SetActive(true);
                } else {
                    optionObjects.transform.GetChild(0).GetComponent<MeshRenderer>().material = whiteMat;
                    scaleObjects.SetActive(false);
                }
                isChangeSizeOpen = !isChangeSizeOpen;
            }
        }

        /// <summary>
        /// Every second time it is called with "b" being true, it expands the option button and shows the three sub buttons.
        /// Every other second time it is called with "b" being true, it collapses the option button and does not show the three sub buttons any longer.
        /// </summary>
        /// <param name="t">Transform (not further needed)</param>
        /// <param name="b">If false it does not do anything, if true it either expands or collapses the option button</param>
        public void EnterOptions(Transform t, bool b) {
            if (b) {
                if (!isOptionsOpen) {
                    optionsKey.GetComponent<MeshRenderer>().material = grayMat;
                    optionObjects.SetActive(true);
                    SetChooseObjectsFalse();
                } else {
                    optionsKey.GetComponent<MeshRenderer>().material = whiteMat;
                    optionObjects.SetActive(false);
                    foreach (GameObject g in subOptionButtons) {
                        g.SetActive(false);
                    }
                    for (int i = 0; i < 3; i++) { 
                        optionObjects.transform.GetChild(i).GetComponent<MeshRenderer>().material = whiteMat;
                    }
                    isAddingNewWord = false;
                    isChoosingLayout = false;
                    isChangeSizeOpen = false;
                }
                isOptionsOpen = !isOptionsOpen;
            }
        }

        /// <summary>
        /// Every second time it is called with "b" being true, it expands the "add word" button and shows its sub button.
        /// Every other second time it is called with "b" being true, it collapses the "add word" button and does not show its sub button any longer.
        /// </summary>
        /// <param name="t">Transform (not further needed)</param>
        /// <param name="b">If false it does not do anything, if true it either expands or collapses the "add word" button</param>
        public void EnterAddWordMode(Transform t, bool b) {
            if (b) {
                if (!isAddingNewWord) {
                    optionObjects.transform.GetChild(2).GetComponent<MeshRenderer>().material = grayMat;
                    addKey.SetActive(true);
                    lastInputWord = "";
                    keyboardText.transform.gameObject.SetActive(true);
                    keyboardText.text = "";
                    GPC.bestWord = "";
                    previewWord.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<Text>().text = "";

                    SetChooseObjectsFalse();
                } else {
                    optionObjects.transform.GetChild(2).GetComponent<MeshRenderer>().material = whiteMat;
                    addKey.SetActive(false);
                    keyboardText.transform.gameObject.SetActive(false);
                }
                isAddingNewWord = !isAddingNewWord;
            }
        }

        /// <summary>
        /// Every second time it is called with "b" being true, it expands the "change layout" button and shows its sub buttons (different layouts to choose from).
        /// Every other second time it is called with "b" being true, it collapses the "change layout" button and does not show its sub buttons any longer.
        /// </summary>
        /// <param name="t">Transform (not further needed)</param>
        /// <param name="b">If false it does not do anything, if true it either expands or collapses the "change layout" button</param>
        public void EnterLayoutChoose(Transform t, bool b) {
            if (b) {
                if (!isChoosingLayout) {
                    if (!generatedLayoutKeys) {
                        for (int i = 0; i < FH.layouts.Count; i++) {
                            GameObject Key = Instantiate(layoutKey, layoutsObjects.transform, false) as GameObject;
                            Key.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = FH.layouts[i];
                            Key.transform.localPosition = new Vector3(0, 0, Key.transform.localScale.y * 1.1f * i);
                            Key.SetActive(true);
                            generatedLayoutKeys = true;
                        }
                    }
                    optionObjects.transform.GetChild(1).GetComponent<MeshRenderer>().material = grayMat;
                    layoutsObjects.SetActive(true);
                } else {
                    optionObjects.transform.GetChild(1).GetComponent<MeshRenderer>().material = whiteMat;
                    //foreach (Transform child in layoutsObjects.transform) {
                    //    Destroy(child.gameObject);
                    //}
                    layoutsObjects.SetActive(false);
                }
                isChoosingLayout = !isChoosingLayout;
            }
        }

        /// <summary>
        /// Changes the WGKeyboard's layout and updates all positions of all buttons according to the WGKeyboard's size changes.
        /// </summary>
        /// <param name="layout">The layout to which the WGKeyboard should be switched</param>
        public void changeLayout(string layout) {
            FH.isLoading = true;
            foreach (Transform child in this.transform) {
                GameObject.Destroy(child.gameObject);
            }
            startingLayout = layout;
            FH.layout = layout;
            KH.CreateKeyboardOverlay(FH.GetLayoutCompositions()[layout]);
            FH.LoadWordGraphs(layout);
            DelayActivateLayoutButtons();
            KH.MakeSpaceAndBackspaceHitbox(FH.GetLayoutCompositions()[layout]);
            UpdateObjectPositions();
            GPC.bestWord = "";
            previewWord.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<Text>().text = "";
            SetChooseObjectsFalse();
        }

        /// <summary>
        /// Calls another function that adds the word written in the WGKeyboard's text field to the lexicon.
        /// </summary>
        /// <param name="t">Transform (not further needed)</param>
        /// <param name="b">If false it only changes the "add word to dict" button's color to white, if true it sets its color to gray and calls a function to add the word written in the WGKeyboard's text field to the lexicon</param>
        public void AddNewWord(Transform t, bool b) {
            if (b) {
                addKey.GetComponent<MeshRenderer>().material = grayMat;
                string newWord = keyboardText.text; // maybe needs to be changed, but maybe let it be with Text Object for adding a word -> wouldn't interfere with input in query
                FH.AddNewWordToDict(newWord, GPC);
                keyboardText.text = "";
            } else {
                addKey.GetComponent<MeshRenderer>().material = whiteMat;
            }
        }

        /// <summary>
        /// Changes the WGKeyboard's color.
        /// </summary>
        /// <param name="b">If true it changes the WGKeyboard's color to white, if false (and user is not writing) it changes the WGKeyboard's color to a gray tone</param>
        public void HoverKeyboard(bool b) {
            if (b) {
                transform.GetComponent<MeshRenderer>().material = whiteMat;
            } else if (!b && !isWriting) {  // don't want to make keyboard white when interacting with in (in sense of writing on it)
                transform.GetComponent<MeshRenderer>().material = keyboardMat;
            }
        }

        /// <summary>
        /// Swaps the last inputted word in the text field with the word written in the text object given as "text".
        /// </summary>
        /// <param name="text">Text object of the "choose word" key</param>
        public void ChangeWord(Text text) {
            wordInputSound.Play();

            string tempWord = text.text;
            text.text = lastInputWord;
            lastInputWord = tempWord;

            deleteEvent.Invoke();
            result.Invoke(tempWord);
        }

        /// <summary>
        /// Makes layout keys invisible until FileHandler loaded all graphs (otherwise can be possible that it is loading multple graphs parallel, which leads to errors).
        /// </summary>
        async public void DelayActivateLayoutButtons() {
            layoutsObjects.SetActive(false);
            await Task.Run(() => {
                while (FH.isLoading) {
                    Task.Delay(10);
                }
            });
            layoutsObjects.SetActive(true);
        }

        /// <summary>
        /// Sets the gameobjects of the buttons from which the user can choose a word to false.
        /// </summary>
        public void SetChooseObjectsFalse() {
            for (int i = 0; i < 4; i++) {
                chooseObjects.transform.GetChild(i).gameObject.SetActive(false);
            }
        }
    }
}