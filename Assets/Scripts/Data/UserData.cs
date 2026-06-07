using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class UserData
{
    public string username;
    public string encryptedPassword;
    public string loginTime;
    public string avatar;
    public string themeStyle;
    public List<string> backpackItems;
}

[Serializable]
public class UserDataList
{
    public List<UserData> users;
}
