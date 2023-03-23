using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class AdditionalActionController : ActionBasedController
{
    protected override void UpdateInput(XRControllerState controllerState)
    {
        base.UpdateInput(controllerState);

        controllerState.selectInteractionState.SetFrameState(
            IsPressed(this.selectAction.action) || IsPressed(this.activateAction.action));
    }
}
