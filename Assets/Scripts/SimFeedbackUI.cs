using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
#pragma warning disable 0649    // Variable declared but never assigned to

namespace Tamu.Tvd
{
	// ============================================================================================
	// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
	// ============================================================================================
	/**
	 *  Handle UI elements representing feedback from simulation fulfilment evaluation.
	 *  
	 *  TODO: Account for various failure states
	 */
	// ============================================================================================
	// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
	// ============================================================================================
	[RequireComponent(typeof(PhotonView))]
	public class SimFeedbackUI : MonoBehaviourPun
	{
		// Fields =================================================================================
		public Transform HeightIndicator;
		[Space]
		public TimerText TimerText;
		public TMPro.TextMeshProUGUI FeedbackText;
		[Space]
		[SerializeField, Multiline] private string SuccessMessage;
		[SerializeField, Multiline] private string HeightFailureMessage;
		private string _validationMessage = "Validating your tower...";

		private MarshmallowDetector _mallow;
		private int _simLayer;
		// ========================================================================================

		// Mono ===================================================================================
		void Awake ()
		{
			if (this.TimerText == null)
				Debug.LogError("SimFeedbackUI is missing a reference to a TimerText.", this);
			if (this.FeedbackText == null)
				Debug.LogError("SimFeedbackUI is missing a reference to feedback Text.", this);

			_simLayer = LayerMask.NameToLayer("Simulation Objects");
		}
		// ========================================================================================

		// Methods ================================================================================
		// Start Evaluation -------------------------------------------------------------
		public void StartEvaluation()
		{
			if (_mallow != null)
			{
				_mallow.TimeEvent.RemoveListener(this.UpdateTimer);
				_mallow.SuccessEvent.RemoveListener(this.SignalSuccess);
				_mallow.StopDetection();
			}

			_mallow = GameObject.FindObjectsOfType<MarshmallowDetector>()
				.FirstOrDefault(m => m.gameObject.layer == _simLayer);
			if (_mallow != null)
			{
				this.TimerText.Time = _mallow.ValidationTime;
				this.TimerText.FillImage.fillAmount = 1;
				this.FeedbackText.text = _validationMessage;
				this.TimerText.Show();

				_mallow.HeightMarker = this.HeightIndicator;
				_mallow.TimeEvent.AddListener(this.UpdateTimer);
				_mallow.SuccessEvent.AddListener(this.SignalSuccess);
				_mallow.DetectFulfilment();

				this.photonView.RPC("SetMallowTimer", RpcTarget.Others, _mallow.ValidationTime);
			}
		}

		private void UpdateTimer(float time)
		{
			this.TimerText.Time = time;
			this.TimerText.FillImage.fillAmount = time / _mallow.ValidationTime;
		}

		[PunRPC]
		private void SetMallowTimer(float time)
		{
			this.TimerText.StartTimer(time).Show();
			this.FeedbackText.text = _validationMessage;
		}
		// ------------------------------------------------------------------------------
		// Stop Evaluation --------------------------------------------------------------
		private void SignalSuccess(bool success)
		{
			this.TimerText.StopTimer();
			this.FeedbackText.text = success ? this.SuccessMessage : this.HeightFailureMessage;
			this.photonView.RPC("StopMallowTimer", RpcTarget.Others, success ? this.SuccessMessage : this.HeightFailureMessage);
		}
		[PunRPC]
		private void StopMallowTimer(string message)
		{
			this.TimerText.StopTimer();
			this.FeedbackText.text = message;
		}

		public void StopEvaluation()
		{
			this.TimerText.StopTimer().Hide();
			this.photonView.RPC("EndMallowTimer", RpcTarget.Others);
		}
		[PunRPC]
		private void EndMallowTimer()
		{
			this.TimerText.StopTimer().Hide();
		}
		// ------------------------------------------------------------------------------
		// ========================================================================================

	}
	// ============================================================================================
	// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
	// ============================================================================================
}