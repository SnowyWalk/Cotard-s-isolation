
using UdonSharp;
using UdonSharpEditor;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

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
