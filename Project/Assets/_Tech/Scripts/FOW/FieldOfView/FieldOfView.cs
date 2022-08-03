using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class FieldOfView : MonoBehaviour {
	public event Action<Transform> OnTargetFound;

	[Range(0, 360)][SerializeField] private float _viewAngle;

	[SerializeField] private MeshFilter _viewMeshFilter;
	[SerializeField] private LayerMask _targetMask;
	[SerializeField] private LayerMask _obstacleMask;
	[SerializeField] private float _meshResolution;
	[SerializeField] private int _edgeResolveIterations;
	[SerializeField] private float _edgeDstThreshold;
	[SerializeField] private float _maskCutawayDst = .1f;

	private float _viewRadius;
	private Mesh _viewMesh;

	public void Init(float radius) {
		SetViewRadius(radius);

		_viewMesh = new Mesh();
		_viewMesh.name = "View Mesh";
		_viewMeshFilter.mesh = _viewMesh;

		//StartCoroutine(FindTargetsWithDelay(.2f));

		//OnTargetFound += (t) => Debug.Log("found");
	}

	public void SetViewRadius(float value) {
		_viewRadius = value;
	}

	//private IEnumerator FindTargetsWithDelay(float delay)
	//{
	//	while (true)
	//	{
	//		yield return new WaitForSeconds(delay);
	//		FindVisibleTargets();
	//	}
	//}

	private void LateUpdate() {
		DrawFieldOfView();
	}

	private void FindVisibleTargets() {
		Collider[] targetsInViewRadius = Physics.OverlapSphere(transform.position, _viewRadius, _targetMask);

		for (int i = 0; i < targetsInViewRadius.Length; i++) {
			Transform target = targetsInViewRadius[i].transform;
			Vector3 dirToTarget = (target.position - transform.position).normalized;
			if (Vector3.Angle(transform.forward, dirToTarget) < _viewAngle / 2) {
				float dstToTarget = Vector3.Distance(transform.position, target.position);

				if (!Physics.Raycast(transform.position, dirToTarget, dstToTarget, _obstacleMask)) {
					OnTargetFound?.Invoke(target);
				}
			}
		}
	}

	private void DrawFieldOfView() {
		int stepCount = Mathf.RoundToInt(_viewAngle * _meshResolution);
		float stepAngleSize = _viewAngle / stepCount;
		List<Vector3> viewPoints = new List<Vector3>();
		ViewCastInfo oldViewCast = new ViewCastInfo();
		for (int i = 0; i <= stepCount; i++) {
			float angle = transform.eulerAngles.y - _viewAngle / 2 + stepAngleSize * i;
			ViewCastInfo newViewCast = ViewCast(angle);

			if (i > 0) {
				bool edgeDstThresholdExceeded = Mathf.Abs(oldViewCast.dst - newViewCast.dst) > _edgeDstThreshold;
				if (oldViewCast.hit != newViewCast.hit || (oldViewCast.hit && newViewCast.hit && edgeDstThresholdExceeded)) {
					EdgeInfo edge = FindEdge(oldViewCast, newViewCast);
					if (edge.pointA != Vector3.zero) {
						viewPoints.Add(edge.pointA);
					}
					if (edge.pointB != Vector3.zero) {
						viewPoints.Add(edge.pointB);
					}
				}

			}

			viewPoints.Add(newViewCast.point);
			oldViewCast = newViewCast;
		}

		int vertexCount = viewPoints.Count + 1;
		Vector3[] vertices = new Vector3[vertexCount];
		int[] triangles = new int[(vertexCount - 2) * 3];

		vertices[0] = Vector3.zero;
		for (int i = 0; i < vertexCount - 1; i++) {
			vertices[i + 1] = transform.InverseTransformPoint(viewPoints[i]) + Vector3.forward * _maskCutawayDst;

			if (i < vertexCount - 2) {
				triangles[i * 3] = 0;
				triangles[i * 3 + 1] = i + 1;
				triangles[i * 3 + 2] = i + 2;
			}
		}

		_viewMesh.Clear();

		_viewMesh.vertices = vertices;
		_viewMesh.triangles = triangles;
		_viewMesh.RecalculateNormals();
	}


	private EdgeInfo FindEdge(ViewCastInfo minViewCast, ViewCastInfo maxViewCast) {
		float minAngle = minViewCast.angle;
		float maxAngle = maxViewCast.angle;
		Vector3 minPoint = Vector3.zero;
		Vector3 maxPoint = Vector3.zero;

		for (int i = 0; i < _edgeResolveIterations; i++) {
			float angle = (minAngle + maxAngle) / 2;
			ViewCastInfo newViewCast = ViewCast(angle);

			bool edgeDstThresholdExceeded = Mathf.Abs(minViewCast.dst - newViewCast.dst) > _edgeDstThreshold;
			if (newViewCast.hit == minViewCast.hit && !edgeDstThresholdExceeded) {
				minAngle = angle;
				minPoint = newViewCast.point;
			} else {
				maxAngle = angle;
				maxPoint = newViewCast.point;
			}
		}

		return new EdgeInfo(minPoint, maxPoint);
	}


	private ViewCastInfo ViewCast(float globalAngle) {
		Vector3 dir = DirFromAngle(globalAngle, true);
		RaycastHit hit;

		if (Physics.Raycast(transform.position, dir, out hit, _viewRadius, _obstacleMask)) {
			return new ViewCastInfo(true, hit.point, hit.distance, globalAngle);
		} else {
			return new ViewCastInfo(false, transform.position + dir * _viewRadius, _viewRadius, globalAngle);
		}
	}

	public Vector3 DirFromAngle(float angleInDegrees, bool angleIsGlobal) {
		if (!angleIsGlobal) {
			angleInDegrees += transform.eulerAngles.y;
		}

		return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
	}

	public float GetViewRadius() {
		return _viewRadius;
	}

	public float GetViewAngle() {
		return _viewAngle;
	}

	private void OnDrawGizmosSelected() {
		Gizmos.color = Color.white;

		int segments = 32;

		DrawCircleAround(segments, _viewRadius);
	}

	private void DrawCircleAround(int segments, float radius) {
		for (int i = 0; i < segments; i++) {
			float prev_t = (i - 1) / (float)segments;
			float t = i / (float)segments;
			float prev_rad = t * Mathf.PI * 2f;
			float rad = prev_t * Mathf.PI * 2f;

			Vector3 prevPoint = transform.position + new Vector3(Mathf.Cos(prev_rad), 0f, Mathf.Sin(prev_rad)) * radius;
			Vector3 nextPoint = transform.position + new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)) * radius;

			Gizmos.DrawLine(prevPoint, nextPoint);
		}
	}

	public struct ViewCastInfo {
		public bool hit;
		public Vector3 point;
		public float dst;
		public float angle;

		public ViewCastInfo(bool _hit, Vector3 _point, float _dst, float _angle) {
			hit = _hit;
			point = _point;
			dst = _dst;
			angle = _angle;
		}
	}

	public struct EdgeInfo {
		public Vector3 pointA;
		public Vector3 pointB;

		public EdgeInfo(Vector3 _pointA, Vector3 _pointB) {
			pointA = _pointA;
			pointB = _pointB;
		}
	}
}