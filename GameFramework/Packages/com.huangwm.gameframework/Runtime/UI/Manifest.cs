using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Manifest : MonoBehaviour
{
    public List<NameToObject> list = new List<NameToObject>();
}

public class NameToObject : ScriptableObject
{ 
    public string name;
    public GameObject obj;
}
