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

            _enableButton.clicked += () => SetImportMaterialsEnabled(true);
            _disableButton.clicked +=  () => SetImportMaterialsEnabled(false);

            string path = AssetDatabase.GetAssetPath(_asset);
            bool isEnabled = CustomAssetData.Get(_asset)?.importMaterialColors ?? false;
            _enableButton.SetEnabled(!isEnabled);
            _disableButton.SetEnabled(isEnabled);
        }

        private void SetImportMaterialsEnabled(bool enabled)
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