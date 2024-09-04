using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Scripts
{
    public class PauseMenu : MonoBehaviour
    {
        public bool gameIsPaused;
        public bool delayPause;
        public bool bypassDelay;
        public float delay = 1f;
        public UnityEvent startEvent, resumeEvent;

        public bool GameIsPaused
        {
            get => gameIsPaused;
            set => gameIsPaused = value;
        }
        private void Start()
        {
            startEvent.Invoke();
            DelayPause();
        }

        private void Update()
        {
            if (bypassDelay)
            {
                //yield return new WaitForSeconds(delay);
                delayPause = false;
                GameIsPaused = false;
                Time.timeScale = 1f;
            }
        }

        public void StartResume()
        {
            GameIsPaused = false;
            Time.timeScale = 1f;
        }

        public void StartPause()
        {
            GameIsPaused = true;
            Time.timeScale = 0f;
        }
        public void BypassDelay()
        {
            bypassDelay = true;
        }
        
        public void BypassDelayOff()
        {
            bypassDelay = false;
        }
        
        public void DelayPause()
        {
            if (delayPause)
            {
                StartCoroutine(StartDelay());
            }
        }
        
        IEnumerator StartDelay()
        {
            yield return new WaitForSeconds(delay);
            delayPause = false;
            GameIsPaused = true;
            Time.timeScale = 0f;
        }

    }
}
