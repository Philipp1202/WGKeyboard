using UnityEngine;

namespace WordGestureKeyboard
{
  public class KeyManager : MonoBehaviour
  {
    public MaterialHolder materials;
    private Material _keyHoverMat;
    private Material _normalMat;


    private void Start()
    {
      _keyHoverMat = materials.keyHoverMat;
      _normalMat = materials.keyMat;
    }

    /// <summary>
    /// Changes the color of a key object.
    /// </summary>
    /// <param name="b">If true, changes color to a gray tone, otherwise if false, it changes the color to a brighter tone</param>
    public void IsHovered(bool b)
    {
      transform.GetComponent<MeshRenderer>().material = b ? _keyHoverMat : _normalMat;
    }
  }
}