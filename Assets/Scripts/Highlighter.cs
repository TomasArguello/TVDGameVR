using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
#pragma warning disable 0649    // Variable declared but never assigned to

namespace Tamu.Tvd
{
    // ============================================================================================
    // ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
    // ============================================================================================
    /**
     *  Controls highlighting of an object to make it easier for other scripts to access
     */
    // ============================================================================================
    // ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
    // ============================================================================================
    //[RequireComponent(typeof(Collider))]	// CollisionHighlighter expects a Collider, but this is not nec. universal
    public class Highlighter : MonoBehaviour
    {
        // Fields =================================================================================
        [SerializeField] private MeshRenderer _renderer;
        [SerializeField, ReadOnly] private Color _originalColor;

        [Space]
        public Color HighlightColor = Color.yellow;
        public bool ReplaceColor = false;
        // ========================================================================================

        // Mono ===================================================================================
        void Awake()
        {
            if (_renderer == null)
                Debug.LogError("Highlighter is missing its reference to a MeshRenderer.", this);

            _originalColor = _renderer.material.color;
        }
        // ========================================================================================

        // Methods ================================================================================
        public void ShowHighlight() => this.ShowHighlight(this.HighlightColor, this.ReplaceColor);
        public void ShowHighlight(Color highlightColor, bool replaceColor = false)
        {
            _renderer.material.color = replaceColor
                ? highlightColor
                : new Color(
                    highlightColor.r * _originalColor.r,
                    highlightColor.g * _originalColor.g,
                    highlightColor.b * _originalColor.b
                    );
        }

        public void HideHighlight() => _renderer.material.color = _originalColor;
        // ========================================================================================

    }
    // ============================================================================================
    // ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
    // ============================================================================================
}
