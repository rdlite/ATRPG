using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class BattleHandler {
    private const string APPEAR_RANGE = "_AppearRoundRange";
    private const string APPEAR_CENTER_POINT_UV = "_AppearCenterPointUV";
    private UIRoot _uiRoot;
    private DecalProjector _decalProjector;
    private BattleGridData _battleGridData;
    private BattleRaycaster _battleRaycaster;
    private bool[] _mouseOverSelectionMap;
    private CharacterWalker _currentSelectedCharacterWalker;
    private float _currentAppearRange = 0f;

    public void Init(
        CameraSimpleFollower cameraFollower, BattleGridData battleGridData, DecalProjector decalProjector,
        UIRoot uiRoot) {
        _uiRoot = uiRoot;
        _decalProjector = decalProjector;
        _battleGridData = battleGridData;
        _mouseOverSelectionMap = new bool[_battleGridData.Units.Count];
        _battleRaycaster = new BattleRaycaster(_battleGridData.CharactersLayerMask, cameraFollower);
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
    }

    private void SetCharacterSelect(CharacterWalker character) {
        _decalProjector.gameObject.SetActive(false);

        _currentSelectedCharacterWalker?.SetActiveOutline(false);
        _currentSelectedCharacterWalker = character;
        _currentSelectedCharacterWalker.SetActiveOutline(true);

        if (_currentSelectedCharacterWalker is PlayerCharacterWalker) {
            _uiRoot.GetPanel<BattlePanel>().EnableUnitPanel(this);
        } else {
            _uiRoot.GetPanel<BattlePanel>().DisableUnitsPanel();
        }
    }

    public void SwitchWalkableViewForCurrentUnit() {
        if (_currentSelectedCharacterWalker != null) {
            ShowUnitWalkingDistance();
        }
    }

    private void ShowUnitWalkingDistance() {
        _decalProjector.gameObject.SetActive(true);
        _currentAppearRange = 0f;
        _decalProjector.material.SetFloat(APPEAR_RANGE, 0f);

        Vector3 currentUnityPosition = _currentSelectedCharacterWalker.transform.position;

        Node startNode = _battleGridData.GlobalGrid.GetNodeFromWorldPoint(currentUnityPosition);

        List<Node> possibleNodes = new List<Node>(25);
        Node[] neighbours;
        List<Node> resultNodes = new List<Node>(25);

        possibleNodes.Add(_battleGridData.GlobalGrid.GetNodeFromWorldPoint(currentUnityPosition));

        int unitMaxWalkDistance = 5;//_unitsData[_currentUnit].WalkRange;
        int crushProtection = 0;

        for (int x = 0; x < _battleGridData.Width; x++) {
            for (int y = 0; y < _battleGridData.Height; y++) {
                _battleGridData.WalkableMap[x, y] = false;
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

        Vector3 minPoint = _battleGridData.LDPoint.position;
        Vector3 maxPoint = _battleGridData.RUPoint.position;

        float xLerpPoint = Mathf.InverseLerp(minPoint.x, maxPoint.x, currentUnityPosition.x);
        float zLerpPoint = Mathf.InverseLerp(minPoint.z, maxPoint.z, currentUnityPosition.z);

        Debug.Log(xLerpPoint + " " + zLerpPoint);

        _decalProjector.material.SetVector(APPEAR_CENTER_POINT_UV, new Vector2(xLerpPoint, zLerpPoint));

        ShowView();
    }

    private void ShowView() {
        Color blackCol = Color.black;
        Color whiteCol = Color.white;

        for (int x = 0; x < _battleGridData.Width; x++) {
            for (int y = 0; y < _battleGridData.Height; y++) {
                _battleGridData.ViewTexture.SetPixel(x, y, _battleGridData.WalkableMap[x, y] ? whiteCol : blackCol);
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

}