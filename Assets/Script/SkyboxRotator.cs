using UnityEngine;

class SkyboxRotator : MonoBehaviour
{
    // skybox will rotate by this amount
    public static Vector3 LastPlayerMovement;

    public Camera SkyboxCamera;
    // later on also add the ability to change the sky box with game restart/level change/etc
    // If you change the skybox in playmode, you have to use the DynamicGI.UpdateEnvironment function call to update the ambient probe.

    void Update() => SkyboxCamera.transform.Rotate(LastPlayerMovement);
}
