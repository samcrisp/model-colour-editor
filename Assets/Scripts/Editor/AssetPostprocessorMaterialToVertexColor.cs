﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class AssetPostprocessorMaterialToVertexColor : AssetPostprocessor
{
    private Material _defaultMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Materials/Resources/mat_default_lit.mat");

    private Dictionary<Renderer, List<Color>> _colors;
    private bool _shouldProcess = false;

    public void OnPreprocessModel()
    {
        _shouldProcess = false;
        CustomAssetData customAssetData = CustomAssetData.Get(assetImporter);
        if (customAssetData == null) { return; }
        if (!customAssetData.importMaterialColors) { return; }
        _shouldProcess = true;

        _colors = new Dictionary<Renderer, List<Color>>();
    }

    public void OnAssignMaterialModel(Material material, Renderer renderer)
    {
        if (!_shouldProcess) { return; }

        if (!_colors.ContainsKey(renderer))
        {
            _colors.Add(renderer, new List<Color>());
        }

        _colors[renderer].Add(material.color);
    }

    public void OnPostprocessModel(GameObject root)
    {
        if (!_shouldProcess) { return; }

        MeshFilter[] meshFilters = root.GetComponentsInChildren<MeshFilter>();
        
        foreach(var meshFilter in meshFilters)
        {
            Mesh mesh = meshFilter.sharedMesh;
            if (mesh.vertexCount == 0) { continue; }
            Renderer renderer = meshFilter.GetComponent<MeshRenderer>();

            List<Color> colors = new List<Color>();
            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<List<int>> indices = new List<List<int>>();

            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                indices.Add(new List<int>());
                Dictionary<int, int> triangleMap = new Dictionary<int, int>();
                var subMesh = mesh.GetSubMesh(i);
                Color color = _colors[renderer][i];
                
                for(int j = subMesh.indexStart; j < subMesh.indexStart + subMesh.indexCount; j++)
                {
                    int index = mesh.triangles[j];
                    
                    if (!triangleMap.ContainsKey(index))
                    {
                        triangleMap.Add(index, vertices.Count);
                        vertices.Add(mesh.vertices[index]);
                        colors.Add(color);
                        normals.Add(mesh.normals[index]);
                    }

                    indices[i].Add(triangleMap[index]);
                }
            }

            meshFilter.sharedMesh.SetVertices(vertices);
            meshFilter.sharedMesh.SetColors(colors);
            meshFilter.sharedMesh.SetNormals(normals);
            for (int i = 0; i < indices.Count; i++)
            {
                meshFilter.sharedMesh.SetTriangles(indices[i], i, true);
            }

            // meshFilter.sharedMesh.subMeshCount = 1;
            // meshFilter.sharedMesh.SetSubMesh(0, new UnityEngine.Rendering.SubMeshDescriptor(0, mesh.vertexCount));
            // renderer.sharedMaterials = new Material[1] { _defaultMaterial };

            renderer.sharedMaterials = Enumerable.Repeat(_defaultMaterial, meshFilter.sharedMesh.subMeshCount).ToArray();
        }

        Debug.Log($"Imported material to vertex colours {assetPath}");
    }
}
