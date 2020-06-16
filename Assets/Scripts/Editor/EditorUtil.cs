using UnityEngine;

namespace ModelColourEditor
{
    public static class EditorUtil
    {
        public static bool PlusButton(Rect position)
        {
            return GUI.Button(new Rect(position.x - 2f, position.y - 2f, 12f, 13f), GUIContent.none, (GUIStyle) "OL Plus");
        }

        public static bool MinusButton(Rect position)
        {
            return GUI.Button(new Rect(position.x - 2f, position.y - 2f, 12f, 13f), GUIContent.none, (GUIStyle) "OL Minus");
        }
    }
}