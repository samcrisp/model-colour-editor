using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ModelColourEditor
{
    public class AssetPostprocessorVertexColor : AssetPostprocessor
    {
        public override int GetPostprocessOrder() => 210;

        public void OnPostprocessModel(GameObject root)
        {
            CustomAssetData customAssetData = CustomAssetData.Get(assetImporter);
            if (customAssetData == null) { return; }
            if (!customAssetData.HasMeshColours) { return; }

            var meshColorData = customAssetData.meshColors;
            var meshDictionary = meshColorData.GroupBy(mc => mc.meshName).ToDictionary(mc => mc.Key);

            MeshFilter[] meshFilters = root.GetComponentsInChildren<MeshFilter>();
            
            foreach(var meshFilter in meshFilters)
            {
                Mesh sharedMesh = meshFilter.sharedMesh;

                if (meshDictionary.TryGetValue(sharedMesh.name, out var meshColors))
                {
                    var meshColorDictionary = meshColors.ToDictionary(mc => mc.materialIndex);
                    Debug.Log(meshColors.Count());

                    Color[] colors = new Color[sharedMesh.vertexCount];
                    sharedMesh.colors.CopyTo(colors, 0);

                    for (int i = 0; i < sharedMesh.subMeshCount; i++)
                    {
                        if (meshColorDictionary.TryGetValue(i, out var meshColor))
                        {
                            var subMesh = sharedMesh.GetSubMesh(i);
                            Debug.Log($"{i} {subMesh.vertexCount} {sharedMesh.vertexCount}");
                            System.Array.Copy(Enumerable.Repeat(meshColor.color.linear, subMesh.vertexCount).ToArray(), 0, colors, subMesh.firstVertex, subMesh.vertexCount);
                        }
                    }

                    sharedMesh.SetColors(colors);

                    Debug.Log(sharedMesh.colors[0]);

                    // sharedMesh.SetColors(Enumerable.ToList<TResult>(Enumerable.Repeat(meshColor.color.linear, sharedMesh.vertexCount)));

                    if (ModelColourEditorSettings.Instance.defaultMaterial != null)
                    {
                        Renderer renderer = meshFilter.GetComponent<MeshRenderer>();
                        renderer.sharedMaterials = Enumerable.Repeat(ModelColourEditorSettings.Instance.defaultMaterial, sharedMesh.subMeshCount).ToArray();
                    }
                }
                
            }
        }
    }
}