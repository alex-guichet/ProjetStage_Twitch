using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using UnityEngine;
using UnityEngine.InputSystem;

[Serializable]
public struct CheatParameter
{
    public string[] CheatName;
    public MessageParam[] Parameter;
}

public class CheatManager : Singleton<CheatManager>
{
    [Header("Parameters can be split with ':' if necessary")]
    public CheatParameter[] ControlFKeyArray = new CheatParameter[12];
    public CheatParameter[] ShiftFKeyArray = new CheatParameter[12];
    public CheatParameter[] ControlShiftFKeyArray = new CheatParameter[12];
    [Header("References")]
    public Canvas debugValueScreen;

    public bool infiniteBlissActive { get; private set; } = false;

    public bool vibrationActive { get; private set; } = false;

    private void Execute(CheatParameter cheat)
    {
        for (var i = 0; i < cheat.CheatName.Length; i++)
        {
            switch (cheat.CheatName[i])
            {
                case "message_received":
                    {
                        StartCoroutine(SendMessages(cheat.Parameter));
                        break;
                    }
                
                case "end_step":
                {
                    LevelManager.Instance.EarlyEndStep();
                    break;
                }
                
                case "update_score_recipe":
                {
                    LevelManager.Instance.SaveScoreAndUpdate(cheat.Parameter[0].viewer_id, Int32.Parse(cheat.Parameter[0].viewer_name));
                    break;
                }
                
                
                case "update_score_recipe_player_prefs":
                {
                    PlayerPrefs.SetInt(cheat.Parameter[0].viewer_id, Int32.Parse(cheat.Parameter[0].viewer_name));
                    break;
                }
                

                default:
                    {
                        Debug.LogError($"Cheat not found: {cheat.CheatName}");
                        break;
                    }
            }
        }
    }

    private void OnValidate()
    {
        /* -- Handle cheats without parameters
        foreach( CheatParameter cheat_parameter in ControlFKeyArray )
        {
            Debug.Assert( cheat_parameter.CheatName.Length != cheat_parameter.Parameter.Length, $"{cheat_parameter.CheatName} doesn't have the same number of parameter" );
        }
        */
    }

    /*
    public override void Awake()
    {
        base.Awake();

        debugValueScreen.enabled = false;
#if UNITY_EDITOR
        vibrationActive = false;
#else
        vibrationActive = true;
#endif
    }
    */

    private void Update()
    {
#if !UNITY_SWITCH
        bool is_shifted = Keyboard.current.shiftKey.isPressed;
        bool is_controled = Keyboard.current.ctrlKey.isPressed;
        //bool is_alted = Keyboard.current.altKey.isPressed;

        for (int i = (int)Key.F1; i <= (int)Key.F12; i++)
        {
            if (Keyboard.current[(Key)i].wasPressedThisFrame)
            {
                var cheat_index = i - (int)Key.F1;

                if (is_controled && is_shifted)
                {
                    Execute(ControlShiftFKeyArray[cheat_index]);
                }

                if (is_controled)
                {
                    Execute(ControlFKeyArray[cheat_index]);
                }

                if (is_shifted)
                {
                    Execute(ShiftFKeyArray[cheat_index]);
                }
            }
        }
#endif
    }
    IEnumerator SendMessages(MessageParam[] parameters)
    {
        foreach(MessageParam parameter in parameters)
        {
            TranslatorManager.Instance.TranslateAndSend(parameter.viewer_id,parameter.viewer_name, parameter.sent_text);
            yield return new WaitForEndOfFrame();
        }
    }
}

[System.Serializable]
public class MessageParam
{
    public string viewer_id;
    public string viewer_name;
    public string sent_text;
}


