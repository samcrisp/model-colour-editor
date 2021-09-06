using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ModelColourEditor
{
    public class AssetPostprocessorMaterialToVertexColor : AssetPostprocessor
    {
        private Dictionary<int, List<Color>> _colors; // mesh instance id -> colours
        private bool _shouldProcess = false;
        private bool _importMaterialColours = false;

        public override int GetPostprocessOrder() => 200;

        public void OnPreprocessModel()
        {
            _colors = new Dictionary<int, List<Color>>();

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
            
            if (_shouldProcess)
            {
                var importer = assetImporter as ModelImporter;
                if (importer != null)
                {
                    importer.materialImportMode = ModelImporterMaterialImportMode.None;
                }
            }
        }

        public Material OnAssignMaterialModel(Material material, Renderer renderer)
        {
            if (!_shouldProcess) { return null; }

            var id = renderer.GetComponent<MeshFilter>().sharedMesh.GetInstanceID();

            if (!_colors.ContainsKey(id))
            {
                _colors.Add(id, new List<Color>());
            }
            
            _colors[id].Add(PlayerSettings.colorSpace == ColorSpace.Gamma ? material.color.gamma : material.color);

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

            foreach(var meshFilter in root.GetComponentsInChildren<MeshFilter>())
            {
                ProcessMesh(meshFilter.sharedMesh, meshFilter.GetComponent<Renderer>(), meshHashSet);
            }
            
            foreach(var skinnedMeshRenderer in root.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                ProcessMesh(skinnedMeshRenderer.sharedMesh, skinnedMeshRenderer, meshHashSet);
            }

            Debug.Log($"Imported material to vertex colours {assetPath}");
        }

        private void ProcessMesh(Mesh sharedMesh, Renderer renderer, HashSet<string> meshHashSet)
        {
            if (sharedMesh.vertexCount == 0) { return; }

            var colors = new List<Color>();
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var tangents = new List<Vector4>();
            var indices = new List<List<int>>();
            var uvs = new List<Vector2>();
            var blendShapeData = BlendShapeData.CreateFromMesh(sharedMesh);
            var hasUvs = sharedMesh.uv.Length > 0;
            
            for (int i = 0; i < sharedMesh.subMeshCount; i++)
            {
                indices.Add(new List<int>());
                Dictionary<int, int> triangleMap = new Dictionary<int, int>();
                var subMesh = sharedMesh.GetSubMesh(i);
                Color color = _colors[sharedMesh.GetInstanceID()][i];
                
                for(int j = subMesh.indexStart; j < subMesh.indexStart + subMesh.indexCount; j++)
                {
                    int index = sharedMesh.triangles[j];
                    
                    if (!triangleMap.ContainsKey(index))
                    {
                        triangleMap.Add(index, vertices.Count);
                        vertices.Add(sharedMesh.vertices[index]);
                        colors.Add(color);
                        normals.Add(sharedMesh.normals[index]);
                        tangents.Add(sharedMesh.tangents[index]);
                        if (hasUvs) uvs.Add(sharedMesh.uv[index]);
                        blendShapeData.AddIndex(index);
                    }

                    indices[i].Add(triangleMap[index]);
                }
            }

            sharedMesh.SetVertices(vertices);
            sharedMesh.SetNormals(normals);
            sharedMesh.SetUVs(0, uvs);
            sharedMesh.SetTangents(tangents);

            // Apply blend shape data
            blendShapeData.ApplyToMesh(sharedMesh);
            
            if (_importMaterialColours || meshHashSet.Contains(sharedMesh.name))
            {
                sharedMesh.SetColors(colors);
            }
            
            for (int i = 0; i < indices.Count; i++)
            {
                sharedMesh.SetTriangles(indices[i], i, true);
            }

            // meshFilter.sharedMesh.subMeshCount = 1;
            // meshFilter.sharedMesh.SetSubMesh(0, new UnityEngine.Rendering.SubMeshDescriptor(0, mesh.vertexCount));
            // renderer.sharedMaterials = new Material[1] { _defaultMaterial };

            if (ModelColourEditorSettings.Instance.defaultMaterial != null)
            {
                renderer.sharedMaterials = Enumerable.Repeat(ModelColourEditorSettings.Instance.defaultMaterial, sharedMesh.subMeshCount).ToArray();
            }
        }

        private class BlendShapeData
        {
            public int blendShapeCount;
            public BlendShapeShape[] blendShapes;

            public static BlendShapeData CreateFromMesh(Mesh sharedMesh)
            {
                var blendShapeData = new BlendShapeData
                {
                    blendShapeCount = sharedMesh.blendShapeCount,
                    blendShapes = new BlendShapeShape[sharedMesh.blendShapeCount]
                };
                
                for (int i = 0; i < blendShapeData.blendShapeCount; i++)
                {
                    var blendShapeFrameCount = sharedMesh.GetBlendShapeFrameCount(i);
                    blendShapeData.blendShapes[i] = new BlendShapeShape
                    {
                        frameCount = blendShapeFrameCount,
                        name = sharedMesh.GetBlendShapeName(i),
                        frames = new BlendShapeFrame[blendShapeFrameCount]
                    };

                    for (int j = 0; j < blendShapeFrameCount; j++)
                    {
                        var blendShapeFrame = new BlendShapeFrame
                        {
                            weight = sharedMesh.GetBlendShapeFrameWeight(i, j),
                            shapeIndex = i,
                            frameIndex = j,
                            deltaNormals = new Vector3[sharedMesh.vertexCount],
                            deltaTangents = new Vector3[sharedMesh.vertexCount],
                            deltaVertices = new Vector3[sharedMesh.vertexCount],
                            newDeltaNormals = new List<Vector3>(),
                            newDeltaTangents = new List<Vector3>(),
                            newDeltaVertices = new List<Vector3>()
                        };
                        
                        sharedMesh.GetBlendShapeFrameVertices(i, j, blendShapeFrame.deltaVertices,
                            blendShapeFrame.deltaNormals, blendShapeFrame.deltaTangents);

                        blendShapeData.blendShapes[i].frames[j] = blendShapeFrame;
                    }
                }

                return blendShapeData;
            }

            public void ApplyToMesh(Mesh sharedMesh)
            {
                sharedMesh.ClearBlendShapes();
                for (var i = 0; i < blendShapeCount; i++)
                {
                    var blendShape = blendShapes[i];
                
                    for (int j = 0; j < blendShape.frameCount; j++)
                    {
                        var frame = blendShape.frames[j];
                        
                        sharedMesh.AddBlendShapeFrame(blendShape.name, frame.weight, frame.newDeltaVertices.ToArray(),
                            frame.newDeltaNormals.ToArray(), frame.newDeltaTangents.ToArray());
                    }
                }
            }

            public void AddIndex(int index)
            {
                for (var i = 0; i < blendShapeCount; i++)
                {
                    var blendShape = blendShapes[i];
                
                    for (int j = 0; j < blendShape.frameCount; j++)
                    {
                        var frame = blendShape.frames[j];
                    
                        frame.newDeltaNormals.Add(frame.deltaNormals[index]);
                        frame.newDeltaVertices.Add(frame.deltaVertices[index]);
                        frame.newDeltaTangents.Add(frame.deltaTangents[index]);
                    }
                }
            }
        }

        private class BlendShapeShape
        {
            public int frameCount;
            public string name;
            public BlendShapeFrame[] frames;
        }

        private class BlendShapeFrame
        {
            public float weight;
            public int shapeIndex;
            public int frameIndex;
            public Vector3[] deltaVertices;
            public Vector3[] deltaNormals;
            public Vector3[] deltaTangents;
            public List<Vector3> newDeltaVertices;
            public List<Vector3> newDeltaNormals;
            public List<Vector3> newDeltaTangents;
        }
    }
}