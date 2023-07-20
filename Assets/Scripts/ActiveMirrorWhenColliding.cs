
using UdonSharp;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;

public class ActiveMirrorWhenColliding : UdonSharpBehaviour
{
    private MeshRenderer mirrorRenderer;
    public PostProcessVolume postprocesser;
    public AudioSource heartbeatSound;
    public AudioSource horrorSound;

    void Start()
    {
        mirrorRenderer = GetComponent<MeshRenderer>();
        mirrorRenderer.enabled = false;
    }

    public override void OnPlayerTriggerEnter(VRCPlayerApi player)
    {
        base.OnPlayerTriggerEnter(player);

        // Do only for me.
        if (!player.isLocal)
            return;

        // Enable the mirror
        mirrorRenderer.enabled = true;
        postprocesser.enabled = true;
        heartbeatSound.pitch = 1.5f;
        horrorSound.enabled = true;
    }

    public override void OnPlayerTriggerExit(VRCPlayerApi player)
    {
        base.OnPlayerTriggerExit(player);

        // Do only for me.
        if (!player.isLocal)
            return;

        // Enable the mirror
        mirrorRenderer.enabled = false;
        postprocesser.enabled = false;
        heartbeatSound.pitch = 1f;
    }

}
