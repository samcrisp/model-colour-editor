using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.EditorCoroutines.Editor;

namespace ModelColourEditor
{
    public class ModelColourEditor : EditorWindow
    {
        public const float MAX_MILLISECONDS_PER_FRAME = 33;

        private delegate CustomAssetData MeshDataAction(Mesh mesh, int materialIndex, CustomAssetData data);
        private delegate CustomAssetData ModelDataAction(GameObject asset, CustomAssetData data);

        private List<Mesh> _selectedMeshes = new List<Mesh>();
        private List<GameObject> _selectedModels = new List<GameObject>();
        private List<CustomAssetData.MeshColor> _previewColours = new List<CustomAssetData.MeshColor>();
        private Dictionary<Mesh, GameObject> _meshModelDictionary = new Dictionary<Mesh, GameObject>();
        private Dictionary<GameObject, CustomAssetData> _modelDataDictionary = new Dictionary<GameObject, CustomAssetData>();

        private ObjectField _modelField;
        private ObjectField _meshField;
        private VisualElement _modelsSelected;
        private TextElement _modelsSelectedCountElement;
        private VisualElement _meshesSelected;
        private TextElement _meshesSelectedCountElement;
        private VisualElement _sceneReferences;
        private TextElement _sceneReferencesCountElement;
        private TextElement _influencesModelVertexColours;
        private TextElement _influencesModelMaterialColours;
        private TextElement _influencesCustomOverrideColours;
        private ModelColourEditorPreviewElement _previewColorElement;
        private IMGUIContainer _previewModel;
        private ColorField _colourPicker;
        private bool _hasSelection;
        private int _sceneReferencesCount;
        private bool _hasEditorVertexColour;
        private bool _hasMaterialImportColour;
        private Editor _editor;
        private Button _setColourButton;
        private Button _removeColourButton;
        private VisualElement _importMaterialColoursGroup;
        private VisualElement _allMaterialColoursGroup;
        private IMGUIContainer _colourPickerInlineScriptableObject;
        private Button _reimportedSelectedButton;
        private ObjectField _colourPickerAsset;
        private Foldout _colourPickerAssetFoldout;
        private Button _colourPickerSetColourButton;
        private Button _colourPickerNewButton;
        private EditorCoroutine _coroutine;
        private System.Diagnostics.Stopwatch _stopwatch = new System.Diagnostics.Stopwatch();
        private float _stopwatchThreshold;
        private Queue<System.Action> _taskQueue = new Queue<System.Action>();
        private List<MeshFilter> _meshFilters;
        private Button _tabEditorButton;
        private Button _tabSettingsButton;
        private int _tabIndex;
        private VisualElement _tabEditor;
        private VisualElement _tabSettings;
        private Editor _colourPickerInlineScriptableObjectEditor;

        public static void OpenWindow()
        {
            var window = (ModelColourEditor)EditorWindow.GetWindow(typeof(ModelColourEditor));
            window.Show();
            
            window.minSize = new Vector2(400, 200);
            window.titleContent = new GUIContent("Model Colour Editor");
        }

        private void OnEnable()
        {
            Build();

            Selection.selectionChanged += OnSelectionChanged;

            var settings = JsonUtility.FromJson<EditorSettings>(EditorPrefs.GetString("ModelColourEditorSettings"));
            _colourPicker.value = settings.selectedColor;
            _colourPickerAsset.value = AssetDatabase.LoadAssetAtPath<AbstractColourPickerTool>(AssetDatabase.GUIDToAssetPath(settings.colourPickerTool));

            OnColourPickerChanged(_colourPickerAsset.value);
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged; 

            var settings = new EditorSettings()
            {
                selectedColor = _colourPicker.value,
                colourPickerTool = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(_colourPickerAsset.value))
            };

            EditorPrefs.SetString("ModelColourEditorSettings", JsonUtility.ToJson(settings));
        }

        private void Build()
        {
            var template = Resources.Load<VisualTreeAsset>(Resource.MODEL_COLOUR_EDITOR_TEMPLATE);
            var styles = Resources.Load<StyleSheet>(Resource.MODEL_COLOUR_STYLES);

            template.CloneTree(rootVisualElement);
            rootVisualElement.styleSheets.Add(styles);
            
            if (EditorGUIUtility.isProSkin) { rootVisualElement.AddToClassList("dark-theme"); }

            // Get references

            _tabEditorButton = rootVisualElement.Q<Button>("tabEditorButton");
            _tabEditorButton.clicked += OnTabEditorButtonClicked;
            _tabSettingsButton = rootVisualElement.Q<Button>("tabSettingsButton");
            _tabSettingsButton.clicked += OnTabSettingsButtonClicked;
            _tabEditor = rootVisualElement.Q<VisualElement>("tabEditor");
            _tabSettings = rootVisualElement.Q<VisualElement>("tabSettings");
            UpdateTabs();

            _modelField = rootVisualElement.Q<ObjectField>("selectionModel");
            _modelField.objectType = typeof(GameObject);
            
            _meshField = rootVisualElement.Q<ObjectField>("selectionMesh");
            _meshField.objectType = typeof(Mesh);

            _modelsSelected = rootVisualElement.Q<VisualElement>("selectionModelsSelected");
            _modelsSelectedCountElement = _modelsSelected.Q<TextElement>("selectionModelsSelectedCount");
            _meshesSelected = rootVisualElement.Q<VisualElement>("selectionMeshesSelected");
            _meshesSelectedCountElement = _meshesSelected.Q<TextElement>("selectionMeshesSelectedCount");
            _sceneReferences = rootVisualElement.Q<VisualElement>("selectionSceneReference");
            _sceneReferencesCountElement = _sceneReferences.Q<TextElement>("selectionSceneReferenceCount");

            _influencesModelVertexColours = rootVisualElement.Q<TextElement>("influencesModelVertexColours");
            _influencesModelMaterialColours = rootVisualElement.Q<TextElement>("influencesModelMaterialColours");
            _influencesCustomOverrideColours = rootVisualElement.Q<TextElement>("influencesCustomOverrideColours");

            _previewColorElement = rootVisualElement.Q<ModelColourEditorPreviewElement>("previewColor");
            _previewColorElement.SetOverrideColorEvent += color => _colourPicker.value = color;

            _previewModel = rootVisualElement.Q<IMGUIContainer>("previewModel");
            _previewModel.onGUIHandler = OnDrawPreviewModelGUI;

            _colourPicker = rootVisualElement.Q<ColorField>("setColourColourPicker");

            _setColourButton = rootVisualElement.Q<Button>("setColourButton");
            _setColourButton.clicked += SetColour;
            _removeColourButton = rootVisualElement.Q<Button>("removeColourButton");
            _removeColourButton.clicked += RemoveColour;

            _importMaterialColoursGroup = rootVisualElement.Q<VisualElement>("importMaterialColoursGroup");
            _allMaterialColoursGroup = rootVisualElement.Q<VisualElement>("allMaterialColoursGroup");
            var enableAllMaterialColoursButton = rootVisualElement.Q<Button>("enableAllMaterialColoursButton");
            var disableAllMaterialColoursButton = rootVisualElement.Q<Button>("disableAllMaterialColoursButton");
            var resetAllMaterialColoursButton = rootVisualElement.Q<Button>("resetAllMaterialColoursButton");
            enableAllMaterialColoursButton.clicked += EnableMaterialColours;
            disableAllMaterialColoursButton.clicked += DisableMaterialColours;
            resetAllMaterialColoursButton.clicked += ResetMaterialColours;

            _colourPickerAsset = rootVisualElement.Q<ObjectField>("colourPickerAsset");
            _colourPickerAsset.objectType = typeof(AbstractColourPickerTool);
            _colourPickerAsset.RegisterValueChangedCallback(evt => OnColourPickerChanged(evt.newValue));

            _colourPickerAssetFoldout = rootVisualElement.Q<Foldout>("colourPickerAssetFoldout");
            _colourPickerAssetFoldout.RegisterValueChangedCallback(OnColourPickerFoldoutChanged);
            
            _colourPickerSetColourButton = rootVisualElement.Q<Button>("colourPickerSetColourButton");
            _colourPickerSetColourButton.clicked += SetColourPickerColours;
            
            _colourPickerNewButton = rootVisualElement.Q<Button>("newColourPickerButton");
            BuildNewColourPickerMenu();
            _colourPickerNewButton.RegisterCallback<MouseDownEvent>(NewColourPicker);

            _colourPickerInlineScriptableObject = rootVisualElement.Q<IMGUIContainer>("colourPickerInlineScriptableObject");
            _colourPickerInlineScriptableObject.onGUIHandler = OnColourPickerInlineScriptableObjectInspectorGUI;

            _reimportedSelectedButton = rootVisualElement.Q<Button>("reimportedSelectedButton");
            _reimportedSelectedButton.clicked += ReimportSelected;

            OnSelectionChanged();
        }

        private void OnTabSettingsButtonClicked()
        {
            _tabIndex = 1;
            UpdateTabs();
        }

        private void OnTabEditorButtonClicked()
        {
            _tabIndex = 0;
            UpdateTabs();
        }

        private void UpdateTabs()
        {
            _tabEditorButton.EnableInClassList("active", _tabIndex == 0);
            _tabSettingsButton.EnableInClassList("active", _tabIndex == 1);

            _tabEditor.SetVisible(_tabIndex == 0);
            _tabSettings.SetVisible(_tabIndex == 1);
        }

        private void OnDrawPreviewModelGUI()
        {
            _editor?.OnInteractivePreviewGUI(_previewModel.contentRect, null);
        }

        private void OnSelectionChanged()
        {
            if (_coroutine != null) { this.StopCoroutine(_coroutine); }
            _taskQueue.Clear();

            GetSelection();

            _coroutine = this.StartCoroutine(RunQueue());
        }

        private void GetSelection()
        {
            _selectedMeshes.Clear();
            _selectedModels.Clear();
            _previewColours.Clear();

            _hasSelection = Selection.activeObject != null;
            
            foreach(var selection in Selection.objects)
            {
                if (selection is Mesh)
                {
                    Mesh mesh = selection as Mesh;
                    _taskQueue.Enqueue(() => GetMeshColorsCoroutine(mesh));
                }
                else if (selection is GameObject)
                {
                    var meshFilters = (selection as GameObject).GetComponentsInChildren<MeshFilter>();
                    IEnumerable<Mesh> meshes = meshFilters.Select(m => m.sharedMesh).Distinct();

                    foreach (var mesh in meshes)
                    {
                        _taskQueue.Enqueue(() => GetMeshColorsCoroutine(mesh));
                    }
                }
            }

            _taskQueue.Enqueue(GetSceneReferences);
        }

        private void GetSceneReferences()
        {
            _meshFilters = FindObjectsOfType<MeshFilter>().ToList();
            
            _hasEditorVertexColour = false;
            _hasMaterialImportColour = false;
            _sceneReferencesCount = 0;

            foreach(var mesh in _selectedMeshes)
            {
                _taskQueue.Enqueue(() => GetSceneReferencesForMesh(mesh));
            }

            _taskQueue.Enqueue(UpdateEditor);
        }

        private void GetSceneReferencesForMesh(Mesh mesh)
        {
            for (int i = _meshFilters.Count - 1; i >= 0; i--)
            {
                if (_meshFilters[i].sharedMesh == mesh)
                {
                    _sceneReferencesCount++;
                    _meshFilters.RemoveAt(i);
                }
            }

            if (_hasMaterialImportColour && _hasEditorVertexColour) { return; }

            GameObject model = _meshModelDictionary[mesh];
            if (!_modelDataDictionary.TryGetValue(model, out var data))
            {
                data = CustomAssetData.Get(model);
                _modelDataDictionary.Add(model, data);
            }

            _hasEditorVertexColour |= data?.meshColors.Any(mc => mc.meshName == mesh.name) ?? false;
            _hasMaterialImportColour |= data?.ShouldImportMaterialColors ?? false;
        }

        private IEnumerator RunQueue(System.Action callback = null)
        {
            rootVisualElement.SetEnabled(false);

            StopwatchSetup();

            while(_taskQueue.Count > 0)
            {
                _taskQueue.Dequeue()();

                if (_stopwatch.ElapsedMilliseconds > _stopwatchThreshold)
                {
                    _stopwatch.Stop();
                    yield return null;
                    _stopwatchThreshold = _stopwatch.ElapsedMilliseconds + MAX_MILLISECONDS_PER_FRAME;
                    _stopwatch.Start();
                }
            }

            _stopwatch.Stop();

            rootVisualElement.SetEnabled(true);

            callback?.Invoke();
        }

        private void GetMeshColorsCoroutine(Mesh mesh)
        {
            if (!_meshModelDictionary.TryGetValue(mesh, out var model))
            {
                model = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GetAssetPath(mesh));
                _meshModelDictionary.Add(mesh, model);
            }

            if (model == null) { return; } // Mesh is a library mesh like the default Unity cube
            
            _selectedMeshes.Add(mesh);
            _meshModelDictionary[mesh] = model;

            if (mesh.colors != null && mesh.colors.Length > 0) 
            {
                for (int i = 0; i < mesh.subMeshCount; i++)
                {
                    var submesh = mesh.GetSubMesh(i);
                    _previewColours.Add(new CustomAssetData.MeshColor(mesh, mesh.colors[submesh.firstVertex], i));
                }
            }

            if (!_selectedModels.Contains(model)) { _selectedModels.Add(model); }
        }

        private void StopwatchSetup()
        {
            _stopwatchThreshold = MAX_MILLISECONDS_PER_FRAME;
            _stopwatch.Reset();
            _stopwatch.Start();
        }

        private void UpdateEditor()
        {
            int meshesCount = _selectedMeshes.Count;
            int modelsCount = _selectedModels.Count;

            _hasSelection &= meshesCount > 0 || modelsCount > 0;

            if (!_hasSelection)
            {
                _meshField.value = null;
                _modelField.value = null;
            }

            _meshField.value = meshesCount == 1 ? _selectedMeshes[0] : null;
            _meshField.SetVisible(meshesCount <= 1);
            _meshesSelected.SetVisible(meshesCount > 1);
            _meshesSelectedCountElement.text = meshesCount.ToString() + " selected";
            if (meshesCount > 1) { _meshesSelectedCountElement.tooltip = _selectedMeshes.Select(m => m.name).Aggregate((current, next) => current + "\n" + next); }

            _modelField.value = modelsCount == 1 ? _selectedModels[0] : null;
            _modelField.SetVisible(modelsCount <= 1);
            _modelsSelected.SetVisible(modelsCount > 1);
            _modelsSelectedCountElement.text = modelsCount.ToString() + " selected";
            if (modelsCount > 1) { _modelsSelectedCountElement.tooltip = _selectedModels.Select(m => m.name).Aggregate((current, next) => current + "\n" + next); }

            _sceneReferencesCountElement.text = _sceneReferencesCount.ToString();

            _influencesModelVertexColours.AddToClassList("hidden");
            _influencesModelMaterialColours.EnableInClassList("has-color", _hasMaterialImportColour);
            _influencesModelMaterialColours.text = $"{(_hasMaterialImportColour ? "✓" : "✗")} Model Material Colours";
            _influencesCustomOverrideColours.EnableInClassList("has-color", _hasEditorVertexColour);
            _influencesCustomOverrideColours.text = $"{(_hasEditorVertexColour ? "✓" : "✗")} Custom Override Colours";

            _previewColorElement.SetColors(_previewColours);
            
            _setColourButton.SetEnabled(_hasSelection);
            _removeColourButton.SetEnabled(_hasEditorVertexColour);
            _colourPickerSetColourButton.SetEnabled(_hasSelection);

            GenerateImportMaterialButtons();

            // Update preview window
            if (modelsCount > 0)
            {
                Editor.CreateCachedEditor(_selectedModels[0], null, ref _editor);
            }
            else
            {
                _editor = null;
            }
        }

        private void GenerateImportMaterialButtons()
        {
            _importMaterialColoursGroup.Clear();

            foreach(var model in _selectedModels)
            {
                var element = new ModelColourEditorImportMaterialsElement(model);
                _importMaterialColoursGroup.Add(element);
                element.OnImportMaterialsChangedEvent += OnImportMaterialsChanged;
            }

            _allMaterialColoursGroup.SetVisible(_selectedModels.Count > 1);
        }

        private void OnImportMaterialsChanged()
        {
            _modelDataDictionary.Clear();
            OnSelectionChanged();
        }

        private void SetColour()
        {
            ApplyChangeToMeshes("Would you like to set colours on all meshes?",
            (mesh, index, data) => {
                data.meshColors.RemoveAll(md => md.meshName == mesh.name && md.materialIndex == index);
                data.meshColors.Add(new CustomAssetData.MeshColor(mesh, _colourPicker.value, index));
                return data;
            });
        }

        private void RemoveColour()
        {
            ApplyChangeToMeshes("Would you like to remove colours on all meshes?",
            (mesh, index, data) => {
                data.meshColors.RemoveAll(md => md.meshName == mesh.name && md.materialIndex == index);
                return data;
            });
        }
        
        private void ApplyChangeToMeshes(string multipleWarning, MeshDataAction action)
        {
            // Dialogue popup warning
            if (_previewColorElement.SelectedIndices.Count > 1)
            {
                if (!EditorUtility.DisplayDialog("Multiple mesh confirmation", $"You have {_previewColorElement.SelectedIndices.Count} submeshes selected. {multipleWarning}", "Yes", "No"))
                {
                    return;
                }
            }

            var modelData = new Dictionary<GameObject, CustomAssetData>();

            foreach(var meshColor in _previewColorElement.SelectedIndices.Select(i => _previewColours[i]))
            {
                var mesh = meshColor.mesh;
                var index = meshColor.materialIndex;
                var asset = _meshModelDictionary[meshColor.mesh];
                if (!modelData.ContainsKey(asset))
                {
                    CustomAssetData data = CustomAssetData.Get(asset) ?? new CustomAssetData();
                    modelData.Add(asset, data);
                }

                modelData[asset] = action(mesh, index, modelData[asset]);
            }

            foreach(var data in modelData)
            {
                string path = AssetDatabase.GetAssetPath(data.Key);
                CustomAssetData.Set(data.Key, data.Value);
                AssetDatabase.ImportAsset(path);
            }

            _modelDataDictionary.Clear();

            OnSelectionChanged();
        }

        private void ApplyChangeToModels(string multipleWarning, ModelDataAction action)
        {
            // Dialogue popup warning
            if (_selectedModels.Count > 1)
            {
                if (!EditorUtility.DisplayDialog("Multiple asset confirmation", $"You have {_selectedModels.Count} assets selected. {multipleWarning}", "Yes", "No"))
                {
                    return;
                }
            }

            foreach(var model in _selectedModels)
            {
                string path = AssetDatabase.GetAssetPath(model);
                CustomAssetData data = CustomAssetData.Get(model) ?? new CustomAssetData();
                data = action(model, data);
                CustomAssetData.Set(path, data);
                AssetDatabase.ImportAsset(path);
            }

            _modelDataDictionary.Clear();

            OnSelectionChanged();
        }

        private void SetImportMaterialColours(ChangeEvent<bool> evt)
        {
            ApplyChangeToModels("Would you like to set Import Material Colours on all assets?", 
            (mesh, data) =>
            {
                data.importMaterialColors = evt.newValue;
                return data;
            }
            );
        }

        private void DisableMaterialColours()
        {
            ApplyChangeToModels("Would you like to disable Import Material Colours on all assets?", 
            (mesh, data) =>
            {
                data.importMaterialColors = false;
                return data;
            }
            );
        }

        private void EnableMaterialColours()
        {
            ApplyChangeToModels("Would you like to enable Import Material Colours on all assets?", 
            (mesh, data) =>
            {
                data.importMaterialColors = true;
                return data;
            }
            );
        }

        private void ResetMaterialColours()
        {
            ApplyChangeToModels("Would you like to reset Import Material Colours on all assets?", 
            (mesh, data) =>
            {
                data.importMaterialColors = null;
                return data;
            }
            );
        }

        private void SetColourPickerColours()
        {
            ApplyChangeToMeshes("Would you like to set colours on all meshes?",
            (mesh, index, data) => {
                Color? color = (_colourPickerAsset.value as AbstractColourPickerTool).GetColor(mesh);
                if (!color.HasValue) { return data; }
                data.meshColors.RemoveAll(md => md.meshName == mesh.name && md.materialIndex == index);
                data.meshColors.Add(new CustomAssetData.MeshColor(mesh, color.Value, index));
                return data;
            });
        }

        private void BuildNewColourPickerMenu()
        {
            _colourPickerNewButton.AddManipulator(new ContextualMenuManipulator(e => {

                Type abstractType = typeof(AbstractColourPickerTool);
                var assembly = Assembly.GetAssembly(abstractType);
                var types = assembly.GetTypes().Where(t => t != abstractType && abstractType.IsAssignableFrom(t));

                foreach(var type in types)
                {
                    string name = ObjectNames.NicifyVariableName(Regex.Replace(type.Name, @"^ColourPickerTool", string.Empty));

                    e.menu.AppendAction($"New {name}", action => {
                        // Create new colour picker asset
                        string path = EditorUtility.SaveFilePanelInProject($"Create new {name}", $"New{type.Name}", "asset", "Please enter a file name to save the asset to");
                        if (path.Length == 0) { return; }
                        var asset = ScriptableObject.CreateInstance(type);
                        AssetDatabase.CreateAsset(asset, path);
                        _colourPickerAsset.value = asset;
                    });
                }

                e.menu.AppendSeparator();
                e.menu.AppendAction($"Create Colour Picker Tool Script", action => {
                    // Create new colour picker script
                    string path = EditorUtility.SaveFilePanelInProject($"Create new Colour Picker Tool Script", "ColourPickerToolCustom", "cs", "Please enter a file name to save the script to");

                    if (!path.Contains("Editor"))
                    {
                        EditorUtility.DisplayDialog("Error creating Colour Picker Tool Script", "Script must be saved in an Editor folder", "OK");
                        return;
                    }

                    var template = Resources.Load(Resource.COLOUR_PICKER_TOOL_TEMPLATE);
                    UnityEditor.ProjectWindowUtil.CreateScriptAssetFromTemplateFile(AssetDatabase.GetAssetPath(template), path);    
                });
            }));

            _colourPickerNewButton.clickable.activators.Clear();
        }

        private void OnColourPickerChanged(UnityEngine.Object newValue)
        {
            Editor.CreateCachedEditor(newValue, null, ref _colourPickerInlineScriptableObjectEditor);

            _colourPickerAssetFoldout.SetEnabled(newValue != null);
            _colourPickerAssetFoldout.value = newValue != null;
            _colourPickerInlineScriptableObject.SetVisible(newValue != null);
        }

        private void OnColourPickerInlineScriptableObjectInspectorGUI()
        {
            if (_colourPickerAsset.value == null || _colourPickerInlineScriptableObjectEditor == null) { return; }
            _colourPickerInlineScriptableObjectEditor.OnInspectorGUI();
        }

        private void OnColourPickerFoldoutChanged(ChangeEvent<bool> evt)
        {
            _colourPickerInlineScriptableObject.SetVisible(evt.newValue);
        }

        private void NewColourPicker(MouseDownEvent evt)
        {
            _colourPickerNewButton.panel.contextualMenuManager.DisplayMenu(evt, _colourPickerNewButton);

            evt.StopPropagation();
        }

        private void ReimportSelected()
        {
            foreach(var model in _selectedModels)
            {
                string path = AssetDatabase.GetAssetPath(model);
                AssetDatabase.ImportAsset(path);
            }

            OnSelectionChanged();
        }

        [System.Serializable]
        private class EditorSettings
        {
            public Color selectedColor;
            public string colourPickerTool;
        }
    }
}