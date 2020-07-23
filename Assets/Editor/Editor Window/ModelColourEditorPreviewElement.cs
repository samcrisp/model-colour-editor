using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ModelColourEditor
{
    public class ModelColourEditorPreviewElement : VisualElement
    {
        private const string EMPTY_COLOUR_SLOT_STRING_MULITPLE = "Select {0} empty colour slots";
        private const string EMPTY_COLOUR_SLOT_STRING_SINGULAR = "Select {0} empty colour slots";
        private const string EMPTY_COLOUR_SLOT_STRING_EMPTY = "No empty colour slots";
        
        public Action<Color> setOverrideColorEvent;
        
        private VisualElement _container;
        private TextElement _noColoursSet;
        private Button _selectAll;
        private HashSet<int> _selectedIndices = new HashSet<int>();
        private int _minSelectionIndex;
        private int _maxSelectionIndex;
        private bool _previousSelectionMax;
        private List<CustomAssetData.MeshColor> _allColors;
        private TextElement _remainingElement;
        private Toggle _selectColourSlotsWithNoColourInformation;
        private VisualElement _previewNoMeshesSelected;

        public HashSet<int> SelectedIndices => _selectedIndices;
        public bool SelectedMeshesWithNoColourInformation => _selectColourSlotsWithNoColourInformation.value;

        public ModelColourEditorPreviewElement()
        {
            Build();
        }

        private void Build()
        {
            var template = Resources.Load<VisualTreeAsset>(Resource.MODEL_COLOUR_EDITOR_PREVIEW_TEMPLATE);

            template.CloneTree(this);

            _container = this.Q<VisualElement>("previewElementContainer");
            _noColoursSet = this.Q<TextElement>("previewNoColoursSet");
            _selectColourSlotsWithNoColourInformation = this.Q<Toggle>("selectColourSlotsWithNoColourInformation");
            _previewNoMeshesSelected = this.Q<VisualElement>("previewNoMeshesSelected");
            
            _selectAll = this.Q<Button>("previewSelectAllButton");
            _selectAll.clicked += () => SetSelected(-1);
        }

        public void SetColors(List<CustomAssetData.MeshColor> colors, int emptyColourSlotsCount, bool hasSelection)
        {
            this.EnableInClassList("expanded", true);
            _allColors = colors;
            _container.Clear();

            for (int i = 0; i < colors.Count; i++)
            {
                if (!colors[i].hasColor)
                {
                    continue;
                }
                
                Color color = colors[i].color;
                CreateColourElement(color, i);

                if (i == 9 && colors.Count > 10)
                {
                    _remainingElement = new TextElement();
                    _remainingElement.AddToClassList("preview__color-element");
                    _remainingElement.text = $"+{colors.Count - 9}";
                    _remainingElement.RegisterCallback<MouseDownEvent>(OnClickRemainingColors);
                    _container.Add(_remainingElement);
                    break;
                }
            }
            
            _noColoursSet.SetVisible(_container.childCount == 0);
            
            foreach(var child in this.Children())
            {
                child.SetVisible(child == _previewNoMeshesSelected ^ hasSelection);
            }

            switch (emptyColourSlotsCount)
            {
                case 0:
                    _selectColourSlotsWithNoColourInformation.text = EMPTY_COLOUR_SLOT_STRING_EMPTY;
                    break;
                case 1:
                    _selectColourSlotsWithNoColourInformation.text = string.Format(EMPTY_COLOUR_SLOT_STRING_SINGULAR, emptyColourSlotsCount);
                    break;
                default:
                    _selectColourSlotsWithNoColourInformation.text = string.Format(EMPTY_COLOUR_SLOT_STRING_MULITPLE, emptyColourSlotsCount);
                    break;
            }
            
            _selectColourSlotsWithNoColourInformation.SetEnabled(emptyColourSlotsCount > 0);
            _selectColourSlotsWithNoColourInformation.value = emptyColourSlotsCount > 0;

            SetSelected(-1);
        }

        private void OnClickRemainingColors(MouseDownEvent evt)
        {
            this.EnableInClassList("expanded", false);
            _container.Remove(_remainingElement);

            for (int i = 10; i < _allColors.Count; i++)
            {
                Color color = _allColors[i].color;
                var element = CreateColourElement(color, i);

                bool enable = _selectedIndices.Contains(i);
                element.EnableInClassList("selected", enable);
            }
        }

        private void SetSelected(int index, MouseDownEvent evt = null)
        {
            if (evt == null) { evt = new MouseDownEvent(); }

            if (evt.button != 0) { return; }

            if (index == -1)
            {
                int count = _allColors.Count;

                _selectedIndices.Clear();
                _selectedIndices.UnionWith(Enumerable.Range(0, count));

                _minSelectionIndex = 0;
                _maxSelectionIndex = _allColors.Count;

                _previousSelectionMax = true;
            }
            else if (evt.shiftKey)
            {
                if (_selectedIndices.Count == 0)
                {
                    _selectedIndices.Add(index);
                    _maxSelectionIndex = _minSelectionIndex = index;
                }
                else
                {
                    bool expandingSelection = index > _maxSelectionIndex || index < _minSelectionIndex;

                    if (index < _minSelectionIndex) { _minSelectionIndex = index; _previousSelectionMax = false; }
                    else if (index > _maxSelectionIndex) { _maxSelectionIndex = index; _previousSelectionMax = true; }

                    if (expandingSelection)
                    {
                        _selectedIndices.UnionWith(Enumerable.Range(_minSelectionIndex, _maxSelectionIndex - _minSelectionIndex + 1));
                    }
                    else
                    {
                        int min = _previousSelectionMax ? _minSelectionIndex : index;
                        int max = _previousSelectionMax ? index : _maxSelectionIndex;
                        
                        _selectedIndices.Clear();
                        _selectedIndices.UnionWith(Enumerable.Range(min, max - min + 1));

                        _minSelectionIndex = _previousSelectionMax ? _minSelectionIndex : index;
                        _maxSelectionIndex = _previousSelectionMax ? index : _maxSelectionIndex;
                    }
                }

            }
            else if (evt.ctrlKey)
            {
                if (_selectedIndices.Count == 0)
                {
                    _selectedIndices.Add(index);
                    _maxSelectionIndex = _minSelectionIndex = index;
                }
                else
                {
                    if (index < _minSelectionIndex) { _minSelectionIndex = index; _previousSelectionMax = false; }
                    if (index > _maxSelectionIndex) { _maxSelectionIndex = index; _previousSelectionMax = true; }

                    if (_selectedIndices.Contains(index))
                    {
                        _selectedIndices.Remove(index);
                    }
                    else
                    {
                        _selectedIndices.Add(index);
                    }
                }
            }
            else
            {
                _selectedIndices.Clear();
                _selectedIndices.Add(index);
                _minSelectionIndex = _maxSelectionIndex = index;
            }

            bool selectAll = true;
            for (int i = 0; i < _container.childCount; i++)
            {
                bool enable = _selectedIndices.Contains(i);
                _container[i].EnableInClassList("selected", enable);

                selectAll &= enable;
            }

            _selectAll.SetEnabled(!selectAll);
        }

        private VisualElement CreateColourElement(Color color, int i)
        {
            VisualElement colorElement = new VisualElement();
            colorElement.AddToClassList("preview__color-element");
            colorElement.style.backgroundColor = color.gamma.ToAlpha(1);
            colorElement.RegisterCallback<MouseDownEvent, int>((e, index) => SetSelected(index, e), i);
            
            colorElement.AddManipulator(new ContextualMenuManipulator(e =>
            {
                e.menu.AppendAction("Copy", action => 
                {
                    EditorGUIUtility.systemCopyBuffer = "#" + ColorUtility.ToHtmlStringRGB(color.gamma);
                });

                e.menu.AppendAction("Pick", action => 
                {
                    setOverrideColorEvent?.Invoke(color.gamma);
                });
            }));
            
            _container.Add(colorElement);

            return colorElement;
        }

        public new class UxmlFactory : UxmlFactory<ModelColourEditorPreviewElement> {}
    }
}