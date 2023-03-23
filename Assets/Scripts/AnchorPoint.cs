using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
#pragma warning disable 0649    // Variable declared but never assigned to

namespace Tamu.Tvd
{
	// ============================================================================================
	// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
	// ============================================================================================
	/**
	 * Act as a reference point to control the relative positioning of child objects, as well as
	 * maintain a "snapped" world position.
	 */
	// ============================================================================================
	// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
	// ============================================================================================
	public class AnchorPoint : MonoBehaviour
	{
		// Fields =================================================================================
		private MeshRenderer _renderer;
		public bool ShowWhenEditing = true;
		public bool IsVisible;

		[Space]
		[SerializeField] private Collider _snappedPoint;
		public Collider SnapPoint => _snappedPoint;
		public bool IsSnapped => _snappedPoint != null;


		[Tooltip("Any child objects of children of this object matching this mask will be treated as anchor point locations.")]
		[SerializeField] private LayerMask _anchorPointsMask;
		private Transform[] _anchorPoints;
		[SerializeField] Transform _nextAnchor;

		[SerializeField, Range(0.1f, 1)] private float _editingSpeed = 0.5f;
		[SerializeField, Range(0.05f, 0.1f)] private float _minimumEditingSpeed = 0.2f;
		private BoolReactiveProperty _editing = new BoolReactiveProperty();
		public BoolReactiveProperty IsEditingAnchor => _editing;

		// ========================================================================================

		// Mono ===================================================================================
		#region Mono
		// ------------------------------------------------------------------------------
		void Awake ()
		{
			_renderer = this.GetComponent<MeshRenderer>();
			if (_renderer == null)
				Debug.LogWarning("AnchorPoint has no attached MeshRenderer.", this);
			else
				this.ObserveEveryValueChanged(a => a.IsVisible)
					.Subscribe(b => _renderer.enabled = b)
					.AddTo(this);

		}
        // ------------------------------------------------------------------------------
        private void Start()
        {
			this.InitAnchorPoints();
        }
        private void InitAnchorPoints()
		{
			List<Transform> points = new List<Transform>();
			for (int i = 0; i < this.transform.childCount; i++)
			{
				Transform t = this.transform.GetChild(i);
				for (int j = 0; j < t.childCount; j++)
				{
					Transform child = t.GetChild(j);
					if (((1 << child.gameObject.layer) & _anchorPointsMask.value) == _anchorPointsMask.value)
						points.Add(child);
				}
			}
			_anchorPoints = points.ToArray();
		}
		// ------------------------------------------------------------------------------
		private void OnValidate()
        {
			_minimumEditingSpeed = Math.Min(_editingSpeed, _minimumEditingSpeed);
        }
		// ------------------------------------------------------------------------------
		void Update()
		{
			_editing.Value = Input.GetButton("Fire3") && _anchorPoints.Length > 1;
			this.IsVisible = _editing.Value && this.ShowWhenEditing;

            if (_editing.Value)
            {
                float x = Mathf.Clamp(Input.GetAxis("Mouse X"), -_editingSpeed, _editingSpeed);
                float y = Mathf.Clamp(Input.GetAxis("Mouse X"), -_editingSpeed, _editingSpeed);

				// TODO: Use ordered anchor points along path rather than just first and last
				_nextAnchor = Math.Sign(x + y) switch
				{
					1 => _anchorPoints[0],
					-1 => _anchorPoints.Last(),
					_ => null
				};
				if (_nextAnchor == null)
					return;

				Vector3 nextPos = Vector3.Lerp(this.transform.position, _nextAnchor.position, _editingSpeed);

				float delta = Mathf.Clamp((nextPos - this.transform.position).magnitude, _minimumEditingSpeed, _editingSpeed) * Math.Sign(x);
				for (int i = 0; i < this.transform.childCount; i++)
				{
                    Transform child = this.transform.GetChild(i);

					// TODO: Calculate actual axis of anchor path instead of assuming x-axis
					Vector3 next = delta * Vector3.right;
					float distToTarget = (child.position - _nextAnchor.position).magnitude;

					Vector3 pos = new Vector3(
						child.localPosition.x / child.localScale.x,
						child.localPosition.y / child.localScale.y,
						child.localPosition.z / child.localScale.z
						);

					if ((pos + next).magnitude <= distToTarget)
						child.localPosition += next;
					else
						child.localPosition = new Vector3(
							distToTarget * child.localScale.x * Math.Sign(delta),
							child.localPosition.y,
							child.localPosition.z
							);
                }
			}
		}
        // ------------------------------------------------------------------------------
        #endregion
        // ========================================================================================

        // Methods ================================================================================
        public void SnapTo(Collider snapPoint)
        {
			_snappedPoint = snapPoint;
			if (snapPoint != null)
				this.transform.position = _snappedPoint.transform.position;
        }
		public void UnSnap()
        {
			_snappedPoint = null;
        }

		public Vector3 ClampToSnapPoints(Vector3 pos)
        {
			if (_anchorPoints == null)
				this.InitAnchorPoints();

			if (_anchorPoints.Length == 0)
				return Vector3.zero;
			else if (_anchorPoints.Length == 1)
				return _anchorPoints[0].localPosition;
            
			// TODO: use ordered anchor points along path rather than just first and last
			Vector3 s1 = _anchorPoints[0].localPosition;
			Vector3 s2 = _anchorPoints[_anchorPoints.Length - 1].localPosition;

			Vector3 sDiff = s2 - s1;
			pos = Vector3.Project(pos, sDiff);

            if ((pos - s1).sqrMagnitude > sDiff.sqrMagnitude)
                pos = s2;
            else if ((pos - s2).sqrMagnitude > sDiff.sqrMagnitude)
                pos = s1;

			return pos;
		}

		public Vector3 ScaleToSnapPoints(float percentageAlongPoints)
		{
			if (_anchorPoints == null)
				this.InitAnchorPoints();

			if (_anchorPoints.Length == 0)
				return Vector3.zero;
			else if (_anchorPoints.Length == 1)
				return _anchorPoints[0].localPosition;

            // TODO: use ordered anchor points along path rather than just first and last
            Vector3 s1 = _anchorPoints[0].localPosition;
			Vector3 s2 = _anchorPoints[_anchorPoints.Length - 1].localPosition;

			return s1 + (s2 - s1) * percentageAlongPoints;
		}

		public float PercentageAlongPoints(Vector3 localPos)
		{
			if (_anchorPoints == null)
				this.InitAnchorPoints();
			if (_anchorPoints.Length < 2)
				return 0.5f;

			Vector3 anchor1 = _anchorPoints[0].localPosition;
			Vector3 anchor2 = _anchorPoints[_anchorPoints.Length - 1].localPosition;
			Vector3 diff = anchor2 - anchor1;

			//return (localPos - anchor1).sqrMagnitude / (anchor2 - anchor1).sqrMagnitude;
			return Vector3.Project(localPos - anchor1, diff.normalized).magnitude / diff.magnitude;
		}
		// ========================================================================================

	}
	// ============================================================================================
	// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
	// ============================================================================================
}