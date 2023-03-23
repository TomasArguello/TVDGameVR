using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using UniRx.Triggers;
using Photon.Pun;
using Cysharp.Threading.Tasks;
using Unity.Jobs;
using Unity.Collections;

#pragma warning disable 0649    // Variable declared but never assigned to

namespace Tamu.Tvd
{
    // ============================================================================================
    // ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
    // ============================================================================================
    /**
     *  Preview object placement using Rigidbody.SweepTest to detect the object placement point.
     *  
     *  TODO: Find a better way to make the static ghost vars inspector-editable
     */
    // ============================================================================================
    // ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
    // ============================================================================================
    [RequireComponent(typeof(ContactPool))]
    public class PlacementCaster : MonoBehaviourPun
    {
        // Fields =================================================================================
        [SerializeField, ReadOnly] private bool _isActive = true;
        private const string STRUCTURAL_TAG = "Structural";

        [Header("Raycasting Values")]
        public float MaxRaycastDistance = 30f;
        private float _lastHitDistance = 30f;

        [Header("Object Preview")]
        public Transform PlacementPrefab;
        [SerializeField, ReadOnly] private bool _prefabIsStructural;
        private GameObject _preview;
        private MeshRenderer _previewRenderer;
        public Transform AnchorPrefab;
        private Transform _previewAnchor;
        [Space]
        public bool HidePreviewInCursorMode = true;
        public Vector3 PreviewInitialRotation = Vector3.zero;
        private float _pivotOffsetPercent = 0.5f;

        [Space]
        [SerializeField, ReadOnly] public int _ghostLayer;
        [SerializeField] private Material _ghostMaterial;
        public static int GhostLayer;
        public static Material GhostMaterial;
        private Color _ghostedColor;

        [Space]
        public Transform SweepPoint;
        private Vector3 _originalSweepPosition;
        private GameObject _sweeper;
        private Rigidbody _sweeperRb;
        private AnchorPoint _sweeperAnchor;
        public bool IsEditingAnchor => _sweeperAnchor?.IsEditingAnchor.Value ?? false;


#if UNITY_EDITOR
#pragma warning disable IDE0052
        [Header("Collision Info")]
        [SerializeField, ReadOnly] private GameObject _collidingWith;
        [SerializeField, ReadOnly] private Vector3 _collisionPoint;
        [SerializeField, ReadOnly] private float _collisionDistance;
#pragma warning restore IDE0052
#endif
        public RaycastHit? HitInfo => null; //TODO: This is only here so that PlayerController still compiles,
                                            //but its functionality will be broken, get rid of this

        private ContactPool _contactPool;
        [SerializeField] private LayerMask _contactFilter = int.MinValue;


        [Header("Snapping")]
        [SerializeField] private LayerMask _snappingMask;
        [SerializeField, ReadOnly] private Collider _snappedPoint;
        [SerializeField, Range(0.01f, 1)] private float _snappingRadius = 0.1f;
        public GameObject SnappedObject => _snappedPoint == null ? null : _snappedPoint.gameObject;
        public bool IsSnappedToTape => _snappedPoint != null && _snappedPoint.CompareTag(STRUCTURAL_TAG);

        [Space]
        public Transform TapePrefab;
        [SerializeField] private LayerMask _tapableObjects;

        // ========================================================================================

        // Mono ===================================================================================
        // ------------------------------------------------------------------------------
        void Awake()
        {
            if (!this.photonView.IsMine)
                return;

            _ghostLayer = LayerMask.NameToLayer("Ghost");
            GhostLayer = _ghostLayer;
            GhostMaterial = _ghostMaterial;

            _contactPool = this.GetComponent<ContactPool>();

            _originalSweepPosition = this.SweepPoint.localPosition;  // TODO: enforce SweepPoint not null
        }
        // ------------------------------------------------------------------------------
        void Start()
        {
            if (!this.photonView.IsMine)
            {
                this.IsActive = false;
                return;
            }

            this.IsActive = _isActive;

            string ownership = $"{this.photonView.Owner.NickName}'s ";
            this.ObserveEveryValueChanged(pc => pc.PlacementPrefab)
                .Subscribe(p =>
                {
                    _prefabIsStructural = p != null && p.CompareTag(STRUCTURAL_TAG);

                    this.InitCasterObjects(ownership);
                    //this.photonView.RPC("UpdatePrefabSelection", RpcTarget.OthersBuffered, p == null ? "" : p.name);
                })
                .AddTo(this);
        }
        // ------------------------------------------------------------------------------
        private void OnValidate()
        {
            this.MaxRaycastDistance = Math.Max(0, this.MaxRaycastDistance);
        }
        // ------------------------------------------------------------------------------
        // ========================================================================================

        // Initialization Methods =================================================================
        #region Internal
        // Initialize new preview and sweeper objects -----------------------------------
        private void InitCasterObjects(string namePrefix)
        {
            Quaternion sweeperRotation = Quaternion.Euler(this.PreviewInitialRotation);
            _previewRenderer = null;

            if (_previewAnchor != null)
            {
                PhotonNetwork.Destroy(_preview.gameObject);
                GameObject.Destroy(_previewAnchor.gameObject);
            }
            if (_sweeperAnchor != null)
            {
                sweeperRotation = _sweeperAnchor.transform.rotation;
                Vector3 sweeperOffset = new Vector3(
                    _sweeper.transform.localPosition.x / _sweeper.transform.localScale.x,
                    _sweeper.transform.localPosition.y / _sweeper.transform.localScale.y,
                    _sweeper.transform.localPosition.z / _sweeper.transform.localScale.z
                    );
                if (_sweeper.transform.childCount > 1)
                {
                    Vector3 c1 = _sweeper.transform.GetChild(_sweeper.transform.childCount - 1).localPosition;
                    Vector3 c2 = _sweeper.transform.GetChild(0).localPosition;
                    Vector3 dir = c2 - c1;
                    Vector3 anchorOffset = sweeperOffset - c1;

                    _pivotOffsetPercent = Vector3.Project(anchorOffset, dir.normalized).magnitude / dir.magnitude;
                }
                GameObject.Destroy(_sweeperAnchor.gameObject);
            }

            if (this.PlacementPrefab != null)
            {
                this.InstantiatePreview(out _previewAnchor, out _preview, $"{namePrefix}Preview");
                GameObject.Destroy(_previewAnchor.GetComponent<AnchorPoint>());

                MeshRenderer paRenderer = _previewAnchor.GetComponent<MeshRenderer>();
                _previewRenderer = _preview.GetComponent<MeshRenderer>();
                _ghostedColor = GhostedColor(_previewRenderer.material.color);
                _previewRenderer.ObserveEveryValueChanged(r => r.isVisible)
                    .Subscribe(b => paRenderer.enabled = b)
                    .AddTo(_previewAnchor);
                _previewRenderer.ObserveEveryValueChanged(r => r.isVisible)
                    .Where(b => !b)
                    .Subscribe(_ => _contactPool.Clear())
                    .AddTo(this);

                if (_preview.TryGetComponent(out Collider _))
                {
                    _preview.AddComponent<CollisionHighlighter>();
                    CollisionIndicator ci = _preview.AddComponent<CollisionIndicator>();
                    ci.ContactPool = _contactPool;
                    ci.Filter = _contactFilter;
                }
                GameObject.Destroy(_preview.GetComponent<CostValue>());


                this.InstantiatePreview(out Transform sAnchor, out _sweeper, $"{namePrefix}Sweeper", true);
                _sweeperAnchor = sAnchor.GetComponent<AnchorPoint>();
                _sweeperAnchor.ShowWhenEditing = false;
                _sweeperAnchor.transform.rotation = sweeperRotation;
                _previewAnchor.rotation = sweeperRotation;

                this.SweepPoint.ObserveEveryValueChanged(t => t.position)
                    .Subscribe(pos => _sweeperAnchor.transform.position = pos)
                    .AddTo(_sweeperAnchor);

                _sweeper.GetComponent<MeshRenderer>().enabled = false;
                GameObject.Destroy(_sweeper.GetComponent<OutlineManager>());
                GameObject.Destroy(_sweeper.GetComponent<Smooth.SmoothSyncPUN2>());
                GameObject.Destroy(_sweeper.GetComponent<PhotonView>());
                GameObject.Destroy(_sweeper.GetComponent<CostValue>());
                for (int i = 0; i < _sweeper.transform.childCount; i++)
                {
                    GameObject.Destroy(_sweeper.transform.GetChild(i).GetComponent<Collider>());
                    GameObject.Destroy(_sweeper.transform.GetChild(i).gameObject.GetComponent<MeshRenderer>());
                }
                _sweeper.transform.ObserveEveryValueChanged(t => t.localPosition)
                    .Subscribe(pos =>
                    {
                        _preview.transform.localPosition = pos;
                        _pivotOffsetPercent = _sweeperAnchor.PercentageAlongPoints(new Vector3(
                            pos.x / _sweeper.transform.localScale.x,
                            pos.y / _sweeper.transform.localScale.y,
                            pos.z / _sweeper.transform.localScale.z
                            ));
                    })
                    .AddTo(_preview);

                _sweeperRb = _sweeper.GetComponent<Rigidbody>();

                new Action(
                    async () =>
                    {
                        await UniTask.NextFrame();
                        await UniTask.WaitForEndOfFrame();
                        _sweeper.transform.localPosition = Vector3.Scale(
                            _sweeperAnchor.ScaleToSnapPoints(_pivotOffsetPercent),
                            _sweeper.transform.localScale
                            );
                    }
                    ).Invoke();
            }
        }
        // ------------------------------------------------------------------------------
        // Instantiate an anchor and child object ---------------------------------------
        private void InstantiatePreview(out Transform anchor, out GameObject obj, string objectName, bool local = false)
        {
            anchor = GameObject.Instantiate(this.AnchorPrefab, this.transform.position, Quaternion.identity);
            anchor.name = $"{objectName} Anchor";

            if (local)
            {
                obj = GameObject.Instantiate(this.PlacementPrefab, this.transform.position, Quaternion.identity).gameObject;
                InitPreviewObject(obj, objectName);
            }
            else
            {
                obj = PhotonNetwork.Instantiate(
                    this.PlacementPrefab.name,
                    this.transform.position,
                    Quaternion.identity,
                    data: new object[] { true, objectName }
                    )
                    .gameObject;
            }

            obj.SetActive(this.IsActive);
            obj.transform.SetParent(anchor.transform);

            GameObject.DontDestroyOnLoad(anchor);
        }
        #endregion Internal
        public static void InitPreviewObject(GameObject preview, string previewName)
        {
            preview.name = $"{previewName} Object";
            preview.layer = GhostLayer;
            for (int i = 0; i < preview.transform.childCount; i++)
            {
                Transform child = preview.transform.GetChild(i);
                GameObject.Destroy(child.GetComponent<Collider>());
                GameObject.Destroy(child.GetComponent<MeshRenderer>());
                GameObject.Destroy(child.GetComponent<CostValue>());
                GameObject.Destroy(child.GetComponent<OutlineManager>());
            }

            MeshRenderer rend = preview.GetComponent<MeshRenderer>();
            Color originalColor = rend.material.color;
            Texture texture = rend.material.mainTexture;
            rend.material = GhostMaterial;
            rend.material.color = GhostedColor(originalColor);
            rend.material.mainTexture = texture;
        }
        public static Color GhostedColor(Color originalColor)
        {
            return new Color(
                originalColor.r * 0.65f,
                originalColor.g * 0.65f,
                originalColor.b * 0.65f,
                GhostMaterial.color.a
                );
        }
        // ------------------------------------------------------------------------------
        // ========================================================================================

        // State Methods ==========================================================================
        // Whether this component's managed objects are enabled -------------------------
        public bool IsActive
        {
            get => _isActive;
            set
            {
                _isActive = value;
                if (_preview != null)
                    _preview.SetActive(value || !this.HidePreviewInCursorMode);
                if (_sweeper != null)
                    _sweeper.SetActive(value);
            }
        }
        // ------------------------------------------------------------------------------
        // Access properties of the preview object --------------------------------------
        public bool PreviewIsVisible => _previewRenderer != null && _previewRenderer.isVisible;
        public Vector3 PreviewPosition => _preview == null ? Vector3.positiveInfinity : _preview.transform.position;
        public Quaternion PreviewRotation => _preview == null ? Quaternion.identity : _preview.transform.rotation;
        public int PreviewID => _preview == null ? -1 : _preview.GetPhotonView().ViewID;

        public float PreviewPivotOffset
        {
            get => _pivotOffsetPercent;
            set
            {
                _pivotOffsetPercent = value;
                if (_sweeper != null)
                    _sweeper.transform.localPosition = Vector3.Scale(
                        _sweeperAnchor.ScaleToSnapPoints(_pivotOffsetPercent),
                        _sweeper.transform.localScale
                        );
                    // Preview should be subscribed to this change, so we don't update that here
            }
        }

        // ------------------------------------------------------------------------------
        // Check whether a gameobject is Structural and not on the Snapping layer -------
        public GameObject FilterStructural(GameObject checkObj) =>
            ((1 << checkObj.layer) & _snappingMask) == 0 && checkObj.CompareTag(STRUCTURAL_TAG)
            ? checkObj
            : null;
        // ------------------------------------------------------------------------------
        // Alert other clients to update their models of our prefab property ------------
        [PunRPC]
        private void UpdatePrefabSelection(string prefabName)
        {
            if (this.photonView.IsMine || string.IsNullOrEmpty(prefabName))
                return;

            this.PlacementPrefab = (Resources.Load(prefabName) as GameObject).transform;
        }
        // ------------------------------------------------------------------------------

        public Vector3 ResetSweepPosition() => this.SweepPoint.localPosition = _originalSweepPosition;
        // ========================================================================================

        // Placement Methods ======================================================================
        // Change position of managed objects -------------------------------------------
        public void UpdatePlacementPosition(Vector3 direction, Vector3 sweepFromPoint)
        {
            if (!this.IsActive || _preview == null || _sweeperAnchor.IsEditingAnchor.Value)
                return;

            this.SweepPoint.transform.position = sweepFromPoint;
            this.UpdatePlacementPosition(direction);
            this.ResetSweepPosition();
        }
        public void UpdatePlacementPosition(Vector3 direction)
        {
            if (!this.IsActive || _preview == null || _sweeperAnchor.IsEditingAnchor.Value)
                return;

            direction = direction.normalized;

            RaycastHit hit;
            bool wasHit = _sweeperRb.SweepTest(direction, out hit, this.MaxRaycastDistance, QueryTriggerInteraction.Ignore);
            //_preview.SetActive(wasHit);

            if (wasHit)
            {
                _lastHitDistance = hit.distance;
                _previewRenderer.material.color = _ghostedColor;
            }
            else
            {
                _previewRenderer.material.color = new Color(1f, 0f, 0f, _ghostedColor.a);
            }
#if UNITY_EDITOR
            _collidingWith = hit.collider != null ? hit.collider.gameObject : null;
            _collisionPoint = hit.point;
            _collisionDistance = hit.distance;
#endif
            _previewAnchor.position = _sweeperAnchor.transform.position + direction * _lastHitDistance;

            if (!_prefabIsStructural)
                return;

            _snappedPoint = null;
            Collider[] snaps = Physics.OverlapSphere(_previewAnchor.position, _snappingRadius)
                .Where(c => ((1 << c.gameObject.layer) | _snappingMask) == _snappingMask)
                .ToArray();
            if (snaps.Length > 0)
            {
                _snappedPoint = snaps[0];
                for (int i = 1; i < snaps.Length; i++)
                {
                    if ((snaps[i].transform.position - _previewAnchor.position).sqrMagnitude
                        < (_snappedPoint.transform.position - _previewAnchor.position).sqrMagnitude)
                        _snappedPoint = snaps[i];
                }
                _previewAnchor.position = _snappedPoint.transform.position;
            }

        }
        // ------------------------------------------------------------------------------
        // Change rotation of managed objects -------------------------------------------
        public void UpdatePlacementRotation(int degrees, bool rotateAboutY = true, bool snapToWorld = false)
        {
            if (!this.IsActive || _sweeperAnchor == null || _sweeperAnchor.IsEditingAnchor.Value)
                return;

            int outOfAlignment = 0;
            if (snapToWorld)
            {
                outOfAlignment = rotateAboutY
                    ? (int)_sweeperAnchor.transform.eulerAngles.y
                    : (int)_sweeperAnchor.transform.eulerAngles.z;
                
                outOfAlignment %= degrees;
                if (outOfAlignment > 0 && degrees < 0)
                    outOfAlignment = degrees + outOfAlignment;
            }

            if (rotateAboutY)
                _sweeperAnchor.transform.RotateAround(
                    _sweeperAnchor.transform.position, Vector3.up, degrees - outOfAlignment);
            else
                _sweeperAnchor.transform.Rotate(Vector3.forward, degrees - outOfAlignment);

            _previewAnchor.rotation = _sweeperAnchor.transform.rotation;
        }
        public void SetPlacementRotation(Quaternion rotation)
        {
            if (_sweeperAnchor == null) return;

            _sweeperAnchor.transform.rotation = rotation;
            _previewAnchor.rotation = rotation;
        }
        public void SetPlacementRotation(int degrees, bool axisIsY)
        {
            if (_sweeperAnchor == null) return;

            Vector3 current = _sweeperAnchor.transform.rotation.eulerAngles;
            if (axisIsY)
                _sweeperAnchor.transform.rotation = Quaternion.Euler(current.x, degrees, current.z);
            else
                _sweeperAnchor.transform.rotation = Quaternion.Euler(current.x, current.y, degrees);

            _previewAnchor.rotation = _sweeperAnchor.transform.rotation;
        }
        // ------------------------------------------------------------------------------
        // Instantiate a room object and tape according to the preview ------------------
        public void PlaceObject()
        {
            CollisionIndicator ci = _preview.GetComponent<CollisionIndicator>();
            if (ci.CollisionCount == 0 || _previewAnchor == null)
                return;

            bool snapped = _snappedPoint != null;
            List<Vector3> hitPoints = PunTapeHandler.UntapedLocations(
                ci.FilteredCollisionPoints(_tapableObjects, true)
                )
                .ToList();

            if (snapped && PunTapeHandler.UntapedLocations(new Vector3[] { _previewAnchor.position }).Length > 0)
                hitPoints.Add(_previewAnchor.position);

            this.photonView.RPC("PlaceObject", RpcTarget.MasterClient,
                this.PreviewID,
                hitPoints.ToArray(),
                snapped
                );
        }
        [PunRPC]
        private void PlaceObject(int previewViewID, Vector3[] hitPositions, bool snapped)
        {
            if (!PhotonNetwork.IsMasterClient || previewViewID == -1)
                return;

            GameObject preview = PhotonView.Find(previewViewID)?.gameObject;
            if (preview == null)
                return;

            string prefabName = preview.GetComponent<PunPrefabName>().PrefabName;
            if (!prefabName.StartsWith("Tape")) // Don't spawn tape objects, just auto-spawn them below
                PhotonNetwork.InstantiateRoomObject(
                    preview.GetComponent<PunPrefabName>().PrefabName,
                    preview.transform.position,
                    preview.transform.rotation
                    );

            if (!preview.CompareTag(STRUCTURAL_TAG)) // Don't tape marshmallows
                return;

            List<Vector3> hits = hitPositions.ToList();
            int i = hits.Count() - 1;
            while(hits.Count > 0 && i >= 0)
            {
                GameObject g = PhotonNetwork.InstantiateRoomObject(
                    this.TapePrefab.name,
                    hits[i],
                    Quaternion.identity,
                    data: new object[] { snapped }  // TODO: Handle snapPoint disabling differently with multi-taping!!!
                    );                              // OR do we even want to disable snapPoints anymore?...

                PunTapeHandler tape = g.GetComponent<PunTapeHandler>();
                hits.RemoveAt(i);
                hits = hits.Where(p => !tape.Overlaps(p)).ToList();
                i = hits.Count - 1;
            }
        }
        // ------------------------------------------------------------------------------
        // ========================================================================================
    }
    // ============================================================================================
    // ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
    // ============================================================================================
}