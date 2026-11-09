using System;
using UnityEngine;

namespace ZUI.Dialogue
{
    /// <summary>
    /// Represents a single line of spoken dialogue in a conversation sequence.
    /// </summary>
    public class DialogueLine
    {
        /// <summary>
        /// Name of the speaker displayed before the text (e.g. "Telerick", "Jimmy").
        /// If null or empty, displays as a narrator / ambient line without a header.
        /// </summary>
        public string Speaker { get; set; }

        /// <summary>
        /// The subtitle message body text.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Color used for the speaker's name tag in the subtitle bubble.
        /// </summary>
        public Color SpeakerColor { get; set; } = new Color(1f, 0.835f, 0.31f); // Default #FFD54F

        /// <summary>
        /// Duration in seconds this line remains on screen.
        /// If 0 or less, auto-calculated from audio clip duration or word count.
        /// </summary>
        public float Duration { get; set; } = 0f;

        /// <summary>
        /// Pause in seconds before this line starts displaying (default: 0.2s).
        /// </summary>
        public float DelayBefore { get; set; } = 0.2f;

        /// <summary>
        /// Optional audio voice line clip to play alongside this subtitle.
        /// </summary>
        public AudioClip Audio { get; set; }

        /// <summary>
        /// Optional name of an audio clip to find dynamically in memory if Audio is not directly assigned.
        /// </summary>
        public string AudioName { get; set; }

        /// <summary>
        /// Playback volume for the voice line (0.0 to 1.0).
        /// </summary>
        public float AudioVolume { get; set; } = 1.0f;

        /// <summary>
        /// Optional callback triggered when this specific line begins displaying.
        /// </summary>
        public Action OnLineStart { get; set; }

        public DialogueLine() { }

        public DialogueLine(string speaker, string message, Color speakerColor, float duration = 0f, float delayBefore = 0.2f, AudioClip audio = null, string audioName = null)
        {
            Speaker = speaker;
            Message = message;
            SpeakerColor = speakerColor;
            Duration = duration;
            DelayBefore = delayBefore;
            Audio = audio;
            AudioName = audioName;
        }

        public DialogueLine(string speaker, string message, string speakerHex = "#FFD54F", float duration = 0f, float delayBefore = 0.2f, AudioClip audio = null, string audioName = null)
        {
            Speaker = speaker;
            Message = message;
            Duration = duration;
            DelayBefore = delayBefore;
            Audio = audio;
            AudioName = audioName;

            Color color = new Color(1f, 0.835f, 0.31f);
            if (!string.IsNullOrEmpty(speakerHex))
            {
                string hex = speakerHex.StartsWith("#") ? speakerHex : "#" + speakerHex;
                if (!ColorUtility.TryParseHtmlString(hex, out color))
                {
                    color = new Color(1f, 0.835f, 0.31f);
                }
            }
            SpeakerColor = color;
        }
    }
}
