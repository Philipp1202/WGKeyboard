using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace WordGestureKeyboard {
    public class UserInputHandler {
        LineRenderer LR;
        Transform transform;
        int pointCount = 0;
        bool isSamplingPoints = false;
        bool isLastDistShort = false;
        float minAngle = 10;
        float minSegmentDist = 0.015f;

        public UserInputHandler(LineRenderer LR, Transform t) {
            this.LR = LR;
            this.transform = t;
        }

        /// <summary>
        /// Takes the position of the object that interacts with the WGKeyboard and then returns the point transformed such that it lies on the WGKeyboard.
        /// </summary>
        /// <param name="colPos">Position of the object interacting with the WGKeyboard (controller)</param>
        /// <returns>The given point's position on the WGKeyboard</returns>
        public Vector3 GetHitPoint(Vector3 colPos) {
            Vector3 transformedPoint = transform.parent.InverseTransformPoint(colPos);
            Vector3 point = new Vector3(transformedPoint.x, 0.008f, transformedPoint.z);
            transformedPoint = transform.parent.TransformPoint(point);
            return transformedPoint;
        }

        /// <summary>
        /// Checks if the given point is within the WGKeyboard's boundaries.
        /// </summary>
        /// <param name="point">Point to check (not in localspace of WGKeyboard)</param>
        /// <returns>True if it is within the WGKeyboard's boundaries, false if it is outside of the WGKeyboard's boundaries</returns>
        public bool CheckPoint(Vector3 point) {
            float keyboardLength = transform.localScale.x;
            float keyboardWidth = transform.localScale.y;
            Vector3 localTransformedPoint = transform.parent.InverseTransformPoint(point);
            if (keyboardLength / 2 >= Mathf.Abs(localTransformedPoint.x) && keyboardWidth / 2 >= Mathf.Abs(localTransformedPoint.z)) {
                return true;
            }
            return false;

            /*
            Vector2 pointOnKeyboard = new Vector2((localTransformedPoint.x + keyboardLength / 2) / Mathf.Max(keyboardLength, keyboardWidth), (localTransformedPoint.z + keyboardWidth / 2) / Mathf.Max(keyboardLength, keyboardWidth));
            float xMax = 1;
            float yMax = 1;
            if (keyboardLength > keyboardWidth) {
                yMax = keyboardWidth / keyboardLength;
            } else {
                xMax = keyboardLength / keyboardWidth;
            }

            if (0 > pointOnKeyboard.x || pointOnKeyboard.x > xMax) {
                return false;
            }
            if (0 > pointOnKeyboard.y || pointOnKeyboard.y > yMax) {
                return false;
            }
            return true;
            */
        }

        /// <summary>
        /// Transforms all points of the LineRenderer LR such that they lie in x-y-range 0-1. (longer side 0-1, shorter side 0-(shorter side / longer side)).
        /// </summary>
        /// <param name="modify">boolean, if true, it changes some additional values (needed if this function is called, when the user ends writing)</param>
        /// <returns>A List of transformed points made from points of the LineRenderer</returns>
        public List<Vector2> GetTransformedPoints(bool modify) {
            List<Vector2> pointsList = new List<Vector2>();
            Vector3 point;
            for (int i = 0; i < LR.positionCount; i++) {
                point = LR.GetPosition(i);
                Vector3 localTransformedPoint = transform.parent.InverseTransformPoint(point);
                float keyboardLength = transform.localScale.x;
                float keyboardWidth = transform.localScale.y;
                pointsList.Add(new Vector2((localTransformedPoint.x + keyboardLength / 2) / Mathf.Max(keyboardLength, keyboardWidth), (localTransformedPoint.z + keyboardWidth / 2) / Mathf.Max(keyboardLength, keyboardWidth)));   // lower left corner of WGKeyboard is at (0/0)
            }
            if (modify) {
                pointCount = 0;
                LR.positionCount = 0;
                isLastDistShort = false;
            }
            return pointsList;
        }

        /// <summary>
        /// The LineRenderer would normally take all the points the user inputs. This function looks, if it is okay to drop the previous set point considering the new one.
        /// </summary>
        /// <param name="hitPoint">New Point to consider</param>
        async public void SamplePoints(Vector3 hitPoint) { // I worked with async, because FPS dropped from 90 to (worst case observed) around 40. Can't use Linerenderer functions in async Task.Run(), therefore worked with some "unnecessary" variables
            isSamplingPoints = true;
            int posCount = LR.positionCount;
            Vector3 startPoint = new Vector3(0, 0, 0);
            Vector3 middlePoint = new Vector3(0, 0, 0);
            if (posCount > 2) {
                startPoint = LR.GetPosition(pointCount - 2);
                middlePoint = LR.GetPosition(pointCount - 1);
            }
            await Task.Run(() => {
                if (pointCount < 3) {
                    pointCount++;
                } else {
                    Vector3 lastPoint;
                    float angle = Vector3.Angle(middlePoint - startPoint, hitPoint - middlePoint);
                    if (angle < minAngle || angle > 180 - minAngle) {
                        if (isLastDistShort) {
                            lastPoint = startPoint;
                        } else {
                            pointCount++;
                            lastPoint = middlePoint;
                        }
                        if ((hitPoint - lastPoint).magnitude < minSegmentDist) {
                            isLastDistShort = true;
                        } else {
                            isLastDistShort = false;
                        }
                    } else {
                        if (isLastDistShort) {
                            lastPoint = startPoint;
                        } else {
                            pointCount++;
                            lastPoint = middlePoint;
                        }
                        if ((hitPoint - lastPoint).magnitude < minSegmentDist / 5) {
                            isLastDistShort = true;
                        } else {
                            isLastDistShort = false;
                        }
                    }
                }
            });
            LR.positionCount = pointCount;
            LR.SetPosition(pointCount - 1, hitPoint);
            isSamplingPoints = false;
        }

        public bool GetIsSamplingPoints() {
            return isSamplingPoints;
        }
    }
}