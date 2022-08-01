using System;
using System.Collections.Generic;
using UnityEngine;

public class PathRequestManager {
    [SerializeField] private AStarGrid _grid;

    private static PathRequestManager _instance;

    private Queue<PathRequest> _pathRequestQueue = new Queue<PathRequest>();
    private PathRequest _currentPathRequest;
    private AStarGrid _aStarGrid;
    private AStarPathfinder _pathfinder;

    private bool _isProcessingPath;

    public void Init(AStarGrid aStarGrid, AStarPathfinder pathfinder) {
        _instance = this;

        _aStarGrid = aStarGrid;
        _pathfinder = pathfinder;
    }

    public static void RequestPath(Vector3 wPosStart, Vector3 wPosEnd, Action<Vector3[], bool> callback = null) {
        PathRequest newRequest = new PathRequest(wPosStart, wPosEnd, callback);
        _instance._pathRequestQueue.Enqueue(newRequest);
        _instance.TryProcessNext();
    }

    public void FinishedProcessingPath(Vector3[] path, bool success) {
        _currentPathRequest.Callback(path, success);
        _isProcessingPath = false;
        TryProcessNext();
    }

    private void TryProcessNext() {
        if (!_isProcessingPath && _pathRequestQueue.Count > 0) {
            _currentPathRequest = _pathRequestQueue.Dequeue();
            _isProcessingPath = true;
            _pathfinder.StartFindPath(_currentPathRequest.PathStart, _currentPathRequest.PathEnd);
        }
    }

    private struct PathRequest {
        public Vector3 PathStart, PathEnd;
        public Action<Vector3[], bool> Callback;

        public PathRequest(Vector3 pathStart, Vector3 pathEnd, Action<Vector3[], bool> callback) {
            PathStart = pathStart;
            PathEnd = pathEnd;
            Callback = callback;
        }
    }
}
