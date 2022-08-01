using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class BattleGridGenerator : MonoBehaviour {
    [SerializeField] private List<UnitTestData> _unitsTestData;
    [SerializeField] private DecalProjector _decalProjector;
    [SerializeField] private Transform _startPoint;
    [SerializeField] protected Transform _LDPoint, _RUPoint;
    [SerializeField] private float _circularAppearanceSpeed = 5f;
    [SerializeField] private bool _isDebug;

    private Node[,] _nodesArray;
    private bool[,] _isWalkableMap;
    private AStarGrid _globalGrid;
    private Texture2D _viewTexture;
    private Texture2D _walkingPointsTexture;
    private float _circularAppearance;
    private int _width, _height, _startNodeIDX, _startNodeIDY;
    private int _additionalResolutionForWalkingPoint = 2;
    private bool _isGenerated;

    private void Start() {
        _globalGrid = FindObjectOfType<AStarGrid>();
    }

    private void Update() {
        //if (!_isGenerated) {
        //    if (Input.GetKeyDown(KeyCode.Space)) {
        //        GenerateStaticDataForBattle();
        //    }
        //}

        //if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Space)) {
        //    PickUnit();
        //}

        //if (_circularAppearance <= 1f) {
        //    _circularAppearance += Time.deltaTime * _circularAppearanceSpeed * _unitsTestData[_currentUnit].WalkRange / 50f;
        //    _decalProjector.material.SetFloat("_AppearRoundRange", _circularAppearance);
        //}
    }

    private void GenerateStaticDataForBattle() {
        _isGenerated = true;

        Color blackCol = Color.black;
        Color whiteCol = Color.white;

        Node ldNode = _globalGrid.GetNodeFromWorldPoint(_LDPoint.position);
        Node ruNode = _globalGrid.GetNodeFromWorldPoint(_RUPoint.position);
        _width = ruNode.GridX - ldNode.GridX;
        _height = ruNode.GridY - ldNode.GridY;
        _startNodeIDX = ldNode.GridX;
        _startNodeIDY = ldNode.GridY;

        _nodesArray = _globalGrid.GetNodesFiledWithinWorldPoints(_LDPoint.position, _RUPoint.position);
        _isWalkableMap = new bool[_width, _height];

        _viewTexture = new Texture2D(_width, _height);
        _walkingPointsTexture = new Texture2D(_width * _additionalResolutionForWalkingPoint, _height * _additionalResolutionForWalkingPoint);

        for (int x = 0; x < _width * _additionalResolutionForWalkingPoint; x++) {
            for (int y = 0; y < _height * _additionalResolutionForWalkingPoint; y++) {
                _walkingPointsTexture.SetPixel(x, y, blackCol);
            }
        }

        _circularAppearance = 1.1f;

        for (int x = 0; x < _width; x++) {
            for (int y = 0; y < _height; y++) {
                _isWalkableMap[x, y] = _nodesArray[x, y].IsWalkable;

                _viewTexture.SetPixel(x, y, blackCol);

                _walkingPointsTexture.SetPixel(
                    x * _additionalResolutionForWalkingPoint,
                    y * _additionalResolutionForWalkingPoint,
                    whiteCol);
            }
        }

        _viewTexture.Apply();
        _walkingPointsTexture.Apply();

        Vector3 decalPosition = (_nodesArray[_width - 1, _height - 1].WorldPosition - _nodesArray[0, 0].WorldPosition) / 2f;
        decalPosition = decalPosition.RemoveYCoord();

        _decalProjector = Instantiate(_decalProjector);
        _decalProjector.transform.position = 
            _nodesArray[0, 0].WorldPosition
            + decalPosition
            + Vector3.right * (_globalGrid.NodeRadius / 4f)
            + Vector3.forward * (_globalGrid.NodeRadius / 4f)
            + Vector3.up * 10f;
        _decalProjector.size = new Vector3(_width * _globalGrid.NodeRadius * 2f, _height * _globalGrid.NodeRadius * 2f, _decalProjector.size.z);
        SetDecal();

        PlaceUnitsOnGrid();
    }

    private void PlaceUnitsOnGrid() {
        for (int i = 0; i < _unitsTestData.Count; i++) {
            StartCoroutine(SmoothMovementToUnitNode(_unitsTestData[i].Unit.transform));
        }
    }

    private IEnumerator SmoothMovementToUnitNode(Transform unit) {
        Node nearestNode = _globalGrid.GetNodeFromWorldPoint(unit.position);

        if (!nearestNode.IsWalkable) {
            List<Node> neighbours = _globalGrid.GetNeighbours(nearestNode, true);

            List<Node> nearestWalkableNodes = new List<Node>();

            foreach (Node neighbour in neighbours) {
                if (neighbour.IsWalkable) {
                    nearestWalkableNodes.Add(neighbour);
                }
            }

            nearestNode = nearestWalkableNodes[Random.Range(0, nearestWalkableNodes.Count)];
        }

        Vector3 targetPoint = nearestNode.WorldPosition + Vector3.up;
        Vector3 startPoint = unit.position;

        float t = 0f;

        while (t <= 1f) {
            t += Time.deltaTime * 4f;

            unit.position = Vector3.Lerp(startPoint, targetPoint, t);

            yield return null;
        }
    }

    private int _currentUnit;
    private void PickUnit() {
        _currentUnit++;

        if (_currentUnit == _unitsTestData.Count) {
            _currentUnit = 0;
        }

        _circularAppearance = 0f;
        _decalProjector.material.SetFloat("_AppearRoundRange", 0f);

        Vector3 unitPosition = _unitsTestData[_currentUnit].Unit.position.RemoveYCoord();
        Vector3 minPos = _nodesArray[0, 0].WorldPosition.RemoveYCoord();
        Vector3 maxPos = _nodesArray[_width - 1, _height - 1].WorldPosition.RemoveYCoord();

        float uvX = Mathf.InverseLerp(minPos.x, maxPos.x, unitPosition.x); 
        float uvY = Mathf.InverseLerp(minPos.z, maxPos.z, unitPosition.z); 

        _decalProjector.material.SetVector("_AppearCenterPointUV", new Vector2(uvX, uvY));

        ShowUnitWalkingDistance();
    }

    private void ShowUnitWalkingDistance() {
        Vector3 currentUnityPosition = _unitsTestData[_currentUnit].Unit.position;

        Node startNode = _globalGrid.GetNodeFromWorldPoint(currentUnityPosition);

        List<Node> possibleNodes = new List<Node>(25);
        Node[] neighbours;
        List<Node> resultNodes = new List<Node>(25);

        possibleNodes.Add(_globalGrid.GetNodeFromWorldPoint(currentUnityPosition));

        int unitMaxWalkDistance = _unitsTestData[_currentUnit].WalkRange;
        int crushProtection = 0;

        for (int x = 0; x < _width; x++) {
            for (int y = 0; y < _height; y++) {
                _isWalkableMap[x, y] = false;
            }
        }
        
        List<Node> nodesPath;

        while (possibleNodes.Count > 0) {
            crushProtection++;

            if (crushProtection > 100000) {
                print("CRUSHED, DOLBOEB!!!");
                break;
            }

            neighbours = _globalGrid.GetNeighbours(possibleNodes[0], true).ToArray();
            possibleNodes.RemoveAt(0);
            
            foreach (Node neighbour in neighbours) {
                if (!resultNodes.Contains(neighbour) && 
                    neighbour.IsWalkable && 
                    (neighbour.GridX >= _startNodeIDX && neighbour.GridX < _startNodeIDX + _width) && (neighbour.GridY >= _startNodeIDY && neighbour.GridY < _startNodeIDY + _height)) {
                    if (unitMaxWalkDistance >= Mathf.CeilToInt(_globalGrid.GetPathLength(startNode, neighbour))) {
                        resultNodes.Add(neighbour);
                        possibleNodes.Add(neighbour);
                        _isWalkableMap[neighbour.GridX - _startNodeIDX, neighbour.GridY - _startNodeIDY] = true;
                    }
                }
            }
        }

        ShowView();
    }

    private void ShowView() {
        Color blackCol = Color.black;
        Color whiteCol = Color.white;

        for (int x = 0; x < _width; x++) {
            for (int y = 0; y < _height; y++) {
                _viewTexture.SetPixel(x, y, _isWalkableMap[x, y] ? whiteCol : blackCol);
            }
        }

        _viewTexture.Apply();
        _walkingPointsTexture.Apply();

        SetDecal();
    }

    private void SetDecal() {
        _decalProjector.material.SetTexture("_MainTex", _viewTexture);
        _decalProjector.material.SetTexture("_WalkingPointsMap", _walkingPointsTexture);
        _decalProjector.material.SetFloat("_TextureOffset", 0f);
        _decalProjector.material.SetFloat("_WalkPointsTextureOffset", -.0042f);
    }

    private void OnDrawGizmos() {
        if (Application.isPlaying && _isDebug) {
            for (int x = 0; x < _width; x++) {
                for (int y = 0; y < _height; y++) {
                    Gizmos.color = _isWalkableMap[x, y] ? Color.green : Color.red;

                    Gizmos.DrawSphere(_nodesArray[x, y].WorldPosition, .15f);
                }
            }
        }
    }

    [System.Serializable]
    private class UnitTestData {
        public Transform Unit;
        public int WalkRange = 5;
    }
}

public class CarveObstacleDataObject {
    public CarveObjectObstacle Obstacle;
    public Vector3 PrevPos;

    public CarveObstacleDataObject(CarveObjectObstacle obstacle, Vector3 position) {
        Obstacle = obstacle;
        PrevPos = position;
    }
}