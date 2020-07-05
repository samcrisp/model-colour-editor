using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ModelColourEditor
{
    public class ModelColourEditorImportMaterialsElement : VisualElement
    {
        private Label _label;
        private Button _enableButton;
        private Button _disableButton;
        private Button _resetButton;
        private GameObject _asset;

        public System.Action OnImportMaterialsChangedEvent;

        public ModelColourEditorImportMaterialsElement(GameObject asset)
        {
            _asset = asset;

            Build();
        }

        private void Build()
        {
            var template = Resources.Load<VisualTreeAsset>(Resource.MODEL_COLOUR_EDITOR_IMPORT_MATERIALS_TEMPLATE);

            template.CloneTree(this);

            _label = this.Q<Label>("importMaterialColoursName");
            _label.text = _asset.name;

            _enableButton = this.Q<Button>("enableMaterialColoursButton");
            _disableButton = this.Q<Button>("disableMaterialColoursButton");
            _resetButton = this.Q<Button>("resetMaterialColoursButton");

            _enableButton.clicked += () => SetImportMaterialsEnabled(true);
            _disableButton.clicked += () => SetImportMaterialsEnabled(false);
            _resetButton.clicked += () => SetImportMaterialsEnabled(null);

            string path = AssetDatabase.GetAssetPath(_asset);
            var data = CustomAssetData.Get(_asset);
            bool isEnabled = data?.ShouldImportMaterialColors ?? ModelColourEditorSettings.Instance.importMaterialColoursByDefault;
            bool hasValue = data?.importMaterialColors.hasValue ?? false;
            _enableButton.SetEnabled(!isEnabled);
            _disableButton.SetEnabled(isEnabled);
            _resetButton.SetEnabled(hasValue);
        }

        private void SetImportMaterialsEnabled(bool? enabled)
        {
            string path = AssetDatabase.GetAssetPath(_asset);
            var importer = AssetImporter.GetAtPath(path);
            CustomAssetData data = CustomAssetData.Get(_asset) ?? new CustomAssetData();
            
            data.importMaterialColors = enabled;

            CustomAssetData.Set(path, data);

            // if (enabled) { (importer as ModelImporter).materialImportMode = ModelImporterMaterialImportMode.None; }
            AssetDatabase.ImportAsset(path);

            OnImportMaterialsChangedEvent?.Invoke();
        }
    }
}