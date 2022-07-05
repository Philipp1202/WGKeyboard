using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;  // for Max(), Min(), ...
using System.Threading.Tasks;

namespace WordGestureKeyboard {
    public class UserInputHandler {
        bool isSamplingPoints;
        LineRenderer LR;
        Transform transform;
        int pointCount;
        bool lastDistShort = false;

        public UserInputHandler(LineRenderer LR, Transform t) {
            isSamplingPoints = false;
            this.LR = LR;
            this.transform = t;
            pointCount = 0;
        }

        /// <summary>
        /// Casts a ray in the direction "forward" and looks, if it hits an Collider with the layer "WGKeyboard". 
        /// If yes, it return the point, where the ray hit the object, else it returns a Vector3(1000,1000,1000)
        /// </summary>
        /// <param name="colPos">Position of the origin of the ray (normally VR controller)</param>
        /// <param name="forward">Direction of the ray.</param>
        public Vector3 getHitPoint(Vector3 colPos, Vector3 forward) {
            int layerMask = LayerMask.GetMask("Default");
            RaycastHit hit;
            Vector3 transformedPoint = transform.parent.InverseTransformPoint(colPos);
            Vector3 point = new Vector3(transformedPoint.x, 0.008f, transformedPoint.z);
            transformedPoint = transform.parent.TransformPoint(point);
            Debug.Log("POINTTT " + transformedPoint);
            Debug.Log("ON KEYBOARD " + point);
            return transformedPoint;
            /*if (Physics.Raycast(colPos, forward, out hit, Mathf.Infinity, layerMask)) {
                Debug.Log("RIGHTPOINT " + hit.point);
                return hit.point;
            } else if (Physics.Raycast(colPos, -forward, out hit, Mathf.Infinity, layerMask)) {
                return hit.point;
                Debug.Log("RIGHTPOINT " + hit.point);
            }
            return new Vector3(1000, 1000, 1000);// return this, because Vector3 can't be null*/
        }

        public bool checkPoint(Vector3 point) {
            float keyboardLength = transform.localScale.x;
            float keyboardWidth = transform.localScale.y;
            Vector3 localTransformedPoint = transform.parent.InverseTransformPoint(point);
            Vector2 pointOnKeyboard = new Vector2((localTransformedPoint.x + keyboardLength / 2) / Mathf.Max(keyboardLength, keyboardWidth), (localTransformedPoint.z + keyboardWidth / 2) / Mathf.Max(keyboardLength, keyboardWidth));
            float xMax = 1;
            float yMax = 1;
            if (keyboardLength > keyboardWidth) {
                yMax = keyboardWidth / keyboardLength;
            } else {
                xMax = keyboardLength / keyboardWidth;
            }
            Debug.Log("CHECKPOINT VALUES: " + pointOnKeyboard + " : " + xMax + " : " + yMax);

            if (0 > pointOnKeyboard.x || pointOnKeyboard.x > xMax) {
                return false;
            }
            if (0 > pointOnKeyboard.y || pointOnKeyboard.y > yMax) {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Transforms all points of the LineRenderer to the local space of the parent of the wgkeyboard and normalizes them to be in x range 0-1. (used to transform user input into 2D space)
        /// </summary>
        public List<Vector2> getTransformedPoints() {   // gives point lieing in 0-1 on x-axis
            List<Vector2> pointsList = new List<Vector2>();
            Vector3 point;

            for (int i = 0; i < LR.positionCount; i++) {
                point = LR.GetPosition(i);
                Vector3 localTransformedPoint = transform.parent.InverseTransformPoint(point);
                float keyboardLength = transform.localScale.x;
                float keyboardWidth = transform.localScale.y;
                pointsList.Add(new Vector2((localTransformedPoint[0] + keyboardLength / 2) / keyboardLength, (localTransformedPoint[2] + keyboardWidth / 2) / keyboardLength)); // adding magnitudes, such that lower left corner of "coordinate system" is at (0/0) and not middle point at (0/0)
                //Debug.Log("PONT: " + pointsList[i]);
            }
            pointCount = 0;
            LR.positionCount = 0;
            lastDistShort = false;
            return pointsList;
        }

        public List<Vector2> getTransformedPoints2() {   // gives point lieing in 0-1 on x-axis
            List<Vector2> pointsList = new List<Vector2>();
            Vector3 point;

            for (int i = 0; i < LR.positionCount; i++) {
                point = LR.GetPosition(i);
                Vector3 localTransformedPoint = transform.parent.InverseTransformPoint(point);
                float keyboardLength = transform.localScale.x;
                float keyboardWidth = transform.localScale.y;
                pointsList.Add(new Vector2((localTransformedPoint[0] + keyboardLength / 2) / keyboardLength, (localTransformedPoint[2] + keyboardWidth / 2) / keyboardLength)); // adding magnitudes, such that lower left corner of "coordinate system" is at (0/0) and not middle point at (0/0)
                //Debug.Log("PONT: " + pointsList[i]);
            }
            return pointsList;
        }

        /// <summary>
        /// The LineRenderer would normally take all the points the user inputs. This function looks, if it's okay to drop some previous set points considering the new one.
        /// </summary>
        /// <param name="hitPoint">New Point to consider or to discard.</param>
        async public void samplePoints(Vector3 hitPoint) { // I worked with async, because FPS dropped from 90 to (worst case observed) around 40. Can't use Linerenderer functions in async Task.Run(), therefore worked with some "unnecessary" variables
            isSamplingPoints = true;
            int posCount = LR.positionCount;
            Debug.Log(posCount);
            bool setPoint = false;
            Vector3 startPoint = new Vector3(0, 0, 0);
            Vector3 middlePoint = new Vector3(0, 0, 0);
            if (posCount > 2) {
                startPoint = LR.GetPosition(pointCount - 2);
                middlePoint = LR.GetPosition(pointCount - 1);
            }
            //Debug.Log("BEFORE ASYNC");
            await Task.Run(() => {
                //Debug.Log("IN ASYNC");
                float minAngle = 10;
                float minSegmentDist = 0.015f;
                if (hitPoint != new Vector3(1000, 1000, 1000)) { // check if point needs to get set or if it can be ignored
                    if (pointCount < 3) {
                        pointCount++;
                        setPoint = true;
                    } else {
                        Vector3 lastPoint;
                        if (Vector3.Angle(middlePoint - startPoint, hitPoint - middlePoint) < minAngle) { // set Point if almost a straight line
                            if (lastDistShort) {
                                setPoint = true;
                                lastPoint = startPoint;
                            } else {
                                pointCount++;
                                setPoint = true;
                                lastPoint = middlePoint;
                            }
                            if ((hitPoint - lastPoint).magnitude < minSegmentDist) {
                                lastDistShort = true;
                            } else {
                                lastDistShort = false;
                            }
                        } else {
                            if (lastDistShort) {
                                setPoint = true;
                                lastPoint = startPoint;
                            } else {
                                pointCount++;
                                setPoint = true;
                                lastPoint = middlePoint;
                            }
                            if ((hitPoint - lastPoint).magnitude < minSegmentDist / 5) {
                                lastDistShort = true;
                            } else {
                                lastDistShort = false;
                            }
                        }
                    }
                }
            });
            if (setPoint) {
                LR.positionCount = pointCount;
                //print("HERE MIGHT BE AN ERROR: " + LR.positionCount + " , " + pointCount);
                LR.SetPosition(pointCount - 1, hitPoint);

            }
            setPoint = false;
            isSamplingPoints = false;
        }

        public bool getIsSampling() {
            return isSamplingPoints;
        }
    }
}