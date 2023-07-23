
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Door : UdonSharpBehaviour
{
    public AudioSource sound;

    public override void Interact()
    {
        base.Interact();

        sound.Play();
    }
}
