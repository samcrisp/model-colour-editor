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

        public void SetColors(IEnumerable<Color> colors)
        {
            _container.Clear();
            
            int count = 0;
            foreach (var color in colors)
            {
                VisualElement colorElement = new VisualElement();
                colorElement.AddToClassList("preview__color-element");
                colorElement.style.backgroundColor = color.gamma.ToAlpha(1);
                colorElement.RegisterCallback<MouseDownEvent, int>((e, i) => SetSelected(i), count);
                _container.Add(colorElement);
                count++;
            }

            foreach(var child in this.Children())
            {
                child.SetVisible(child == _noColoursSet ^ _container.childCount > 0);
            }

            SetSelected(0);
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