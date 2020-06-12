using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class CustomAssetData
{
    public List<MeshColor> meshColors = new List<MeshColor>();
    public bool importMaterialColors = false;

    public bool HasMeshColours => meshColors.Count > 0;

    [System.Serializable]
    public struct MeshColor
    {
        public MeshColor(string meshName, Color color)
        {
            this.meshName = meshName;
            this.color = color;
            this.valid = true;
        }
        
        public string meshName;
        public Color color;
        public bool valid;
    }

    #if UNITY_EDITOR
    public static void Set(string path, CustomAssetData data)
    {
        var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
        var importer = AssetImporter.GetAtPath(path);
        importer.userData = JsonUtility.ToJson(data);
        EditorUtility.SetDirty(asset);
    }

    public static void Set(UnityEngine.Object asset, CustomAssetData data)
    {
        var path = AssetDatabase.GetAssetPath(asset);
        var importer = AssetImporter.GetAtPath(path);
        importer.userData = JsonUtility.ToJson(data);
        EditorUtility.SetDirty(asset);
    }

    public static CustomAssetData Get(string assetPath)
    {
        return Get(AssetImporter.GetAtPath(assetPath));
    }

    public static CustomAssetData Get(UnityEngine.Object asset)
    {
        return Get(AssetDatabase.GetAssetPath(asset));
    }

    public static CustomAssetData Get(AssetImporter importer)
    {
        if (string.IsNullOrEmpty(importer.userData)) { return null; }
        return JsonUtility.FromJson<CustomAssetData>(importer.userData);
    }
    #endif
}