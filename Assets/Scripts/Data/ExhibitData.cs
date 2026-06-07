using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ExhibitData
{
    public string id;
    public string name;
    public string category;
    public string description;
    public string history;
    public string craft;
    public string meaning;
    public string modelType;
    public string imageName;
}

[Serializable]
public class ExhibitDataList
{
    public List<ExhibitData> exhibits;
}
