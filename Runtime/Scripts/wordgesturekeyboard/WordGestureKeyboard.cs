using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System;
using System.Threading.Tasks;

namespace WordGestureKeyboard
{
  public class WordGestureKeyboard : MonoBehaviour
  {
    [Serializable]
    public class TextInputEvent : UnityEvent<string>
    {
    }

    [Serializable]
    public class WordDeleteEvent : UnityEvent
    {
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

    private List<GameObject> _subOptionButtons = new();

    private BoxCollider _boxCollider;
    private Text _keyboardText;
    private LineRenderer _lr;

    private UserInputHandler _uih;
    private GraphPointsCalculator _gpc;
    private FileHandler _fh;
    private KeyboardHelper _kh;

    private Material _whiteMat;
    private Material _grayMat;
    private Material _keyboardMat;

    private bool _isWriting;
    private string _lastInputWord = "";
    private Transform _controller;

    private bool _notEnded;
    private bool _isOptionsOpen;
    private bool _isAddingNewWord;
    private bool _isChoosingLayout;
    private bool _isChangeSizeOpen;
    private bool _generatedLayoutKeys;
    private bool _pressedChangeLayout;

    // Start is called before the first frame update
    private void Start()
    {
      _whiteMat = materials.whiteMat;
      _grayMat = materials.grayMat;
      _keyboardMat = materials.keyboardMat;

      _subOptionButtons.Add(optionObjects);
      _subOptionButtons.Add(addKey);
      _subOptionButtons.Add(layoutsObjects);
      _subOptionButtons.Add(scaleObjects);

      var parent = transform.parent;
      _boxCollider = parent.GetComponent<BoxCollider>();
      _keyboardText = parent.Find("Canvas").GetChild(0).GetComponent<Text>();
      _lr = GetComponent<LineRenderer>();
      _lr.numCapVertices = 5;
      _lr.numCornerVertices = 5;

      if (startingLayout == "")
      {
        // if user didn't specify another layout, the standard qwertz layout will be used
        startingLayout = "qwertz";
      }

      _fh = new FileHandler(startingLayout);
      _fh.LoadLayouts();
      _kh = new KeyboardHelper(transform, key, _boxCollider);
      _kh.CreateKeyboardOverlay(_fh.GetLayoutCompositions()[startingLayout]);
      _uih = new UserInputHandler(_lr, transform);
      _gpc = new GraphPointsCalculator();

      _fh.LoadWordGraphs(startingLayout);
      _kh.MakeSpaceAndBackspaceHitBox(_fh.GetLayoutCompositions()[startingLayout]);

      UpdateObjectPositions();
    }

    private void Update()
    {
      if (_isWriting)
      {
        if (!_uih.GetIsSamplingPoints())
        {
          var hitPoint = _uih.GetHitPoint(_controller.position);
          if (_uih.CheckPoint(hitPoint))
          {
            _uih.SamplePoints(hitPoint);
          }
        }

        if (!_gpc.isCalculatingPreview)
        {
          previewWord.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<Text>().text = _gpc.bestWord;
          var pointsList = _uih.GetTransformedPoints(false);
          if (pointsList.Count != 0)
          {
            if (GraphPointsCalculator.IsBackSpaceOrSpace(pointsList, _kh.backSpaceHitBox, _kh.spaceHitBox) == 0)
            {
              _gpc.CalcBestWords(pointsList, _fh.GetLocationWordPointsDict(), _fh.GetNormalizedWordPointsDict(),
                _kh.sigma, _kh.keyRadius, _fh.wordRanking, true);
              //Task.Run(async () => await GPC.calcBestWords(pointsList, FH.GetLocationWordPointsDict(), FH.GetNormalizedWordPointsDict(), KH.sigma, KH.keyRadius, FH.wordRanking, true));
            }
            else
            {
              _gpc.bestWord = "";
              previewWord.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<Text>().text = "";
            }
          }
        }
      }
      else if (!_uih.GetIsSamplingPoints() && _notEnded)
      {
        // these two bools tell us, whether the calculation has ended and notEnded tells us, if the user input has been further processed
        var pointsList = _uih.GetTransformedPoints(true);
        if (pointsList.Count != 0)
        {
          // user pressed trigger button, but somehow it didn't register any points
          if (GraphPointsCalculator.IsBackSpaceOrSpace(pointsList, _kh.backSpaceHitBox, _kh.spaceHitBox) == -1)
          {
            wordInputSound.Play();
            if (_isAddingNewWord)
            {
              if (_keyboardText.text != "")
              {
                _keyboardText.text = _keyboardText.text.Substring(0, _keyboardText.text.Length - 1);
              }
            }
            else
            {
              deleteEvent.Invoke();
            }

            _gpc.bestWord = "";
            previewWord.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<Text>().text = "";
            SetChooseObjectsFalse();
          }
          else if (GraphPointsCalculator.IsBackSpaceOrSpace(pointsList, _kh.backSpaceHitBox, _kh.spaceHitBox) == 1)
          {
            if (!_isAddingNewWord)
            {
              wordInputSound.Play();
              result.Invoke(" ");
              SetChooseObjectsFalse();
            }

            _gpc.bestWord = "";
            previewWord.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<Text>().text = "";
          }
          else
          {
            _gpc.CalcBestWords(pointsList, _fh.GetLocationWordPointsDict(), _fh.GetNormalizedWordPointsDict(),
              _kh.sigma, _kh.keyRadius, _fh.wordRanking, false);
          }

          _notEnded = false;
        }
      }
      else if (_gpc.sortedDict != null && _gpc.sortedDict.Count != 0)
      {
        SetChooseObjectsFalse();
        var word = _gpc.sortedDict[0];
        previewWord.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<Text>().text = word;
        if (word != "")
        {
          wordInputSound.Play();
          if (_isAddingNewWord)
          {
            // putting text into textfield from keyboard
            _keyboardText.text += _gpc.sortedDict[0];
          }
          else
          {
            // putting text into inputfield of query
            for (var i = 0; i < _gpc.sortedDict.Count - 1; i++)
            {
              if (i > 3)
              {
                // maximum of 4 best words apart from top word
                break;
              }

              chooseObjects.transform.GetChild(i).GetChild(0).GetChild(0).GetComponent<Text>().text =
                _gpc.sortedDict[i + 1];
              chooseObjects.transform.GetChild(i).gameObject.SetActive(true);
            }

            _lastInputWord = _gpc.sortedDict[0];
            result.Invoke(_gpc.sortedDict[0]);
          }
        }
        else
        {
          Debug.Log("no word found");
        }

        _gpc.sortedDict = null;
      }
    }

    /// <summary>
    /// Updates the positions of all objects of the WGKeyboard (especially needed if layout changes)
    /// </summary>
    private void UpdateObjectPositions()
    {
      var localScale = transform.localScale;
      var optionsKeyScale = optionsKey.transform.localScale;
      var optionsObjectsScale = optionObjects.transform.localScale;
      var addKeyScale = addKey.transform.localScale;
      var previewWordScale = previewWord.transform.localScale;
      var previewWordPosition = previewWord.transform.localPosition;

      optionsKey.transform.localPosition = new Vector3(0.5f * _kh.keyboardLength + optionsKeyScale.x * 0.5f + 0.005f, 0,
        localScale.y * 0.5f - optionsKeyScale.z * 0.5f);
      optionObjects.transform.localPosition = new Vector3(
        0.5f * _kh.keyboardLength + optionsObjectsScale.x * 0.5f + 0.005f, optionObjects.transform.localPosition.y,
        optionsKey.transform.localPosition.z +
        (MathF.Sin((90 - 360 + optionObjects.transform.localEulerAngles.x) * Mathf.PI / 180)) *
        (optionsObjectsScale.z * 3));
      //optionObjects.transform.localPosition.z + transform.localScale.y * 0.5f - optionsKey.transform.localScale.z * 0.5f);
      addKey.transform.localPosition = new Vector3(0.5f * _kh.keyboardLength + addKeyScale.x * 0.5f + 0.005f, 0,
        -localScale.y * 0.5f + addKeyScale.z * 0.5f);
      previewWordPosition = new Vector3(0, previewWordPosition.y,
        localScale.y * 0.5f + previewWordScale.z * 0.5f * 1.1f + 0.001f);
      previewWord.transform.localPosition = previewWordPosition;
      chooseObjects.transform.localPosition = new Vector3(0, chooseObjects.transform.localPosition.y,
        previewWordPosition.z + previewWordScale.z * 0.5f + chooseObjects.transform.localScale.y * 0.35f);
      if (!((addKey.transform.localPosition + addKey.transform.localScale * 0.5f -
             (optionsKey.transform.localPosition - optionsKey.transform.localScale * 0.5f)).z >= 0)) return;
      var addKeyPosition = addKey.transform.localPosition;
      addKeyPosition = new Vector3(addKeyPosition.x, addKeyPosition.y,
        optionsKey.transform.localPosition.z - optionsKey.transform.localScale.z * 0.5f -
        addKey.transform.localScale.z * 0.5f - 0.02f);
      addKey.transform.localPosition = addKeyPosition;
    }

    /// <summary>
    /// Used if user interacts with keyboard with the correct controller button.
    /// If they press said button, the WGKeyboard is set in "drawing mode" which means, it modifies the hitbox of the WGKeyboard itself and activates the character keys' hitboxes.
    /// </summary>
    /// <param name="t">Transform that interacts with WGKeyboard's hitbox</param>
    /// <param name="b">Boolean that indicates if the controller button is pressed (true) or released (false)</param>
    public void DrawWord(Transform t, bool b)
    {
      if (b)
      {
        SetChooseObjectsFalse();
        _isWriting = true;
        _controller = t;
        var center = _boxCollider.center;
        _boxCollider.center = new Vector3(center.x, 0.007f, center.z);
        var size = _boxCollider.size;
        _boxCollider.size = new Vector3(size.x, 0.001f, size.z);
        foreach (Transform child in transform)
        {
          child.GetComponent<BoxCollider>().isTrigger = true;
        }

        transform.GetComponent<MeshRenderer>().material = _whiteMat;
      }
      else
      {
        _isWriting = false;
        _notEnded = true;
        var center1 = _boxCollider.center;
        _boxCollider.center = new Vector3(center1.x, 0.03f, center1.z);
        var size1 = _boxCollider.size;
        _boxCollider.size = new Vector3(size1.x, 0.05f, size1.z);
        foreach (Transform child in transform)
        {
          child.GetComponent<BoxCollider>().isTrigger = false;
        }

        var center = _boxCollider.center;
        var size = _boxCollider.size;
        var posOnCollider = _boxCollider.transform.InverseTransformPoint(_controller.position) - center;
        if (!(Mathf.Abs(posOnCollider.x) - 0.006f <= size.x / 2 && Mathf.Abs(posOnCollider.y) - 0.01f <= size.y / 2 &&
              Mathf.Abs(posOnCollider.z) - 0.006f <= size.z / 2))
        {
          transform.GetComponent<MeshRenderer>().material = _keyboardMat;
        }
      }
    }

    /// <summary>
    /// Makes the WGKeyboard larger if "b" is true.
    /// </summary>
    /// <param name="t">Transform (not further needed)</param>
    /// <param name="b">True if WGKeyboard should get larger, otherwise false</param>
    public void ScalePlus(Transform t, bool b)
    {
      if (b && transform.parent.localScale.x < 2)
      {
        scaleObjects.transform.GetChild(0).GetComponent<MeshRenderer>().material = _grayMat;
        transform.parent.localScale += new Vector3(0.05f, 0.05f, 0.05f);
        _kh.keyboardScale += 0.05f;
      }
      else
      {
        scaleObjects.transform.GetChild(0).GetComponent<MeshRenderer>().material = _whiteMat;
      }
    }

    /// <summary>
    /// Makes the WGKeyboard smaller if "b" is true.
    /// </summary>
    /// <param name="t">Transform (not further needed)</param>
    /// <param name="b">True if WGKeyboard should get smaller, otherwise false</param>
    public void ScaleMinus(Transform t, bool b)
    {
      if (b && transform.parent.localScale.x > 0.5)
      {
        scaleObjects.transform.GetChild(1).GetComponent<MeshRenderer>().material = _grayMat;
        transform.parent.localScale -= new Vector3(0.05f, 0.05f, 0.05f);
        _kh.keyboardScale -= 0.05f;
      }
      else
      {
        scaleObjects.transform.GetChild(1).GetComponent<MeshRenderer>().material = _whiteMat;
      }
    }

    /// <summary>
    /// Every second time it is called with "b" being true, it expands the scale button by a "+" and a "-" button.
    /// Every other second time, it is called with "b" being true, it collapses the scale button and sets the "+" and "-" buttons' visibility to false.
    /// </summary>
    /// <param name="t">Transform (not futher needed)</param>
    /// <param name="b">If false, does not do anything, if true it either expands or collapses the "+" and "-" buttons</param>
    public void ChangeSize(Transform t, bool b)
    {
      if (b)
      {
        if (!_isChangeSizeOpen)
        {
          optionObjects.transform.GetChild(0).GetComponent<MeshRenderer>().material = _grayMat;
          scaleObjects.SetActive(true);
        }
        else
        {
          optionObjects.transform.GetChild(0).GetComponent<MeshRenderer>().material = _whiteMat;
          scaleObjects.SetActive(false);
        }

        _isChangeSizeOpen = !_isChangeSizeOpen;
      }
    }

    /// <summary>
    /// Every second time it is called with "b" being true, it expands the option button and shows the three sub buttons.
    /// Every other second time it is called with "b" being true, it collapses the option button and does not show the three sub buttons any longer.
    /// </summary>
    /// <param name="t">Transform (not further needed)</param>
    /// <param name="b">If false it does not do anything, if true it either expands or collapses the option button</param>
    public void EnterOptions(Transform t, bool b)
    {
      if (b)
      {
        if (!_isOptionsOpen)
        {
          optionsKey.GetComponent<MeshRenderer>().material = _grayMat;
          optionObjects.SetActive(true);
          SetChooseObjectsFalse();
        }
        else
        {
          optionsKey.GetComponent<MeshRenderer>().material = _whiteMat;
          optionObjects.SetActive(false);
          foreach (var g in _subOptionButtons)
          {
            g.SetActive(false);
          }

          for (var i = 0; i < 3; i++)
          {
            optionObjects.transform.GetChild(i).GetComponent<MeshRenderer>().material = _whiteMat;
          }

          _isAddingNewWord = false;
          _isChoosingLayout = false;
          _isChangeSizeOpen = false;
        }

        _isOptionsOpen = !_isOptionsOpen;
      }
    }

    /// <summary>
    /// Every second time it is called with "b" being true, it expands the "add word" button and shows its sub button.
    /// Every other second time it is called with "b" being true, it collapses the "add word" button and does not show its sub button any longer.
    /// </summary>
    /// <param name="t">Transform (not further needed)</param>
    /// <param name="b">If false it does not do anything, if true it either expands or collapses the "add word" button</param>
    public void EnterAddWordMode(Transform t, bool b)
    {
      if (b)
      {
        if (!_isAddingNewWord)
        {
          optionObjects.transform.GetChild(2).GetComponent<MeshRenderer>().material = _grayMat;
          addKey.SetActive(true);
          _lastInputWord = "";
          _keyboardText.transform.gameObject.SetActive(true);
          _keyboardText.text = "";
          _gpc.bestWord = "";
          previewWord.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<Text>().text = "";

          SetChooseObjectsFalse();
        }
        else
        {
          optionObjects.transform.GetChild(2).GetComponent<MeshRenderer>().material = _whiteMat;
          addKey.SetActive(false);
          _keyboardText.transform.gameObject.SetActive(false);
        }

        _isAddingNewWord = !_isAddingNewWord;
      }
    }

    /// <summary>
    /// Every second time it is called with "b" being true, it expands the "change layout" button and shows its sub buttons (different layouts to choose from).
    /// Every other second time it is called with "b" being true, it collapses the "change layout" button and does not show its sub buttons any longer.
    /// </summary>
    /// <param name="t">Transform (not further needed)</param>
    /// <param name="b">If false it does not do anything, if true it either expands or collapses the "change layout" button</param>
    public void EnterLayoutChoose(Transform t, bool b)
    {
      if (!b) return;

      if (!_isChoosingLayout)
      {
        if (!_generatedLayoutKeys)
        {
          for (var i = 0; i < _fh.layouts.Count; i++)
          {
            var keyboardKey = Instantiate(layoutKey, layoutsObjects.transform, false);
            keyboardKey.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = _fh.layouts[i];
            keyboardKey.transform.localPosition = new Vector3(0, 0, keyboardKey.transform.localScale.y * 1.1f * i);
            keyboardKey.SetActive(true);
            _generatedLayoutKeys = true;
          }
        }

        optionObjects.transform.GetChild(1).GetComponent<MeshRenderer>().material = _grayMat;
        layoutsObjects.SetActive(true);
      }
      else
      {
        optionObjects.transform.GetChild(1).GetComponent<MeshRenderer>().material = _whiteMat;
        //foreach (Transform child in layoutsObjects.transform) {
        //    Destroy(child.gameObject);
        //}
        layoutsObjects.SetActive(false);
      }

      _isChoosingLayout = !_isChoosingLayout;
    }

    /// <summary>
    /// Changes the WGKeyboard's layout and updates all positions of all buttons according to the WGKeyboard's size changes.
    /// </summary>
    /// <param name="layout">The layout to which the WGKeyboard should be switched</param>
    public void ChangeLayout(string layout)
    {
      if (!_pressedChangeLayout)
      {
        _pressedChangeLayout = true;
        _fh.isLoading = true;
        foreach (Transform child in transform)
        {
          Destroy(child.gameObject);
        }

        startingLayout = layout;
        _fh.layout = layout;
        _kh.CreateKeyboardOverlay(_fh.GetLayoutCompositions()[layout]);
        _fh.LoadWordGraphs(layout);
        DelayActivateLayoutButtons();
        _kh.MakeSpaceAndBackspaceHitBox(_fh.GetLayoutCompositions()[layout]);
        UpdateObjectPositions();
        _gpc.bestWord = "";
        previewWord.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<Text>().text = "";
        SetChooseObjectsFalse();
      }
    }

    /// <summary>
    /// Calls another function that adds the word written in the WGKeyboard's text field to the lexicon.
    /// </summary>
    /// <param name="t">Transform (not further needed)</param>
    /// <param name="b">If false it only changes the "add word to dict" button's color to white, if true it sets its color to gray and calls a function to add the word written in the WGKeyboard's text field to the lexicon</param>
    public void AddNewWord(Transform t, bool b)
    {
      if (b)
      {
        addKey.GetComponent<MeshRenderer>().material = _grayMat;
        var
          newWord = _keyboardText
            .text; // maybe needs to be changed, but maybe let it be with Text Object for adding a word -> wouldn't interfere with input in query
        _fh.AddNewWordToDict(newWord, _gpc);
        _keyboardText.text = "";
      }
      else
      {
        addKey.GetComponent<MeshRenderer>().material = _whiteMat;
      }
    }

    /// <summary>
    /// Changes the WGKeyboard's color.
    /// </summary>
    /// <param name="b">If true it changes the WGKeyboard's color to white, if false (and user is not writing) it changes the WGKeyboard's color to a gray tone</param>
    public void HoverKeyboard(bool b)
    {
      if (b)
      {
        transform.GetComponent<MeshRenderer>().material = _whiteMat;
      }
      else if (!_isWriting)
      {
        // don't want to make keyboard white when interacting with in (in sense of writing on it)
        transform.GetComponent<MeshRenderer>().material = _keyboardMat;
      }
    }

    /// <summary>
    /// Swaps the last inputted word in the text field with the word written in the text object given as "text".
    /// </summary>
    /// <param name="text">Text object of the "choose word" key</param>
    public void ChangeWord(Text text)
    {
      wordInputSound.Play();

      var tempWord = text.text;
      text.text = _lastInputWord;
      _lastInputWord = tempWord;

      deleteEvent.Invoke();
      result.Invoke(tempWord);
    }

    /// <summary>
    /// Makes layout keys invisible until FileHandler loaded all graphs (otherwise can be possible that it is loading multple graphs parallel, which leads to errors).
    /// </summary>
    private async void DelayActivateLayoutButtons()
    {
      layoutsObjects.SetActive(false);
      await Task.Run(() =>
      {
        while (_fh.isLoading)
        {
          Task.Delay(10);
        }
      });
      layoutsObjects.SetActive(true);
      _pressedChangeLayout = false;
    }

    /// <summary>
    /// Sets the gameobjects of the buttons from which the user can choose a word to false.
    /// </summary>
    private void SetChooseObjectsFalse()
    {
      for (var i = 0; i < 4; i++)
      {
        chooseObjects.transform.GetChild(i).gameObject.SetActive(false);
      }
    }
  }
}