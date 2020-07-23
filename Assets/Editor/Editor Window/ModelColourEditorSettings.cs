using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.UI;
using UnityEngine.Rendering;

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

                HasAsset = _instance != null && PrefabUtility.IsPartOfPrefabAsset(_instance);
                
                if (_instance == null)
                {
                    _instance = ScriptableObject.CreateInstance<ModelColourEditorSettings>();
                }

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
            #if !EDITOR_COROUTINES
            var infoBox = CreateInfoBox("Editor Coroutines package is not installed.\n\nModel Colour Editor will still work but might cause the editor to pause when selecting objects.\n\nIf the package is installed in the Packages folder rather than the Assets folder this dependency should be resolved automatically.\n");
            var infoBoxContents = infoBox.Q("contents");
            var addPackageButton = new Button();
            addPackageButton.text = "Install Editor Coroutines package";
            addPackageButton.clicked += () =>
            {
                infoBoxContents.SetEnabled(false);
                addPackageButton.text = "Installing...";
                Client.Add("com.unity.editorcoroutines");
            };
            infoBoxContents.Add(addPackageButton);
            var moveToPackagesFolderButton = new Button();
            moveToPackagesFolderButton.text = "Move plugin to Packages folder";
            moveToPackagesFolderButton.clicked += () =>
            {
                var settingsAssetPath = Directory
                    .GetParent(AssetDatabase.GetAssetPath(
                        MonoScript.FromScriptableObject(ModelColourEditorSettings.Instance)));
                var directoryInfo = settingsAssetPath?.Parent?.Parent;
                if (directoryInfo == null || directoryInfo.Name != "Model Colour Editor")
                {
                    Debug.LogWarning("Plugin folder has been modified. Cannot move to Packages folder. Try moving the plugin folder manually.");
                    return;
                }

                var packagePath = directoryInfo.FullName;
                infoBoxContents.SetEnabled(false);
                moveToPackagesFolderButton.text = "Moving...";
                
                FileUtil.MoveFileOrDirectory(packagePath, $"Packages/{directoryInfo.Name}");

                var metaFile = packagePath + ".meta";
                if (File.Exists(metaFile))
                {
                    FileUtil.MoveFileOrDirectory(metaFile, $"Packages/Model Colour Editor.meta");
                }
                
                AssetDatabase.Refresh();
            };
            infoBoxContents.Add(moveToPackagesFolderButton);
            this.Add(infoBox);
            #endif
            
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

            _warningBox = CreateInfoBox(
                "More than one ModelColourEditorSettings asset exists in the project. This is not recommended because the plugin will automatically select the first one it finds to use regardless of which asset is selected here.");
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
            Editor.CreateCachedEditor(ModelColourEditorSettings.Instance, typeof(ModelColourEditorSettingsInspector), ref _settingsEditor);
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

        private VisualElement CreateInfoBox(string text)
        {
            var borderColor = EditorGUIUtility.isProSkin ? new Color32(35, 35, 35, 255) : new Color32(169, 169, 169, 255);
            var backgroundColor = EditorGUIUtility.isProSkin ? new Color32(64, 64, 64, 255) : new Color32(202, 202, 202, 255);
            
            var infoBox = new VisualElement();
            infoBox.style.flexDirection = FlexDirection.Row;
            infoBox.style.alignItems = Align.Center;
            infoBox.style.marginBottom = infoBox.style.marginRight = infoBox.style.marginLeft = infoBox.style.marginTop = 2;
            infoBox.style.paddingBottom = infoBox.style.paddingRight = infoBox.style.paddingLeft = infoBox.style.paddingTop = 1;
            infoBox.style.borderBottomColor = infoBox.style.borderRightColor = infoBox.style.borderLeftColor = infoBox.style.borderTopColor = new StyleColor(borderColor);
            infoBox.style.backgroundColor = new StyleColor(backgroundColor);
            infoBox.style.borderTopLeftRadius = infoBox.style.borderTopRightRadius = infoBox.style.borderBottomLeftRadius = infoBox.style.borderBottomRightRadius = 3;
            infoBox.style.fontSize = 10;
            infoBox.AddToClassList("unity-box");
            var image = new Image() { image = EditorGUIUtility.FindTexture("console.warnicon"), scaleMode = ScaleMode.ScaleToFit };
            var contents = new VisualElement {name = "contents"};
            var label = new Label(text);
            label.style.whiteSpace = WhiteSpace.Normal;
            image.style.flexShrink = 0;
            contents.Add(label);
            infoBox.Add(image);
            infoBox.Add(contents);
            return infoBox;
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
            
            // Draw default material field and autofill button
            var defaultMaterialProperty = serializedObject.FindProperty("defaultMaterial");
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(defaultMaterialProperty);
            var icon = "TreeEditor.Material";
            if (EditorGUIUtility.isProSkin) { icon = "d_" + icon; }
            var iconContent = EditorGUIUtility.IconContent(icon);
            iconContent.tooltip = "Use example PBR vertex colour material as default";
            if (GUILayout.Button(iconContent, GUILayout.Width(18), GUILayout.Height(18)))
            {
                UseExampleMaterialAsDefault(defaultMaterialProperty);
            }
            EditorGUILayout.EndHorizontal();
            
            Editor.DrawPropertiesExcluding(serializedObject, new[] { "m_Script", "defaultMaterial" });

            GUI.enabled = enabled;
            EditorGUIUtility.labelWidth = labelWidth;

            serializedObject.ApplyModifiedProperties();
        }

        private void UseExampleMaterialAsDefault(SerializedProperty defaultMaterialProperty)
        {
            var settingsAssetPath = Directory
                .GetParent(AssetDatabase.GetAssetPath(
                    MonoScript.FromScriptableObject(ModelColourEditorSettings.Instance)));

            var errorMessage = "Cannot find example material";
            if (settingsAssetPath == null) { Debug.LogWarning(errorMessage); return; }
            settingsAssetPath = settingsAssetPath.Parent;
            if (settingsAssetPath == null) { Debug.LogWarning(errorMessage); return; }
            settingsAssetPath = settingsAssetPath.Parent;
            if (settingsAssetPath == null) { Debug.LogWarning(errorMessage); return; }

            var path = Path.Combine(settingsAssetPath.FullName, "Examples\\Materials\\");
            path = path.Replace(Directory.GetParent(Application.dataPath).FullName + "\\", string.Empty);
            path = Regex.Replace(path, @"^Library\\PackageCache\\.+?\\", "Packages\\com.skatebee.model-colour-editor\\");

            var currentRenderPipeline = GraphicsSettings.currentRenderPipeline;
            if (currentRenderPipeline != null)
            {
                var renderPipelineType = currentRenderPipeline.GetType().Name;
                if (renderPipelineType.Contains("HDRenderPipelineAsset"))
                {
                    path += "mat_hdrp_vertexColour_lit.mat";
                }
                else if (renderPipelineType.Contains("UniversalRenderPipelineAsset"))
                {
                    path += "mat_urp_vertexColour_lit.mat";
                }
                else
                {
                    path += "mat_inbuilt_vertexColour_lit.mat"; 
                }
            }
            else
            {
                path += "mat_inbuilt_vertexColour_lit.mat";
;           }

            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            defaultMaterialProperty.objectReferenceValue = material;
        }
    }
}