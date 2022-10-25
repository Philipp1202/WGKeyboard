using UnityEngine;
using UnityEngine.UI;

namespace WordGestureKeyboard
{
  public class LayoutKeyScript : MonoBehaviour
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
    /// It takes the text written on the key to which this script is attached and calls another function that changes the layout to the text written on the key.
    /// </summary>
    /// <param name="t">Transform (not further needed)</param>
    /// <param name="b">If true it changes the "change layout" button's color to gray and calls another function to change the layout, if false it changes the the "change layout" button's color to white</param>
    public void ChooseLayout(Transform t, bool b)
    {
      if (b)
      {
        transform.GetComponent<MeshRenderer>().material = _grayMat;
        var layout = transform.GetChild(0).GetChild(0).GetComponent<Text>().text;
        transform.parent.parent.parent.Find("WGKeyboard").GetComponent<WordGestureKeyboard>().ChangeLayout(layout);
      }
      else
      {
        transform.GetComponent<MeshRenderer>().material = _whiteMat;
      }
    }
  }
}