using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ModelColourEditor
{
    public abstract class AbstractColourPickerTool : ScriptableObject
    {
        public abstract Color? GetColor(Mesh mesh);
    }
}
