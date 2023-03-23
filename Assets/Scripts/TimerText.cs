using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
#pragma warning disable 0649    // Variable declared but never assigned to

namespace Tamu.Tvd
{
	// ============================================================================================
	// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
	// ============================================================================================
	/**
	 *  Update a text field according to an internal timer.
	 *  
	 *  NOTE: This is not how the master player's timer actually updates state. The master client
	 *  timer is externally set in SimFeedbackUI to allow for networking propogation.
	 *  TODO: The above is really convoluted, should should find a better means of replication.
	 */
	// ============================================================================================
	// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
	// ============================================================================================
	[RequireComponent(typeof(MenuActivator))]
	public class TimerText : MonoBehaviour
	{
		// Fields =================================================================================
		private MenuActivator _activator;

		[SerializeField] private TextMeshProUGUI _text;
		[SerializeField] private Image _fillImage;
		public Image FillImage => _fillImage;
		private float _startingTime;

		private float _timer;
		public float Time
        {
			get => _timer;
			set { _timer = value; _text.text = ((int)value).ToString(); }
        }

		private bool _isTiming;
		public bool IsActive => _isTiming;
		// ========================================================================================

		// Mono ===================================================================================
		void Awake ()
		{
            if (_text == null)
                Debug.LogError("TimerText is missing its reference to a Text component.", this);

			_activator = this.GetComponent<MenuActivator>();
		}
		// ------------------------------------------------------------------------------
		void Update ()
		{
			if (_isTiming)
            {
				this.Time = Math.Max(0, _timer - UnityEngine.Time.deltaTime);
				if (_fillImage != null)
					_fillImage.fillAmount = this.Time / _startingTime;
            }
		}
		// ========================================================================================

		// Methods ================================================================================
		public TimerText StartTimer(float time)
        {
			_isTiming = true;
			this.Time = time;
			_startingTime = time;

			return this;
        }
		public TimerText StopTimer()
        {
			_isTiming = false;
			return this;
        }

		public void Show() => _activator.Activate(false);
		public void Hide() => _activator.Deactivate();
		// ========================================================================================
		
	}
	// ============================================================================================
	// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
	// ============================================================================================
}