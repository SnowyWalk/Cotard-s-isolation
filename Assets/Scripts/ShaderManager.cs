
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ShaderManager : UdonSharpBehaviour
{
    Material sharedMaterial;


    private void Start()
    {
        sharedMaterial = GetComponent<Renderer>().sharedMaterial;
    }

    public int ToggleShaderKey(string key)
    {
        sharedMaterial.SetInt(key, 1 - sharedMaterial.GetInt(key));
        return sharedMaterial.GetInt(key);
    }
}
