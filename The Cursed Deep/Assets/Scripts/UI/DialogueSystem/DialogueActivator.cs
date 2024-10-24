using UI.DialogueSystem;
using UnityEngine;
using UnityEngine.Events;

public class DialogueActivator : MonoBehaviour, IInteractable
{
    public ID id;
    [SerializeField] private GameAction action;
    [SerializeField] private DialogueData dialogueData;
    public PlayerDialogueActivator playerActivator;
    [SerializeField] private UnityEvent onInteract;
    
    public void UpdateDialogueObject(DialogueData dData)
    {
        this.dialogueData = dData;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out IDBehavior idBehavior) && idBehavior.id == id)
        {
            if (other.TryGetComponent(out PlayerDialogueActivator player))
            {
                
                player.interactable = this;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out IDBehavior idBehavior) && idBehavior.id == id)
        {
            if (other.TryGetComponent(out PlayerDialogueActivator player))
            {
                player.dialogueUI.typewriterEffect.Stop();
                player.dialogueUI.CloseDialogueBox(dialogueData);
                player.interactable = null;
            }
        }
    }

    public void OnTrigger()
    {
        if (playerActivator)
        {
            
            playerActivator.interactable = this;
            onInteract.Invoke();
        }
    }
    
    public void OffTrigger()
    {
        if (!playerActivator) return;
        playerActivator.interactable = null;
    }

    public void Interact(PlayerDialogueActivator player)
    {
        foreach (DialogueResponseEvents responseEvents in GetComponents<DialogueResponseEvents>())
        {
            if (responseEvents.DialogueData)
            {
                player.dialogueUI.AddResponseEvents(responseEvents.Events);
                break;
            }
        }
        player.dialogueUI.ShowDialogue(dialogueData);
        dialogueData.DialogueEvent(action);
    }
}
