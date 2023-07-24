
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class CameraFader : UdonSharpBehaviour
{
    [SerializeField] GameObject fadeOut;
    [SerializeField] GameObject fadeIn;
    [SerializeField] GameObject canvas;

    public void FadeOut()
    {
        CanvasOn();
        fadeOut.SetActive(true);
        fadeIn.SetActive(false);
    }

    public void FadeIn()
    {
        CanvasOn();
        fadeIn.SetActive(true);
        fadeOut.SetActive(false);
    }

    public void CanvasOn()
    {
        canvas.SetActive(true);
    }

    public void CanvasOff()
    {
        canvas.SetActive(false);
    }
}
