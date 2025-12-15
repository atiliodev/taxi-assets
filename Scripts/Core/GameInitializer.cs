using UnityEngine;

namespace CrazyTaxi.Core
{
    public class GameInitializer : MonoBehaviour
    {
        [SerializeField] private GameObject gameManagerPrefab;
        [SerializeField] private GameObject audioManagerPrefab;
        
        private void Awake()
        {
            // Garantir que managers existem
            if (GameManager.Instance == null && gameManagerPrefab != null)
            {
                Instantiate(gameManagerPrefab);
            }
            
            if (Audio.AudioManager.Instance == null && audioManagerPrefab != null)
            {
                Instantiate(audioManagerPrefab);
            }
        }
    }
}