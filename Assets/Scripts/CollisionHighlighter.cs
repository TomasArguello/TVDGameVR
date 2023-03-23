using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#pragma warning disable 0649    // Variable declared but never assigned to


namespace Tamu.Tvd
{
    // ============================================================================================
    // ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
    // ============================================================================================
    /**
     *  Activate the Highlighter components of other objects when colliding with them.
     *  
     *  TODO: I'm assuming the Highilighter component is on the same gameobject as the collider
     *  without enforcing this assumption anywhere. I'd like a better solution than requiring it in
     *  the Highlighter script...
     */
    // ============================================================================================
    // ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
    // ============================================================================================
    [RequireComponent(typeof(Collider))]
    public class CollisionHighlighter : MonoBehaviour
    {
        // Mono ===================================================================================
        private void OnCollisionEnter(Collision collision)
        {
            if (collision.collider.TryGetComponent(out Highlighter h))
                h.ShowHighlight();
        }
        private void OnCollisionExit(Collision collision)
        {
            if (collision.collider.TryGetComponent(out Highlighter h))
                h.HideHighlight();
        }
        // ========================================================================================
    }
    // ============================================================================================
    // ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
    // ============================================================================================
}
