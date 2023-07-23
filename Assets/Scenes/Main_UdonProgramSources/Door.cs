
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Door : UdonSharpBehaviour
{
    public AudioSource sound;



    public override void interact()

        sound.Play();

        base.Interact();


    


    void Start()
    {
    

    }
}
