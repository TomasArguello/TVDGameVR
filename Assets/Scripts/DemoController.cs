using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UniRx;
using UniRx.Triggers;
using ControlMode = Tamu.Tvd.ControlModeManager.ControlMode;
#pragma warning disable 0649    // Variable declared but never assigned to

namespace Tamu.Tvd
{
	// ============================================================================================
	// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
	// ============================================================================================
	/**
	 *  Drive player state to determine when each aspect of user control modes are available.
	 *  
	 *  Most functionality here is split across distinct modes/components such that control modes
	 *  don't overlap with each other in usage.
	 *  
	 *  Intended for use with a fixed* camera view to allow most actions to be completely mouse-
	 *  driven rather than relying on user coordination in 3D space.
	 *  (*fixed as in constrained position/orientation, as opposed to freeform)
	 */
	// ============================================================================================
	// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
	// ============================================================================================
	public class DemoController : UserControlDriver
	{
		// Fields =================================================================================
		public static DemoController LocalInstance =>
			UserControlDriver.LocalPlayerInstance as DemoController;

		// UserControlDriver properties -------------------
		private Camera _camera;
		public override Camera Camera => _camera;
		public override SelectionMenu ObjectMenu { get; set; }
		public override RotationSettings RotationSettings { get; set; }
		public override PlayButton PlayButton { get; set; }
		// ------------------------------------------------
		
		// Injected UI properties -------------------------
		public SelectionMenu ToolMenu { get; set; }
		public CameraSettings CameraSettings { get; set; }

		private CompositeDisposable _selectorStreams;
		private SelectorActions _selectorActions;
		public SelectorActions SelectorActions
        {
			get => _selectorActions;
			set
            {
				_selectorActions = value;
				if (_selectorStreams != null)
					_selectorStreams.Dispose();
				_selectorStreams = new CompositeDisposable();

				_selectorActions.GrabButton.onClick.AsObservable()
					.Subscribe(_ => _selector.GrabSelectedObject())
					.AddTo(_selectorStreams);

				_selectorActions.RemoveButton.onClick.AsObservable()
					.Subscribe(_ => _selector.DeleteSelectedObject())
					.AddTo(_selectorStreams);

				_selector.ObserveEveryValueChanged(s => s.SelectedObject)
					.Subscribe(g =>
					{
						if (_selector.IsActive)
							_selectorActions.Show(g != null);
						else
							_selectorActions.Hide();
					})
					.AddTo(_selectorStreams);
            }
        }
		// ------------------------------------------------

		// Managed components -----------------------------
		private PlacementCaster _placer;
		public PlacementCaster PlacementCaster => _placer;
		private bool _placerHidesPreview;

		private SelectionCaster _selector;
		private SelectionCaster SelectCaster => _selector;

		private GrabCaster _grabber;
		public GrabCaster GrabCaster => _grabber;

		private RemovalCaster _remover;
		public RemovalCaster RemovalCaster => _remover;
		// ------------------------------------------------

		//private SelectionPreview _previewUI;

		// ========================================================================================

		// Mono ===================================================================================
		// Gather references to managed components and objects --------------------------
		new void Awake ()
		{
			base.Awake();
			bool isMine = this.photonView.IsMine;

			_camera = this.GetComponentInChildren<Camera>();
			this.InitCameraSettings(_camera);

   //         _placer = this.GetComponentInChildren<PlacementCaster>();
   //         if (_placer == null)
   //             Debug.LogError("DemoController is unable to find a PlacementCaster component in children.", this);
   //         _placerHidesPreview = _placer.HidePreviewInCursorMode;
   //         _placer.enabled = isMine;

   //         _selector = this.GetComponentInChildren<SelectionCaster>();
   //         if (_selector == null)
   //             Debug.LogError("DemoController is unable to find a SelectionCaster component in children.", this);
   //         _selector.enabled = isMine;

   //         _grabber = this.GetComponentInChildren<GrabCaster>();
			//if (_grabber == null)
			//	Debug.LogError("DemoController is unable to find a GrabCaster component in children.", this);
			//_grabber.enabled = isMine;

			//_remover = this.GetComponentInChildren<RemovalCaster>();
			//if (_remover == null)
			//	Debug.LogError("PlayerController is unable to find a RemovalCaster component in children.", this);
			//_remover.enabled = isMine;
		}
		// ------------------------------------------------------------------------------
		// Initialize interactions with managed objects ---------------------------------
		void Start()
		{
			if ((!this.photonView.IsMine && PhotonNetwork.IsConnected)
				|| this != LocalPlayerInstance)
				return;

			//_mode.RegisterAllModes(
			//	this.gameObject,
			//	(ControlMode m) =>
			//	{
			//		Cursor.visible = m != ControlMode.Placement;
			//		// TODO: Set cursors for different control modes

			//		//_placer.IsActive = m == ControlMode.Placement;
			//		//_selector.IsActive = m == ControlMode.Selection
			//		//	|| (m == ControlMode.InMenus && (ControlMode)this.ToolMenu.SelectedIndex == ControlMode.Selection);
			//		//_grabber.IsActive = m == ControlMode.Arrangement;
			//		//_remover.IsActive = m == ControlMode.Removal;

			//		if (m != ControlMode.InMenus)
   //                 {
			//			this.RotationSettings.Hide();
			//			this.SelectorActions.Hide();
   //                 }

			//		if (m == ControlMode.Placement)
   //                 {
			//			this.RotationSettings.Show();
			//			//if (this.ToolMenu.SelectedIndex != (int)ControlMode.Placement)
			//			//	this.ToolMenu.SelectObject((int)ControlMode.Placement);
			//			this.SelectorActions.Hide();
   //                 }
			//		else if (m == ControlMode.Selection)
			//			this.SelectorActions.Show(_selector.SelectedObject != null);
   //             });

			//this.PlayButton.IsPlaying.Subscribe(b =>
			//{
			//	if (b)
   //             {
			//		this.SuspendMode();
			//		this.ToolMenu.PlayerHide();
			//		this.RotationSettings.Hide();
			//		//this.CameraSettings.Hide();
			//		this.SelectorActions.Hide();
   //             }
			//	else
   //             {
			//		this.RestoreMode();
			//		this.ToolMenu.PlayerShow();
			//		//this.RotationSettings.Show();
			//		//this.CameraSettings.Show();
   //             }
			//})
			//.AddTo(this);

			//this.ObjectMenu.OnSelect.AddListener(this.SelectObject);
			//this.ToolMenu.OnSelect.AddListener(this.ChangeMode);

			//if (this.RotationSettings.SnapHorizontal != null)
			//	this.RotationSettings.SnapHorizontal.OnClickAsObservable()
			//		.Subscribe(_ =>
			//		{
			//			_placer.SetPlacementRotation(180, true);
			//			_placer.SetPlacementRotation(0, false);
			//		})
			//		.AddTo(this);
			//if (this.RotationSettings.SnapVertical != null)
			//	this.RotationSettings.SnapVertical.OnClickAsObservable()
			//		.Subscribe(_ => _placer.SetPlacementRotation(90, false))
			//		.AddTo(this);
		}
		// ------------------------------------------------------------------------------
		// Respond to player input ------------------------------------------------------
		void Update ()
		{
			if ((!this.photonView.IsMine && PhotonNetwork.IsConnected)
				|| this != LocalPlayerInstance)
				return;

			//switch (_mode.ActiveMode)
   //         {
			//	case ControlMode.Placement:
			//		if (!_placer.IsEditingAnchor)
			//		{
			//			// NOTE: Rely on a MousePlacer to update PlacementCaster position for us

			//			float rot = 10f * (float)Math.Round(Input.GetAxis("Mouse ScrollWheel"), 1, MidpointRounding.AwayFromZero)
			//				 * this.RotationSettings.RotationScale;
   //                     if (rot != 0 && !Input.GetButton("Fire2"))
			//			{
			//				int sign = Math.Sign(rot);
			//				int magnitude = Math.Max(1, sign * (int)(rot * this.RotationSettings.DegreesPerRotation));
			//				_placer.UpdatePlacementRotation(
			//					sign * magnitude,
			//					this.RotationSettings.RotationAxisIsY,
			//					this.RotationSettings.SnapToWorld
			//					);
			//			}

			//			if (Input.GetButtonDown("Fire1") && _placer.PlacementPrefab != null)
			//				_placer.PlaceObject();
			//		}
			//		break;

			//	case ControlMode.Selection:
			//		if (Input.GetButton("Fire1"))
			//		{
			//			_selector.SelectTargetObject();
			//		}
			//		break;

			//	case ControlMode.Arrangement:
			//		if (Input.GetButton("Fire1"))
			//		{
			//			_grabber.GrabTargetObject();
			//		}
			//		break;

			//	case ControlMode.Removal:
			//		if (Input.GetButtonDown("Fire1"))
   //                 {
			//			_remover.RemoveTargetObject();
   //                 }
			//		break;

			//	default:
			//		break;
   //         }
		}
		// ------------------------------------------------------------------------------
		// Unsubscribe from events ------------------------------------------------------
		void OnDestroy()
		{
			if (this.ObjectMenu != null)
				this.ObjectMenu.OnSelect.RemoveListener(this.SelectObject);

		}
		// ------------------------------------------------------------------------------
		// Handle anything that needs to happen before OnDestroy ------------------------
		public override void Cleanup()
		{
			//if (_remover != null && _remover.Target != null)
			//	_remover.Target.RemoveOutline(this.photonView.ViewID);
		}
		// ------------------------------------------------------------------------------
		// ========================================================================================

		// Methods ================================================================================
		public void SelectObject(Transform prefab)
		{
			//_placer.PlacementPrefab = prefab;
			//_mode.ActiveMode = ControlMode.Placement;
		}

		private void ChangeMode(Transform toolIcon)
        {
			ControlModeWrapper wrapper = toolIcon.GetComponent<ControlModeWrapper>();
			//if (wrapper != null)
			//	_mode.ActiveMode = wrapper.Mode;
        }

		public void InterruptMode(bool showPreviewObject = false)
		{
			//_placer.HidePreviewInCursorMode = !showPreviewObject;
			//_mode.SetMode(ControlMode.InMenus);
		}
		public void SuspendMode()
		{
			//_placer.HidePreviewInCursorMode = true;
			//_mode.SetMode(ControlMode.NONE);
		}
		public void RestoreMode()
		{
			if (!this.PlayButton.IsPlaying.Value)
            {
				//_placer.HidePreviewInCursorMode = _placerHidesPreview;
				//_mode.SetMode((ControlMode)this.ToolMenu.SelectedIndex);
            }
		}
		// ========================================================================================

	}
	// ============================================================================================
	// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
	// ============================================================================================
}