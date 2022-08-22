using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace WordGestureKeyboard
{
  public class UserInputHandler
  {
    private readonly LineRenderer _lr;
    private readonly Transform _transform;
    private int _pointCount;
    private bool _isSamplingPoints;
    private bool _isLastDistShort;
    private const float MinAngle = 10;
    private const float MinSegmentDist = 0.015f;

    public UserInputHandler(LineRenderer lr, Transform t)
    {
      _lr = lr;
      _transform = t;
    }

    /// <summary>
    /// Takes the position of the object that interacts with the WGKeyboard and then returns the point transformed such that it lies on the WGKeyboard.
    /// </summary>
    /// <param name="colPos">Position of the object interacting with the WGKeyboard (controller)</param>
    /// <returns>The given point's position on the WGKeyboard</returns>
    public Vector3 GetHitPoint(Vector3 colPos)
    {
      var parent = _transform.parent;
      var transformedPoint = parent.InverseTransformPoint(colPos);
      var point = new Vector3(transformedPoint.x, 0.008f, transformedPoint.z);
      transformedPoint = parent.TransformPoint(point);
      return transformedPoint;
    }

    /// <summary>
    /// Checks if the given point is within the WGKeyboard's boundaries.
    /// </summary>
    /// <param name="point">Point to check (not in localspace of WGKeyboard)</param>
    /// <returns>True if it is within the WGKeyboard's boundaries, false if it is outside of the WGKeyboard's boundaries</returns>
    public bool CheckPoint(Vector3 point)
    {
      var localScale = _transform.localScale;
      var keyboardLength = localScale.x;
      var keyboardWidth = localScale.y;
      var localTransformedPoint = _transform.parent.InverseTransformPoint(point);
      return keyboardLength / 2 >= Mathf.Abs(localTransformedPoint.x) &&
             keyboardWidth / 2 >= Mathf.Abs(localTransformedPoint.z);
    }

    /// <summary>
    /// Transforms all points of the LineRenderer LR such that they lie in x-y-range 0-1. (longer side 0-1, shorter side 0-(shorter side / longer side)).
    /// </summary>
    /// <param name="modify">boolean, if true, it changes some additional values (needed if this function is called, when the user ends writing)</param>
    /// <returns>A List of transformed points made from points of the LineRenderer</returns>
    public List<Vector2> GetTransformedPoints(bool modify)
    {
      var pointsList = new List<Vector2>();
      for (var i = 0; i < _lr.positionCount; i++)
      {
        var point = _lr.GetPosition(i);
        var localTransformedPoint = _transform.parent.InverseTransformPoint(point);
        var localScale = _transform.localScale;
        var keyboardLength = localScale.x;
        var keyboardWidth = localScale.y;
        pointsList.Add(new Vector2(
          (localTransformedPoint.x + keyboardLength / 2) / Mathf.Max(keyboardLength, keyboardWidth),
          (localTransformedPoint.z + keyboardWidth / 2) /
          Mathf.Max(keyboardLength, keyboardWidth))); // lower left corner of WGKeyboard is at (0/0)
      }

      if (!modify) return pointsList;
      _pointCount = 0;
      _lr.positionCount = 0;
      _isLastDistShort = false;
      return pointsList;
    }

    /// <summary>
    /// The LineRenderer would normally take all the points the user inputs. This function looks, if it is okay to drop the previous set point considering the new one.
    /// </summary>
    /// <param name="hitPoint">New Point to consider</param>
    public async void SamplePoints(Vector3 hitPoint)
    {
      // I worked with async, because FPS dropped from 90 to (worst case observed) around 40. Can't use Linerenderer functions in async Task.Run(), therefore worked with some "unnecessary" variables
      _isSamplingPoints = true;
      var posCount = _lr.positionCount;
      var startPoint = new Vector3(0, 0, 0);
      var middlePoint = new Vector3(0, 0, 0);
      if (posCount > 2)
      {
        startPoint = _lr.GetPosition(_pointCount - 2);
        middlePoint = _lr.GetPosition(_pointCount - 1);
      }

      await Task.Run(() =>
      {
        if (_pointCount < 3)
        {
          _pointCount++;
        }
        else
        {
          Vector3 lastPoint;
          var angle = Vector3.Angle(middlePoint - startPoint, hitPoint - middlePoint);
          if (angle is < MinAngle or > 180 - MinAngle)
          {
            if (_isLastDistShort)
            {
              lastPoint = startPoint;
            }
            else
            {
              _pointCount++;
              lastPoint = middlePoint;
            }

            _isLastDistShort = (hitPoint - lastPoint).magnitude < MinSegmentDist;
          }
          else
          {
            if (_isLastDistShort)
            {
              lastPoint = startPoint;
            }
            else
            {
              _pointCount++;
              lastPoint = middlePoint;
            }

            _isLastDistShort = (hitPoint - lastPoint).magnitude < MinSegmentDist / 5;
          }
        }
      });
      _lr.positionCount = _pointCount;
      _lr.SetPosition(_pointCount - 1, hitPoint);
      _isSamplingPoints = false;
    }

    public bool GetIsSamplingPoints()
    {
      return _isSamplingPoints;
    }
  }
}