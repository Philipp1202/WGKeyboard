using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;  // for Max(), Min(), ...
using System.Threading.Tasks;

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
    
    public Vector3 getHitPoint(Vector3 colPos, Vector3 forward) {
        if (isSamplingPoints) {
            int layerMask = LayerMask.GetMask("WGKeyboard");
            RaycastHit hit;
            if (Physics.Raycast(colPos, forward, out hit, Mathf.Infinity, layerMask)) {
                return hit.point;
            } else if (Physics.Raycast(colPos, forward, out hit, Mathf.Infinity, layerMask)) {
                return hit.point;
            }
            return new Vector3(1000,1000,1000); // return this, because Vector3 can't be null
        }
        return new Vector3(1000, 1000, 1000);
    }

    public List<Vector2> getTransformedPoints() {
        List<Vector2> pointsList = new List<Vector2>();
        Vector3 point;

        for (int i = 0; i < LR.positionCount; i++) {
            point = LR.GetPosition(i);
            Vector3 localTransformedPoint = transform.parent.InverseTransformPoint(point);
            float keyboardLength = transform.localScale.x;
            float keyboardWidth = transform.localScale.y;
            pointsList.Add(new Vector2((localTransformedPoint[0] + keyboardLength / 2) / keyboardLength, (localTransformedPoint[2] + keyboardWidth / 2) / keyboardLength)); // adding magnitudes, such that lower left corner of "coordinate system" is at (0/0) and not middle point at (0/0)
        }
        pointCount = 0;
        LR.positionCount = 0;
        lastDistShort = false;
        return pointsList;
    }

    async public void samplePoints(Vector3 hitPoint) { // I worked with async, because FPS dropped from 90 to (worst case observed) around 40. Can't use Linerenderer functions in async Task.Run(), therefore worked with some "unnecessary" variables
        isSamplingPoints = true;
        int posCount = LR.positionCount;
        bool setPoint = false;
        Vector3 startPoint = new Vector3(0, 0, 0);
        Vector3 middlePoint = new Vector3(0, 0, 0);
        if (posCount > 2) {
            startPoint = LR.GetPosition(pointCount - 2);
            middlePoint = LR.GetPosition(pointCount - 1);
        }
        await Task.Run(() => {
            float minAngle = 10;
            float minSegmentDist = 0.015f;
            if (hitPoint != new Vector3(0, 0)) { // check if point needs to get set or if it can be ignored
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
}
