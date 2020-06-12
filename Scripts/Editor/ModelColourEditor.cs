using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CustomUIElements;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class ModelColourEditor : EditorWindow
{
    private delegate CustomAssetData MeshDataAction(Mesh mesh, CustomAssetData data);
    private delegate CustomAssetData ModelDataAction(GameObject asset, CustomAssetData data);

    private List<Mesh> _selectedMeshes = new List<Mesh>();
    private List<GameObject> _selectedModels = new List<GameObject>();
    private List<Color> _previewColours = new List<Color>();
    private Dictionary<Mesh, GameObject> _meshModelDictionary = new Dictionary<Mesh, GameObject>();

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
    private Toggle _randomAlpha;
    private Button _setColourButton;
    private Button _removeColourButton;
    private VisualElement _importMaterialColoursGroup;
    private VisualElement _allMaterialColoursGroup;
    private Button _enableAllMaterialColoursButton;
    private Button _disableAllMaterialColoursButton;
    private Button _reimportedSelectedButton;
    private ObjectField _colourPickerAsset;
    private Button _colourPickerSetColourButton;

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
        _randomAlpha.value = settings.randomAlpha;
        _colourPickerAsset.value = settings.colourPickerTool;
    }

    private void OnDisable()
    {
        Selection.selectionChanged -= OnSelectionChanged; 

        var settings = new EditorSettings()
        {
            selectedColor = _colourPicker.value,
            randomAlpha = _randomAlpha.value,
            colourPickerTool = _colourPickerAsset.value as AbstractColourPickerTool
        };

        EditorPrefs.SetString("ModelColourEditorSettings", JsonUtility.ToJson(settings));
    }

    private void Build()
    {
        var template = Resources.Load<VisualTreeAsset>(Resource.MODEL_COLOUR_EDITOR_TEMPLATE);
        var styles = Resources.Load<StyleSheet>(Resource.MODEL_COLOUR_STYLES);

        template.CloneTree(rootVisualElement);
        rootVisualElement.styleSheets.Add(styles);

        // Get references
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
        _previewModel = rootVisualElement.Q<IMGUIContainer>("previewModel");
        _previewModel.onGUIHandler = OnDrawPreviewModelGUI;

        _colourPicker = rootVisualElement.Q<ColorField>("setColourColourPicker");
        _randomAlpha = rootVisualElement.Q<Toggle>("setColourRandomAlpha");

        _setColourButton = rootVisualElement.Q<Button>("setColourButton");
        _setColourButton.clicked += SetColour;
        _removeColourButton = rootVisualElement.Q<Button>("removeColourButton");
        _removeColourButton.clicked += RemoveColour;

        _importMaterialColoursGroup = rootVisualElement.Q<VisualElement>("importMaterialColoursGroup");
        _allMaterialColoursGroup = rootVisualElement.Q<VisualElement>("allMaterialColoursGroup");
        _enableAllMaterialColoursButton = rootVisualElement.Q<Button>("enableAllMaterialColoursButton");
        _disableAllMaterialColoursButton = rootVisualElement.Q<Button>("disableAllMaterialColoursButton");
        _enableAllMaterialColoursButton.clicked += EnableMaterialColours;
        _disableAllMaterialColoursButton.clicked += DisableMaterialColours;

        _colourPickerAsset = rootVisualElement.Q<ObjectField>("colourPickerAsset");
        _colourPickerAsset.objectType = typeof(AbstractColourPickerTool);
        _colourPickerSetColourButton = rootVisualElement.Q<Button>("colourPickerSetColourButton");
        _colourPickerSetColourButton.clicked += SetColourPickerColours;

        _reimportedSelectedButton = rootVisualElement.Q<Button>("reimportedSelectedButton");
        _reimportedSelectedButton.clicked += ReimportSelected;

        OnSelectionChanged();
    }

    private void OnDrawPreviewModelGUI()
    {
        _editor?.OnInteractivePreviewGUI(_previewModel.contentRect, null);
    }

    private void OnSelectionChanged()
    {
        GetSelection();
        UpdateEditor();
    }

    private void GetSelection()
    {
        _selectedMeshes.Clear();
        _selectedModels.Clear();
        _previewColours.Clear();
        _meshModelDictionary.Clear();

        _hasSelection = Selection.activeObject != null;
        
        foreach(var selection in Selection.objects)
        {
            if (selection is Mesh)
            {
                Mesh mesh = selection as Mesh;
                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GetAssetPath(mesh));

                _selectedMeshes.Add(mesh);
                _selectedModels.Add(model);
                _meshModelDictionary[mesh] = model;
                if (mesh.colors != null) { _previewColours.AddRange(mesh.colors.Distinct().Where(c => !_previewColours.Contains(c))); }
            }
            else if (selection is GameObject)
            {
                var meshFilters = (selection as GameObject).GetComponentsInChildren<MeshFilter>();

                IEnumerable<Mesh> meshes = meshFilters.Select(m => m.sharedMesh).Distinct();

                foreach(var mesh in meshes) 
                {
                    GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GetAssetPath(mesh));
                    
                    if (model == null) { continue; } // Mesh is a library mesh like the default Unity cube
                    
                    _selectedMeshes.Add(mesh);
                    _meshModelDictionary[mesh] = model;
                    _previewColours.AddRange(mesh.colors.Distinct().Where(c => !_previewColours.Contains(c)));

                    if (!_selectedModels.Contains(model)) { _selectedModels.Add(model); }
                }
            }
        }

        _sceneReferencesCount = _selectedMeshes.Sum(m => FindObjectsOfType<MeshFilter>().Where(mf => mf.sharedMesh == m).Count());

        var data = _selectedMeshes.Select(m => CustomAssetData.Get(_meshModelDictionary[m]));
        _hasEditorVertexColour = data.Any(d => d?.HasMeshColours ?? false);
        _hasMaterialImportColour = data.Any(d => d?.importMaterialColors ?? false);

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
            element.OnImportMaterialsChangedEvent += OnSelectionChanged;
        }

        _allMaterialColoursGroup.SetVisible(_selectedModels.Count > 1);
    }

    private void SetColour()
    {
        ApplyChangeToMeshes("Would you like to set colours on all meshes?",
        (mesh, data) => {
            data.meshColors.RemoveAll(md => md.meshName == mesh.name);
            data.meshColors.Add(new CustomAssetData.MeshColor(mesh.name, _colourPicker.value.ToAlpha(_randomAlpha.value ? UnityEngine.Random.value : 1)));
            return data;
        });
    }

    private void RemoveColour()
    {
        ApplyChangeToMeshes("Would you like to remove colours on all meshes?",
        (mesh, data) => {
            data.meshColors.RemoveAll(md => md.meshName == mesh.name);
            return data;
        });
    }
    
    private void ApplyChangeToMeshes(string multipleWarning, MeshDataAction action)
    {
        // Dialogue popup warning
        if (_selectedMeshes.Count > 1)
        {
            if (!EditorUtility.DisplayDialog("Multiple mesh confirmation", $"You have {_selectedMeshes.Count} meshes selected. {multipleWarning}", "Yes", "No"))
            {
                return;
            }
        }

        var modelData = new Dictionary<GameObject, CustomAssetData>();

        foreach(var mesh in _selectedMeshes)
        {
            var asset = _meshModelDictionary[mesh];
            if (!modelData.ContainsKey(asset))
            {
                CustomAssetData data = CustomAssetData.Get(asset) ?? new CustomAssetData();
                modelData.Add(asset, data);
            }

            modelData[asset] = action(mesh, modelData[asset]);
        }

        foreach(var data in modelData)
        {
            string path = AssetDatabase.GetAssetPath(data.Key);
            CustomAssetData.Set(data.Key, data.Value);
            AssetDatabase.ImportAsset(path);
        }

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

    private void SetColourPickerColours()
    {
        ApplyChangeToMeshes("Would you like to set colours on all meshes?",
        (mesh, data) => {
            Color? color = (_colourPickerAsset.value as AbstractColourPickerTool).GetColor(mesh);
            if (!color.HasValue) { return data; }
            data.meshColors.RemoveAll(md => md.meshName == mesh.name);
            data.meshColors.Add(new CustomAssetData.MeshColor(mesh.name, color.Value));
            return data;
        });
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
        public bool randomAlpha;
        public AbstractColourPickerTool colourPickerTool;
    }
}
