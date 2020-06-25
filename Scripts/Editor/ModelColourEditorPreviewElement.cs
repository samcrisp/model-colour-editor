using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace ModelColourEditor
{
    public class ModelColourEditorPreviewElement : VisualElement
    {
        private VisualElement _container;
        private TextElement _noColoursSet;
        private TextField _hexField;
        private int _selectedIndex;
        private List<Color> _allColors;
        private TextElement _remainingElement;

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
            _hexField = this.Q<TextField>("previewHexField");
        }

        public void SetColors(List<Color> colors)
        {
            this.EnableInClassList("expanded", true);
            _allColors = colors;
            _container.Clear();
            
            for (int i = 0; i < colors.Count; i++)
            {
                Color color = colors[i];
                VisualElement colorElement = new VisualElement();
                colorElement.AddToClassList("preview__color-element");
                colorElement.style.backgroundColor = color.gamma.ToAlpha(1);
                colorElement.RegisterCallback<MouseDownEvent, int>((e, index) => SetSelected(index), i);
                _container.Add(colorElement);

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

            foreach(var child in this.Children())
            {
                child.SetVisible(child == _noColoursSet ^ _container.childCount > 0);
            }

            SetSelected(0);
        }

        private void OnClickRemainingColors(MouseDownEvent evt)
        {
            this.EnableInClassList("expanded", false);
            _container.Remove(_remainingElement);

            for (int i = 10; i < _allColors.Count; i++)
            {
                Color color = _allColors[i];
                VisualElement colorElement = new VisualElement();
                colorElement.AddToClassList("preview__color-element");
                colorElement.style.backgroundColor = color.gamma.ToAlpha(1);
                colorElement.RegisterCallback<MouseDownEvent, int>((e, index) => SetSelected(index), i);
                _container.Add(colorElement);
            }
        }

        private void SetSelected(int index)
        {
            _selectedIndex = index;

            for (int i = 0; i < _container.childCount; i++)
            {
                _container[i].EnableInClassList("selected", index == i);
            }

            if (index >= 0 && index < _container.childCount)
            {
                _hexField.value = ColorUtility.ToHtmlStringRGB(_container[index].style.backgroundColor.value);
            }
        }

        public new class UxmlFactory : UxmlFactory<ModelColourEditorPreviewElement> {}
    }
}