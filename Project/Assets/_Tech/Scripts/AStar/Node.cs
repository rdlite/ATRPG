using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Node
{
    public bool CheckWalkability
    {
        get => IsWalkable && !_isPlacedByCharacter;
    }

    [System.NonSerialized]
    public List<Node> Neighbours;
    [System.NonSerialized]
    public Node Parent;
    public Vector3 WorldPosition;
    public int GridX, GridY;
    public int gCost, hCost, fCost;
    public bool IsWalkable;
    public int MovementPenalty;
    public SerializableV3 _wPos;
    public List<NodeID> _neighboursID;

    private bool _isPlacedByCharacter;

    public Node(bool walkable, Vector3 wPos, int gridX, int gridY, int movementPenalty)
    {
        IsWalkable = walkable;
        WorldPosition = wPos;
        _wPos = new SerializableV3(wPos.x, wPos.y, wPos.z);
        GridX = gridX;
        GridY = gridY;
        MovementPenalty = movementPenalty;
    }

    public void SetPlacedByUnit(bool value)
    {
        _isPlacedByCharacter = value;
    }

    public void UpdateFCost()
    {
        fCost = gCost + hCost;
    }

    public void SetNeighboursIDs(List<Node> neighbours)
    {
        _neighboursID = new List<NodeID>();

        for (int i = 0; i < neighbours.Count; i++)
        {
            _neighboursID.Add(new NodeID(neighbours[i].GridX, neighbours[i].GridY));
        }
    }

    [System.Serializable]
    public class SerializableV3
    {
        public float x, y, z;

        public SerializableV3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
    }

    [System.Serializable]
    public class NodeID
    {
        public int idX, idY;

        public NodeID(int idX, int idY)
        {
            this.idX = idX;
            this.idY = idY;
        }
    }
}

[System.Serializable]
public class DataToSaveContainer<T>
{
    public List<T> Datas = new List<T>();

    public DataToSaveContainer(T[,] datas)
    {
        for (int x = 0; x < datas.GetLength(0); x++)
        {
            for (int y = 0; y < datas.GetLength(1); y++)
            {
                Datas.Add(datas[x, y]);
            }
        }
    }
}