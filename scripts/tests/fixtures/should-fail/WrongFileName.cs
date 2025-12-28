// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

using UnityEngine;

// This file should FAIL: the class name doesn't match the filename
// File is named WrongFileName.cs but class is named ActualClassName
public class ActualClassName : MonoBehaviour
{
    public int value;
}
