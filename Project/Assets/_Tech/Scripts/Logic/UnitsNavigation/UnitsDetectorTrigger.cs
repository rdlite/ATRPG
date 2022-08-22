using System;
using UnityEngine;

public class UnitsDetectorTrigger : MonoBehaviour {
    public Action<UnitBase> OnCharacterEnterToTrigger;
    public Action<UnitBase> OnCharacterExitedGromTrigger;

    private void OnTriggerEnter(Collider other) {
        if (other.GetComponent<UnitBase>()) {
            OnCharacterEnterToTrigger?.Invoke(other.GetComponent<UnitBase>());
        }
    }

    private void OnTriggerExit(Collider other) {
        if (other.GetComponent<UnitBase>()) {
            OnCharacterExitedGromTrigger?.Invoke(other.GetComponent<UnitBase>());
        }
    }
}