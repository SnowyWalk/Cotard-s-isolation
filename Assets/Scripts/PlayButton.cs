
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class PlayButton : UdonSharpBehaviour
{
    public Transform spawnPointToBasement;
    public CameraFader cameraFader;

    private int progressingTeleportLevel;
    // 0: able to click play.
    // 1: Start Fade out.
    // 2: Wait 1 sec.
    // 3: Teleport & Start Fade in.
    // 4: Wait 1 sec.
    // 5: Clear. return to 0.
    private float timer;

    private void Start()
    {
        progressingTeleportLevel = 0;
    }

    public override void Interact()
    {
        base.Interact();

        if (progressingTeleportLevel != 0)
            return;

        Debug.Log("Lets send RPC - TeleportToBasement");
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(TeleportToBasement));
    }

    //[VRC.SDKBase.RPC]
    public void TeleportToBasement()
    {
        Debug.Log("I received RPC - TeleportToBasement");
        if (progressingTeleportLevel != 0)
            return;
        Debug.Log("Let's go Fade out");

        timer = 0.0f;
        progressingTeleportLevel = 1;
    }

    private void Update()
    {
        switch (progressingTeleportLevel)
        {
            case 0: // 0: able to click play.
                break;
            case 1: // 1: Start Fade out.
                timer = 0.0f;
                cameraFader.FadeOut();
                ++progressingTeleportLevel;
                break;
            case 2: // 2: Wait 1 sec.
                timer += Time.deltaTime;
                if (timer >= 1.0f)
                    ++progressingTeleportLevel;
                break;
            case 3: // 3: Teleport & Start Fade in.
                Networking.LocalPlayer.TeleportTo(spawnPointToBasement.position, spawnPointToBasement.rotation);
                cameraFader.FadeIn();
                ++progressingTeleportLevel;
                timer = 0.0f;
                break;
            case 4: // 4: Wait 1 sec.
                timer += Time.deltaTime;
                if (timer >= 1.0f)
                    ++progressingTeleportLevel;
                break;
            case 5: // 5: Clear. return to 0.
                cameraFader.CanvasOff();
                progressingTeleportLevel = 0;
                break;
        }
    }
}
