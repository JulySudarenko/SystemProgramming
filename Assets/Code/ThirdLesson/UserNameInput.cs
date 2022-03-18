using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Code.ThirdLesson
{
    internal class UserNameInput : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private TMP_InputField _inputField;
        [SerializeField] private Button _button;
        [SerializeField] private Client _client;

        private void Start()
        {
            _inputField.onEndEdit.AddListener((text) => SendMessage());
            _button.onClick.AddListener(() => SendMessage());
            _panel.SetActive(false);
            _client.OnConnection += Activate;
        }

        private void Activate()
        {
            _panel.SetActive(true);
        }
        
        private void SendMessage()
        {
            _client.SendMessage(_inputField.text);
            _inputField.text = "";
            _panel.SetActive(false);
        }
    }
}
