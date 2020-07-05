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

            var meshColors = customAssetData.meshColors;

            MeshFilter[] meshFilters = root.GetComponentsInChildren<MeshFilter>();
            
            foreach(var meshFilter in meshFilters)
            {
                var meshColor = customAssetData.meshColors.FirstOrDefault(m => m.meshName == meshFilter.sharedMesh.name);
                
                if (meshColor.valid)
                {
                    meshFilter.sharedMesh.SetColors(Enumerable.Repeat(meshColor.color.linear, meshFilter.sharedMesh.vertexCount).ToList());

                    if (ModelColourEditorSettings.Instance.defaultMaterial != null)
                    {
                        Renderer renderer = meshFilter.GetComponent<MeshRenderer>();
                        renderer.sharedMaterials = Enumerable.Repeat(ModelColourEditorSettings.Instance.defaultMaterial, meshFilter.sharedMesh.subMeshCount).ToArray();
                    }
                }
            }
        }
    }
}