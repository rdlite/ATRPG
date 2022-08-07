using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class CharactersGroupContainer : MonoBehaviour {
    [field: SerializeField] public CharacterWalker MainCharacter { get; private set; }

    [SerializeField] private List<CharacterWalker> _charactersInGroup;

    private AStarGrid _grid;

    public void Init(
        OnFieldRaycaster fieldRaycaster, AStarGrid grid, SceneAbstractEntitiesMediator abstractEntitiesMediator) {
        _grid = grid;

        foreach (CharacterWalker character in _charactersInGroup) {
            character.Init(fieldRaycaster, abstractEntitiesMediator);
        }
    }

    public void Tick() {
        foreach (CharacterWalker character in _charactersInGroup) {
            character.Tick();
        }
    }

    public void StopCharacters() {
        foreach (CharacterWalker character in _charactersInGroup) {
            character.AbortMovement();
        }
    }

    public void SendCharactersToPoint(Vector3 worldPoint, Vector3 endPointNormal, Action<Vector3, Vector3, Transform, bool> successfulCallback) {
        MainCharacter.GoToPoint(worldPoint, false,
            (callbackData) => {
                if (callbackData.IsSuccessful) {
                    float circleRange = 4f;
                    float maxPathLength = 8f;

                    successfulCallback?.Invoke(worldPoint, endPointNormal, MainCharacter.transform, true);

                    List<Node> uniqueNodes = _grid.GetUniqueNodesInRange(circleRange, worldPoint - callbackData.EndForwardDirection * 1.5f, worldPoint, _charactersInGroup.Count - 1, maxPathLength);

                    foreach (CharacterWalker character in _charactersInGroup) {
                        if (character != MainCharacter) {
                            character.GoToPoint(uniqueNodes[0].WorldPosition + Random.insideUnitSphere * .1f, false, null);
                            successfulCallback?.Invoke(uniqueNodes[0].WorldPosition, endPointNormal, character.transform, false);
                            uniqueNodes.RemoveAt(0);
                        }
                    }
                }});
    }

    public List<CharacterWalker> GetCharacters() {
        return _charactersInGroup;
    }
}