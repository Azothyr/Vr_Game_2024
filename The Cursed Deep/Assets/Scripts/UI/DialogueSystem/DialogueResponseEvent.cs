using UnityEngine;
using System;
using System.Collections.Generic;
using UI.DialogueSystem;

public class DialogueResponseEvents : MonoBehaviour
{
    [SerializeField] private DialogueData dialogueData;
    [SerializeField] private ResponseEvent[] events;
    
    public DialogueData DialogueData => dialogueData;
    public ResponseEvent[] Events => events;
    
    
    public void UpdateResponseEvents(DialogueData dData)
    {
        this.dialogueData = dData;
        OnValidate();
    }

    public void OnValidate()
    {
        if (dialogueData == null) return;
        if (dialogueData.Responses == null) return;
        if (events != null && events.Length == dialogueData.Responses.Length) return;

        if (events == null)
        {
            events = new ResponseEvent[dialogueData.Responses.Length];
        }
        else
        {
            Array.Resize(ref events, dialogueData.Responses.Length);
        }
        
        for (int i = 0; i < dialogueData.Responses.Length; i++)
        {
            Response response = dialogueData.Responses[i];

            if (events[i] != null)
            {
                events[i].name = response.ResponseText;
                continue;
            }

            events[i] = new ResponseEvent() {name = response.ResponseText};
        }
    }
}