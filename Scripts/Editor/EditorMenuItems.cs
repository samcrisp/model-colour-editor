using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ModelColourEditor
{
    public static class EditorMenuItems
    {
        private const string TOP = "Window/";

        [MenuItem(TOP + "Open Model Colour Editor", false, 50)]
        public static void OpenModelColourEditor()
        {
            ModelColourEditor.OpenWindow();
        }
    }
}