using UnityEngine;

public class BattleHandler {
    private BattleGridData _battleGridData;
    private BattleRaycaster _battleRaycaster;
    private bool[] _mouseOverSelectionMap;

    public void Init(CameraSimpleFollower cameraFollower, BattleGridData battleGridData) {
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
                _mouseOverSelectionMap[i] = false;
                _battleGridData.Units[i].SetActiveOutline(false);
            }
        }
    }

    //private int _currentUnit;
    //private void PickUnit() {
    //    _currentUnit++;

    //    if (_currentUnit == _unitsData.Count) {
    //        _currentUnit = 0;
    //    }

    //    _circularAppearance = 0f;
    //    _decalProjector.material.SetFloat("_AppearRoundRange", 1f);
    //    //_decalProjector.material.SetFloat("_AppearRoundRange", 0f);

    //    Vector3 unitPosition = _unitsData[_currentUnit].transform.position.RemoveYCoord();
    //    Vector3 minPos = _nodesArray[0, 0].WorldPosition.RemoveYCoord();
    //    Vector3 maxPos = _nodesArray[_width - 1, _height - 1].WorldPosition.RemoveYCoord();

    //    float uvX = Mathf.InverseLerp(minPos.x, maxPos.x, unitPosition.x); 
    //    float uvY = Mathf.InverseLerp(minPos.z, maxPos.z, unitPosition.z); 

    //    _decalProjector.material.SetVector("_AppearCenterPointUV", new Vector2(uvX, uvY));

    //    ShowUnitWalkingDistance();
    //}

    //private void ShowUnitWalkingDistance() {
    //    Vector3 currentUnityPosition = _battleGridData.Units[_currentUnit].transform.position;

    //    Node startNode = _globalGrid.GetNodeFromWorldPoint(currentUnityPosition);

    //    List<Node> possibleNodes = new List<Node>(25);
    //    Node[] neighbours;
    //    List<Node> resultNodes = new List<Node>(25);

    //    possibleNodes.Add(_globalGrid.GetNodeFromWorldPoint(currentUnityPosition));

    //    int unitMaxWalkDistance = 5;//_unitsData[_currentUnit].WalkRange;
    //    int crushProtection = 0;

    //    for (int x = 0; x < _width; x++) {
    //        for (int y = 0; y < _height; y++) {
    //            _isWalkableMap[x, y] = false;
    //        }
    //    }

    //    while (possibleNodes.Count > 0) {
    //        crushProtection++;

    //        if (crushProtection > 100000) {
    //            print("CRUSHED, DOLBOEB!!!");
    //            break;
    //        }

    //        neighbours = _globalGrid.GetNeighbours(possibleNodes[0], true).ToArray();
    //        possibleNodes.RemoveAt(0);

    //        foreach (Node neighbour in neighbours) {
    //            if (!resultNodes.Contains(neighbour) &&
    //                neighbour.CheckWalkability &&
    //                (neighbour.GridX >= _startNodeIDX && neighbour.GridX < _startNodeIDX + _width) && (neighbour.GridY >= _startNodeIDY && neighbour.GridY < _startNodeIDY + _height)) {
    //                if (unitMaxWalkDistance >= Mathf.CeilToInt(_globalGrid.GetPathLength(startNode, neighbour))) {
    //                    resultNodes.Add(neighbour);
    //                    possibleNodes.Add(neighbour);
    //                    _isWalkableMap[neighbour.GridX - _startNodeIDX, neighbour.GridY - _startNodeIDY] = true;
    //                }
    //            }
    //        }
    //    }

    //    ShowView();
    //}
}