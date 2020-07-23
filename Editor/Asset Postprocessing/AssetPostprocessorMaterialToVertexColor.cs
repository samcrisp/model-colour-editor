using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ModelColourEditor
{
    public class AssetPostprocessorMaterialToVertexColor : AssetPostprocessor
    {
        private Dictionary<Renderer, List<Color>> _colors;
        private bool _shouldProcess = false;
        private bool _importMaterialColours = false;

        public override int GetPostprocessOrder() => 200;

        public void OnPreprocessModel()
        {
            _colors = new Dictionary<Renderer, List<Color>>();

            _importMaterialColours = false;

            var customAssetData = CustomAssetData.Get(assetImporter);
            if (customAssetData == null)
            {
                _shouldProcess = _importMaterialColours = ModelColourEditorSettings.Instance.importMaterialColoursByDefault;
            }
            else
            {
                var hasMeshColours = customAssetData.meshColors != null && customAssetData.meshColors.Count > 0;
                _importMaterialColours = customAssetData.ShouldImportMaterialColors;
                _shouldProcess = hasMeshColours || _importMaterialColours;
            }

            var importer = assetImporter as ModelImporter;
            if (importer != null)
            {
                importer.materialImportMode = _shouldProcess
                    ? ModelImporterMaterialImportMode.None
                    : ModelImporterMaterialImportMode.ImportStandard;
            }
        }

        public Material OnAssignMaterialModel(Material material, Renderer renderer)
        {
            if (!_shouldProcess) { return null; }

            if (!_colors.ContainsKey(renderer))
            {
                _colors.Add(renderer, new List<Color>());
            }

            _colors[renderer].Add(material.color);

            return null;
        }

        public void OnPostprocessModel(GameObject root)
        {
            if (!_shouldProcess) { return; }

            HashSet<string> meshHashSet = null;
            if (!_importMaterialColours)
            {
                var customAssetData = CustomAssetData.Get(assetImporter);
                var meshColorData = customAssetData.meshColors;
                meshHashSet = new HashSet<string>(meshColorData.Select(mc => mc.meshName));
            }
            
            var meshFilters = root.GetComponentsInChildren<MeshFilter>();
            
            foreach(var meshFilter in meshFilters)
            {
                Mesh mesh = meshFilter.sharedMesh;
                if (mesh.vertexCount == 0) { continue; }
                Renderer renderer = meshFilter.GetComponent<MeshRenderer>();

                List<Color> colors = new List<Color>();
                List<Vector3> vertices = new List<Vector3>();
                List<Vector3> normals = new List<Vector3>();
                List<List<int>> indices = new List<List<int>>();
                List<Vector2> uvs = new List<Vector2>();

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
                            uvs.Add(mesh.uv[index]);
                        }

                        indices[i].Add(triangleMap[index]);
                    }
                }

                meshFilter.sharedMesh.SetVertices(vertices);
                meshFilter.sharedMesh.SetNormals(normals);
                meshFilter.sharedMesh.SetUVs(0, uvs);

                if (_importMaterialColours || meshHashSet.Contains(mesh.name))
                {
                    meshFilter.sharedMesh.SetColors(colors);
                }
                
                for (int i = 0; i < indices.Count; i++)
                {
                    meshFilter.sharedMesh.SetTriangles(indices[i], i, true);
                }

                // meshFilter.sharedMesh.subMeshCount = 1;
                // meshFilter.sharedMesh.SetSubMesh(0, new UnityEngine.Rendering.SubMeshDescriptor(0, mesh.vertexCount));
                // renderer.sharedMaterials = new Material[1] { _defaultMaterial };

                if (ModelColourEditorSettings.Instance.defaultMaterial != null)
                {
                    renderer.sharedMaterials = Enumerable.Repeat(ModelColourEditorSettings.Instance.defaultMaterial, meshFilter.sharedMesh.subMeshCount).ToArray();
                }
            }

            Debug.Log($"Imported material to vertex colours {assetPath}");
        }
    }
}