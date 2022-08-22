using UnityEngine;
using UnityEngine.UI;

namespace WordGestureKeyboard
{
  public class BestWordChooseManager : MonoBehaviour
  {
    public MaterialHolder materials;
    private Material _whiteMat;
    private Material _grayMat;

    private void Start()
    {
      _whiteMat = materials.whiteMat;
      _grayMat = materials.grayMat;
    }

    /// <summary>
    /// Calls another function to swap the written word with the word written on the key to which this script is attached and vice versa.
    /// </summary>
    /// <param name="b">If true it calls a function and swaps the word of the text field with the word on the key to which this script is attached and changes the color, if false it only changes the color</param>
    public void ChooseWord(bool b)
    {
      if (b)
      {
        transform.parent.parent.Find("WGKeyboard").GetComponent<WordGestureKeyboard>()
          .ChangeWord(transform.GetChild(0).GetChild(0).GetComponent<Text>());
        transform.GetComponent<MeshRenderer>().material = _grayMat;
      }
      else
      {
        transform.GetComponent<MeshRenderer>().material = _whiteMat;
      }
    }
  }
}