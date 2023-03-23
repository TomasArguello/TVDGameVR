using UnityEngine;

namespace Tamu.Tvd
{
	// ============================================================================================
	// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
	// ============================================================================================
	/**
	 *  Container for the name of the Prefab from which this object derives.
	 *  This is necessary because Photon only instantiates from Prefabs, not from GameObjects, and
	 *  does so by name, looking for a matching name string in its Prefab pool (composed of Prefabs
	 *  in Resources folders with PhotonView components).
	 *  
	 *  TODO: I'd like a more automated solution than a potentially error-prone manual string, but
	 *  this is simplest for now...
	 */
	// ============================================================================================
	// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
	// ============================================================================================
	public class PunPrefabName : MonoBehaviour
	{
		[Tooltip("The name of the Prefab from which this object derives."
			+ "Necessary because Photon only instantiates Prefabs, and does so by name.")]
		[SerializeField] private string _name;
		public string PrefabName => _name;
	}
	// ============================================================================================
	// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
	// ============================================================================================
}