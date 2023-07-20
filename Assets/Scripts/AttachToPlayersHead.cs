using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class AttachToPlayersHead : UdonSharpBehaviour
{
    void Update()
    {
        if (Networking.LocalPlayer == null)
        {
#if UNITY_EDITOR
            Debug.Log("Could not find me. why?");
#endif
            return;
        }

        this.transform.position = Networking.LocalPlayer.GetBonePosition(HumanBodyBones.Head);
    }
}
