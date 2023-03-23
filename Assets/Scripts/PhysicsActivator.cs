using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using UniRx.Triggers;
using Photon.Pun;
#pragma warning disable 0649    // Variable declared but never assigned to

namespace Tamu.Tvd
{
	// ============================================================================================
	// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
	// ============================================================================================
	/**
	 *  Fake a physics simulation by duplicating structural elements from the scene into a
	 *  simulation physics layer and turning on all their physics properties. Simply destroy the
	 *  duplicate objects and re-enable visibility on the original objects to end the simulation.
	 */
	// ============================================================================================
	// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
	// ============================================================================================
	[RequireComponent(typeof(PhotonView))]
	public class PhysicsActivator : MonoBehaviourPun
	{
		// Fields =================================================================================
		[SerializeField] private SimFeedbackUI _feedbackUI;

		[Header("Joint Settings")]
		[SerializeField] private float _breakForce = 150f;
		[SerializeField] private float _breakTorque = 150f;

		private int _physicsLayer;
		private int _snappingLayer;
		private int _physicsMask;
		private int _simulationLayer;

		private BoolReactiveProperty _isActive = new BoolReactiveProperty(false);
		public BoolReactiveProperty IsPhysicsOn => _isActive;
		private bool _wasActive = false;

		private const string IS_SIMULATING = "IsSimulating";
		private const string BUILD_IDS = "BuildIDs";
		private const string SIM_IDS = "SimIDs";
		private const string STRUCTURAL_TAG = "Structural";
		// ========================================================================================

		// Mono ===================================================================================
		void Awake ()
		{
			_physicsLayer = LayerMask.NameToLayer("Physics Objects");
			_snappingLayer = LayerMask.NameToLayer("Snapping Points");
			_physicsMask = (1 << _physicsLayer) | (1 << _snappingLayer);
			_simulationLayer = LayerMask.NameToLayer("Simulation Objects");

			_isActive.Skip(1).Subscribe(b =>
			{
				if (b && !_wasActive)
					this.TurnOnNetworkedPhysics();
				else if (!b && _wasActive)
					this.TurnOffNetworkedPhysics();

				_wasActive = b;
			})
			.AddTo(this);
		}
		// ------------------------------------------------------------------------------
		private async void Start()
        {
			await Cysharp.Threading.Tasks.UniTask.WaitForEndOfFrame();
			await Cysharp.Threading.Tasks.UniTask.WaitForEndOfFrame();
			await Cysharp.Threading.Tasks.UniTask.WaitForEndOfFrame();

            if (!PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom?.CustomProperties != null
				&& PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(IS_SIMULATING, out object isSim))
            {
				_wasActive = (bool)isSim;
				if (_wasActive)
                {
					PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(BUILD_IDS, out object buildIDs);
					PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(SIM_IDS, out object simIDs);
					this.InitializeSimulation((int[])buildIDs, (int[])simIDs);
                }
            }
        }
		// ========================================================================================

		// Common Operations ======================================================================
		public void TogglePhysics() => _isActive.Value = !_isActive.Value;
		private void Enable(Rigidbody rb, bool enable = true)
		{
            rb.GetComponent<MeshRenderer>().enabled = enable;
            rb.detectCollisions = enable;
        }
		// ========================================================================================

		// Local Operations =======================================================================
		#region Local Physics
		private void TurnOnPhysics()
		{
			Rigidbody[] rbs = GameObject.FindObjectsOfType<Rigidbody>()
				.Where(rb => ((1 << rb.gameObject.layer) & _physicsMask) > 0)
				.ToArray();

			Dictionary<Rigidbody, Rigidbody> dupMapping = new Dictionary<Rigidbody, Rigidbody>();
            for (int i = 0; i < rbs.Length; i++)
            {
				Rigidbody dup = GameObject.Instantiate(rbs[i]);
				dup.gameObject.layer = _simulationLayer;
				dup.name = $"sim_{dup.name}";

				GameObject.Destroy(dup.GetComponent<CostValue>());
				GameObject.Destroy(dup.GetComponent<ObservableDestroyTrigger>());

				dupMapping.Add(rbs[i], dup);

				this.Enable(rbs[i], false);
            }
            Rigidbody[] tapes = dupMapping.Keys
				.Where(rb => rb.gameObject.layer == _snappingLayer)
				.Select(rb => dupMapping[rb])
				.ToArray();
            for (int i = 0; i < tapes.Length; i++)
            {
				Collider cTape = tapes[i].GetComponent<Collider>();
				Collider[] tapedObjects = Physics.OverlapSphere(
					tapes[i].position,
					tapes[i].GetComponent<PunTapeHandler>().TapeSize,
					1 << _simulationLayer)
					.Where(t => t.gameObject != tapes[i].gameObject
						&& t.CompareTag(STRUCTURAL_TAG))
					.ToArray();

                for (int j = 0; j < tapedObjects.Length; j++)
				{
					Collider c1 = tapedObjects[j];
                    Physics.IgnoreCollision(cTape, c1);

                    FixedJoint joint = tapes[i].gameObject.AddComponent<FixedJoint>();
                    joint.connectedBody = c1.GetComponent<Rigidbody>();
                    joint.breakForce = _breakForce;
                    joint.breakTorque = _breakTorque;
                    joint.enableCollision = false;
                    joint.anchor = tapes[i].position;
                    //joint.axis = 
                    joint.OnDestroyAsObservable()
                        .Subscribe(_ =>
						{
							// BUG: Why does this never get called?
							Debug.Log("Broken", c1);
							Physics.IgnoreCollision(cTape, c1, false);
                            for (int k = 0; k < tapedObjects.Length; k++)
                            {
								Physics.IgnoreCollision(c1, tapedObjects[k], false);
                            }
						})
                        .AddTo(tapes[i].gameObject);

                    for (int k = j + 1; k < tapedObjects.Length; k++)
                    {
						Collider c2 = tapedObjects[k];
						Physics.IgnoreCollision(c1, c2);
                        //joint.OnDestroyAsObservable()
                        //    .Subscribe(_ =>
                        //    {
						//		  // BUG: This seems to be getting called before the joint dies...
                        //        Debug.Log("Brokeded", c1);
                        //        //Physics.IgnoreCollision(c2, c1, false);
                        //    })
                        //    .AddTo(c1);


                        // TODO: What if objects are jointed to multiple tapes?
                        // One joint's failure would cause the objects to collide even though they
                        // are still jointed elsewhere.
                        // I don't think this should be a concern often if at all because our
                        // objects are all lines, but otherwise this could cause problems.
                    }
				}
			}
			for (int i = 0; i < dupMapping.Values.Count; i++)
			{
				Rigidbody dup = dupMapping.Values.ElementAt(i);
				dup.useGravity = true;
				dup.constraints = RigidbodyConstraints.None;
			}

			if (_feedbackUI != null)
				_feedbackUI.StartEvaluation();
		}

		private void TurnOffPhysics()
        {
			Rigidbody[] allRB = GameObject.FindObjectsOfType<Rigidbody>();

			allRB.Where(rb => ((1 << rb.gameObject.layer) & _physicsMask) > 0)
				.ToList()
				.ForEach(rb => this.Enable(rb));

			allRB.Where(rb => rb.gameObject.layer == _simulationLayer)
                .ToList()
                .ForEach(rb => GameObject.Destroy(rb.gameObject));
		}
		#endregion Local Physics
		// ========================================================================================

		// Networked Operations ===================================================================
		#region Networked Physics
		// Start simulation -------------------------------------------------------------------
		[PunRPC]
		private void RequestSimulation()
        {
			if (PhotonNetwork.IsMasterClient && !_isActive.Value)
				_isActive.Value = true;
        }
		private void TurnOnNetworkedPhysics()
		{
			if (!PhotonNetwork.IsMasterClient)
            {
				this.photonView.RPC("RequestSimulation", RpcTarget.MasterClient);
				return;
            }

			Rigidbody[] rbs = GameObject.FindObjectsOfType<Rigidbody>()
				.Where(rb => ((1 << rb.gameObject.layer) & _physicsMask) > 0)
				.ToArray();

			Dictionary<Rigidbody, Rigidbody> dupMapping = new Dictionary<Rigidbody, Rigidbody>();
			for (int i = 0; i < rbs.Length; i++)
			{
				if (!rbs[i].TryGetComponent(out PunPrefabName p))
				{
					Debug.LogWarning($"Unable to simulate {rbs[i].NameAndID()} because it lacks a PunPrefabName component.", rbs[i]);
					continue;
				}

				Rigidbody dup = p.gameObject.layer == _snappingLayer
					? PhotonNetwork.Instantiate(
							p.PrefabName,
							rbs[i].transform.position,
							rbs[i].transform.rotation,
							data: new object[] { true } // isSnapped
						)
						.GetComponent<Rigidbody>()
					: PhotonNetwork.Instantiate(
							p.PrefabName,
							rbs[i].transform.position,
							rbs[i].transform.rotation,
							data: new object[] { false } // isPreview
						)
						.GetComponent<Rigidbody>();

				dup.gameObject.layer = _simulationLayer;
                dup.name = $"sim_{dup.name}";
                GameObject.Destroy(dup.GetComponent<CostValue>());
                GameObject.Destroy(dup.GetComponent<ObservableDestroyTrigger>());

                dupMapping.Add(rbs[i], dup);

				this.Enable(rbs[i], false);
			}

			int[] buildIDs = dupMapping.Keys.Select(rb => rb.GetComponent<PhotonView>().ViewID).ToArray();
			int[] simIDs = dupMapping.Values.Select(rb => rb.GetComponent<PhotonView>().ViewID).ToArray();
			PhotonNetwork.CurrentRoom.SetCustomProperties(new ExitGames.Client.Photon.Hashtable
			{
				{IS_SIMULATING, true},
				{BUILD_IDS, buildIDs},
				{SIM_IDS, simIDs}
			});
			this.photonView.RPC(
				"InitializeSimulation",
				RpcTarget.Others,
				buildIDs,
				simIDs
				);

			Rigidbody[] tapes = dupMapping.Keys
				.Where(rb => rb.gameObject.layer == _snappingLayer)
				.Select(rb => dupMapping[rb])
				.ToArray();
			for (int i = 0; i < tapes.Length; i++)
			{
				Collider cTape = tapes[i].GetComponent<Collider>();
				Collider[] tapedObjects = Physics.OverlapSphere(
					tapes[i].position,
					tapes[i].GetComponent<PunTapeHandler>().TapeSize,
					1 << _simulationLayer)
					.Where(t => t.gameObject != tapes[i].gameObject
						&& t.CompareTag(STRUCTURAL_TAG))
					.ToArray();

				for (int j = 0; j < tapedObjects.Length; j++)
				{
					Collider c1 = tapedObjects[j];
					Physics.IgnoreCollision(cTape, c1);

					FixedJoint joint = tapes[i].gameObject.AddComponent<FixedJoint>();
					joint.connectedBody = c1.GetComponent<Rigidbody>();
					joint.breakForce = _breakForce;
					joint.breakTorque = _breakTorque;
					joint.enableCollision = false;
					joint.anchor = tapes[i].position;
					//joint.axis = 
					joint.OnDestroyAsObservable()
						.Subscribe(_ =>
						{
							// BUG: Why does this never get called?
							Debug.Log("Broken", c1);
							Physics.IgnoreCollision(cTape, c1, false);
							for (int k = 0; k < tapedObjects.Length; k++)
							{
								Physics.IgnoreCollision(c1, tapedObjects[k], false);
							}
						})
						.AddTo(tapes[i].gameObject);

					for (int k = j + 1; k < tapedObjects.Length; k++)
					{
						Collider c2 = tapedObjects[k];
						Physics.IgnoreCollision(c1, c2);
						//joint.OnDestroyAsObservable()
						//    .Subscribe(_ =>
						//    {
						//		  // BUG: This seems to be getting called before the joint dies...
						//        Debug.Log("Brokeded", c1);
						//        //Physics.IgnoreCollision(c2, c1, false);
						//    })
						//    .AddTo(c1);


						// TODO: What if objects are jointed to multiple tapes?
						// One joint's failure would cause the objects to collide even though they
						// are still jointed elsewhere.
						// I don't think this should be a concern often if at all because our
						// objects are all lines, but otherwise this could cause problems.
					}
				}
			}
			for (int i = 0; i < dupMapping.Values.Count; i++)
			{
				Rigidbody dup = dupMapping.Values.ElementAt(i);
				dup.useGravity = true;
				dup.constraints = RigidbodyConstraints.None;
			}

			if (_feedbackUI != null)
				_feedbackUI.StartEvaluation();
		}

		[PunRPC]
		private void InitializeSimulation(int[] buildIDs, int[] simIDs)
		{
			_isActive.Value = true;

            foreach (int id in buildIDs)
            {
				PhotonView view = PhotonView.Find(id);
				this.Enable(view.GetComponent<Rigidbody>(), false);
            }
			foreach (int id in simIDs)
			{
				PhotonView view = PhotonView.Find(id);
				view.gameObject.layer = _simulationLayer;
				view.gameObject.name = $"sim_{view.gameObject.name}";
				view.gameObject.GetComponent<MeshRenderer>().enabled = true;

				GameObject.Destroy(view.gameObject.GetComponent<ObservableDestroyTrigger>());
				GameObject.Destroy(view.gameObject.GetComponent<CostValue>());

				// We shouldn't need to interact with the rigidbodies here,
				// all physics simulation should be controlled by the master
				// client and transform changes will be propagated to others
			}
		}
		// ------------------------------------------------------------------------------------
		// End Simulation ---------------------------------------------------------------------
		[PunRPC]
		private void RequestEndSimulation()
		{
			if (PhotonNetwork.IsMasterClient && _isActive.Value)
				_isActive.Value = false;
		}
		private void TurnOffNetworkedPhysics()
		{
			if (!PhotonNetwork.IsMasterClient)
			{
				this.photonView.RPC("RequestEndSimulation", RpcTarget.MasterClient);
				return;
			}

			Rigidbody[] allRB = GameObject.FindObjectsOfType<Rigidbody>();

			allRB.Where(rb => rb.gameObject.layer == _simulationLayer)
				.ToList()
				.ForEach(rb => PhotonNetwork.Destroy(rb.gameObject));

			PhotonNetwork.CurrentRoom.SetCustomProperties(new ExitGames.Client.Photon.Hashtable
			{
				{IS_SIMULATING, false},
				{BUILD_IDS, new int[0]},
				{SIM_IDS, new int[0]}
			});
			this.photonView.RPC("EndSimulation", RpcTarget.All);
		}

		[PunRPC]
		private void EndSimulation()
		{
			_isActive.Value = false;

			if (_feedbackUI != null)
				_feedbackUI.StopEvaluation();

			Rigidbody[] allRB = GameObject.FindObjectsOfType<Rigidbody>();

			allRB.Where(rb => ((1 << rb.gameObject.layer) & _physicsMask) > 0)
				.ToList()
				.ForEach(rb => this.Enable(rb));
		}
		// ------------------------------------------------------------------------------------
		#endregion Networked Physics
		// ========================================================================================

	}
	// ============================================================================================
	// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
	// ============================================================================================
}