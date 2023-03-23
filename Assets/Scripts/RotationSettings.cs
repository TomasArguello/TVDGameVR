using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using UniRx;
using UniRx.Triggers;
#pragma warning disable 0649    // Variable declared but never assigned to

namespace Tamu.Tvd
{
	// ============================================================================================
	// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
	// ============================================================================================
	/**
	 *  Handle functionality for the UI that control the rotation settings, including:
	 *  - Angle selection button and popout
	 *  - Left/right rotate buttons
	 *  - Snapping toggle
	 */
	// ============================================================================================
	// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
	// ============================================================================================
	[RequireComponent(typeof(MenuActivator))]
	public class RotationSettings : MonoBehaviour
	{
		// Fields =================================================================================
		private MenuActivator _activator;

		[Header("Rotation")]
		private int _angle;
		public int DegreesPerRotation => _angle;
		public int DefaultDegreesPerRotation = 15;

		[SerializeField] private Slider _rotationScale;
		public float RotationScale => Math.Max(0.01f, _rotationScale.value);

		[SerializeField] private TextMeshProUGUI _angleText;
		[Space]
		public bool ControlsAxisToggle = false;
		public bool HoldToToggleAxis = true;
		public bool StartingAxisIsY = true;
		private bool _rotateAboutYAxis = true;
		public bool RotationAxisIsY => _rotateAboutYAxis;

		[Space]
		[SerializeField] private Button _rotateLeft;
		[SerializeField] private Button _rotateRight;
		[SerializeField] private UnityEvent<int> _onRotate = new UnityEvent<int>();
		public UnityEvent<int> OnRotate => _onRotate;

		[Header("Snapping")]
		[SerializeField] private Toggle _snapToggle;
		[SerializeField] private Image _snapGraphic;
		[SerializeField] private CanvasGroup _snapTooltip;
		private TextMeshProUGUI _snapTooltipText;
		public bool SnapToWorld => _snapToggle.isOn;
		[SerializeField] private Button _snapHorizontal;
		public Button SnapHorizontal => _snapHorizontal;
		[SerializeField] private Button _snapVertical;
		public Button SnapVertical => _snapVertical;

		[Space]
		// TODO: Replace below with better input assignment abstraction
		[SerializeField] private KeyCode _snapKey = KeyCode.X;
		[SerializeField] private KeyCode _axisKey = KeyCode.Space;
		[SerializeField] private KeyCode _horzKey = KeyCode.H;
		[SerializeField] private KeyCode _vertKey = KeyCode.V;
		// ========================================================================================

		// Mono ===================================================================================
		void Awake ()
		{
			// TODO: add null checks for all the things...

			_activator = this.GetComponent<MenuActivator>();

			_snapTooltipText = _snapTooltip.GetComponentInChildren<TextMeshProUGUI>();

			_rotateAboutYAxis = this.StartingAxisIsY;
			this.SetRotation(this.DefaultDegreesPerRotation);

			// TODO: Load/save rotationscale from PayerPrefs
		}
		// ------------------------------------------------------------------------------
		void Start ()
		{
			this.ShowTooltip(false);

			_snapToggle.OnValueChangedAsObservable().Subscribe(b =>
			{
				if (b)
				{
					_snapGraphic.color = Color.white;
					_snapTooltipText.text = "Snap to world axis";
				}
				else
				{
					_snapGraphic.color = _snapToggle.colors.disabledColor;
					_snapTooltipText.text = "Local rotation";
				}
			})
			.AddTo(this);

			_rotateLeft.OnClickAsObservable().Subscribe(_ => _onRotate.Invoke(-_angle)).AddTo(this);
			_rotateRight.OnClickAsObservable().Subscribe(_ => _onRotate.Invoke(_angle)).AddTo(this);
		}
        // ------------------------------------------------------------------------------
        void Update()
		{
			if (Input.GetKeyDown(_snapKey))
				_snapToggle.isOn = !_snapToggle.isOn;
			if (Input.GetKeyDown(_horzKey))
				_snapHorizontal.onClick.Invoke();
			if (Input.GetKeyDown(_vertKey))
				_snapVertical.onClick.Invoke();

			if (this.ControlsAxisToggle)
            {
				if (this.HoldToToggleAxis)
					_rotateAboutYAxis = Input.GetKey(_axisKey);
				else if (Input.GetKeyDown(_axisKey))
					_rotateAboutYAxis = !_rotateAboutYAxis;
            }
		}
		// ------------------------------------------------------------------------------
		// ========================================================================================

		// Methods ================================================================================
		public void Show() => _activator.Activate();
		public void Hide() => _activator.Deactivate();

		public void SetRotation(int degrees)
        {
			_angle = Math.Min(Math.Abs(degrees), 90);
			_angleText.text = $"{_angle}<sup>o</sup>";
        }

		public void ShowTooltip(bool show)
        {
			_snapTooltip.alpha = show ? 1 : 0;
        }
		// ========================================================================================
		
	}
	// ============================================================================================
	// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
	// ============================================================================================
}