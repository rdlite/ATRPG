using UnityEngine;

[ExecuteInEditMode]
public class EditmodeRaycaster : MonoBehaviour {
    [SerializeField] private LayerMask _mask;
    [SerializeField] private Transform _raycastPoint;

    public void Update() {
        RaycastHit hit;

        //if (_raycastPoint != null) {
        //    Physics.Raycast(_raycastPoint.position, Vector3.down, out hit, 10000f, _mask);
        //    Debug.Log(hit.transform == null);
        //}
        if (_raycastPoint != null) {
            print(Physics.OverlapSphere(_raycastPoint.position, 1f, _mask).Length);
        }
    }
}