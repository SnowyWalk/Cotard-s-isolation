
using UdonSharp;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;

public class ActiveMirrorWhenColliding : UdonSharpBehaviour
{
    private MeshRenderer mirrorRenderer;
    public AudioSource heartbeatSound;
    public AudioSource horrorSound;
    public PostProcessVolume postprocesser;
    public GameObject normalPostProcesser;
    public GameObject redBloomPostProcesser;

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
        heartbeatSound.pitch = 1.5f;
        horrorSound.enabled = true;
        normalPostProcesser.SetActive(false);
        redBloomPostProcesser.SetActive(true);
    }

    public override void OnPlayerTriggerExit(VRCPlayerApi player)
    {
        base.OnPlayerTriggerExit(player);

        // Do only for me.
        if (!player.isLocal)
            return;

        // Enable the mirror
        mirrorRenderer.enabled = false;
        heartbeatSound.pitch = 1f;
        normalPostProcesser.SetActive(true);
        redBloomPostProcesser.SetActive(false);
    }

}
