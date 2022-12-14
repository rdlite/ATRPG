using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class AStarGrid : MonoBehaviour
{
    public LayerMask ObstacleMask => _obstacleLayerMask;
    public int GridWidth => _gridSizeX;
    public int GridHeight => _gridSizeY;
    public float NodeRadius => _nodeRadius;
    public Vector3 LDPoint => _worldBoundPoints[0].position;
    public Vector3 RUPoint => _worldBoundPoints[1].position;

    private const int NAVDATA_KEY = 69;

    [SerializeField] private TerrainType[] _regions;
    [SerializeField] private int _notWalkableAroundPenalty = 200;
    [SerializeField] private GizmoDrawType _drawType;
    [SerializeField] private LayerMask _obstacleLayerMask;
    [SerializeField] private Transform[] _worldBoundPoints;
    [Range(.2f, 3f), SerializeField] private float _nodeRadius;
    [Range(0f, 3f), SerializeField] private float _obstacleAvoidance = .5f;
    [SerializeField] private float _heightDelink;

    [SerializeField] private float _maxSurfaceSlope = 30f;

    private Dictionary<int, int> _walkableRegionsDictionary = new Dictionary<int, int>();
    private Node[,] _nodesGrid;
    private TextAsset _navGridData;
    private AStarPathfinder _pathfinder;
    private PathRequestManager _pathRequestManager;
    private LayerMask _walkableMask;
    private float _nodeDiameter;
    private int _gridSizeX, _gridSizeY;
    private int _penaltyMin = int.MaxValue;
    private int _penaltyMax = int.MinValue;

    public void Init()
    {
        if (ReadNavmeshBakedData())
        {
            RegenerateBakedField();
        }
        else
        {
            //CreateGrid();
        }

        _pathfinder = new AStarPathfinder();
        _pathRequestManager = new PathRequestManager();

        _pathfinder.Init(this, _pathRequestManager);
        _pathRequestManager.Init(this, _pathfinder);
    }

    public void SetBounds(Transform ld, Transform ru)
    {
        _worldBoundPoints = new Transform[2];

        _worldBoundPoints[0] = ld;
        _worldBoundPoints[1] = ru;
    }

    public void ClearData()
    {
        _navGridData = null;
        _nodesGrid = null;
    }

    public void SetNodesText(string data)
    {
        _navGridData = new TextAsset(data);
    }

    private bool ReadNavmeshBakedData()
    {
        string path = StringsContainer.NAVGRID_PATH + "/" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + StringsContainer.NAVGRID_RESOLUTION;
        if (File.Exists(path))
        {
            StreamReader reader = new StreamReader(path);

            string navData = reader.ReadToEnd();
            byte[] byteData = Encoding.ASCII.GetBytes(navData);

            for (int i = 0; i < byteData.Length; i++)
            {
                byteData[i] = (byte)(byteData[i] ^ NAVDATA_KEY);
            }

            navData = Encoding.ASCII.GetString(byteData);

            _navGridData = new TextAsset(navData);
            reader.Close();
            return true;
        }
        else
        {
            print("File in path " + path + " NOT EXISTS!");
            return false;
        }
    }

    private void RegenerateBakedField()
    {
        GridSaveData gridSavedData = JsonUtility.FromJson<GridSaveData>(_navGridData.text);
        GridSettings settings = gridSavedData.Settings;
        DataToSaveContainer<Node> savedNodes = gridSavedData.NodesData;

        ResetDataFromSettings(settings);

        int maxX = 0;
        int maxY = 0;

        for (int i = 0; i < savedNodes.Datas.Count; i++)
        {
            if (maxX <= savedNodes.Datas[i].GridX)
            {
                maxX = savedNodes.Datas[i].GridX + 1;
            }

            if (maxY <= savedNodes.Datas[i].GridY)
            {
                maxY = savedNodes.Datas[i].GridY + 1;
            }
        }

        _nodesGrid = new Node[maxX, maxY];

        for (int i = 0; i < savedNodes.Datas.Count; i++)
        {
            _nodesGrid[savedNodes.Datas[i].GridX, savedNodes.Datas[i].GridY] = savedNodes.Datas[i];
        }

        _nodeDiameter = _nodeRadius * 2f;
        _gridSizeX = maxX;
        _gridSizeY = maxY;

        for (int x = 0; x < _gridSizeX; x++)
        {
            for (int y = 0; y < _gridSizeY; y++)
            {
                _nodesGrid[x, y].Neighbours = new List<Node>();
                for (int n = 0; n < _nodesGrid[x, y]._neighboursID.Count; n++)
                {
                    _nodesGrid[x, y].Neighbours.Add(_nodesGrid[_nodesGrid[x, y]._neighboursID[n].idX, _nodesGrid[x, y]._neighboursID[n].idY]);
                }
            }
        }
    }

    private void ResetDataFromSettings(GridSettings settings)
    {
        _regions = settings.TerrainType;
        _obstacleLayerMask = 1 << settings.ObstacleLayerMask.value;
        _notWalkableAroundPenalty = settings.NotWalkableAroundPenalty;
        _nodeRadius = settings.NodeRadius;
        _obstacleAvoidance = settings.ObstacleAvoidance;
        _heightDelink = settings.HeightDelink;
        _maxSurfaceSlope = settings.MaxSurfaceSlope;
    }

    public Node[,] CreateGrid(GridSettings settings = null)
    {
        if (settings != null)
        {
            ResetDataFromSettings(settings);
        }

        _walkableMask.value = 0;
        _walkableRegionsDictionary.Clear();

        foreach (TerrainType terrainType in _regions)
        {
            _walkableMask.value = _walkableMask | terrainType.TerrainMask.value;
            _walkableRegionsDictionary.Add((int)Mathf.Log(terrainType.TerrainMask.value, 2), terrainType.TerrainMovementPenalty);
        }

        _nodeDiameter = _nodeRadius * 2f;
        _gridSizeX = Mathf.RoundToInt(GetGridWorldXSize() / _nodeDiameter);
        _gridSizeY = Mathf.RoundToInt(GetGridWorldZSize() / _nodeDiameter);

        _nodesGrid = new Node[_gridSizeX, _gridSizeY];

        for (int x = 0; x < _gridSizeX; x++)
        {
            for (int y = 0; y < _gridSizeY; y++)
            {
                Vector3 worldPoint = LDPoint + Vector3.right * (x * _nodeDiameter + _nodeRadius) + Vector3.forward * (y * _nodeDiameter + _nodeRadius);
                RaycastHit hitInfo = GroundHit(worldPoint);

                if (hitInfo.transform == null)
                {
                    _nodesGrid[x, y] = new Node(false, worldPoint, x, y, 0);
                    continue;
                }

                Vector3 onGroundPoint = hitInfo.point;

                bool isWalkable = !IsHaveObstacleOnNode(onGroundPoint);

                if (!IsSurfaceNodeWalkableBySlope(hitInfo.normal) && isWalkable)
                {
                    isWalkable = false;
                }

                int movementPenalty = 0;

                if (isWalkable)
                {
                    Ray ray = new Ray(worldPoint + Vector3.up * 50f, Vector3.down);
                    RaycastHit walkableRaycastInfo;
                    if (Physics.Raycast(ray, out walkableRaycastInfo, Mathf.Infinity, _walkableMask))
                    {
                        _walkableRegionsDictionary.TryGetValue(walkableRaycastInfo.collider.gameObject.layer, out movementPenalty);
                    }
                }
                else
                {
                    movementPenalty = _notWalkableAroundPenalty;
                }

                _nodesGrid[x, y] = new Node(isWalkable, onGroundPoint, x, y, movementPenalty);
            }
        }

        for (int x = 0; x < _gridSizeX; x++)
        {
            for (int y = 0; y < _gridSizeY; y++)
            {
                List<Node> result = new List<Node>(9);

                for (int x0 = -1; x0 <= 1; x0++)
                {
                    for (int y0 = -1; y0 <= 1; y0++)
                    {
                        if (x0 == 0 && y0 == 0)
                        {
                            continue;
                        }

                        int checkX = _nodesGrid[x, y].GridX + x0;
                        int checkY = _nodesGrid[x, y].GridY + y0;

                        if (checkX >= 0 && checkY >= 0 && checkX < _gridSizeX && checkY < _gridSizeY)
                        {
                            result.Add(_nodesGrid[checkX, checkY]);
                        }
                    }
                }

                _nodesGrid[x, y].Neighbours = result;
                _nodesGrid[x, y].SetNeighboursIDs(result);
            }
        }

        for (int x = 0; x < _gridSizeX; x++)
        {
            for (int y = 0; y < _gridSizeY; y++)
            {
                foreach (Node neighbour in GetNeighbours(_nodesGrid[x, y], true))
                {
                    Node lowestNode = CheckIfNodeDelinkedByHeightAndReturnLowest(_nodesGrid[x, y], neighbour);

                    if (lowestNode != null)
                    {
                        lowestNode.IsWalkable = false;
                    }
                }
            }
        }

        BlurPenaltyMap(2);

        return _nodesGrid;
    }

    public bool IsSurfaceNodeWalkableBySlope(Vector3 surfaceNormal)
    {
        return (90f - (90f * Vector3.Dot(Vector3.up, surfaceNormal))) < _maxSurfaceSlope;
    }

    public Vector3 GetGroundPoint(Vector3 startPoint)
    {
        RaycastHit outhit;

        LayerMask walkableMask = new LayerMask();

        if (_walkableMask == 0)
        {
            walkableMask.value = 0;

            foreach (TerrainType terrainType in _regions)
            {
                walkableMask.value = walkableMask | terrainType.TerrainMask.value;
            }
        }

        if (Physics.Raycast(startPoint + Vector3.up, Vector3.down, out outhit, Mathf.Infinity, walkableMask | _walkableMask))
        {
            return outhit.point;
        }

        return startPoint;
    }

    public Node CheckIfNodeDelinkedByHeightAndReturnLowest(Node nodeA, Node nodeB)
    {
        bool isDelinkedByHeight = Mathf.Abs(nodeA.WorldPosition.y - nodeB.WorldPosition.y) >= _heightDelink;

        if (isDelinkedByHeight)
        {
            return nodeA.WorldPosition.y > nodeB.WorldPosition.y ? nodeB : nodeA;
        }
        else
        {
            LayerMask checkMask = new LayerMask();
            checkMask.value = 0;

            foreach (TerrainType terrainType in _regions)
            {
                checkMask.value = checkMask | terrainType.TerrainMask.value;
            }

            Vector3 offset = Vector3.up * .1f;

            bool isHaveInstersection = Physics.Linecast(nodeA.WorldPosition + offset, nodeB.WorldPosition + offset, checkMask);

            if (!isHaveInstersection)
            {
                return null;
            }
            else
            {
                return nodeA.WorldPosition.y > nodeB.WorldPosition.y ? nodeB : nodeA; ;
            }
        }
    }

    public bool CheckIfointsDelinkedByHeight(Vector3 nodeA, Vector3 nodeB)
    {
        bool isDelinkedByHeight = Mathf.Abs(nodeA.y - nodeB.y) >= _heightDelink;

        if (isDelinkedByHeight)
        {
            return true;
        }
        else
        {
            LayerMask checkMask = new LayerMask();
            checkMask.value = 0;

            foreach (TerrainType terrainType in _regions)
            {
                checkMask.value = checkMask | terrainType.TerrainMask.value;
            }

            Vector3 offset = Vector3.up * .1f;

            bool isHaveInstersection = Physics.Linecast(nodeA + offset, nodeB + offset, checkMask);

            if (!isHaveInstersection)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }

    public float GetPathLength(Node startNode, Node endNode)
    {
        return _pathfinder.GetPathLength(startNode, endNode);
    }

    private void BlurPenaltyMap(int blurSize)
    {
        int kernelSize = blurSize * 2 + 1;
        int kernelExtends = (kernelSize - 1) / 2;
        int[,] penaltiesHorizontalPass = new int[_gridSizeX, _gridSizeY];
        int[,] penaltiesVerticalPass = new int[_gridSizeX, _gridSizeY];

        for (int y = 0; y < _gridSizeY; y++)
        {
            for (int x = -kernelExtends; x <= kernelExtends; x++)
            {
                int sampleX = Mathf.Clamp(x, 0, kernelExtends);
                penaltiesHorizontalPass[0, y] += _nodesGrid[sampleX, y].MovementPenalty;
            }

            for (int x = 1; x < _gridSizeX; x++)
            {
                int removeIndex = Mathf.Clamp(x - kernelExtends - 1, 0, _gridSizeX);
                int addIndex = Mathf.Clamp(x + kernelExtends, 0, _gridSizeX - 1);

                penaltiesHorizontalPass[x, y] = penaltiesHorizontalPass[x - 1, y] - _nodesGrid[removeIndex, y].MovementPenalty + _nodesGrid[addIndex, y].MovementPenalty;
            }
        }

        for (int x = 0; x < _gridSizeX; x++)
        {
            for (int y = -kernelExtends; y <= kernelExtends; y++)
            {
                int sampleY = Mathf.Clamp(y, 0, kernelExtends);
                penaltiesVerticalPass[x, 0] += penaltiesHorizontalPass[x, sampleY];
            }

            int blurredPenalty = Mathf.RoundToInt((float)penaltiesVerticalPass[x, 0] / (kernelSize * kernelSize));
            _nodesGrid[x, 0].MovementPenalty = blurredPenalty;

            for (int y = 1; y < _gridSizeY; y++)
            {
                int removeIndex = Mathf.Clamp(y - kernelExtends - 1, 0, _gridSizeY);
                int addIndex = Mathf.Clamp(y + kernelExtends, 0, _gridSizeY - 1);

                penaltiesVerticalPass[x, y] = penaltiesVerticalPass[x, y - 1] - penaltiesHorizontalPass[x, removeIndex] + penaltiesHorizontalPass[x, addIndex];

                blurredPenalty = Mathf.RoundToInt((float)penaltiesVerticalPass[x, y] / (kernelSize * kernelSize));
                _nodesGrid[x, y].MovementPenalty = blurredPenalty;

                if (blurredPenalty > _penaltyMax)
                {
                    _penaltyMax = blurredPenalty;
                }

                if (blurredPenalty < _penaltyMin)
                {
                    _penaltyMin = blurredPenalty;
                }
            }
        }
    }

    public Node GetFirstNearestWalkableNode(Node node, bool isCheckHeight, int minXBorder = -1, int maxXBorder = -1, int minYBorder = -1, int maxYBorder = -1)
    {
        return FindNearestWalkableNode(node, isCheckHeight, minXBorder, maxXBorder, minYBorder, maxYBorder);
    }

    private bool IsInsideBorders(Node node, int minX, int maxX, int minY, int maxY)
    {
        if (minX == -1)
        {
            return true;
        }

        return node.GridX >= minX && node.GridX <= maxX && node.GridY >= minY && node.GridY <= maxY;
    }

    private Node FindNearestWalkableNode(Node node, bool isCheckHeight, int minX = -1, int maxX = -1, int minY = -1, int maxY = -1)
    {
        if (node.CheckWalkability && IsInsideBorders(node, minX, maxX, minY, maxY))
        {
            return node;
        }

        float findRadius = .4f;
        float step = 1f;

        int checksAmount = 4;
        bool isLastCircle = false;
        List<Node> nearestNodes = new List<Node>();

        while (true)
        {
            for (int i = 0; i < checksAmount; i++)
            {
                float t = (float)i / checksAmount;

                float circlePoint = t * Mathf.PI * 2f;

                Vector3 checkWPos = node.WorldPosition + new Vector3(Mathf.Cos(circlePoint), 0f, Mathf.Sin(circlePoint)) * findRadius;

                Node nodeCheck = GetNodeFromWorldPoint(checkWPos);
                bool isDelinkedByHeight = isCheckHeight && CheckIfointsDelinkedByHeight(nodeCheck.WorldPosition, node.WorldPosition);

                if (nodeCheck.CheckWalkability && !isDelinkedByHeight && IsInsideBorders(nodeCheck, minX, maxX, minY, maxY))
                {
                    isLastCircle = true;
                    nearestNodes.Add(nodeCheck);
                }
            }

            if (isLastCircle)
            {
                break;
            }

            checksAmount += 4;

            if (checksAmount > 1000)
            {
                print("DOLBOEB");
                break;
            }

            findRadius += step;
        }

        float minDist = Mathf.Infinity;
        Node nearestNode = node;

        for (int i = 0; i < nearestNodes.Count; i++)
        {
            float distance = Vector3.Distance(nearestNodes[i].WorldPosition.RemoveYCoord(), node.WorldPosition.RemoveYCoord());
            if (distance < minDist)
            {
                minDist = distance;
                nearestNode = nearestNodes[i];
            }
        }

        return nearestNode;
    }

    public List<Node> GetUniqueNodesInRange(float circleRange, Vector3 centerPoint, Vector3 excludePoint, int nodesAmount, float maxDistance)
    {
        List<Node> resultNodes = new List<Node>(nodesAmount);
        List<Node> totalNodesPool = new List<Node>();

        Node playerNode = GetNodeFromWorldPoint(excludePoint);

        float checkStep = _nodeRadius / 2f;
        for (float x = centerPoint.x - circleRange / 2f; x <= centerPoint.x + circleRange / 2f; x += checkStep)
        {
            for (float y = centerPoint.z - circleRange / 2f; y <= centerPoint.z + circleRange / 2f; y += checkStep)
            {
                Node node = GetNodeFromWorldPoint(new Vector3(x, centerPoint.y, y));

                if (!totalNodesPool.Contains(node) &&
                    Vector3.Distance(centerPoint, node.WorldPosition) < circleRange / 2f &&
                    node.CheckWalkability &&
                    node != playerNode &&
                    !Physics.Linecast(centerPoint + Vector3.up, node.WorldPosition + Vector3.up, _obstacleLayerMask))
                {
                    totalNodesPool.Add(node);
                }
            }
        }

        //for (int i = 0; i < totalNodesPool.Count; i++) {
        //    Debug.DrawLine(totalNodesPool[i].WorldPosition, totalNodesPool[i].WorldPosition + Vector3.up, Color.blue, 10000f);
        //}

        if (nodesAmount > totalNodesPool.Count)
        {
            resultNodes = new List<Node>(totalNodesPool);
            for (int i = 0; i < nodesAmount - totalNodesPool.Count; i++)
            {
                resultNodes.Add(totalNodesPool[0]);
            }
        }
        else
        {
            for (int i = 0; i < nodesAmount; i++)
            {
                int randomNodeID = UnityEngine.Random.Range(0, totalNodesPool.Count);
                resultNodes.Add(totalNodesPool[randomNodeID]);
                totalNodesPool.RemoveAt(randomNodeID);
            }
        }


        return resultNodes;
    }

    public Node GetNodeFromWorldPoint(Vector3 worldPos)
    {
        Vector3 flatWorldPos = worldPos;
        Vector3 localPos = flatWorldPos - LDPoint;

        int xID = Mathf.FloorToInt(localPos.x / (_nodeRadius * 2));
        int yID = Mathf.FloorToInt(localPos.z / (_nodeRadius * 2));

        xID = Mathf.Clamp(xID, 0, _gridSizeX - 1);
        yID = Mathf.Clamp(yID, 0, _gridSizeY - 1);

        return _nodesGrid[xID, yID];
    }

    private bool IsHaveObstacleOnNode(Vector3 onGroundPos)
    {
        return Physics.OverlapSphere(onGroundPos, _nodeRadius + _obstacleAvoidance, _obstacleLayerMask).Length != 0;
    }

    public bool IsNodesContacts(Node nodeA, Node nodeB)
    {
        int distanceByX = Mathf.Abs(nodeA.GridX - nodeB.GridX);
        int distanceByY = Mathf.Abs(nodeA.GridY - nodeB.GridY);
        return (distanceByX == 1 && distanceByY == 0) || (distanceByX == 0 && distanceByY == 1);
    }

    private RaycastHit GroundHit(Vector3 worldStartPos)
    {
        RaycastHit hitInfo;
        Physics.Raycast(worldStartPos + Vector3.up * 20f, Vector3.down, out hitInfo, Mathf.Infinity, _walkableMask);
        return hitInfo;
    }

    private float GetGridWorldXSize()
    {
        return RUPoint.x - LDPoint.x;
    }

    private float GetGridWorldZSize()
    {
        return RUPoint.z - LDPoint.z;
    }

    public int MaxSize
    {
        get => _gridSizeX * _gridSizeY;
    }

    public Node[,] GetNodesGrid()
    {
        return _nodesGrid;
    }

    public Vector3[] GetPathPoints(Node startPoint, Node endPoint)
    {
        return _pathfinder.CalculatePath(startPoint, endPoint);
    }

    public List<Node> GetNeighbours(Node node, bool isDiagonalMovement)
    {
        return node.Neighbours;
    }

    public Node[,] GetNodesFiledWithinWorldPoints(Vector3 ld, Vector3 ru)
    {
        Node ldNode = GetNodeFromWorldPoint(ld);
        Node ruNode = GetNodeFromWorldPoint(ru);

        int width = ruNode.GridX - ldNode.GridX;
        int height = ruNode.GridY - ldNode.GridY;

        Node[,] returnField = new Node[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                returnField[x, y] = _nodesGrid[ldNode.GridX + x, ldNode.GridY + y];
            }
        }

        return returnField;
    }

    public Node[,] GetNodesInRadiusByMatrix(Vector3 startPosition, Vector3 direction, float length, float angle)
    {
        float checkLength = length;
        Node[,] checkNodes = GetNodesFiledWithinWorldPoints(startPosition - new Vector3(_nodeDiameter * checkLength, 0f, _nodeDiameter * checkLength), startPosition + new Vector3(_nodeDiameter * (checkLength + 1), 0f, _nodeDiameter * (checkLength + 1)));
        Node startNode = GetNodeFromWorldPoint(startPosition);

        for (int x = 0; x < checkNodes.GetLength(0); x++)
        {
            for (int y = 0; y < checkNodes.GetLength(1); y++)
            {
                bool addNode = false;

                if (Vector3.Distance(checkNodes[x, y].WorldPosition, startPosition) <= length && checkNodes[x, y] != startNode)
                {
                    float angleToNode = Vector3.Angle((checkNodes[x, y].WorldPosition - startPosition), direction) * 2f;

                    bool isPossibleToAttackNode = checkNodes[x, y].IsWalkable;

                    if (angleToNode <= angle && isPossibleToAttackNode)
                    {
                        if (!IsHaveObstacleBetweenPoints(startPosition, checkNodes[x, y].WorldPosition))
                        {
                            addNode = true;
                        }
                    }
                }

                if (!addNode)
                {
                    checkNodes[x, y] = null;
                }
            }
        }

        return checkNodes;
    }

    public (Node, bool) GetEndPushingNode(Vector3 startPoint, Vector3 direction, float length, Vector4 bounds, List<Node> additionalObstacles = null)
    {
        Node startNode = GetNodeFromWorldPoint(startPoint);
        Node endNode = null;
        Node lastPossiblePushNode = startNode;
        bool isBreakPath = false;
        float t = 0f;
        float stepAlongLine = .1f;

        while (t <= length)
        {
            t += stepAlongLine;
            Vector3 checkDistance = Vector3.Lerp(startPoint, startPoint + direction * length, t / length);
            Node checkNode = GetNodeFromWorldPoint(checkDistance);

            if (checkNode != lastPossiblePushNode)
            {
                if (!checkNode.IsWalkable || additionalObstacles != null && additionalObstacles.Contains(checkNode))
                {
                    isBreakPath = true;
                    endNode = lastPossiblePushNode;
                    break;
                }
                else if (checkDistance.x <= bounds.x || checkDistance.z <= bounds.y || checkDistance.x >= bounds.z || checkDistance.z >= bounds.w)
                {
                    isBreakPath = false;
                    endNode = lastPossiblePushNode;
                    break;
                }
                else
                {
                    lastPossiblePushNode = checkNode;
                }
            }
        }

        if (endNode == null)
        {
            endNode = GetNodeFromWorldPoint(startPoint + direction * length);
        }

        return (endNode, isBreakPath);
    }

    private bool IsHaveObstacleBetweenPoints(Vector3 pointA, Vector3 pointB)
    {
        return Physics.Linecast(pointA, pointB, _obstacleLayerMask);

        //float t = 0f;
        //float distance = Vector3.Distance(pointA, pointB) * _nodeDiameter;
        //float checksAmount = 10f * distance;
        //float pointsDistanceCheck = 1f / (float)checksAmount;

        //while (t <= 1f)
        //{
        //    t += pointsDistanceCheck;

        //    if (!GetNodeFromWorldPoint(Vector3.Lerp(pointA, pointB, t)).IsWalkable)
        //    {
        //        return true;
        //    }
        //}

        //return false;
    }

    public void SetActiveDecal(bool value)
    {
        DecalProjector decalProjector = GetComponentInChildren<DecalProjector>(true);

        if (_navGridData != null)
        {
            decalProjector.gameObject.SetActive(value);

            if (value && ReadNavmeshBakedData())
            {
                GridSaveData gridSavedData = JsonUtility.FromJson<GridSaveData>(_navGridData.text);
                GridSettings settings = gridSavedData.Settings;
                DataToSaveContainer<Node> savedNodes = gridSavedData.NodesData;

                int maxX = 0;
                int maxY = 0;

                for (int i = 0; i < savedNodes.Datas.Count; i++)
                {
                    if (maxX <= savedNodes.Datas[i].GridX)
                    {
                        maxX = savedNodes.Datas[i].GridX + 1;
                    }

                    if (maxY <= savedNodes.Datas[i].GridY)
                    {
                        maxY = savedNodes.Datas[i].GridY + 1;
                    }
                }

                Texture2D viewTexture = new Texture2D(maxX, maxY);
                _nodesGrid = new Node[maxX, maxY];

                for (int i = 0; i < savedNodes.Datas.Count; i++)
                {
                    _nodesGrid[savedNodes.Datas[i].GridX, savedNodes.Datas[i].GridY] = savedNodes.Datas[i];
                    viewTexture.SetPixel(savedNodes.Datas[i].GridX, savedNodes.Datas[i].GridY, savedNodes.Datas[i].CheckWalkability ? Color.white : Color.black);
                }

                viewTexture.Apply();

                decalProjector.material.SetTexture("_MainTex", viewTexture);

                Vector3 decalPosition = (_nodesGrid[maxX - 1, maxY - 1].WorldPosition - _nodesGrid[0, 0].WorldPosition) / 2f;
                decalPosition = decalPosition.RemoveYCoord();

                decalProjector.transform.position =
                    _nodesGrid[0, 0].WorldPosition
                    + decalPosition
                    + Vector3.right * (settings.NodeRadius * 1.5f)
                    + Vector3.forward * (settings.NodeRadius * 1.5f)
                    + Vector3.up * 100f;
                decalProjector.size = new Vector3(maxX * settings.NodeRadius * 2f, maxY * settings.NodeRadius * 2f, 200f);
            }
        }
        else
        {
            decalProjector.gameObject.SetActive(false);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;

        Vector3 gridCenterPoint = LDPoint + (RUPoint - LDPoint) / 2f;
        Vector3 gridExtend = new Vector3(GetGridWorldXSize(), 1f, GetGridWorldZSize());
        Gizmos.DrawWireCube(gridCenterPoint, gridExtend);

        if (_drawType != GizmoDrawType.None)
        {
            if (_nodesGrid != null)
            {
                for (int x = 0; x < _gridSizeX; x++)
                {
                    for (int y = 0; y < _gridSizeY; y++)
                    {
                        Gizmos.color = Color.white;

                        if (_drawType.HasFlag(GizmoDrawType.PenaltyMap))
                        {
                            Gizmos.color = Color.Lerp(Color.white, Color.black, Mathf.InverseLerp(_penaltyMin, _penaltyMax, _nodesGrid[x, y].MovementPenalty));
                            Gizmos.DrawSphere(_nodesGrid[x, y].WorldPosition, _nodeRadius);
                        }
                        else
                        {
                            if (_drawType.HasFlag(GizmoDrawType.Ground))
                            {
                                if (_nodesGrid[x, y].CheckWalkability)
                                {
                                    Gizmos.color = Color.white;
                                    Gizmos.DrawSphere(_nodesGrid[x, y].WorldPosition, _nodeRadius / 2f);
                                }
                            }

                            if (_drawType.HasFlag(GizmoDrawType.Colliders))
                            {
                                if (!_nodesGrid[x, y].CheckWalkability)
                                {
                                    Gizmos.color = Color.red;
                                    Gizmos.DrawSphere(_nodesGrid[x, y].WorldPosition, _nodeRadius / 2f);
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    [Flags]
    public enum GizmoDrawType
    {
        None = 0, Paths = 1, Ground = 2, Colliders = 4, PenaltyMap = 8
    }

    [Serializable]
    public class TerrainType
    {
        public LayerMask TerrainMask;
        public int TerrainMovementPenalty;
    }
}

[System.Serializable]
public class GridSaveData
{
    public GridSettings Settings;
    public DataToSaveContainer<Node> NodesData;
}

[System.Serializable]
public class GridSettings
{
    public AStarGrid.TerrainType[] TerrainType;
    public int NotWalkableAroundPenalty;
    public LayerMask ObstacleLayerMask;
    public float NodeRadius;
    public float ObstacleAvoidance;
    public float HeightDelink;
    public float MaxSurfaceSlope;

    public GridSettings(
        AStarGrid.TerrainType[] terrainType, int notWalkableAroundPenalty, LayerMask obstacleLayerMask,
        float nodeRadius, float obstacleAvoidance, float heightDelink,
        float maxSurfaceSlope)
    {
        TerrainType = terrainType;
        NotWalkableAroundPenalty = notWalkableAroundPenalty;
        ObstacleLayerMask = obstacleLayerMask;
        NodeRadius = nodeRadius;
        ObstacleAvoidance = obstacleAvoidance;
        HeightDelink = heightDelink;
        MaxSurfaceSlope = maxSurfaceSlope;
    }
}