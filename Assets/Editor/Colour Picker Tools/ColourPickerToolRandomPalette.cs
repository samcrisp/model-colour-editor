using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ModelColourEditor
{
    [CreateAssetMenu(fileName = "New Colour Picker", menuName = "Model Colour Editor/Colour Picker")]
    public class ColourPickerToolRandomPalette : AbstractColourPickerTool
    {
        [HideInInspector]
        public Palette[] palettes;

        [HideInInspector]
        public Modifier[] finalModifiers;

        public override Color? GetColor(Mesh mesh)
        {
            Color? color = null;
            float random = UnityEngine.Random.value;
            float totalProbability = 0;
            for (int i = 0; i < palettes.Length; i++)
            {
                totalProbability += palettes[i].probability;
            }
            random *= totalProbability;

            for (int i = 0; i < palettes.Length; i++)
            {
                random -= palettes[i].probability;
                if (random <= 0)
                {
                    color = palettes[i].PickColor();
                    break;
                }
            }

            if (!color.HasValue) { return null; }

            foreach(var modifier in finalModifiers)
            {
                color = modifier.Apply(color.Value);
            }

            return color;
        }

        [Serializable]
        public class Palette
        {
            [Min(0)]
            public float probability;

            public Color[] colors;

            public Modifier[] modifiers;

            #if UNITY_EDITOR
            public int editor_percentage;
            #endif

            public Color PickColor()
            {
                Color color = colors.PickRandom();
                foreach(var modifier in modifiers)
                {
                    color = modifier.Apply(color);
                }
                return color;
            }
        }

        [Serializable]
        public class Modifier
        {
            public enum Type { Hue, Saturation, Value }

            public Type type;
            
            [Min(0)]
            public float range = 0.01f;

            #if UNITY_EDITOR
            public Color editor_preview = Color.red;
            #endif

            public Color Apply(Color color)
            {
                Color.RGBToHSV(color, out float h, out float s, out float v);

                switch (type)
                {
                    case ColourPickerToolRandomPalette.Modifier.Type.Hue:
                        h = Mathf.Repeat(h + UnityEngine.Random.Range(-range, range), 1);
                        break;

                    case ColourPickerToolRandomPalette.Modifier.Type.Saturation:
                        s = Mathf.Clamp01(s + UnityEngine.Random.Range(-range, range));
                        break;

                    case ColourPickerToolRandomPalette.Modifier.Type.Value:
                        v = Mathf.Clamp01(v + UnityEngine.Random.Range(-range, range));
                        break;

                }

                return Color.HSVToRGB(h, s, v);
            }
        }

        #if UNITY_EDITOR
        private void OnValidate()
        {
            if (palettes == null || palettes.Length == 0) { palettes = new Palette[] { new Palette() { probability = 1 } }; }

            float totalProbability = 0;

            foreach(var palette in palettes)
            {
                totalProbability += palette.probability;

                if (palette.colors == null || palette.colors.Length == 0) { palette.colors = new[] { Color.white }; }
            }

            if (totalProbability > 0)
            {
                foreach(var palette in palettes)
                {
                    palette.editor_percentage = Mathf.RoundToInt(palette.probability / totalProbability * 100);

                    if (palette.modifiers == null) { palette.modifiers = new Modifier[0]; }

                    foreach(var modifier in palette.modifiers)
                    {
                        modifier.editor_preview = palette.colors[0];
                    }
                }
            }

            if (finalModifiers == null) { finalModifiers = new Modifier[0]; }

            foreach(var modifier in finalModifiers)
            {
                modifier.editor_preview = palettes[0].colors[0];
            }
        }

        private void Reset()
        {
            OnValidate();
        }
        #endif
    }

    #if UNITY_EDITOR
    [CustomEditor(typeof(ColourPickerToolRandomPalette), true)]
    public class ColourPickerToolRandomPaletteDrawer : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawDefaultInspector();

            // Draw palettes
            SerializedProperty palettes = serializedObject.FindProperty("palettes");
            palettes.isExpanded = EditorGUILayout.Foldout(palettes.isExpanded, "Palettes", true);
            var style = new GUIStyle() { margin = new RectOffset(0,18,0,0) };

            if (palettes.isExpanded)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginVertical(style);
                for (int i = 0; i < palettes.arraySize; i++)
                {
                    SerializedProperty property = palettes.GetArrayElementAtIndex(i);

                    var removePaletteRect = new Rect(EditorGUILayout.GetControlRect(false, 0));
                    removePaletteRect.x = removePaletteRect.xMax + 3;
                    removePaletteRect.y += 2;
                    removePaletteRect.width = 16;
                    removePaletteRect.height = 16;

                    EditorGUILayout.PropertyField(property);
                    
                    if (palettes.arraySize > 1 && EditorUtil.MinusButton(removePaletteRect)) { palettes.DeleteArrayElementAtIndex(i); break; }

                    DrawUILine(Color.grey);
                }
                EditorGUILayout.EndVertical();
                EditorGUI.indentLevel--;

                var newPaletteRect = new Rect(EditorGUILayout.GetControlRect());
                newPaletteRect.x = newPaletteRect.xMax - 12;
                newPaletteRect.width = 16;
                if (EditorUtil.PlusButton(newPaletteRect)) { palettes.InsertArrayElementAtIndex(palettes.arraySize); }
            }


            // Draw modifiers
            SerializedProperty modifiers = serializedObject.FindProperty("finalModifiers");
            modifiers.isExpanded = EditorGUILayout.Foldout(modifiers.isExpanded, "Final Modifiers", true);

            if (modifiers.isExpanded)
            {
                EditorGUILayout.BeginVertical(style);
                EditorGUI.indentLevel++;
                for (int i = 0; i < modifiers.arraySize; i++)
                {
                    SerializedProperty property = modifiers.GetArrayElementAtIndex(i);

                    var removeModifierRect = new Rect(EditorGUILayout.GetControlRect(false, 0));
                    removeModifierRect.x = removeModifierRect.xMax + 5;
                    removeModifierRect.y += 4;
                    removeModifierRect.width = 16;
                    removeModifierRect.height = 16;

                    EditorGUILayout.PropertyField(property);

                    if (EditorUtil.MinusButton(removeModifierRect)) { modifiers.DeleteArrayElementAtIndex(i); break; }
                }

                if (modifiers.arraySize == 0)
                {
                    var italics = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Italic, clipping = TextClipping.Overflow, alignment = TextAnchor.UpperLeft };
                    EditorGUILayout.LabelField("None", italics, GUILayout.Height(0));
                }

                EditorGUILayout.EndVertical();
                EditorGUI.indentLevel--;

                var newModifierRect = new Rect(EditorGUILayout.GetControlRect());
                newModifierRect.x = newModifierRect.xMax - 12;
                newModifierRect.width = 16;
                if (EditorUtil.PlusButton(newModifierRect)) { modifiers.InsertArrayElementAtIndex(modifiers.arraySize); }
            }

            serializedObject.ApplyModifiedProperties();
        }

        public static void DrawUILine(Color color, int thickness = 1, int padding = 10)
        {
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
            r.height = thickness;
            r.y += padding / 2;
            r.x -= 2;
            r.width += 6;
            EditorGUI.DrawRect(r, color);
        }
    }

    [CustomPropertyDrawer(typeof(ColourPickerToolRandomPalette.Modifier))]
    public class ColourPickerToolRandomPaletteModifierDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var type = property.FindPropertyRelative("type");
            var range = property.FindPropertyRelative("range");
            var preview = property.FindPropertyRelative("editor_preview");

            const float TYPE_WIDTH = 92;
            const float PREVIEW_WIDTH = 30;

            var typeRect = new Rect(position.x, position.y, TYPE_WIDTH, position.height);
            var rangeRect = new Rect(typeRect.xMax, position.y, position.width - typeRect.width - PREVIEW_WIDTH, position.height);
            var previewRect = new Rect(rangeRect.xMax, position.y, PREVIEW_WIDTH, position.height);

            GetColor((ColourPickerToolRandomPalette.Modifier.Type)type.enumValueIndex, preview.colorValue, range.floatValue, out var startColor, out var endColor);
            var gradient = new Gradient() {
                colorKeys = new[] { new GradientColorKey(startColor, 0), new GradientColorKey(endColor, 1) }
            };

            EditorGUI.PropertyField(typeRect, type, GUIContent.none);
            EditorGUI.PropertyField(rangeRect, range, GUIContent.none);
            EditorGUI.GradientField(previewRect, gradient);
        }

        private void GetColor(ColourPickerToolRandomPalette.Modifier.Type type, Color input, float value, out Color start, out Color end)
        {
            Color.RGBToHSV(input, out float h, out float s, out float v);

            float diffH = 0;
            float diffS = 0;
            float diffV = 0;

            switch(type)
            {
                case ColourPickerToolRandomPalette.Modifier.Type.Hue:
                diffH = value;
                break;

                case ColourPickerToolRandomPalette.Modifier.Type.Saturation:
                diffS = value;
                break;

                case ColourPickerToolRandomPalette.Modifier.Type.Value:
                diffV = value;
                break;

            }

            start = Color.HSVToRGB(Mathf.Repeat(h - diffH, 1), Mathf.Clamp01(s - diffS), Mathf.Clamp01(v - diffV));
            end = Color.HSVToRGB(Mathf.Repeat(h + diffH, 1), Mathf.Clamp01(s + diffS), Mathf.Clamp01(v + diffV));
        }
    }

    [CustomPropertyDrawer(typeof(ColourPickerToolRandomPalette.Palette))]
    public class ColourPickerToolRandomPalettePaletteDrawer : PropertyDrawer
    {
        private const float LINE_SPACING = 19f;
        private const float LINE_HEIGHT = 17f;
        private const float BUTTON_SIZE = 12f;
        private const float MIN_COLOR_WIDTH = 50f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return LINE_SPACING * 3 + LINE_SPACING * property.FindPropertyRelative("modifiers").arraySize;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Get properties
            var probability = property.FindPropertyRelative("probability");
            var percentage = property.FindPropertyRelative("editor_percentage");
            var colors = property.FindPropertyRelative("colors");
            var modifiers = property.FindPropertyRelative("modifiers");

            // Get rects - https://twitter.com/CherieDavidson/status/583916667689832448
            var probabilityRect           = new Rect(position.x, position.y, position.width - 40, LINE_HEIGHT);
            var probabilityPercentageRect = new Rect(probabilityRect.xMax - 10, position.y, 50, LINE_HEIGHT);
            
            float colorsWidth   = position.width - EditorGUIUtility.labelWidth - BUTTON_SIZE - 12;
            var colorsLabelRect = new Rect(position.x, position.y + LINE_SPACING, EditorGUIUtility.labelWidth, LINE_HEIGHT);
            var colorsRect      = new Rect(position.x + EditorGUIUtility.labelWidth, position.y + LINE_SPACING, colorsWidth, LINE_HEIGHT);
            var colorsNewRect   = new Rect(position.xMax - BUTTON_SIZE, position.y + LINE_SPACING, BUTTON_SIZE, LINE_HEIGHT);
            
            float modifiersWidth   = position.width - EditorGUIUtility.labelWidth - 6;
            var modifiersLabelRect = new Rect(position.x, position.y + LINE_SPACING * 2, EditorGUIUtility.labelWidth, LINE_HEIGHT);
            var modifiersRect      = new Rect(position.x + EditorGUIUtility.labelWidth - 12, position.y + LINE_SPACING * 2, modifiersWidth, LINE_SPACING * modifiers.arraySize);
            var modifiersNewRect   = new Rect(position.xMax - BUTTON_SIZE, position.y + LINE_SPACING * 2 + LINE_SPACING * modifiers.arraySize, BUTTON_SIZE, LINE_HEIGHT);

            // Draw probability
            EditorGUI.PropertyField(probabilityRect, probability);
            EditorGUI.LabelField(probabilityPercentageRect, $"{percentage.intValue}%");

            // Draw colors
            int colorCount = colors.arraySize;
            if (EditorUtil.PlusButton(colorsNewRect)) { colors.InsertArrayElementAtIndex(colorCount); colorCount = colors.arraySize; }
            EditorGUI.LabelField(colorsLabelRect, colorCount > 1 ? "Colours" : "Colour");

            float width = colorsRect.width / colorCount;
            for (int i = 0; i < colorCount; i++)
            {
                var color = colors.GetArrayElementAtIndex(i);
                var colorRect = new Rect(colorsRect.x + width * i - 12, colorsRect.y, width - 6, LINE_HEIGHT);
                var buttonRect = new Rect(colorRect.xMax + 6, colorsRect.y, BUTTON_SIZE, LINE_HEIGHT);
                color.colorValue = EditorGUI.ColorField(colorRect, GUIContent.none, color.colorValue, false, true, false);
                if (colorCount > 1 && EditorUtil.MinusButton(buttonRect)) { colors.DeleteArrayElementAtIndex(i); break; }
            }

            // Draw modifiers
            int modifierCount = modifiers.arraySize;
            if (EditorUtil.PlusButton(modifiersNewRect)) { modifiers.InsertArrayElementAtIndex(modifierCount); modifierCount = modifiers.arraySize; }
            EditorGUI.LabelField(modifiersLabelRect, "Modifiers");

            for (int i = 0; i < modifierCount; i++)
            {
                var modifier = modifiers.GetArrayElementAtIndex(i);
                var modifierRect = new Rect(modifiersRect.x, modifiersRect.y + LINE_SPACING * i, modifiersRect.width, LINE_HEIGHT);
                var buttonRect = new Rect(modifierRect.xMax + 6, modifierRect.y, BUTTON_SIZE, LINE_HEIGHT);
                EditorGUI.PropertyField(modifierRect, modifier);
                if (EditorUtil.MinusButton(buttonRect)) { modifiers.DeleteArrayElementAtIndex(i); break; }
            }

            if (modifierCount == 0)
            {
                var modifierRect = new Rect(modifiersRect.x, modifiersRect.y + 2, modifiersRect.width, LINE_HEIGHT);
                var italics = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Italic };
                EditorGUI.LabelField(modifierRect, "None", italics);
            }

            EditorGUI.EndProperty();
        }
    }
    #endif
}