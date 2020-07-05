using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ModelColourEditor
{
    public class ModelColourEditorSettings : ScriptableObject
    {
        private const string EDITOR_PREFS_KEY = "ModelColourEditor.Settings";

        public static bool HasAsset { get; private set; }

        private static ModelColourEditorSettings _instance;
        public static ModelColourEditorSettings Instance
        {
            get
            {
                // Save reference to settings asset in EditorPrefs. If there is none set, look for an existing asset of that type and set it.
                // Wish we could save this in ProjectSettings so it could be saved to source control but that's not currently possible with Unity.

                if (_instance == null) 
                {
                    string guid = EditorPrefs.GetString(EDITOR_PREFS_KEY, string.Empty);
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    ModelColourEditorSettings asset = (path == string.Empty) ? null : AssetDatabase.LoadAssetAtPath(path, typeof(ModelColourEditorSettings)) as ModelColourEditorSettings;

                    if (asset == null)
                    {
                        var assets = AssetDatabase.FindAssets("t:ModelColourEditorSettings");
                        if (assets.Length > 0)
                        {
                            path = AssetDatabase.GUIDToAssetPath(assets[0]);
                            EditorPrefs.SetString(EDITOR_PREFS_KEY, assets[0]);
                            asset = AssetDatabase.LoadAssetAtPath<ModelColourEditorSettings>(path);
                        }
                    }

                    _instance = asset;
                }

                HasAsset = _instance != null;

                return _instance;
            }

            set
            {
                _instance = value;
                
                string path = AssetDatabase.GetAssetPath(value);
                string guid = AssetDatabase.AssetPathToGUID(path);
                EditorPrefs.SetString(EDITOR_PREFS_KEY, guid);

                HasAsset = _instance != null;
            }
        }

        public Material defaultMaterial;
        public bool importMaterialColoursByDefault;
    }

    public static class ModelColourEditorSettingsRegister
    {
        private static ModelColourEditorSettingsElement _settings;

        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            var provider = new SettingsProvider("Project/ModelColourEditorSettings", SettingsScope.Project)
            {
                label = "Model Colour Editor",
                activateHandler = (searchContext, rootElement) =>
                {
                    rootElement.style.paddingBottom = rootElement.style.paddingTop = 20;
                    rootElement.style.paddingLeft = rootElement.style.paddingRight = 25;

                    TextElement header = new TextElement();
                    header.text = "Model Colour Editor";
                    header.style.fontSize = 18;
                    header.style.unityFontStyleAndWeight = FontStyle.Bold;
                    header.style.paddingBottom = 15;
                    rootElement.Add(header);

                    ScrollView scrollView = new ScrollView();
                    rootElement.Add(scrollView);

                    _settings = new ModelColourEditorSettingsElement();
                    scrollView.Add(_settings);
                },
                // inspectorUpdateHandler = () => { _settings.Update(); },
                keywords = new HashSet<string>(new[] { "Model", "Colour", "Color" }),
            };

            return provider;
        }
    }

    public class ModelColourEditorSettingsElement : VisualElement
    {
        private Editor _settingsEditor;
        private ObjectField _settingsObjectField;
        private Button _button;
        private VisualElement _warningBox;
        private bool _hasMultipleEditors;
        private IMGUIContainer _imguiContainer;

        public new class UxmlFactory : UxmlFactory<ModelColourEditorSettingsElement> {}

        public ModelColourEditorSettingsElement()
        {
            TextElement label = new Label();
            label.text = "Model Colour Editor Settings";
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            this.Add(label);

            VisualElement container = new VisualElement();
            container.style.display = DisplayStyle.Flex;
            container.style.flexDirection = FlexDirection.Row;
            this.Add(container);

            // Save reference to settings asset in EditorPrefs. If there is none set, look for an existing asset of that type and set it.
            // Wish we could save this in ProjectSettings so it could be saved to source control but that's not currently possible with Unity.

            _settingsObjectField = new ObjectField();
            _settingsObjectField.objectType = typeof(ModelColourEditorSettings);
            _settingsObjectField.RegisterValueChangedCallback(OnSettingsChanged);
            _settingsObjectField.style.flexShrink = 1;
            _settingsObjectField.style.flexGrow = 1;
            container.Add(_settingsObjectField);

            _button = new Button();
            _button.text = "Create Settings Asset";
            _button.clicked += OnNewSettingsButtonClicked;
            _button.style.flexShrink = 1;
            _button.style.flexGrow = 1;
            container.Add(_button);

            _warningBox = new VisualElement();
            _warningBox.style.flexDirection = FlexDirection.Row;
            _warningBox.style.alignItems = Align.Center;
            _warningBox.style.marginBottom = _warningBox.style.marginRight = _warningBox.style.marginLeft = _warningBox.style.marginTop = 2;
            _warningBox.style.paddingBottom = _warningBox.style.paddingRight = _warningBox.style.paddingLeft = _warningBox.style.paddingTop = 1;
            _warningBox.style.borderBottomColor = _warningBox.style.borderRightColor = _warningBox.style.borderLeftColor = _warningBox.style.borderTopColor = new StyleColor(new Color32(169, 169, 169, 255));
            _warningBox.style.backgroundColor = new StyleColor(new Color32(202, 202, 202, 255));
            _warningBox.style.borderTopLeftRadius = _warningBox.style.borderTopRightRadius = _warningBox.style.borderBottomLeftRadius = _warningBox.style.borderBottomRightRadius = 3;
            _warningBox.style.fontSize = 10;
            _warningBox.AddToClassList("unity-box");
            var warningBoxImage = new Image() { image = EditorGUIUtility.FindTexture("console.warnicon"), scaleMode = ScaleMode.ScaleToFit };
            var warningBoxLabel = new Label("More than one ModelColourEditorSettings asset exists in the project. This is not recommended because the plugin will automatically select the first one it finds to use regardless of which asset is selected here.");
            warningBoxLabel.style.whiteSpace = WhiteSpace.Normal;
            warningBoxLabel.style.marginRight = 20;
            warningBoxImage.style.flexShrink = 0;
            _warningBox.Add(warningBoxImage);
            _warningBox.Add(warningBoxLabel);
            this.Add(_warningBox);

            UpdateSettingsField();

            if (!_settingsEditor || _settingsEditor.target != ModelColourEditorSettings.Instance) { CreateCachedEditor(); }

            _imguiContainer = new IMGUIContainer();
            _imguiContainer.onGUIHandler = OnSettingsInspectorGUI;
            _imguiContainer.style.marginTop = 10;
            this.Add(_imguiContainer);
        }

        public void OnSettingsInspectorGUI()
        {
            UpdateSettingsField();

            if (_settingsEditor?.target == null)
            {
                CreateCachedEditor();
            }

            _settingsEditor.OnInspectorGUI();
        }

        private void UpdateSettingsField()
        {
            _settingsObjectField.value = ModelColourEditorSettings.Instance;

            StyleEnum<DisplayStyle> empty = new StyleEnum<DisplayStyle>();
            _settingsObjectField.style.display = !ModelColourEditorSettings.HasAsset ? DisplayStyle.None : empty;
            _button.style.display = ModelColourEditorSettings.HasAsset ? DisplayStyle.None : empty;

            _hasMultipleEditors = AssetDatabase.FindAssets("t:ModelColourEditorSettings").Length > 1;
            _warningBox.style.display = _hasMultipleEditors ? empty : DisplayStyle.None;
        }

        private void CreateCachedEditor()
        {
            ModelColourEditorSettings settings = ModelColourEditorSettings.Instance;

            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<ModelColourEditorSettings>();
            }

            Editor.CreateCachedEditor(settings, typeof(ModelColourEditorSettingsInspector), ref _settingsEditor);

            // if (_imguiContainer != null)
            // {
            //     _imguiContainer.onGUIHandler = _settingsEditor.OnInspectorGUI;
            // }
        }

        private void OnSettingsChanged(ChangeEvent<UnityEngine.Object> evt)
        {
            if (evt.newValue == evt.previousValue) { return; }
            ModelColourEditorSettings.Instance = evt.newValue as ModelColourEditorSettings;
            CreateCachedEditor();
        }

        private void OnNewSettingsButtonClicked()
        {
            string path = EditorUtility.SaveFilePanelInProject("Create new Model Colour Editor Settings asset", "Model Colour Editor Settings", "asset", "Please enter a file name to save the asset to");
            if (path.Length == 0) { return; }
            var asset = ScriptableObject.CreateInstance<ModelColourEditorSettings>();
            AssetDatabase.CreateAsset(asset, path);
            ModelColourEditorSettings.Instance = asset;

            UpdateSettingsField();
            CreateCachedEditor();
        }
    }

    public class ModelColourEditorSettingsInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            bool enabled = GUI.enabled;
            float labelWidth = EditorGUIUtility.labelWidth;

            GUI.enabled = ModelColourEditorSettings.HasAsset;
            EditorGUIUtility.labelWidth = 200;

            Editor.DrawPropertiesExcluding(serializedObject, new[] { "m_Script" });

            GUI.enabled = enabled;
            EditorGUIUtility.labelWidth = labelWidth;

            serializedObject.ApplyModifiedProperties();
        }
    }
}