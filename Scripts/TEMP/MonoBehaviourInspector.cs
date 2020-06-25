using System;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

[CustomEditor(typeof(MonoBehaviour), true), CanEditMultipleObjects]
public class MonoBehaviourInspector : Editor
{
    private Dictionary<MethodInfo, string> _methods;
    private MonoBehaviour _target;

    private void OnEnable()
    {
        _target = target as MonoBehaviour;

        _methods = new Dictionary<MethodInfo, string>();

        if (_target == null) { return; }

        foreach(var method in _target.GetType().GetMethods())
        {
            var attribute = Attribute.GetCustomAttribute(method, typeof(ButtonAttribute)) as ButtonAttribute;
            
            if (attribute == null) { continue; }

            _methods.Add(method, attribute.Text);
        }
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        
        DrawButtons();
    }

    private void DrawButtons()
    {
        foreach(var button in _methods)
        {
            if (GUILayout.Button(button.Value ?? ObjectNames.NicifyVariableName(button.Key.Name)))
            {
                foreach(var target in targets)
                {
                    button.Key.Invoke(target, null);
                }
            }
        }
    }
}