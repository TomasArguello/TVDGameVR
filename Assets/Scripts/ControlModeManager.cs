using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using UniRx.Triggers;
using UnityEngine.Events;
#pragma warning disable 0649    // Variable declared but never assigned to

namespace Tamu.Tvd
{
	// ============================================================================================
	// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
	// ============================================================================================
	/**
	 *  Manage which among a set of modes of control, defined by the ControlMode enum, is the
	 *  currently-active mode, and alert any listeners of a change in the active mode.
	 */
	// ============================================================================================
	// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
	// ============================================================================================
	[Serializable]
	public class ControlModeManager : MonoBehaviour
	{
		public enum ControlMode
        {
			Placement,
			Selection,
			Arrangement,
			Removal,
			InMenus,
			NONE	// Make sure this is always last so we can use it as a compile-time count
		}

		// Fields =================================================================================
		//private ControlMode _activeMode = ControlMode.NONE;
		private ReactiveProperty<ControlMode> _activeMode = new ReactiveProperty<ControlMode>(ControlMode.NONE);
		public ControlMode ActiveMode { get => _activeMode.Value; set => this.SetMode(value); }

		protected UnityEvent<bool>[] _modeEvents = new UnityEvent<bool>[(int)ControlMode.NONE + 1];
		protected CompositeDisposable _unregistrationSubscriptions = new CompositeDisposable();

		// ========================================================================================

		// Mono ===================================================================================
		protected void Awake()
        {
			for (int i = 0; i < _modeEvents.Length; i++)
				_modeEvents[i] = new UnityEvent<bool>();
        }
        protected void OnDestroy()
        {
			_unregistrationSubscriptions.Dispose();
        }

		// ========================================================================================

		// Methods ================================================================================
		public void RegisterForMode(ControlMode mode, GameObject listener, UnityAction<bool> callback)
        {
			UnityEvent<bool> trigger = _modeEvents[(int)mode];
			trigger.AddListener(callback);
			listener.OnDestroyAsObservable()
				.Subscribe(_ => trigger.RemoveListener(callback))
				.AddTo(_unregistrationSubscriptions);
		}
		public void RegisterAllModes(GameObject listener, UnityAction<ControlMode> callback)
		{
			_activeMode.Subscribe(cm => callback.Invoke(cm)).AddTo(listener);
		}

		public void SetMode(ControlMode mode)
        {
			_modeEvents[(int)_activeMode.Value].Invoke(false);
			
			if (mode > ControlMode.NONE || mode < 0)
				mode = ControlMode.NONE;

			_activeMode.Value = mode;
			_modeEvents[(int)mode].Invoke(true);
		}

		// ========================================================================================
	}
	// ============================================================================================
	// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
	// ============================================================================================
}