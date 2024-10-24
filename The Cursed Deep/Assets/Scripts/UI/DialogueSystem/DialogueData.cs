using UnityEngine;
using UnityEngine.Events;

namespace UI.DialogueSystem
{
    [CreateAssetMenu(menuName = "Dialogue/DialogueData")]
    public class DialogueData : ScriptableObject
    {
        [SerializeField] private string dialogueName;
        [SerializeField] private GameAction action;
        [SerializeField] [TextArea] private string[] dialogue;
        [SerializeField] private Response[] responses;
        [SerializeField] private UnityEvent onTrigger;
        public string[] Dialogue => dialogue;

        public bool hasResponses => responses is { Length: > 0 };
        public Response[] Responses => responses;

        private void OnEnable()
        {
            if (action == null) return;
            action.Raise += DialogueEvent;
        
        }

        public void DialogueEvent(GameAction _)
        {
            onTrigger.Invoke();
        }
        
    }
}