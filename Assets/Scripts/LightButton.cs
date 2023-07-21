
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class LightButton : UdonSharpBehaviour
{
    public GameObject lightGo;

    public override void Interact()
    {
        base.Interact();

        lightGo.SetActive(!lightGo.activeSelf);
    }
}
