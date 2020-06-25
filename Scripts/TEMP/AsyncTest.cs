using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.EditorCoroutines.Editor;
using System.Threading;

public class AsyncTest : MonoBehaviour
{
    public const float MAX_MILLISECONDS_PER_FRAME = 33;

    public List<Color> colors = new List<Color>();
    public GameObject model;
    
    private Queue<System.Action> _taskQueue = new Queue<System.Action>();
    private EditorCoroutine _coroutine;
    private System.Diagnostics.Stopwatch _stopwatch = new System.Diagnostics.Stopwatch();
    private float _stopwatchThreshold;

    [Button]
    public void Test()
    {
        if (_coroutine != null) { EditorCoroutineUtility.StopCoroutine(_coroutine); }

        _taskQueue.Clear();

        for (int i = 0; i < 100; i++)
        {
            _taskQueue.Enqueue(Sleep);
        }

        _coroutine = EditorCoroutineUtility.StartCoroutineOwnerless(RunQueue());
    }

    private IEnumerator RunQueue(System.Action callback = null)
    {
        Debug.Log("started queue");

        StopwatchSetup();

        while (_taskQueue.Count > 0)
        {
            _taskQueue.Dequeue()();

            if (_stopwatch.ElapsedMilliseconds > _stopwatchThreshold)
            {
                _stopwatch.Stop();
                yield return null;
                _stopwatchThreshold = _stopwatch.ElapsedMilliseconds + MAX_MILLISECONDS_PER_FRAME;
                _stopwatch.Start();
            }
        }

        callback?.Invoke();

        Debug.Log("end queue");
    }

    private void Sleep()
    {
        Thread.Sleep(10);
    }

    private void StopwatchSetup()
    {
        _stopwatchThreshold = MAX_MILLISECONDS_PER_FRAME;
        _stopwatch.Reset();
        _stopwatch.Start();
    }
    
    public void Run()
    {
        if (_coroutine != null) { EditorCoroutineUtility.StopCoroutine(_coroutine); }
        _coroutine = EditorCoroutineUtility.StartCoroutine(GetMeshColorsAsync(), this);
    }

    private IEnumerator GetMeshColorsAsync()
    {
        const float THRESHOLD = 20;
        
        UnityEngine.Debug.Log("Starting");
        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();

        var meshFilters = model.GetComponentsInChildren<MeshFilter>();
        var meshes = meshFilters.Select(m => m.sharedMesh).Distinct().ToList();
        colors.Clear();

        float threshold = THRESHOLD;
        foreach(var mesh in meshes)
        {
            HashSet<Color> colorsHashSet = new HashSet<Color>();
            foreach(var color in mesh.colors)
            {
                if (colorsHashSet.Contains(color)) { continue; }
                colorsHashSet.Add(color);
                colors.Add(color);
                
                if (stopwatch.ElapsedMilliseconds > threshold)
                {
                    threshold = stopwatch.ElapsedMilliseconds + THRESHOLD;

                    stopwatch.Stop();
                    yield return null;
                    stopwatch.Start();
                }
            }
        }

        stopwatch.Stop();
        UnityEngine.Debug.Log("Finished");
    }
}
