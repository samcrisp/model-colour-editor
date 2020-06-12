using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public static class VisualElementExtensions
{
    public static void SetVisible(this VisualElement element, bool visible) => element.EnableInClassList("hidden", !visible);
}
