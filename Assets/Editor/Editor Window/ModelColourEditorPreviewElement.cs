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
        private const string EMPTY_COLOUR_SLOT_STRING_MULTIPLE = "Select {0} empty colour slots";
        private const string EMPTY_COLOUR_SLOT_STRING_SINGULAR = "Select {0} empty colour slot";
        private const string EMPTY_COLOUR_SLOT_STRING_EMPTY = "No empty colour slots";
        private const string COLOUR_SLOTS_SELECTED_STRING = "{0} selected";
        private const string COLOUR_SLOTS_SELECTED_STRING_EMPTY = "None selected";


        private const int SELECT_ALL = -2;
        private const int SELECT_NONE = -1;
        private const int UNEXPANDED_COLOUR_COUNT = 10;

        public Action<Color> setOverrideColorEvent;
        public Action<bool> onSelectionChangeEvent;

        private VisualElement _container;
        private VisualElement _containerGrouped;
        private TextElement _noColoursSet;
        private Button _selectAll;
        private Button _selectNone;
        private readonly HashSet<int> _selectedIndices = new HashSet<int>();
        private int _minSelectionIndex;
        private int _maxSelectionIndex;
        private bool _previousSelectionMax;
        private VisualElement _remainingElement;
        private VisualElement _remainingElementGrouped;
        private Toggle _selectColourSlotsWithNoColourInformation;
        private Toggle _groupSameColours;
        private VisualElement _previewNoMeshesSelected;
        private readonly List<Color?> _allColors = new List<Color?>();
        private readonly Dictionary<Color, HashSet<int>> _uniqueColorGroups = new Dictionary<Color, HashSet<int>>();
        private readonly HashSet<int> _emptyColourGroup = new HashSet<int>();
        private readonly Dictionary<int, Color?> _colourGroupIndices = new Dictionary<int, Color?>();
        private readonly List<int> _uniqueColourIndices = new List<int>();
        private int _elementCount;
        private int _elementGroupedCount;
        private Label _selectedColoursLabel;

        public bool SelectedMeshesWithNoColourInformation => _selectColourSlotsWithNoColourInformation.value;
        public bool GroupSameColours => _groupSameColours.value;

        public ModelColourEditorPreviewElement()
        {
            Build();
        }

        public HashSet<int> GetSelectedIndices()
        {
            if (GroupSameColours)
            {
                HashSet<int> UnionCollection(HashSet<int> total, HashSet<int> next)
                {
                    total.UnionWith(next);
                    return total;
                }

                // Get the colours from the current (grouped) selection, and select all indices belonging to those colours  
                return _selectedIndices
                    .Select(i =>
                        _colourGroupIndices[i].HasValue
                            ? _uniqueColorGroups[_colourGroupIndices[i].Value]
                            : _emptyColourGroup).Aggregate(new HashSet<int>(), UnionCollection);
            }

            return _selectedIndices;
        }

        private void Build()
        {
            var template = Resources.Load<VisualTreeAsset>(Resource.MODEL_COLOUR_EDITOR_PREVIEW_TEMPLATE);

            template.CloneTree(this);

            _container = this.Q<VisualElement>("previewElementContainer");
            _containerGrouped = this.Q<VisualElement>("previewElementContainerGrouped");
            _noColoursSet = this.Q<TextElement>("previewNoColoursSet");
            _selectColourSlotsWithNoColourInformation = this.Q<Toggle>("selectColourSlotsWithNoColourInformation");
            _previewNoMeshesSelected = this.Q<VisualElement>("previewNoMeshesSelected");
            _groupSameColours = this.Q<Toggle>("groupSameColours");
            _groupSameColours.RegisterValueChangedCallback(evt => OnGroupSameColoursValueChanged(evt.newValue));
            _selectedColoursLabel = this.Q<Label>("selectedColoursLabel");

            _selectAll = this.Q<Button>("previewSelectAllButton");
            _selectNone = this.Q<Button>("previewSelectNoneButton");
            _selectAll.clicked += () => SetSelected(SELECT_ALL);
            _selectNone.clicked += () => SetSelected(SELECT_NONE);
        }

        private void OnGroupSameColoursValueChanged(bool value)
        {
            _container.SetVisible(!value);
            _containerGrouped.SetVisible(value);

            SetSelected(SELECT_NONE);
        }

        public void SetColors(List<CustomAssetData.MeshColor> colors, int emptyColourSlotsCount, bool hasSelection)
        {
            this.EnableInClassList("expanded", false);
            _allColors.Clear();
            _container.Clear();
            _containerGrouped.Clear();
            _uniqueColorGroups.Clear();
            _colourGroupIndices.Clear();
            _uniqueColourIndices.Clear();
            _uniqueColorGroups.Clear();
            _emptyColourGroup.Clear();

            VisualElement colourElement = null;
            _elementCount = 0;
            _elementGroupedCount = 0;

            // Add colour elements
            for (int i = 0; i < colors.Count; i++)
            {
                if (!colors[i].hasColor)
                {
                    continue;
                }

                Color color = colors[i].color;
                _allColors.Add(color);

                // New unique colour instance
                if (!_uniqueColorGroups.TryGetValue(color, out var sameColourIndices))
                {
                    sameColourIndices = new HashSet<int> {i};
                    _uniqueColorGroups.Add(color, sameColourIndices);
                    _colourGroupIndices.Add(_elementGroupedCount, color);
                    _uniqueColourIndices.Add(_elementGroupedCount);

                    _elementGroupedCount++;

                    if (_elementGroupedCount <= UNEXPANDED_COLOUR_COUNT)
                    {
                        colourElement = CreateColourElement(color, _elementGroupedCount - 1);
                        _containerGrouped.Add(colourElement);
                    }
                }
                // Existing colour instance
                else
                {
                    sameColourIndices.Add(i);
                }

                _elementCount++;

                if (_elementCount <= UNEXPANDED_COLOUR_COUNT)
                {
                    colourElement = CreateColourElement(color, _elementCount - 1);
                    _container.Add(colourElement);
                }
            }

            // Add empty colour slots
            for (int i = 0; i < emptyColourSlotsCount; i++)
            {
                _allColors.Add(null);

                if (i == 0)
                {
                    _colourGroupIndices.Add(_elementGroupedCount, null);
                    _uniqueColourIndices.Add(_elementGroupedCount);

                    _elementGroupedCount++;

                    if (_elementGroupedCount <= UNEXPANDED_COLOUR_COUNT)
                    {
                        colourElement = CreateEmptyColourElement(_elementGroupedCount - 1);
                        _containerGrouped.Add(colourElement);
                    }
                }

                _emptyColourGroup.Add(i);
                _elementCount++;

                if (_elementCount <= UNEXPANDED_COLOUR_COUNT)
                {
                    colourElement = CreateEmptyColourElement(_elementCount - 1);
                    _container.Add(colourElement);
                }
            }

            CreateRemainingElement(_elementCount, _container);
            CreateRemainingElement(_elementGroupedCount, _containerGrouped);

            _noColoursSet.SetVisible(_container.childCount == 0);

            // Hide container if selection is empty
            foreach (var child in this.Children())
            {
                child.SetVisible(child == _previewNoMeshesSelected ^ hasSelection);
            }

            switch (emptyColourSlotsCount)
            {
                case 0:
                    _selectColourSlotsWithNoColourInformation.text = EMPTY_COLOUR_SLOT_STRING_EMPTY;
                    break;
                case 1:
                    _selectColourSlotsWithNoColourInformation.text =
                        string.Format(EMPTY_COLOUR_SLOT_STRING_SINGULAR, emptyColourSlotsCount);
                    break;
                default:
                    _selectColourSlotsWithNoColourInformation.text =
                        string.Format(EMPTY_COLOUR_SLOT_STRING_MULTIPLE, emptyColourSlotsCount);
                    break;
            }

            _selectColourSlotsWithNoColourInformation.SetEnabled(emptyColourSlotsCount > 0);
            _selectColourSlotsWithNoColourInformation.value = emptyColourSlotsCount > 0;

            SetSelected(SELECT_NONE);
        }

        private VisualElement CreateRemainingElement(int count, VisualElement container)
        {
            if (count > UNEXPANDED_COLOUR_COUNT)
            {
                container.ElementAt(container.childCount - 1).SetVisible(false);

                var remainingElement = new TextElement();
                remainingElement.AddToClassList("preview__color-element");
                remainingElement.text = $"+{count - (UNEXPANDED_COLOUR_COUNT - 1)}";
                remainingElement.RegisterCallback<MouseDownEvent, VisualElement>(OnClickRemainingColors,
                    remainingElement);
                container.Add(remainingElement);
                return remainingElement;
            }

            return null;
        }

        private void OnClickRemainingColors(MouseDownEvent evt, VisualElement remainingElement)
        {
            var container = remainingElement.parent;
            this.EnableInClassList("expanded", true);
            container.Remove(remainingElement);
            container.ElementAt(container.childCount - 1).SetVisible(true);

            if (GroupSameColours)
            {
                for (int i = UNEXPANDED_COLOUR_COUNT; i < _elementGroupedCount; i++)
                {
                    Color? color = _colourGroupIndices[_uniqueColourIndices[i]];
                    var element = color.HasValue ? CreateColourElement(color.Value, i) : CreateEmptyColourElement(i);
                    _containerGrouped.Add(element);

                    bool enable = _selectedIndices.Contains(i);
                    element.EnableInClassList("selected", enable);
                }
            }
            else
            {
                for (int i = UNEXPANDED_COLOUR_COUNT; i < _elementCount; i++)
                {
                    Color? color = _allColors[i];
                    var element = color.HasValue ? CreateColourElement(color.Value, i) : CreateEmptyColourElement(i);
                    _container.Add(element);

                    bool enable = _selectedIndices.Contains(i);
                    element.EnableInClassList("selected", enable);
                }
            }
        }

        private void SetSelected(int index, MouseDownEvent evt = null)
        {
            if (evt == null)
            {
                evt = new MouseDownEvent();
            }

            if (evt.button != 0)
            {
                return;
            }

            if (index == SELECT_ALL)
            {
                int count = GroupSameColours ? _elementGroupedCount : _elementCount;

                _selectedIndices.Clear();
                _selectedIndices.UnionWith(Enumerable.Range(0, count));

                _minSelectionIndex = 0;
                _maxSelectionIndex = _allColors.Count;

                _previousSelectionMax = true;
            }
            else if (index == SELECT_NONE)
            {
                _selectedIndices.Clear();
                _minSelectionIndex = _maxSelectionIndex = 0;
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

                    if (index < _minSelectionIndex)
                    {
                        _minSelectionIndex = index;
                        _previousSelectionMax = false;
                    }
                    else if (index > _maxSelectionIndex)
                    {
                        _maxSelectionIndex = index;
                        _previousSelectionMax = true;
                    }

                    if (expandingSelection)
                    {
                        _selectedIndices.UnionWith(Enumerable.Range(_minSelectionIndex,
                            _maxSelectionIndex - _minSelectionIndex + 1));
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
                    if (index < _minSelectionIndex)
                    {
                        _minSelectionIndex = index;
                        _previousSelectionMax = false;
                    }

                    if (index > _maxSelectionIndex)
                    {
                        _maxSelectionIndex = index;
                        _previousSelectionMax = true;
                    }

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
            bool selectNone = true;
            var container = GroupSameColours ? _containerGrouped : _container;
            for (int i = 0; i < container.childCount; i++)
            {
                bool enable = _selectedIndices.Contains(i);
                container[i].EnableInClassList("selected", enable);

                selectAll &= enable;
                selectNone &= !enable;
            }

            _selectAll.SetEnabled(!selectAll);
            _selectNone.SetEnabled(!selectNone);

            var selectedColoursCount = GetSelectedIndices().Count;
            var hasSelection = selectedColoursCount > 0;
            _selectedColoursLabel.text = hasSelection
                ? string.Format(COLOUR_SLOTS_SELECTED_STRING, selectedColoursCount)
                : COLOUR_SLOTS_SELECTED_STRING_EMPTY;
            _selectedColoursLabel.SetEnabled(hasSelection);

            onSelectionChangeEvent?.Invoke(hasSelection);
        }

        private VisualElement CreateColourElement(Color color, int i)
        {
            VisualElement colorElement = new VisualElement();
            colorElement.AddToClassList("preview__color-element");
            colorElement.style.backgroundColor = color.gamma.ToAlpha(1);
            colorElement.RegisterCallback<MouseDownEvent, int>((e, index) => SetSelected(index, e), i);

            colorElement.AddManipulator(new ContextualMenuManipulator(e =>
            {
                e.menu.AppendAction("Copy",
                    action => { EditorGUIUtility.systemCopyBuffer = "#" + ColorUtility.ToHtmlStringRGB(color.gamma); });

                e.menu.AppendAction("Pick", action => { setOverrideColorEvent?.Invoke(color.gamma); });
            }));

            return colorElement;
        }

        private VisualElement CreateEmptyColourElement(int i)
        {
            VisualElement colorElement = new VisualElement();
            colorElement.AddToClassList("preview__color-element");
            colorElement.AddToClassList("preview__empty-color-element");
            colorElement.RegisterCallback<MouseDownEvent, int>((e, index) => SetSelected(index, e), i);

            return colorElement;
        }

        public new class UxmlFactory : UxmlFactory<ModelColourEditorPreviewElement>
        {
        }
    }
}