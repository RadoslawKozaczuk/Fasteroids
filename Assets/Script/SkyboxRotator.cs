using UnityEngine;

class SkyboxRotator : MonoBehaviour
{
    // should be dynamic based on player's movement
    public float RotationSpeed = 0.5f;
    float _currentRotation = 0f;

    // later on also add the ability to change the sky box with game restart/level change/etc
    // If you change the skybox in playmode, you have to use the DynamicGI.UpdateEnvironment function call to update the ambient probe.

    void Update()
    {
        _currentRotation += RotationSpeed * Time.deltaTime;
        RenderSettings.skybox.SetFloat("_Rotation", _currentRotation);
    }
}
