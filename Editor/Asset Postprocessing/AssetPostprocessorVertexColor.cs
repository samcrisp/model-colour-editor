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
            var customAssetData = CustomAssetData.Get(assetImporter);
            if (customAssetData == null) { return; }
            if (!customAssetData.HasMeshColours) { return; }

            var meshColorData = customAssetData.meshColors;
            IReadOnlyDictionary<string, IGrouping<string, CustomAssetData.MeshColor>> meshDictionary =
                meshColorData.GroupBy(mc => mc.meshName).ToDictionary(mc => mc.Key);

            foreach(var meshFilter in root.GetComponentsInChildren<MeshFilter>())
            {
                ProcessMesh(meshFilter.sharedMesh, meshFilter.GetComponent<MeshRenderer>(), meshDictionary);
            }
            
            foreach(var skinnedMeshRenderer in root.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                ProcessMesh(skinnedMeshRenderer.sharedMesh, skinnedMeshRenderer, meshDictionary);
            }
        }

        private void ProcessMesh(Mesh sharedMesh, Renderer renderer,
            IReadOnlyDictionary<string, IGrouping<string, CustomAssetData.MeshColor>> meshDictionary)
        {
            if (meshDictionary.TryGetValue(sharedMesh.name, out var meshColors))
            {
                var meshColorDictionary = meshColors.ToDictionary(mc => mc.materialIndex);

                Color[] colors = new Color[sharedMesh.vertexCount];
                sharedMesh.colors.CopyTo(colors, 0);

                for (int i = 0; i < sharedMesh.subMeshCount; i++)
                {
                    if (meshColorDictionary.TryGetValue(i, out var meshColor))
                    {
                        var subMesh = sharedMesh.GetSubMesh(i);
                        var color = PlayerSettings.colorSpace == ColorSpace.Gamma ? meshColor.color : meshColor.color.linear;
                        System.Array.Copy(Enumerable.Repeat(color, subMesh.vertexCount).ToArray(), 0, colors, subMesh.firstVertex, subMesh.vertexCount);
                    }
                }

                sharedMesh.SetColors(colors);

                if (ModelColourEditorSettings.Instance.defaultMaterial != null)
                {
                    renderer.sharedMaterials = Enumerable.Repeat(ModelColourEditorSettings.Instance.defaultMaterial, sharedMesh.subMeshCount).ToArray();
                }
            }
        }
    }
}