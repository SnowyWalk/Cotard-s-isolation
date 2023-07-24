
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class PlayButton : UdonSharpBehaviour
{
    public Transform spawnPointToBasement;

    public override void Interact()
    {
        base.Interact();

        Debug.Log("Lets send RPC - TeleportToBasement");
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(TeleportToBasement));
    }

    //[VRC.SDKBase.RPC]
    public void TeleportToBasement()
    {
        Debug.Log("I received RPC - TeleportToBasement");
        Networking.LocalPlayer.TeleportTo(spawnPointToBasement.position, spawnPointToBasement.rotation);
    }
}
