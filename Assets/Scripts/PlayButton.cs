
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class PlayButton : UdonSharpBehaviour
{
    public Transform spawnPointToBasement;

    public override void Interact()
    {
        base.Interact();

        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(TeleportToBasement));
    }

    //[VRC.SDKBase.RPC]
    public void TeleportToBasement()
    {
        Networking.LocalPlayer.TeleportTo(spawnPointToBasement.position, spawnPointToBasement.rotation);
    }
}
