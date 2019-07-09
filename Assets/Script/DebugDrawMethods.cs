using Unity.Mathematics;
using UnityEngine;
using static Assets.Scripts.Systems.QuadrantSystem;

namespace Assets.Scripts
{
    public static class DebugDrawMethods
    {
        public static void DebugDrawCircle(float centerX, float centerY, float r, Color color)
        {
            float theta = 0;  // angle that will be increased each loop
            float step = 15f;  // amount to add to theta each time (degrees)

            Vector3 previousPoint;
            Vector3 newPoint = Vector3.zero;

            bool firstRun = true;

            while (theta <= 360)
            {
                float x = centerX + r * math.cos(Mathf.Deg2Rad * theta);
                float y = centerY + r * math.sin(Mathf.Deg2Rad * theta);

                previousPoint = newPoint;
                newPoint = new Vector3(x, y);

                theta += step;

                if (firstRun)
                {
                    firstRun = false;
                    continue;
                }

                Debug.DrawLine(previousPoint, newPoint, color, 0f, false);
            }
        }

        public static void DebugDrawQuadrant(float posX, float posY, Color color)
        {
            Vector3 lowerLeft = new Vector3(
                math.floor(posX / QuadrantCellSize) * QuadrantCellSize,
                math.floor(posY / QuadrantCellSize) * QuadrantCellSize);

            Debug.DrawLine(lowerLeft, lowerLeft + new Vector3(+1, +0) * QuadrantCellSize, color, 0f, false);
            Debug.DrawLine(lowerLeft, lowerLeft + new Vector3(+0, +1) * QuadrantCellSize, color, 0f, false);
            Debug.DrawLine(lowerLeft + new Vector3(+1, +0) * QuadrantCellSize, lowerLeft + new Vector3(+1, +1) * QuadrantCellSize, color, 0f, false);
            Debug.DrawLine(lowerLeft + new Vector3(+0, +1) * QuadrantCellSize, lowerLeft + new Vector3(+1, +1) * QuadrantCellSize, color, 0f, false);
        }
    }
}
