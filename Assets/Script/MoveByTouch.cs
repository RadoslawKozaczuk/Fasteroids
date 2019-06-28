using UnityEngine;

namespace Assets.Script
{
    class MoveByTouch : MonoBehaviour
    {
        void Update()
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Vector3 touchPosition = Camera.main.ScreenToWorldPoint(Input.touches[i].position);
                // Vector3.zero is the center of the screen
                Debug.DrawLine(Vector3.zero, touchPosition, Color.red);
            }
        }
    }
}
