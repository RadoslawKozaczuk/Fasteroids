using System.Collections;
using TMPro;
using UnityEngine;

namespace Assets.Script
{
    class TextReveal : MonoBehaviour
    {
        public TextMeshProUGUI Message;

        void Start()
        {
            ShowNewMessage(
                    "Year 2098, due to the malfunction of the turbo plasma reactor a portal to the outer space has been opened. \n" 
                    + "Countless amount of Aliens poured down on Earth spreading death and terror everywhere. \n"
                    + "You are the only survivor and blah, blah, blah...");
        }

        public void HideMessage()
        {
            Message.text = "";
            Message.ForceMeshUpdate(true);
        }

        public void ShowNewMessage(string text)
        {
            Message.text = text;
            Message.ForceMeshUpdate(true);
            StopAllCoroutines();
            StartCoroutine("Reveal");
        }

        /// <summary>
        /// Reveals the text and after 5 seconds clears it up
        /// </summary>
        /// <returns></returns>
        IEnumerator Reveal()
        {
            int totalVisibleCharacters = Message.textInfo.characterCount;
            int counter = 0;

            while (true)
            {
                int visibleCount = counter % (totalVisibleCharacters + 1);

                Message.maxVisibleCharacters = visibleCount;

                if (visibleCount >= totalVisibleCharacters)
                    break;

                counter++;

                yield return new WaitForSeconds(0.05f);
            }

            yield return new WaitForSeconds(5f);

            Message.text = "";
        }
    }
}
