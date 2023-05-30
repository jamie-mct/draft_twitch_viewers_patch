using System;
using UnityEngine;
using UnityEngine.UI;

namespace DraftTwitchViewers
{
    public class TextTyper : MonoBehaviour
    {
        public Text Target;
        public string Cursor;
        public string FullText
        {
            get { return fullText; }
            set
            {
                fullText = value;
                //currentCharacter = 0;
                //timeSinceLastCharacter = 0f;
                //timeSinceLastBlink = 0f;
                //Target.text = Cursor;
                //isBlinkOn = true;
            }
        }
        private string fullText = "";
        //private int currentCharacter = 0;

        //private float timeSinceLastCharacter = 0f;
        private const float charactersPerSecond = 60f;
        //private const float timeBetweenCharacters = 1f / charactersPerSecond;

        //private float timeBetweenBlinks = 0.75f;
        //private float timeSinceLastBlink = 0f;
        //private bool isBlinkOn = false;

#if false
        void Update()
        {
            if (currentCharacter < fullText.Length)
            {
                timeSinceLastCharacter += Time.deltaTime;
                if (timeSinceLastCharacter >= timeBetweenCharacters)
                {
                    int charactersToAdd = (int)(timeSinceLastCharacter / timeBetweenCharacters);
                    timeSinceLastCharacter -= timeBetweenCharacters * charactersToAdd;
                    currentCharacter = Math.Min(currentCharacter + charactersToAdd, fullText.Length);
                    Target.text = fullText.Substring(0, currentCharacter) + Cursor;
                }

                return;
            }

            if (timeSinceLastBlink < timeBetweenBlinks)
            {
                timeSinceLastBlink += Time.deltaTime;
                if (timeSinceLastBlink >= timeBetweenBlinks)
                {
                    timeSinceLastBlink -= timeBetweenBlinks;
                    isBlinkOn = !isBlinkOn;
                    Target.text = fullText.Substring(0, currentCharacter) + (isBlinkOn ? Cursor : "");
                }
            }
        }
 #endif
   }
}
