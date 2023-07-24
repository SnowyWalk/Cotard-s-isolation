
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class SubtitleTestButton : UdonSharpBehaviour
{
    [SerializeField] GameObject canvas;

    private int level = 0;
    // 0: nothing
    // 1: active on
    // 2: wait 3 sec
    // 3: active off
    private float timer;

    public override void Interact()
    {
        base.Interact();

        level = 1;
    }


    private void Update()
    {
        switch (level)
        {
            case 0:
                break;
            case 1:
                canvas.SetActive(true);
                ++level;
                timer = 0.0f;
                break;
            case 2:
                timer += Time.deltaTime;
                if (timer >= 3.0f)
                    ++level;
                break;
            case 3:
                canvas.SetActive(false);
                level = 0;
                break;

        }


    }
}
