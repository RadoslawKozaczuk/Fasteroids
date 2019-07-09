using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts
{
    public class FPSCounter : MonoBehaviour
    {
        const int BufferSize = 60;

        public int AverageMs { get; private set; }
        [SerializeField] Text _averageFPSLabel;

        // strings are pre-prepared to avoid countless string concatenation and memory pollution
        readonly string[] _stringsFrom00To99 = {
        "00", "01", "02", "03", "04", "05", "06", "07", "08", "09",
        "10", "11", "12", "13", "14", "15", "16", "17", "18", "19",
        "20", "21", "22", "23", "24", "25", "26", "27", "28", "29",
        "30", "31", "32", "33", "34", "35", "36", "37", "38", "39",
        "40", "41", "42", "43", "44", "45", "46", "47", "48", "49",
        "50", "51", "52", "53", "54", "55", "56", "57", "58", "59",
        "60", "61", "62", "63", "64", "65", "66", "67", "68", "69",
        "70", "71", "72", "73", "74", "75", "76", "77", "78", "79",
        "80", "81", "82", "83", "84", "85", "86", "87", "88", "89",
        "90", "91", "92", "93", "94", "95", "96", "97", "98", "99"
    };

        int[] _msBuffer; // we store all values from the last second
        int _msBufferIndex; // index of the currently stored value

        void Update()
        {
            Display(_averageFPSLabel, AverageMs);

            if (_msBuffer == null || _msBuffer.Length != BufferSize)
                InitializeBuffer();

            UpdateBuffer();
            CalculateFPS();
        }

        void Display(Text label, int ms) => label.text = "ms/frame: " + _stringsFrom00To99[Mathf.Clamp(ms, 0, 99)];

        void UpdateBuffer()
        {
            _msBufferIndex++;
            if (_msBufferIndex >= BufferSize)
                _msBufferIndex = 0;

            // it is better to use unscaled delta time because it always gives the time that took to process
            // the last frame delta time on the other hand is affected by the time settings
            _msBuffer[_msBufferIndex] = (int)(Time.unscaledDeltaTime * 1000);
        }

        void InitializeBuffer()
        {
            _msBuffer = new int[BufferSize];
            _msBufferIndex = 0;
        }

        void CalculateFPS()
        {
            int sum = 0;
            for (int i = 0; i < BufferSize; i++)
            {
                int ms = _msBuffer[i];
                sum += ms;
            }

            AverageMs = sum / BufferSize;
        }
    }
}