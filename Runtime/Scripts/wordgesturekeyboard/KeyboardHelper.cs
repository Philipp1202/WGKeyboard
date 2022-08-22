using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using Object = UnityEngine.Object;

namespace WordGestureKeyboard
{
  public class KeyboardHelper
  {
    private readonly GameObject _key;
    private readonly Transform _transform;
    private readonly BoxCollider _boxCollider;

    private float _longestKeyboardLine;
    public float keyRadius;
    public float keyboardLength;
    private float _keyboardWidth;
    public readonly float[] backSpaceHitBox = {0, 0, 0, 0};
    public readonly float[] spaceHitBox = {0, 0, 0, 0};
    public float sigma;
    public float keyboardScale = 1;
    private const float KeyboardKeyWidth = 0.05f;

    public KeyboardHelper(Transform t, GameObject k, BoxCollider b)
    {
      _transform = t;
      _key = k;
      _boxCollider = b;
    }

    /// <summary>
    /// Generates the keys for the word-gesture keyboard for the given layout and puts it on the keyboard (also determines the size of the WGKeyboard).
    /// </summary>
    /// <param name="layoutComposition">A tuple that contains two lists, one with the lines of characters and one with the lines' indents of the layout for which the keyboard should be generated</param>
    public void CreateKeyboardOverlay(Tuple<List<float>, List<string>> layoutComposition)
    {
      var keyList = layoutComposition.Item2;
      var indentList = layoutComposition.Item1;
      var count = keyList.Count;
      _longestKeyboardLine = 0;
      for (var i = 0; i < count; i++)
      {
        var lineLength = keyList[i].Length + Mathf.Abs(indentList[i]);
        if (keyList[i].Contains(" "))
        {
          lineLength += 7; // because length of spacebar is 8 * normal keysize, that means 7 * keysize extra
        }

        if (keyList[i].Contains("<"))
        {
          lineLength += 1; // because length of backspace is 2 * normal keysize, that means 1 * keysize extra
        }

        if (lineLength > _longestKeyboardLine)
        {
          _longestKeyboardLine = lineLength;
        }
      }

      sigma = 1 / _longestKeyboardLine /
              2; // right now key radius after transformation of x to length 1 (but could be changed)
      keyRadius = 1 / _longestKeyboardLine / 2; // key radius after transformation of x to length 1
      keyboardLength = KeyboardKeyWidth * _longestKeyboardLine;
      _keyboardWidth = KeyboardKeyWidth * count;
      _transform.localScale =
        new Vector3(keyboardLength, _keyboardWidth,
          _transform.localScale.z); // keyboard gets bigger if more keys on one line, but keys always have the same size
      _boxCollider.size = new Vector3(keyboardLength, 0.05f, _keyboardWidth);

      var parent = _transform.parent;
      var tempRot = parent.localRotation;
      parent.localRotation =
        new Quaternion(0, 0, 0,
          0); // set to 0,0,0,0, because otherwise could lead to wrong positions and rotations for keys, when attached to keyboard
      parent.localScale = new Vector3(1, 1, 1);
      const float l = KeyboardKeyWidth / 1.1f;
      var y = count - 1;
      foreach (var s in keyList)
      {
        var offset = indentList[count - y - 1] * l;
        var x = 0;
        foreach (var letter in s)
        {
          var specificKey = Object.Instantiate(_key);
          var scale = 1;
          float offsetSpecial = 0;
          switch (letter.ToString())
          {
            case "<":
            {
              scale = 2;
              offsetSpecial = KeyboardKeyWidth / 2;
              var borderLeft = specificKey.transform.GetChild(2);
              var borderRight = specificKey.transform.GetChild(3);
              var leftBorderScale = borderLeft.localScale;
              borderLeft.localScale = new Vector3(leftBorderScale.x / scale, leftBorderScale.y, leftBorderScale.z);
              var rightBorderScale = borderRight.localScale;
              borderRight.localScale = new Vector3(rightBorderScale.x / scale, rightBorderScale.y, rightBorderScale.z);
              break;
            }
            case " ":
            {
              scale = 8;
              offsetSpecial = KeyboardKeyWidth * 3.5f;
              var borderLeft = specificKey.transform.GetChild(2);
              var borderRight = specificKey.transform.GetChild(3);
              var leftBorderScale = borderLeft.localScale;
              borderLeft.localScale = new Vector3(leftBorderScale.x / scale, leftBorderScale.y, leftBorderScale.z);
              var rightBorderScale = borderRight.localScale;
              borderRight.localScale = new Vector3(rightBorderScale.x / scale, rightBorderScale.y, rightBorderScale.z);
              break;
            }
          }

          var position = _transform.position;
          specificKey.transform.position = new Vector3(
            position.x - KeyboardKeyWidth * (_longestKeyboardLine / 2 - x) + offset + offsetSpecial +
            KeyboardKeyWidth / 2, position.y + 0.005f,
            position.z - KeyboardKeyWidth * (count / 2.0f - y - 1) - KeyboardKeyWidth / 2);
          specificKey.transform.Find("Canvas").Find("Text").GetComponent<Text>().text = letter.ToString();
          specificKey.transform.localScale =
            new Vector3(l + KeyboardKeyWidth * (scale - 1), l, specificKey.transform.localScale.z);
          specificKey.transform.SetParent(_transform);

          offset += offsetSpecial * 2; // to get all the keys in right position that are behind the special-sized keys

          x += 1;
        }

        y -= 1;
      }

      parent.localScale = new Vector3(keyboardScale, keyboardScale, keyboardScale);
      parent.localRotation = tempRot;
    }

    /// <summary>
    /// Finds the hit boxes for the space and backspace keys if they are present in the keyboard layout and assigns it to the fields "backSpaceHitBox" and "spaceHitBox".
    /// </summary>
    /// <param name="layoutComposition">The composition of the layout for which the space and backspace hit boxes have to be found.</param>
    public void MakeSpaceAndBackspaceHitBox(Tuple<List<float>, List<string>> layoutComposition)
    {
      var backspaceFound = false;
      var spaceFound = false;
      var count = layoutComposition.Item2.Count;
      for (var y = 0; y < count; y++)
      {
        var
          xIndent = 0; // needed if space and backspace are on the same line, have to consider that they have a bigger size than 1
        for (var x = 0; x < layoutComposition.Item2[count - y - 1].Length; x++)
        {
          switch (layoutComposition.Item2[count - y - 1][x].ToString())
          {
            case "<":
            {
              backspaceFound = true;
              const int scale = 2;
              const float offsetSpecial = scale / 2f;
              var offset = layoutComposition.Item1[count - y - 1];
              var xPos = x + offset + offsetSpecial;
              var yPos = y + 0.5f;
              backSpaceHitBox[0] = (xPos - offsetSpecial + xIndent) / _longestKeyboardLine;
              backSpaceHitBox[1] = (xPos + offsetSpecial + xIndent) / _longestKeyboardLine;
              backSpaceHitBox[2] = (yPos - 0.5f) / _longestKeyboardLine;
              backSpaceHitBox[3] = (yPos + 0.5f) / _longestKeyboardLine;
              xIndent += scale - 1;
              break;
            }
            case " ":
            {
              spaceFound = true;
              const int scale = 8;
              const float offsetSpecial = scale / 2f;
              var offset = layoutComposition.Item1[count - y - 1];
              var xPos = x + offset + offsetSpecial;
              var yPos = y + 0.5f;
              spaceHitBox[0] = (xPos - offsetSpecial + xIndent) / _longestKeyboardLine;
              spaceHitBox[1] = (xPos + offsetSpecial + xIndent) / _longestKeyboardLine;
              spaceHitBox[2] = (yPos - 0.5f) / _longestKeyboardLine;
              spaceHitBox[3] = (yPos + 0.5f) / _longestKeyboardLine;
              xIndent += scale - 1;
              break;
            }
          }
        }
      }

      if (!backspaceFound)
      {
        backSpaceHitBox[0] = 0;
        backSpaceHitBox[1] = 0;
        backSpaceHitBox[2] = 0;
        backSpaceHitBox[3] = 0;
      }

      if (spaceFound) return;
      spaceHitBox[0] = 0;
      spaceHitBox[1] = 0;
      spaceHitBox[2] = 0;
      spaceHitBox[3] = 0;
    }
  }
}