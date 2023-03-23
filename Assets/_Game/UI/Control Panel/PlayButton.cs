using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Tamu.Tvd.VR {
    // ================================================================================================
    // ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
    // ================================================================================================
    /**
     * Manage play state and update UI to reflect state.
     */
    // ================================================================================================
    // ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
    // ================================================================================================
    [RequireComponent(typeof(PhysicalButton))]
    public class PlayButton : MonoBehaviour {
        // ==================================================================================
        // Fields & Properties
        // ==================================================================================
        private PhysicalButton _button;
        [SerializeField] private PhysicsActivator _physics;

        [Header("Text")]
        //[SerializeField] private TMPro.TextMeshPro _textUI;
        [SerializeField] private string _startText;
        [SerializeField] private string _stopText;

        [Header("State")]
        /*[SerializeField]*/
        private BoolReactiveProperty _playing = new BoolReactiveProperty(false);
        public BoolReactiveProperty IsPlaying => _playing;

        public Color StartColor = Color.green;
        public Color StopColor = new Color(1, .75f, 0);

        [SerializeField]
        private TMPro.TextMeshPro playText;

        public Sprite activated;
        public Sprite deactivated;
        public SpriteRenderer buttonSprite;

        public GameObject[] otherButtons;

        // ==================================================================================
        // MonoBehaviour
        // ==================================================================================
        // ------------------------------------------------------------------------
        void Awake() {
            _button = this.GetComponent<PhysicalButton>();
        }
        // ------------------------------------------------------------------------
        void Start() {
            _playing.Subscribe(b => {
                //_textUI.text = b ? _stopText : _startText;
                _button.BaseColor = b ? StopColor : StartColor;
                _button.UpdateColor();
            })
            .AddTo(this);
            //_button.Interactible.Subscribe(b => _textUI.enabled = b).AddTo(this);

            if (_physics != null) {
                _physics.IsPhysicsOn.Subscribe(b => {
                    if (b != _playing.Value)
                        _button.OnPress.Invoke();
                })
                .AddTo(this);

                _playing.Subscribe(b => {
                    if (b != _physics.IsPhysicsOn.Value)
                        _physics.IsPhysicsOn.Value = b;
                })
                .AddTo(this);
            }
        }

        void Update() {
            if (_playing.Value == false) {
                playText.text = "Test";
                playText.color = Color.white;
                buttonSprite.sprite = deactivated;
            } else {
                playText.text = "Stop";
                playText.color = Color.red;
                buttonSprite.sprite = activated;
            }
        }

        // ------------------------------------------------------------------------
        // ==================================================================================
        // Methods
        // ==================================================================================
        public void TogglePlayState() {
            _playing.Value = !_playing.Value;
        }

        public async void DisableFor(int milliseconds) => await _button.DisableFor(milliseconds);
        // ==================================================================================

#if UNITY_EDITOR
        [CustomEditor(typeof(PlayButton))]
        public class PlayButtonEditor : Editor
        {
            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();
                PlayButton playButt = (PlayButton)target;
                if (GUILayout.Button("TurnOn"))
                {
                    playButt.TogglePlayState();
                }
            }
        }
#endif
    }
    // ================================================================================================
    // ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
    // ================================================================================================ 
}
