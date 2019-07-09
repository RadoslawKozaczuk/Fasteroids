using System;
using UnityEngine;

namespace Assets.Scripts
{
    class Utils
    {
        // Get Mouse Position in World with Z = 0f
        public static Vector3 GetMouseWorldPosition()
        {
            Vector3 vec = GetMouseWorldPositionWithZ(Input.mousePosition, Camera.main);
            vec.z = 0f;
            return vec;
        }
        public static Vector3 GetMouseWorldPositionWithZ() => GetMouseWorldPositionWithZ(Input.mousePosition, Camera.main);

        public static Vector3 GetMouseWorldPositionWithZ(Camera worldCamera) => GetMouseWorldPositionWithZ(Input.mousePosition, worldCamera);

        public static Vector3 GetMouseWorldPositionWithZ(Vector3 screenPosition, Camera worldCamera)
        {
            Vector3 worldPosition = worldCamera.ScreenToWorldPoint(screenPosition);
            return worldPosition;
        }

        // Returns 00-FF, value 0->255
        public static string DecToHex(int value) => value.ToString("X2");

        // Returns 0-255
        public static int Hex_to_Dec(string hex) => Convert.ToInt32(hex, 16);

        // Returns a hex string based on a number between 0->1
        public static string Dec01ToHex(float value) => DecToHex((int)Mathf.Round(value * 255f));

        // Returns a float between 0->1
        public static float HexToDec01(string hex) => Hex_to_Dec(hex) / 255f;

        // Get Hex Color FF00FF
        public static string GetStringFromColor(Color color)
        {
            string red = Dec01ToHex(color.r);
            string green = Dec01ToHex(color.g);
            string blue = Dec01ToHex(color.b);
            return red + green + blue;
        }

        // Get Hex Color FF00FFAA
        public static string GetStringFromColorWithAlpha(Color color)
        {
            string alpha = Dec01ToHex(color.a);
            return GetStringFromColor(color) + alpha;
        }

        // Sets out values to Hex String 'FF'
        public static void GetStringFromColor(Color color, out string red, out string green, out string blue, out string alpha)
        {
            red = Dec01ToHex(color.r);
            green = Dec01ToHex(color.g);
            blue = Dec01ToHex(color.b);
            alpha = Dec01ToHex(color.a);
        }

        // Get Hex Color FF00FF
        public static string GetStringFromColor(float r, float g, float b)
        {
            string red = Dec01ToHex(r);
            string green = Dec01ToHex(g);
            string blue = Dec01ToHex(b);
            return red + green + blue;
        }

        // Get Hex Color FF00FFAA
        public static string GetStringFromColor(float r, float g, float b, float a)
        {
            string alpha = Dec01ToHex(a);
            return GetStringFromColor(r, g, b) + alpha;
        }

        // Get Color from Hex string FF00FFAA
        public static Color GetColorFromString(string color)
        {
            float red = HexToDec01(color.Substring(0, 2));
            float green = HexToDec01(color.Substring(2, 2));
            float blue = HexToDec01(color.Substring(4, 2));
            float alpha = 1f;
            if (color.Length >= 8)
            {
                // Color string contains alpha
                alpha = HexToDec01(color.Substring(6, 2));
            }
            return new Color(red, green, blue, alpha);
        }

        public static Vector3 GetVectorFromAngle(int angle)
        {
            // angle = 0 -> 360
            float angleRad = angle * (Mathf.PI / 180f);
            return new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad));
        }

        public static float GetAngleFromVectorFloat(Vector3 dir)
        {
            dir = dir.normalized;
            float n = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            if (n < 0) n += 360;

            return n;
        }

        public static int GetAngleFromVector(Vector3 dir)
        {
            dir = dir.normalized;
            float n = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            if (n < 0) n += 360;
            int angle = Mathf.RoundToInt(n);

            return angle;
        }

        public static int GetAngleFromVector180(Vector3 dir)
        {
            dir = dir.normalized;
            float n = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            int angle = Mathf.RoundToInt(n);

            return angle;
        }

        public static Vector3 ApplyRotationToVector(Vector3 vec, Vector3 vecRotation) => ApplyRotationToVector(vec, GetAngleFromVectorFloat(vecRotation));

        public static Vector3 ApplyRotationToVector(Vector3 vec, float angle) => Quaternion.Euler(0, 0, angle) * vec;
    }
}
