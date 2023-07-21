
using UdonSharp;
using UdonSharp.Examples.Utilities;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ShaderChangeButton : UdonSharpBehaviour
{
    public ShaderManager shaderManager;
    public string shaderKey;

    private InteractToggle interact;

    private void Start()
    {
        interact = GetComponent<InteractToggle>();
    }

    public override void Interact()
    {
        base.Interact();

        interact.InteractionText = "Current : " + (shaderManager.ToggleShaderKey(shaderKey) == 1 ? "ON" : "OFF");
    }
}
