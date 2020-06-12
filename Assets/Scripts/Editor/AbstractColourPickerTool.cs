using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AbstractColourPickerTool : ScriptableObject
{
    public abstract Color? GetColor(Mesh mesh);
}
