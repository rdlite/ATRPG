using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class BattleHandler {
    private const string APPEAR_RANGE = "_AppearRoundRange";
    private const string APPEAR_CENTER_POINT_UV = "_AppearCenterPointUV";
    private bool[] _mouseOverSelectionMap;
    private AssetsContainer _assetsContainer;
    private UIRoot _uiRoot;
    private DecalProjector _decalProjector;
    private BattleGridData _battleGridData;
    private BattleRaycaster _battleRaycaster;
    private CharacterWalker _currentSelectedCharacterWalker;
    private GameObject _createdCharacterSelection;
    private LineRenderer _movementLinePrefab;
    private LineRenderer _createdLinePrefab;
    private float _currentAppearRange = 0f;
    private bool _isCurrentlyShowWalkingDistance;

    public void Init(
        CameraSimpleFollower cameraFollower, BattleGridData battleGridData, DecalProjector decalProjector,
        UIRoot uiRoot, AssetsContainer assetsContainer, LineRenderer movementLinePrefab) {
        _movementLinePrefab = movementLinePrefab;
        _assetsContainer = assetsContainer;
        _uiRoot = uiRoot;
        _decalProjector = decalProjector;
        _battleGridData = battleGridData;
        _mouseOverSelectionMap = new bool[_battleGridData.Units.Count];
        _battleRaycaster = new BattleRaycaster(_battleGridData.CharactersLayerMask, cameraFollower, _battleGridData.GroundLayerMask);
    }

    public void Tick() {
        CharacterWalker currentMouseOverSelectionUnit = _battleRaycaster.GetCurrentMouseOverSelectionUnit();
        for (int i = 0; i < _battleGridData.Units.Count; i++) {
            if (_battleGridData.Units[i] == currentMouseOverSelectionUnit && !_mouseOverSelectionMap[i]) {
                _mouseOverSelectionMap[i] = true;
                _battleGridData.Units[i].SetActiveOutline(true);
            } else if (_battleGridData.Units[i] != currentMouseOverSelectionUnit && _mouseOverSelectionMap[i]) {
                if (!(_currentSelectedCharacterWalker != null && _currentSelectedCharacterWalker == _battleGridData.Units[i])) {
                    _mouseOverSelectionMap[i] = false;
                    _battleGridData.Units[i].SetActiveOutline(false);
                }
            }
        }

        if (Input.GetMouseButtonDown(0) && currentMouseOverSelectionUnit != null) {
            if (currentMouseOverSelectionUnit != _currentSelectedCharacterWalker) {
                SetCharacterSelect(currentMouseOverSelectionUnit);
            }
        }

        if (_currentSelectedCharacterWalker != null) {
            if (_currentAppearRange <= 1f) {
                _currentAppearRange += Time.deltaTime;
                _decalProjector.material.SetFloat(APPEAR_RANGE, _currentAppearRange);
            }
        }

        if (_isCurrentlyShowWalkingDistance) {
            DrawWalkLine();
        }
    }

    private void DrawWalkLine() {
        Vector3 groundPoint = _battleRaycaster.GetRaycastPoint();

        Node startNode = _battleGridData.GlobalGrid.GetNodeFromWorldPoint(_currentSelectedCharacterWalker.transform.position);
        Node endNode = _battleGridData.GlobalGrid.GetFirstNearestWalkableNode(_battleGridData.GlobalGrid.GetNodeFromWorldPoint(groundPoint));

        Vector3[] path = _battleGridData.GlobalGrid.GetPathPoints(startNode, endNode);
        if (_createdLinePrefab != null) {
            Object.Destroy(_createdLinePrefab.gameObject);
        }

        if (path.Length != 0) {
            _createdLinePrefab = Object.Instantiate(_movementLinePrefab);
            _createdLinePrefab.positionCount = path.Length;

            for (int i = 0; i < path.Length; i++) {
                _createdLinePrefab.SetPosition(i, path[i] + Vector3.up * .1f);
            }
        }
    }

    private void SetCharacterSelect(CharacterWalker character) {
        _isCurrentlyShowWalkingDistance = false;
        CreateCharacterSelection(character);

        if (_createdLinePrefab != null) {
            Object.Destroy(_createdLinePrefab.gameObject);
        }

        _decalProjector.gameObject.SetActive(false);

        _currentSelectedCharacterWalker?.SetActiveOutline(false);
        _currentSelectedCharacterWalker = character;
        _currentSelectedCharacterWalker.SetActiveOutline(true);

        _uiRoot.GetPanel<BattlePanel>().DisableUnitsPanel(this);

        if (_currentSelectedCharacterWalker is PlayerCharacterWalker) {
            _uiRoot.GetPanel<BattlePanel>().EnableUnitPanel(this);
        }
    }

    public void SwitchWalking() {
        _isCurrentlyShowWalkingDistance = !_isCurrentlyShowWalkingDistance;

        if (!_isCurrentlyShowWalkingDistance) {
            HideWalkingDistance();
        }
    }

    public void ShowWalkingDistance() {
        ShowUnitWalkingDistance();
    }

    public void WalkingPointerExit() {
        if (!_isCurrentlyShowWalkingDistance) {
            HideWalkingDistance();
        }
    }

    public void HideWalkingDistance() {
        _decalProjector.gameObject.SetActive(false);
    }

    private void ShowUnitWalkingDistance() {
        _decalProjector.gameObject.SetActive(true);
        _currentAppearRange = 0f;
        _decalProjector.material.SetFloat(APPEAR_RANGE, 0f);

        Vector3 currentUnitPosition = _currentSelectedCharacterWalker.transform.position;

        Node startNode = _battleGridData.GlobalGrid.GetNodeFromWorldPoint(currentUnitPosition);

        List<Node> possibleNodes = new List<Node>(25);
        Node[] neighbours;
        List<Node> resultNodes = new List<Node>(25);

        possibleNodes.Add(_battleGridData.GlobalGrid.GetNodeFromWorldPoint(currentUnitPosition));

        int unitMaxWalkDistance = 20;//_unitsData[_currentUnit].WalkRange;
        int crushProtection = 0;

        for (int x = 0; x < _battleGridData.Width; x++) {
            for (int y = 0; y < _battleGridData.Height; y++) {
                _battleGridData.WalkableMap[x, y] = false;
            }
        }
        
        for (int i = 0; i < _battleGridData.Units.Count; i++) {
            Node unitNode = _battleGridData.GlobalGrid.GetNodeFromWorldPoint(_battleGridData.Units[i].transform.position);

            if (unitNode == startNode) {
                unitNode.SetPlacedByCharacter(false);
            } else {
                unitNode.SetPlacedByCharacter(true);
            }
        }

        while (possibleNodes.Count > 0) {
            crushProtection++;

            if (crushProtection > 100000) {
                Debug.Log("CRUSHED, DOLBOEB!!!");
                break;
            }

            neighbours = _battleGridData.GlobalGrid.GetNeighbours(possibleNodes[0], true).ToArray();
            possibleNodes.RemoveAt(0);

            foreach (Node neighbour in neighbours) {
                if (!resultNodes.Contains(neighbour) &&
                    neighbour.CheckWalkability &&
                    (
                        neighbour.GridX >= _battleGridData.StartNodeIDX &&
                        neighbour.GridX < _battleGridData.StartNodeIDX + _battleGridData.Width) &&
                        (neighbour.GridY >= _battleGridData.StartNodeIDY &&
                        neighbour.GridY < _battleGridData.StartNodeIDY + _battleGridData.Height)) {
                    if (unitMaxWalkDistance >= Mathf.CeilToInt(_battleGridData.GlobalGrid.GetPathLength(startNode, neighbour))) {
                        resultNodes.Add(neighbour);
                        possibleNodes.Add(neighbour);
                        _battleGridData.NodesGrid[neighbour.GridX - _battleGridData.StartNodeIDX, neighbour.GridY - _battleGridData.StartNodeIDY].SetPlacedByCharacter(false);
                        _battleGridData.WalkableMap[neighbour.GridX - _battleGridData.StartNodeIDX, neighbour.GridY - _battleGridData.StartNodeIDY] = true;
                    }
                }
            }
        }

        for (int x = 0; x < _battleGridData.Width; x++) {
            for (int y = 0; y < _battleGridData.Height; y++) {
                _battleGridData.NodesGrid[x, y].SetPlacedByCharacter(!_battleGridData.WalkableMap[x, y]);
            }
        }

        Vector3 minPoint = _battleGridData.LDPoint.position;
        Vector3 maxPoint = _battleGridData.RUPoint.position;

        float xLerpPoint = Mathf.InverseLerp(minPoint.x, maxPoint.x, currentUnitPosition.x);
        float zLerpPoint = Mathf.InverseLerp(minPoint.z, maxPoint.z, currentUnitPosition.z);

        _decalProjector.material.SetVector(APPEAR_CENTER_POINT_UV, new Vector2(xLerpPoint, zLerpPoint));

        ShowView();
    }

    private void ShowView() {
        Color blackCol = Color.black;
        Color whiteCol = Color.white;

        for (int x = 0; x < _battleGridData.Width; x++) {
            for (int y = 0; y < _battleGridData.Height; y++) {
                //_battleGridData.ViewTexture.SetPixel(x, y, _battleGridData.WalkableMap[x, y] ? whiteCol : blackCol);
                _battleGridData.ViewTexture.SetPixel(x * _battleGridData.ViewTextureResolution, y * _battleGridData.ViewTextureResolution, _battleGridData.WalkableMap[x, y] ? whiteCol : blackCol);
                _battleGridData.ViewTexture.SetPixel(x * _battleGridData.ViewTextureResolution + 1, y * _battleGridData.ViewTextureResolution, _battleGridData.WalkableMap[x, y] ? whiteCol : blackCol);
                _battleGridData.ViewTexture.SetPixel(x * _battleGridData.ViewTextureResolution, y * _battleGridData.ViewTextureResolution + 1, _battleGridData.WalkableMap[x, y] ? whiteCol : blackCol);
                _battleGridData.ViewTexture.SetPixel(x * _battleGridData.ViewTextureResolution + 1, y * _battleGridData.ViewTextureResolution + 1, _battleGridData.WalkableMap[x, y] ? whiteCol : blackCol);
            }
        }

        _battleGridData.ViewTexture.Apply();
        _battleGridData.WalkingPointsTexture.Apply();

        SetDecal();
    }

    private void SetDecal() {
        _decalProjector.material.SetTexture("_MainTex", _battleGridData.ViewTexture);
        _decalProjector.material.SetTexture("_WalkingPointsMap", _battleGridData.WalkingPointsTexture);
        _decalProjector.material.SetFloat("_TextureOffset", 0f);
        _decalProjector.material.SetFloat("_WalkPointsTextureOffset", -.0042f);
    }

    private void CreateCharacterSelection(CharacterWalker characterWalker) {
        if (_createdCharacterSelection != null) {
            Object.Destroy(_createdCharacterSelection);
        }

        _createdCharacterSelection = Object.Instantiate(_assetsContainer.BattleOverCharacterSelectionPrefab);
        _createdCharacterSelection.transform.position = characterWalker.GetOverCharacterPoint();
        _createdCharacterSelection.transform.SetParent(characterWalker.transform);
    }
}