using UI.DialogueSystem;
using UnityEngine;

public class DialoguePurcheseHandler : MonoBehaviour
{
    [SerializeField] private IntData playerCoins;
    [SerializeField] private int cost;
    
    [SerializeField] private DialogueData successDialogue, failDialogue;
    [SerializeField] private DialogueResponseEvents response;
    [SerializeField] public DialogueUI dialogueUI;
    
    
    public void Purchase()
    {
        if (playerCoins >= cost)
        {
            Debug.Log($"- {cost} coins");
            ContinueDialogue(successDialogue);
            
        }
        else
        {
            Debug.Log("Not enough coins");
            ContinueDialogue(failDialogue);
        }
    }
    
    private void ContinueDialogue(DialogueData dialogueData)
    {
        Debug.Log("Continuing dialogue");
        response.UpdateResponseEvents(dialogueData);
        //dialogueUI.ShowDialogue(dialogueData);
    }
}