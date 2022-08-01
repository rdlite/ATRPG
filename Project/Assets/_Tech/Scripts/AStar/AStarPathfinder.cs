using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AStarPathfinder {
    private List<Node> _openSet;
    private bool[,] _listOpenSetContains;
    private bool[,] _listCloseSetContains;
    private AStarGrid _grid;
    private PathRequestManager _pathRequestManager;
    private Vector3 _exactEndPos;

    public void Init(AStarGrid grid, PathRequestManager pathRequestManager) {
        _grid = grid;
        _pathRequestManager = pathRequestManager;

        _openSet = new List<Node>(_grid.MaxSize);

        _listOpenSetContains = new bool[_grid.GridWidth, _grid.GridHeight];
        _listCloseSetContains = new bool[_grid.GridWidth, _grid.GridHeight];
    }

    public void StartFindPath(Vector3 wPosStart, Vector3 wPosEnd) {
        Pathfinding(wPosStart, wPosEnd);
    }

    private void Pathfinding(Vector3 startPos, Vector3 endPos) {
        _exactEndPos = endPos;

        Vector3[] resultWaypoints = new Vector3[0];
        bool pathSuccess = false;
        
        Node startNode = _grid.GetNodeFromWorldPoint(startPos);
        Node endNode = _grid.GetNodeFromWorldPoint(endPos);

        int gridW = _grid.GridWidth;
        int gridH = _grid.GridHeight;

        for (int x = 0; x < gridW; x++) {
            for (int y = 0; y < gridH; y++) {
                _listOpenSetContains[x, y] = false;
                _listCloseSetContains[x, y] = false;
            }
        }

        _openSet.Clear();
        _openSet.Add(startNode);
        _listOpenSetContains[startNode.GridX, startNode.GridY] = true;

        Node currentCheckNode = null;
        int opensetCount = 0;

        while (_openSet.Count > 0) {
            Node currentNode = _openSet[0];

            opensetCount = _openSet.Count;

            for (int i = 0; i < opensetCount; i++) {
                currentCheckNode = _openSet[i];

                if (currentCheckNode.fCost < currentNode.fCost || currentCheckNode.fCost == currentNode.fCost && currentCheckNode.hCost < currentNode.hCost) {
                    currentNode = currentCheckNode;
                }
            }

            _listOpenSetContains[currentNode.GridX, currentNode.GridY] = false;
            _openSet.Remove(currentNode);
            _listCloseSetContains[currentNode.GridX, currentNode.GridY] = true;

            if (currentNode == endNode) {
                pathSuccess = true;

                break;
            }

            foreach (Node neighbourNode in _grid.GetNeighbours(currentNode, true)) {
                if (!neighbourNode.IsWalkable || _listCloseSetContains[neighbourNode.GridX, neighbourNode.GridY]) {
                    continue;
                }

                int newMovementCostToNeighbour = currentNode.gCost + GetDistance(currentNode, neighbourNode) + neighbourNode.MovementPenalty;

                if (newMovementCostToNeighbour < neighbourNode.gCost || !_listOpenSetContains[neighbourNode.GridX, neighbourNode.GridY]) {
                    neighbourNode.gCost = newMovementCostToNeighbour;
                    neighbourNode.hCost = GetDistance(neighbourNode, endNode);
                    neighbourNode.UpdateFCost();
                    neighbourNode.Parent = currentNode;

                    if (!_listOpenSetContains[neighbourNode.GridX, neighbourNode.GridY]) {
                        _openSet.Add(neighbourNode);
                        _listOpenSetContains[neighbourNode.GridX, neighbourNode.GridY] = true;
                    }
                }
            }
        }

        if (pathSuccess) {
            resultWaypoints = RetracePath(startNode, endNode);
        }

        if (startNode == endNode) {
            pathSuccess = false;
        }

        _pathRequestManager.FinishedProcessingPath(resultWaypoints, pathSuccess);
    }

    public float GetPathLength(Node startNode, Node endNode) {
        int gridW = _grid.GridWidth;
        int gridH = _grid.GridHeight;

        for (int x = 0; x < gridW; x++) {
            for (int y = 0; y < gridH; y++) {
                _listOpenSetContains[x, y] = false;
                _listCloseSetContains[x, y] = false;
            }
        }

        _openSet.Clear();
        _openSet.Add(startNode);
        _listOpenSetContains[startNode.GridX, startNode.GridY] = true;

        Node currentCheckNode = null;
        int opensetCount = 0;

        while (_openSet.Count > 0) {
            Node currentNode = _openSet[0];

            opensetCount = _openSet.Count;

            for (int i = 0; i < opensetCount; i++) {
                currentCheckNode = _openSet[i];

                if (currentCheckNode.fCost < currentNode.fCost || currentCheckNode.fCost == currentNode.fCost && currentCheckNode.hCost < currentNode.hCost) {
                    currentNode = currentCheckNode;
                }
            }

            _listOpenSetContains[currentNode.GridX, currentNode.GridY] = false;
            _openSet.Remove(currentNode);
            _listCloseSetContains[currentNode.GridX, currentNode.GridY] = true;

            if (currentNode == endNode) {
                break;
            }

            foreach (Node neighbourNode in _grid.GetNeighbours(currentNode, true)) {
                if (!neighbourNode.IsWalkable || _listCloseSetContains[neighbourNode.GridX, neighbourNode.GridY]) {
                    continue;
                }

                int newMovementCostToNeighbour = currentNode.gCost + GetDistance(currentNode, neighbourNode) + neighbourNode.MovementPenalty;

                if (newMovementCostToNeighbour < neighbourNode.gCost || !_listOpenSetContains[neighbourNode.GridX, neighbourNode.GridY]) {
                    neighbourNode.gCost = newMovementCostToNeighbour;
                    neighbourNode.hCost = GetDistance(neighbourNode, endNode);
                    neighbourNode.UpdateFCost();
                    neighbourNode.Parent = currentNode;

                    if (!_listOpenSetContains[neighbourNode.GridX, neighbourNode.GridY]) {
                        _openSet.Add(neighbourNode);
                        _listOpenSetContains[neighbourNode.GridX, neighbourNode.GridY] = true;
                    }
                }
            }
        }

        return RetracePathLength(startNode, endNode);
    }

    private Vector3[] RetracePath(Node startNode, Node endNode) {
        List<Node> path = new List<Node>();
        Node currentNode = endNode;
        while (currentNode != startNode) {
            path.Add(currentNode);
            currentNode = currentNode.Parent;
        }

        Vector3[] waypoints = ConvertPath(path);
        Array.Reverse(waypoints);

        return waypoints;
    }

    private float RetracePathLength(Node startNode, Node endNode) {
        Node currentNode = endNode;
        float length = 0f;

        while (currentNode != startNode) {
            float xDist = currentNode.GridX - currentNode.Parent.GridX;
            float yDist = currentNode.GridY - currentNode.Parent.GridY;

            length += (xDist != 0 && yDist != 0) ? 1.4f : 1f;

            currentNode = currentNode.Parent;
        }

        return length;
    }

    private Vector3[] ConvertPath(List<Node> path) {
        List<Vector3> result = new List<Vector3>();

        result.Add(_exactEndPos);

        Vector3 directionOld = Vector3.zero;
        for (int i = 1; i < path.Count; i++) {
            Vector3 directionNew = path[i - 1].WorldPosition - path[i].WorldPosition;
            if (directionNew != directionOld) {
                result.Add(path[i].WorldPosition);
            }

            directionOld = directionNew;
        }

        return result.ToArray();
    }

    private int GetDistance(Node nodeA, Node nodeB) {
        int dstX = nodeA.GridX - nodeB.GridX;
        int dstY = nodeA.GridY - nodeB.GridY;

        dstX = dstX < 0 ? -dstX : dstX;
        dstY = dstY < 0 ? -dstY : dstY;

        if (dstX > dstY) {
            return 14 * dstY + 10 * (dstX - dstY);
        }

        return 14 * dstX + 10 * (dstY - dstX);
    }
}