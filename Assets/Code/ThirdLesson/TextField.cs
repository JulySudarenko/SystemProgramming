using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Code.ThirdLesson
{
    internal class TextField : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _textObject;
        [SerializeField] private Scrollbar _scrollbar;
        
        private List<string> messages = new List<string>();

        private void Start()
        {
            _scrollbar.onValueChanged.AddListener((float value)=> UpdateText());
        }

        public void ReceiveMessage(object message)
        {
            messages.Add(message.ToString());
            float value = (messages.Count - 1) * _scrollbar.value;
            _scrollbar.value = Mathf.Clamp(value, 0, 1);
            UpdateText();
        }

        public void UpdateText()
        {
            string text = "";
            int index = (int)(messages.Count * _scrollbar.value);
            for (int i = index; i < messages.Count; i++)
            {
                text += messages[i] + "\n";
            }
            _textObject.text = text;
        }
    }
}
