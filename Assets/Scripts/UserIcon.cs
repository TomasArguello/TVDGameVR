using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using UniRx;
#pragma warning disable 0649    // Variable declared but never assigned to

namespace Tamu.Tvd
{
	// ============================================================================================
	// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
	// ============================================================================================
	/**
	 *  This class does things...
	 */
	// ============================================================================================
	// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
	// ============================================================================================
	[RequireComponent(typeof(RectTransform))]
	public class UserIcon : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
	{
		// Fields =================================================================================
		private RectTransform _rt;
		private RectTransform _canvasRt;

		[SerializeField] private int _playerID;
		public int ID => _playerID;
		private string _playerName;
		private Color _playerColor;
		private Color _labelColor;

		[Header("Images")]
		[SerializeField] private Image _background;
		[SerializeField] private Image _image;
		[SerializeField] private Color _altColor = Color.white;
		
		private bool _isOn = false;
		public bool IsOn => _isOn;

		[Header("Label")]
		[SerializeField] private Image _label;
		private TextMeshProUGUI _text;
		public float TextMargin = 5f;
		// ========================================================================================

		// Mono ===================================================================================
		// Init -------------------------------------------------------------------------
		void Awake ()
		{
			_rt = this.GetComponent<RectTransform>();
			Canvas parentCanvas = this.GetComponentInParent<Canvas>();
			if (parentCanvas != null)
				_canvasRt = parentCanvas.GetComponent<RectTransform>();

			if (_image != null)
				_rt.ObserveEveryValueChanged(rt => rt.sizeDelta)
					.Subscribe(sd => _image.rectTransform.sizeDelta = sd)
					.AddTo(this);

			if (_label != null)
            {
				_text = _label.GetComponentInChildren<TextMeshProUGUI>();

				_rt.ObserveEveryValueChanged(rt => rt.position)
					.Subscribe(pos =>
					{
						this.UpdatePosition();
					})
					.AddTo(this);

				this.ShowLabel(false);
            }
		}
		// ------------------------------------------------------------------------------
		// ========================================================================================

		// IPointer Handlers ======================================================================
		public void OnPointerEnter(PointerEventData eventData)
		{
			this.ShowLabel(true);
		}
		public void OnPointerExit(PointerEventData eventData)
		{
			this.ShowLabel(false);
		}
		// ========================================================================================

		// Methods ================================================================================
		// Initialization ---------------------------------------------------------------
		public async void Init(UserControlDriver player)
        {
            _playerID = player.photonView.ViewID;
			_playerName = player.photonView.Owner.NickName;
			_playerColor = player.Color;

			_image.color = _altColor;
			_background.color = _playerColor;

			_text.text = _playerName;
			_text.color = _altColor;
			
			await Cysharp.Threading.Tasks.UniTask.WaitForEndOfFrame();

			_labelColor = _playerColor;
			_labelColor.a = 0.8f;
			_label.rectTransform.sizeDelta = new Vector2(
				_text.GetRenderedValues().x + this.TextMargin,
                _label.rectTransform.sizeDelta.y
                );

			await Cysharp.Threading.Tasks.UniTask.WaitForEndOfFrame();

			this.UpdatePosition();
		}
		// ------------------------------------------------------------------------------
		// State ------------------------------------------------------------------------
		public void UpdatePosition()
		{
			RectTransform lr = _label.rectTransform;
			lr.localPosition = new Vector2(0, lr.localPosition.y);

			float diff = lr.position.x - lr.sizeDelta.x / 2 < 0
			? lr.sizeDelta.x / 2 - lr.position.x
			: _canvasRt != null && lr.position.x + lr.sizeDelta.x >= _canvasRt.rect.width
			? _canvasRt.rect.width - (lr.position.x + lr.sizeDelta.x)
			: 0;

			lr.localPosition = new Vector3(
				diff,
				lr.localPosition.y,
				lr.localPosition.z
				);
		}

		public void ToggleState()
        {
			if (_isOn)
			{
				_image.color = _altColor;
				_background.color = _playerColor;
			}
			else
			{
				_image.color = _playerColor;
				_background.color = _altColor;
			}
			_isOn = !_isOn;
		}

		public async void ShowLabel(bool show)
        {
			_text.enabled = show;

			await Cysharp.Threading.Tasks.UniTask.WaitForEndOfFrame();

			_label.color = show ? _labelColor : Color.clear;
			_label.rectTransform.sizeDelta = new Vector2(
				_text.GetRenderedValues().x + this.TextMargin,
				//_label.rectTransform.sizeDelta.y
				_text.GetRenderedValues().y + this.TextMargin
				);

			await Cysharp.Threading.Tasks.UniTask.WaitForEndOfFrame();

			this.UpdatePosition();
		}
        // ------------------------------------------------------------------------------
        // ========================================================================================

    }
	// ============================================================================================
	// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
	// ============================================================================================
}