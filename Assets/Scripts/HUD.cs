
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class HUD : UdonSharpBehaviour
{

    private void LateUpdate()
    {
        this.transform.position = Networking.LocalPlayer.GetBonePosition(HumanBodyBones.Head); // + Vector3.forward * 1.5f;
        this.transform.rotation = Networking.LocalPlayer.GetBoneRotation(HumanBodyBones.Head);
        this.transform.position = this.transform.position + this.transform.forward * 1.5f;
    }
}
