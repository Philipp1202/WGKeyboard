using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class staticTest : MonoBehaviour
{
    public static List<GameObject> objectsList = new List<GameObject> ();
    // Start is called before the first frame update
    void Start()
    {
        objectsList.Add(gameObject);
        print(objectsList.Count);
        print(objectsList[objectsList.Count - 1]);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
