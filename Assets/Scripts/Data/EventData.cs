using System;
using System.Collections.Generic;

[Serializable]
public class EventItem
{
    public string id;
    public string category;
    public string title;
    public string era;
    public string description;
    public string storyText;
    public string imageName;
}

[Serializable]
public class EventDataList
{
    public List<EventItem> events;
}
