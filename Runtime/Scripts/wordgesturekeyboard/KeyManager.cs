using System;
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
    /// <param name="hovered">If true, changes color to a gray tone, otherwise if false, it changes the color to a brighter tone</param>
    public void IsHovered(bool hovered)
    {
      transform.GetComponent<MeshRenderer>().material = hovered ? _keyHoverMat : _normalMat;
    }

    private void OnTriggerEnter(Collider other)
    {
      IsHovered(true);
    }

    private void OnTriggerExit(Collider other)
    {
      IsHovered(false);
    }
  }
}