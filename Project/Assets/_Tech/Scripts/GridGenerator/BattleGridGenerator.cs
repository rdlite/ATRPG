using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class BattleGridGenerator : MonoBehaviour {
    [SerializeField] private DecalProjector _decalProjector;
    [SerializeField] private Transform _startPoint;
    [SerializeField] private float _circularAppearanceSpeed = 5f;
    [SerializeField] private float _borderForGeneratedRect = 10f;
    [SerializeField] private bool _isDebug;

    private List<CharacterWalker> _unitsData;
    private Node[,] _nodesArray;
    private bool[,] _isWalkableMap;
    private Transform _LDPoint, _RUPoint;
    private CameraSimpleFollower _cameraFollower;
    private AStarGrid _globalGrid;
    private Texture2D _viewTexture;
    private Texture2D _walkingPointsTexture;
    private float _circularAppearance;
    private int _width, _height, _startNodeIDX, _startNodeIDY;
    private int _additionalResolutionForWalkingPoint = 2;
    private bool _isGenerated;

    public void Init(AStarGrid globalGrid) {
        _globalGrid = globalGrid;
    }

    public void Tick() {
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

    public void StartBattle(
        CharactersGroupContainer charactersContainer, EnemyCharacterWalker triggeredEnemy, CameraSimpleFollower cameraFollower) {
        _cameraFollower = cameraFollower;

        _unitsData = new List<CharacterWalker>();
        _LDPoint = new GameObject("BattleLDPoint").transform;
        _RUPoint = new GameObject("BattleRUPoint").transform;
        _LDPoint.SetParent(transform);
        _RUPoint.SetParent(transform);

        _unitsData.AddRange(charactersContainer.GetCharacters());
        _unitsData.AddRange(triggeredEnemy.GetAllConnectedEnemies());

        float minXPos = Mathf.Infinity, maxXPos = -Mathf.Infinity;
        float minZPos = Mathf.Infinity, maxZPos = -Mathf.Infinity;

        for (int i = 0; i < _unitsData.Count; i++) {
            if (_unitsData[i].transform.position.x < minXPos) {
                minXPos = _unitsData[i].transform.position.x;
            }
            if (_unitsData[i].transform.position.x > maxXPos) {
                maxXPos = _unitsData[i].transform.position.x;
            }
            if (_unitsData[i].transform.position.z < minZPos) {
                minZPos = _unitsData[i].transform.position.z;
            }
            if (_unitsData[i].transform.position.z > maxZPos) {
                maxZPos = _unitsData[i].transform.position.z;
            }
        }

        _LDPoint.transform.position = new Vector3(minXPos - _borderForGeneratedRect, 0f, minZPos - _borderForGeneratedRect);
        _RUPoint.transform.position = new Vector3(maxXPos + _borderForGeneratedRect, 0f, maxZPos + _borderForGeneratedRect);

        _cameraFollower.SetMovementRestrictions(_LDPoint.position, _RUPoint.position);

        PlaceUnitsOnGrid();
        GenerateStaticDataForBattle();
    }

    public void Cleanup() {
        _unitsData.Clear();
        Destroy(_LDPoint.gameObject);
        Destroy(_RUPoint.gameObject);
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
                _isWalkableMap[x, y] = _nodesArray[x, y].CheckWalkability;

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
    }

    private void PlaceUnitsOnGrid() {
        for (int i = 0; i < _unitsData.Count; i++) {
            StartCoroutine(SmoothMovementToUnitNode(_unitsData[i].transform));
        }
    }

    private IEnumerator SmoothMovementToUnitNode(Transform unit) {
        Node nearestNode = _globalGrid.GetNodeFromWorldPoint(unit.position);

        if (!nearestNode.CheckWalkability) {
            nearestNode = _globalGrid.GetFirstNearestWalkableNode(nearestNode);
        }

        nearestNode.SetPlacedByCharacter(true);

        Vector3 targetPoint = nearestNode.WorldPosition;
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

        if (_currentUnit == _unitsData.Count) {
            _currentUnit = 0;
        }

        _circularAppearance = 0f;
        _decalProjector.material.SetFloat("_AppearRoundRange", 1f);
        //_decalProjector.material.SetFloat("_AppearRoundRange", 0f);

        Vector3 unitPosition = _unitsData[_currentUnit].transform.position.RemoveYCoord();
        Vector3 minPos = _nodesArray[0, 0].WorldPosition.RemoveYCoord();
        Vector3 maxPos = _nodesArray[_width - 1, _height - 1].WorldPosition.RemoveYCoord();

        float uvX = Mathf.InverseLerp(minPos.x, maxPos.x, unitPosition.x); 
        float uvY = Mathf.InverseLerp(minPos.z, maxPos.z, unitPosition.z); 

        _decalProjector.material.SetVector("_AppearCenterPointUV", new Vector2(uvX, uvY));

        ShowUnitWalkingDistance();
    }

    private void ShowUnitWalkingDistance() {
        Vector3 currentUnityPosition = _unitsData[_currentUnit].transform.position;

        Node startNode = _globalGrid.GetNodeFromWorldPoint(currentUnityPosition);

        List<Node> possibleNodes = new List<Node>(25);
        Node[] neighbours;
        List<Node> resultNodes = new List<Node>(25);

        possibleNodes.Add(_globalGrid.GetNodeFromWorldPoint(currentUnityPosition));

        int unitMaxWalkDistance = 5;//_unitsData[_currentUnit].WalkRange;
        int crushProtection = 0;

        for (int x = 0; x < _width; x++) {
            for (int y = 0; y < _height; y++) {
                _isWalkableMap[x, y] = false;
            }
        }
        
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
                    neighbour.CheckWalkability && 
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
}

public class CarveObstacleDataObject {
    public CarveObjectObstacle Obstacle;
    public Vector3 PrevPos;

    public CarveObstacleDataObject(CarveObjectObstacle obstacle, Vector3 position) {
        Obstacle = obstacle;
        PrevPos = position;
    }
}