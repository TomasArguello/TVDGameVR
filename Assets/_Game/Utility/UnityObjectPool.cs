using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace Tamu.Tvd.VR
{
    public class UnityObjectPool<T> where T : UnityObject
    {
        // Fields =================================================================================
        [SerializeField] private T _prefab;

        private List<T> _inactive = new List<T>();
        private List<T> _active = new List<T>();

        public delegate void OnShow(T obj);
        public delegate void OnHide(T obj);
        private OnShow _show;
        private OnHide _hide;
        // ========================================================================================

        // Constructor ============================================================================
        public UnityObjectPool(T prefabToInstantiate, OnShow onShow, OnHide onHide)
        {
            _prefab = prefabToInstantiate;
            _show = onShow;
            _hide = onHide;
        }
        // ========================================================================================

        // Methods ================================================================================
        /// <summary>
        /// Get a new UnityObject from the pool.
        /// </summary>
        public T Next
        {
            get
            {
                T t = _inactive.RemoveGrabAt(0);
                if (t == null)
                    t = GameObject.Instantiate(_prefab);

                _active.Add(t);
                _show(t);
                return t;
            }
        }

        /// <summary>
        /// Deactivate all UnityObjects in the pool.
        /// </summary>
        public void Clear()
        {
            while (_active.Count > 0)
            {
                T t = _active.RemoveGrabAt(0);
                if (t != null)
                {
                    _inactive.Add(t);
                    _hide(t);
                }
            }
            _inactive = _inactive.Where(t => t != null).ToList();
        }

        /// <summary>
        /// Deactivate a specific UnityObject in the pool.
        /// </summary>
        public bool Clear(T obj)
        {
            bool success = _active.Remove(obj);
            if (success)
            {
                _inactive.Add(obj);
                _hide(obj);
            }
            return success;
        }

        /// <summary>
        /// Set the pool's count of active UnityObjects to N and get them.
        /// </summary>
        /// <param name="howMany">The number of UnityObjects to get.</param>
        /// <returns>The set of active UnityObjects.</returns>
        public T[] Current(int howMany)
        {
            T[] objs = new T[howMany];
            int i = 0;
            while (i < objs.Length && _active.Count > 0)
            {
                T next = _active.RemoveGrabAt(0);
                if (next == null) continue;

                objs[i] = next;
                _show(objs[i]);
                i++;
            }
            while (i < objs.Length)
            {
                objs[i++] = this.Next;
            }

            this.Clear();
            for (int j = 0; j < objs.Length; j++)
                _active.Add(objs[j]);

            return objs.Reverse().ToArray();
        }

        /// <summary>
        /// Set the pool's count of active UnityObjects to 1 and get it.
        /// </summary>
        public T First => this.Current(1)[0];

        /// <summary>
        /// How many objects are currently active in the pool.
        /// </summary>
        public int ActiveCount => _active.Count;

        public bool IsActive(T obj) => _active.Contains(obj);
        public bool Owns(T obj) => _active.Contains(obj) || _inactive.Contains(obj);

        // ========================================================================================


    } 
}
