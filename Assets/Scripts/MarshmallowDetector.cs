using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Photon.Pun;
#pragma warning disable 0649    // Variable declared but never assigned to

namespace Tamu.Tvd
{
	// ============================================================================================
	// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
	// ============================================================================================
	/**
	 *  Perform fulfilment-checking for marshmallow conditions:
	 *  - Stationary, 2ft above surface
	 *  - TODO: No more than 2in out of plumb
	 */
	// ============================================================================================
	// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
	// ============================================================================================
	[RequireComponent(typeof(Rigidbody))]
	public class MarshmallowDetector : MonoBehaviourPun
	{
		// Fields =================================================================================
		private Rigidbody _rb;

		//[Header("Timer")]
		[Tooltip("Milliseconds to delay timer after start trigger.")]
		public int ValidationDelay = 500;

		[Tooltip("Seconds from which the timer counts down.")]
		[SerializeField] private float _timeToValidate;
		public float ValidationTime => _timeToValidate;
		private float _validationTimer;

		private bool _isDetecting;

		private UnityEvent<float> _timeEvent = new UnityEvent<float>();
		public UnityEvent<float> TimeEvent => _timeEvent;

		
		private UnityEvent<bool> _successEvent = new UnityEvent<bool>();
		public UnityEvent<bool> SuccessEvent => _successEvent;
		public Transform HeightMarker { get; set; }
		// ========================================================================================

		// Mono ===================================================================================
		// ------------------------------------------------------------------------------
		void Awake ()
		{
			_rb = this.GetComponent<Rigidbody>();
		}
		// ------------------------------------------------------------------------------
        private void OnValidate()
        {
			_timeToValidate = Math.Abs(_timeToValidate);
        }
        // ------------------------------------------------------------------------------
		private void Update()
        {
			if (_isDetecting)
			{
				if (this.HeightMarker != null && this.transform.position.y < this.HeightMarker.position.y)
				{
					_isDetecting = false;
					_successEvent.Invoke(false);
					return;
				}

				_validationTimer = Math.Max(_validationTimer - Time.deltaTime, 0);
				_timeEvent.Invoke(_validationTimer);
				
				if (_validationTimer == 0)
                {
					_isDetecting = false;
					_successEvent.Invoke(true);
                }
            }
		}
        // ------------------------------------------------------------------------------
        // ========================================================================================

        // Methods ================================================================================
        public async void DetectFulfilment()
        {
			if (_isDetecting)
				return;

			_validationTimer = _timeToValidate;
			await Cysharp.Threading.Tasks.UniTask.Delay(this.ValidationDelay);
			_isDetecting = true;
        }

		public void StopDetection()
        {
			_isDetecting = false;
        }
		// ========================================================================================
		
	}
	// ============================================================================================
	// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
	// ============================================================================================
}