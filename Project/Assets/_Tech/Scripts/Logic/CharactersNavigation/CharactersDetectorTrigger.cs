using System;
using UnityEngine;

public class CharactersDetectorTrigger : MonoBehaviour {
    public Action<CharacterWalker> OnCharacterEnterToTrigger;
    public Action<CharacterWalker> OnCharacterExitedGromTrigger;

    private void OnTriggerEnter(Collider other) {
        if (other.GetComponent<CharacterWalker>()) {
            OnCharacterEnterToTrigger?.Invoke(other.GetComponent<CharacterWalker>());
        }
    }

    private void OnTriggerExit(Collider other) {
        if (other.GetComponent<CharacterWalker>()) {
            OnCharacterExitedGromTrigger?.Invoke(other.GetComponent<CharacterWalker>());
        }
    }
}