using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class BattleGridGenerator : MonoBehaviour {
    [SerializeField] private DecalProjector _decalProjector;
    [SerializeField] private LayerMask _charactersMask;
    [SerializeField] private Transform _startPoint;
    [SerializeField] private float _circularAppearanceSpeed = 5f;
    [SerializeField] private float _borderForGeneratedRect = 10f;
    [SerializeField] private bool _isDebug;

    private BattleGridData _battleGridData;
    private BattleHandler _battlehandler;
    private int _additionalResolutionForWalkingPoint = 2;
    private bool _isGenerated;

    public void Init(AStarGrid globalGrid) {
        _battleGridData = new BattleGridData();
        _battleGridData.CharactersLayerMask = _charactersMask;
        _battleGridData.GlobalGrid = globalGrid;
    }

    public void Tick() {
        _battlehandler.Tick();
    }

    public void StartBattle(
        CharactersGroupContainer charactersContainer, EnemyCharacterWalker triggeredEnemy, CameraSimpleFollower cameraFollower,
        UIRoot uiRoot) {
        _battleGridData.Units = new List<CharacterWalker>();
        _battleGridData.LDPoint = new GameObject("BattleLDPoint").transform;
        _battleGridData.RUPoint = new GameObject("BattleRUPoint").transform;
        _battleGridData.LDPoint.SetParent(transform);
        _battleGridData.RUPoint.SetParent(transform);

        _battleGridData.Units.AddRange(charactersContainer.GetCharacters());
        _battleGridData.Units.AddRange(triggeredEnemy.GetAllConnectedEnemies());

        float minXPos = Mathf.Infinity, maxXPos = -Mathf.Infinity;
        float minZPos = Mathf.Infinity, maxZPos = -Mathf.Infinity;

        for (int i = 0; i < _battleGridData.Units.Count; i++) {
            if (_battleGridData.Units[i].transform.position.x < minXPos) {
                minXPos = _battleGridData.Units[i].transform.position.x;
            }
            if (_battleGridData.Units[i].transform.position.x > maxXPos) {
                maxXPos = _battleGridData.Units[i].transform.position.x;
            }
            if (_battleGridData.Units[i].transform.position.z < minZPos) {
                minZPos = _battleGridData.Units[i].transform.position.z;
            }
            if (_battleGridData.Units[i].transform.position.z > maxZPos) {
                maxZPos = _battleGridData.Units[i].transform.position.z;
            }
        }

        _battleGridData.LDPoint.transform.position = new Vector3(minXPos - _borderForGeneratedRect, 0f, minZPos - _borderForGeneratedRect);
        _battleGridData.RUPoint.transform.position = new Vector3(maxXPos + _borderForGeneratedRect, 0f, maxZPos + _borderForGeneratedRect);

        cameraFollower.SetMovementRestrictions(_battleGridData.LDPoint.position, _battleGridData.RUPoint.position);

        PlaceUnitsOnGrid();
        GenerateStaticDataForBattle();

        _battlehandler = new BattleHandler();
        _battlehandler.Init(
            cameraFollower, _battleGridData, _decalProjector,
            uiRoot);
    }

    public void Cleanup() {
        _battleGridData.Units.Clear();
        Destroy(_battleGridData.LDPoint.gameObject);
        Destroy(_battleGridData.RUPoint.gameObject);
    }

    private void GenerateStaticDataForBattle() {
        _isGenerated = true;

        Color blackCol = Color.black;
        Color whiteCol = Color.white;

        Node ldNode = _battleGridData.GlobalGrid.GetNodeFromWorldPoint(_battleGridData.LDPoint.position);
        Node ruNode = _battleGridData.GlobalGrid.GetNodeFromWorldPoint(_battleGridData.RUPoint.position);
        _battleGridData.Width = ruNode.GridX - ldNode.GridX;
        _battleGridData.Height = ruNode.GridY - ldNode.GridY;
        _battleGridData.StartNodeIDX = ldNode.GridX;
        _battleGridData.StartNodeIDY = ldNode.GridY;

        _battleGridData.NodesGrid = _battleGridData.GlobalGrid.GetNodesFiledWithinWorldPoints(_battleGridData.LDPoint.position, _battleGridData.RUPoint.position);
        _battleGridData.WalkableMap = new bool[_battleGridData.Width, _battleGridData.Height];

        _battleGridData.ViewTexture = new Texture2D(_battleGridData.Width, _battleGridData.Height);
        _battleGridData.WalkingPointsTexture = new Texture2D(_battleGridData.Width * _additionalResolutionForWalkingPoint, _battleGridData.Height * _additionalResolutionForWalkingPoint);

        for (int x = 0; x < _battleGridData.Width * _additionalResolutionForWalkingPoint; x++) {
            for (int y = 0; y < _battleGridData.Height * _additionalResolutionForWalkingPoint; y++) {
                _battleGridData.WalkingPointsTexture.SetPixel(x, y, blackCol);
            }
        }

        for (int x = 0; x < _battleGridData.Width; x++) {
            for (int y = 0; y < _battleGridData.Height; y++) {
                _battleGridData.WalkableMap[x, y] = _battleGridData.NodesGrid[x, y].CheckWalkability;

                _battleGridData.ViewTexture.SetPixel(x, y, blackCol);

                _battleGridData.WalkingPointsTexture.SetPixel(
                    x * _additionalResolutionForWalkingPoint,
                    y * _additionalResolutionForWalkingPoint,
                    whiteCol);
            }
        }

        _battleGridData.ViewTexture.Apply();
        _battleGridData.WalkingPointsTexture.Apply();

        Vector3 decalPosition = (_battleGridData.NodesGrid[_battleGridData.Width - 1, _battleGridData.Height - 1].WorldPosition - _battleGridData.NodesGrid[0, 0].WorldPosition) / 2f;
        decalPosition = decalPosition.RemoveYCoord();

        _decalProjector = Instantiate(_decalProjector);
        _decalProjector.transform.position =
            _battleGridData.NodesGrid[0, 0].WorldPosition
            + decalPosition
            + Vector3.right * (_battleGridData.GlobalGrid.NodeRadius / 4f)
            + Vector3.forward * (_battleGridData.GlobalGrid.NodeRadius / 4f)
            + Vector3.up * 10f;
        _decalProjector.size = new Vector3(_battleGridData.Width * _battleGridData.GlobalGrid.NodeRadius * 2f, _battleGridData.Height * _battleGridData.GlobalGrid.NodeRadius * 2f, _decalProjector.size.z);
        _decalProjector.transform.SetParent(transform);
        SetDecal();
    }

    private void PlaceUnitsOnGrid() {
        for (int i = 0; i < _battleGridData.Units.Count; i++) {
            StartCoroutine(SmoothMovementToUnitNode(_battleGridData.Units[i].transform));
        }
    }

    private IEnumerator SmoothMovementToUnitNode(Transform unit) {
        Node nearestNode = _battleGridData.GlobalGrid.GetNodeFromWorldPoint(unit.position);

        if (!nearestNode.CheckWalkability) {
            nearestNode = _battleGridData.GlobalGrid.GetFirstNearestWalkableNode(nearestNode);
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

    private void SetDecal() {
        _decalProjector.material.SetTexture("_MainTex", _battleGridData.ViewTexture);
        _decalProjector.material.SetTexture("_WalkingPointsMap", _battleGridData.WalkingPointsTexture);
        _decalProjector.material.SetFloat("_TextureOffset", 0f);
        _decalProjector.material.SetFloat("_WalkPointsTextureOffset", -.0042f);
    }

    private void OnDrawGizmos() {
        if (Application.isPlaying && _isDebug) {
            for (int x = 0; x < _battleGridData.Width; x++) {
                for (int y = 0; y < _battleGridData.Height; y++) {
                    Gizmos.color = _battleGridData.WalkableMap[x, y] ? Color.green : Color.red;

                    Gizmos.DrawSphere(_battleGridData.NodesGrid[x, y].WorldPosition, .15f);
                }
            }
        }
    }
}

public class BattleGridData {
    public AStarGrid GlobalGrid;
    public Texture2D ViewTexture;
    public Texture2D WalkingPointsTexture;
    public List<CharacterWalker> Units;
    public Node[,] NodesGrid;
    public bool[,] WalkableMap;
    public LayerMask CharactersLayerMask;
    public Transform LDPoint, RUPoint;
    public int Width, Height, StartNodeIDX, StartNodeIDY;
}

public class CarveObstacleDataObject {
    public CarveObjectObstacle Obstacle;
    public Vector3 PrevPos;

    public CarveObstacleDataObject(CarveObjectObstacle obstacle, Vector3 position) {
        Obstacle = obstacle;
        PrevPos = position;
    }
}