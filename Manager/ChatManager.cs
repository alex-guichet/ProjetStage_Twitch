using UnityEngine;
using TMPro;

public class ChatManager : MonoBehaviour
{
    private TMP_InputField _inputField;

    private void SendTextToTranslator()
    {
        if(_inputField.text != string.Empty)
        {
            TranslatorManager.Instance.TranslateAndSend("00001","ChatBox", _inputField.text);
        }
    }

    private void Awake()
    {
        _inputField = GetComponent<TMP_InputField>();
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Return) && _inputField.text != string.Empty)
        {
            SendTextToTranslator();
        }
    }
}
