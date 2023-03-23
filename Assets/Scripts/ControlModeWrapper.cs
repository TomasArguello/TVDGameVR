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
	 *  Wrap a ControlMode value in a component.
	 *  
	 *  TODO: This feels terribly inelegant, but it is the easiest way to centralize control under
	 *  the DemoController without writing a new script for the Tool menu that basically duplicates
	 *  SelectionMenu. This wouldn't be an issue if Unity could serialize generic components...
	 */
	// ============================================================================================
	// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
	// ============================================================================================
	public class ControlModeWrapper : MonoBehaviour
	{
		[SerializeField] private ControlModeManager.ControlMode _mode;
		public ControlModeManager.ControlMode Mode => _mode;
	}
	// ============================================================================================
	// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
	// ============================================================================================
}