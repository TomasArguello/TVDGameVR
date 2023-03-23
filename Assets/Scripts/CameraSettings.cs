using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UniRx;
using UniRx.Triggers;
#pragma warning disable 0649    // Variable declared but never assigned to

namespace Tamu.Tvd
{
	// ============================================================================================
	// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
	// ============================================================================================
	/**
	 *  Wrapper for button-hold event registration for buttons associated with the rotation and
	 *  zooming of the camera.
	 */
	// ============================================================================================
	// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
	// ============================================================================================
	[RequireComponent(typeof(MenuActivator))]
	public class CameraSettings : MonoBehaviour
	{
		// Fields =================================================================================
		private MenuActivator _activator;

		[SerializeField] private Button _rotateLeft;
		[SerializeField] private Button _rotateRight;
		private IntReactiveProperty _rotation = new IntReactiveProperty(0);

		[SerializeField] private Button _zoomIn;
		[SerializeField] private Button _zoomOut;
		private IntReactiveProperty _zoom = new IntReactiveProperty(0);

		[SerializeField] private Slider _zoomScale;
		public float ZoomScale => Math.Max(0.01f, _zoomScale.value);
		// ========================================================================================

		// Mono ===================================================================================
		private void Awake()
        {
			_activator = this.GetComponent<MenuActivator>();

			CompositeDisposable disposables = new CompositeDisposable();

			_rotateLeft.OnPointerDownAsObservable().Subscribe(_ => _rotation.Value = 1).AddTo(disposables);
			_rotateRight.OnPointerDownAsObservable().Subscribe(_ => _rotation.Value = -1).AddTo(disposables);
			_rotateLeft.OnPointerUpAsObservable().Subscribe(_ => _rotation.Value = 0).AddTo(disposables);
			_rotateRight.OnPointerUpAsObservable().Subscribe(_ => _rotation.Value = 0).AddTo(disposables);

			_zoomIn.OnPointerDownAsObservable().Subscribe(_ => _zoom.Value = 1).AddTo(disposables);
			_zoomOut.OnPointerDownAsObservable().Subscribe(_ => _zoom.Value = -1).AddTo(disposables);
			_zoomIn.OnPointerUpAsObservable().Subscribe(_ => _zoom.Value = 0).AddTo(disposables);
			_zoomOut.OnPointerUpAsObservable().Subscribe(_ => _zoom.Value = 0).AddTo(disposables);

			disposables.AddTo(this);

			// TODO: Load/save zoomscale from PayerPrefs
		}
		// ========================================================================================

		// Methods ================================================================================
		public void Show() => _activator.Activate();
		public void Hide() => _activator.Deactivate();

        public void RegisterRotation(GameObject listener, Action<int> callback)
        {
			_rotation.Subscribe(r => callback(r)).AddTo(listener);
		}
		public void RegisterZoom(GameObject listener, Action<int> callback)
		{
			_zoom.Subscribe(z => callback(z)).AddTo(listener);
		}
		// ========================================================================================
	}
	// ============================================================================================
	// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
	// ============================================================================================
}