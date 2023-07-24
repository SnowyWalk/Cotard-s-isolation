
using System.Collections;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class StartingDoorAnimation : UdonSharpBehaviour
{
    private float closedDoorLightOnTime = 2.5f;
    private float closedDoorLightOffTime = 2.5f;
    private float openDoorLightOnTime = 2.5f;
    private float openDoorLightOffTime = 2.5f;

    private float timer;
    private int level; // Show state. 0: closed door, 1: lights off, 2: opened door, 3: lights off

    public GameObject closedDoorWithLight;
    public GameObject openDoorWithLight;

    void Start()
    {
        timer = 0.0f;
        level = 0;
        closedDoorWithLight.SetActive(true);
        openDoorWithLight.SetActive(false);
    }

    private void Update()
    {
        timer += Time.deltaTime;

        switch (level)
        {
            case 0: // show closed door -> lights off
                if (timer >= closedDoorLightOnTime)
                {
                    closedDoorWithLight.SetActive(false);
                    openDoorWithLight.SetActive(false);
                    timer -= closedDoorLightOnTime;
                    ++level;
                }
                break;
            case 1: // lights off -> show open door
                if (timer >= closedDoorLightOffTime)
                {
                    closedDoorWithLight.SetActive(false);
                    openDoorWithLight.SetActive(true);
                    timer -= closedDoorLightOffTime;
                    ++level;
                }
                break;
            case 2: // show open door -> lights off
                if (timer >= openDoorLightOnTime)
                {
                    closedDoorWithLight.SetActive(false);
                    openDoorWithLight.SetActive(false);
                    timer -= openDoorLightOnTime;
                    ++level;
                }
                break;
            case 3: // lights off -> show closed door
                if (timer >= openDoorLightOffTime)
                {
                    closedDoorWithLight.SetActive(true);
                    openDoorWithLight.SetActive(false);
                    timer -= openDoorLightOffTime;
                    level = 0;
                }
                break;
        }
    }
}
