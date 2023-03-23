using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;

namespace Tamu.Tvd
{
	// ============================================================================================
	// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
	// ============================================================================================
	/**
	 *  Manage play state and update UI to reflect state.
	 */
	// ============================================================================================
	// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
	// ============================================================================================
	[RequireComponent(typeof(Button))]
	public class PlayButton : MonoBehaviour
	{
		// Fields =================================================================================
		private Button _button;
		[SerializeField] private PhysicsActivator _physics;

		[Header("Text")]
		[SerializeField] private TMPro.TextMeshProUGUI _textUI;
		[SerializeField] private string _startText;
		[SerializeField] private string _stopText;

		[Header("State")]
		[SerializeField] private BoolReactiveProperty _playing = new BoolReactiveProperty(false);
		public BoolReactiveProperty IsPlaying => _playing;

		// ========================================================================================

		// Mono ===================================================================================
		private void Awake()
		{
			_button = this.GetComponent<Button>();
		}
		void Start()
		{
			_playing.Subscribe(b =>
			{
				_textUI.text = _playing.Value ? _stopText : _startText;
			})
			.AddTo(this);

			if (_physics != null)
			{
				_physics.IsPhysicsOn.Subscribe(b =>
				{
					if (b != _playing.Value)
						_button.onClick.Invoke();
				})
				.AddTo(this);

				_playing.Subscribe(b =>
				{
					if (b != _physics.IsPhysicsOn.Value)
						_physics.IsPhysicsOn.Value = b;
				})
				.AddTo(_physics);
			}
        }
        // ========================================================================================

        // Methods ================================================================================
        public void TogglePlayState()
        {
			_playing.Value = !_playing.Value;
        }

		public async void DisableFor(int milliseconds)
        {
			milliseconds = Math.Max(0, milliseconds);

			_button.interactable = false;
			await Cysharp.Threading.Tasks.UniTask.Delay(milliseconds);
			_button.interactable = true;
        }
		// ========================================================================================
		
	}
	// ============================================================================================
	// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
	// ============================================================================================
}