using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace CrazyTaxi.UI
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }
        
        [Header("Painéis")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject gameplayPanel;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject gameOverPanel;
        
        [Header("Gameplay UI")]
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI deliveriesText;
        [SerializeField] private TextMeshProUGUI passengerStatusText;
        
        [Header("Feedback")]
        [SerializeField] private TextMeshProUGUI feedbackText;
        [SerializeField] private float feedbackDuration = 2f;
        [SerializeField] private GameObject bonusPopupPrefab;
        [SerializeField] private Transform popupContainer;
        
        [Header("Game Over")]
        [SerializeField] private TextMeshProUGUI finalScoreText;
        [SerializeField] private TextMeshProUGUI highScoreText;
        [SerializeField] private TextMeshProUGUI finalDeliveriesText;
        
        [Header("Timer Warning")]
        [SerializeField] private Color normalTimerColor = Color.white;
        [SerializeField] private Color warningTimerColor = Color.red;
        [SerializeField] private float warningThreshold = 15f;
        
        [Header("Animações")]
        [SerializeField] private Animator uiAnimator;
        
        // Referências ao GameManager
        private Core.GameManager gameManager;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }
        
        private void Start()
        {
            gameManager = Core.GameManager.Instance;
            
            if (gameManager != null)
            {
                // Inscrever nos eventos
                gameManager.OnTimeChanged += UpdateTimer;
                gameManager.OnScoreChanged += UpdateScore;
                gameManager.OnPassengerPickedUp += OnPassengerPickedUp;
                gameManager.OnPassengerDelivered += OnPassengerDelivered;
                gameManager.OnGameOver += ShowGameOver;
                gameManager.OnGameStarted += OnGameStarted;
                gameManager.OnTimeWarning += OnTimeWarning;
            }
            
            // Mostrar menu inicial
            ShowMainMenu();
        }
        
        private void OnDestroy()
        {
            if (gameManager != null)
            {
                gameManager.OnTimeChanged -= UpdateTimer;
                gameManager.OnScoreChanged -= UpdateScore;
                gameManager.OnPassengerPickedUp -= OnPassengerPickedUp;
                gameManager.OnPassengerDelivered -= OnPassengerDelivered;
                gameManager.OnGameOver -= ShowGameOver;
                gameManager.OnGameStarted -= OnGameStarted;
                gameManager.OnTimeWarning -= OnTimeWarning;
            }
        }
        
        // Navegação de Painéis
        public void ShowMainMenu()
        {
            SetPanelActive(mainMenuPanel, true);
            SetPanelActive(gameplayPanel, false);
            SetPanelActive(pausePanel, false);
            SetPanelActive(gameOverPanel, false);
        }
        
        public void ShowGameplay()
        {
            SetPanelActive(mainMenuPanel, false);
            SetPanelActive(gameplayPanel, true);
            SetPanelActive(pausePanel, false);
            SetPanelActive(gameOverPanel, false);
        }
        
        public void ShowPause()
        {
            SetPanelActive(pausePanel, true);
        }
        
        public void HidePause()
        {
            SetPanelActive(pausePanel, false);
        }
        
        public void ShowGameOver()
        {
            SetPanelActive(gameplayPanel, false);
            SetPanelActive(gameOverPanel, true);
            
            // Atualizar textos finais
            if (finalScoreText != null)
            {
                finalScoreText.text = $"Pontuação: {gameManager.CurrentScore}";
            }
            if (highScoreText != null)
            {
                int highScore = PlayerPrefs.GetInt("HighScore", 0);
                highScoreText.text = $"Recorde: {highScore}";
            }
            if (finalDeliveriesText != null)
            {
                finalDeliveriesText.text = $"Entregas: {gameManager.DeliveriesCompleted}";
            }
        }
        
        private void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null)
            {
                panel.SetActive(active);
            }
        }
        
        // Atualizações de UI
        private void UpdateTimer(float time)
        {
            if (timerText == null) return;
            
            int minutes = Mathf.FloorToInt(time / 60f);
            int seconds = Mathf.FloorToInt(time % 60f);
            timerText.text = $"{minutes:00}:{seconds:00}";
            
            // Mudar cor se tempo baixo
            timerText.color = time <= warningThreshold ? warningTimerColor : normalTimerColor;
            
            // Escalar quando tempo baixo
            if (time <= warningThreshold)
            {
                float scale = 1f + Mathf.Sin(Time.time * 5f) * 0.1f;
                timerText.transform.localScale = Vector3.one * scale;
            }
            else
            {
                timerText.transform.localScale = Vector3.one;
            }
        }
        
        private void UpdateScore(int score)
        {
            if (scoreText != null)
            {
                scoreText.text = $"${score}";
                
                // Animação de punch
                StartCoroutine(PunchScale(scoreText.transform));
            }
        }
        
        private void UpdateDeliveries()
        {
            if (deliveriesText != null && gameManager != null)
            {
                deliveriesText.text = $"Entregas: {gameManager.DeliveriesCompleted}";
            }
        }
        
        // Eventos do Jogo
        private void OnGameStarted()
        {
            ShowGameplay();
            UpdatePassengerStatus("Procure um passageiro!");
        }
        
        private void OnPassengerPickedUp(Core.Passenger passenger)
        {
            string destName = passenger.AssignedDestination?.LocationName ?? "Destino";
            UpdatePassengerStatus($"Leve para: {destName}");
            ShowFeedback("Passageiro embarcou!", Color.green);
        }
        
        private void OnPassengerDelivered(int score, float bonusTime)
        {
            UpdateDeliveries();
            UpdatePassengerStatus("Procure outro passageiro!");
            
            // Mostrar popup de bônus
            ShowBonusPopup($"+${score}", Color.green);
            ShowBonusPopup($"+{bonusTime:F0}s", Color.cyan);
            
            ShowFeedback("Entrega completa!", Color.yellow);
        }
        
        private void OnTimeWarning(float time)
        {
            ShowFeedback("TEMPO ACABANDO!", Color.red);
        }
        
        private void UpdatePassengerStatus(string status)
        {
            if (passengerStatusText != null)
            {
                passengerStatusText.text = status;
            }
        }
        
        // Feedback Visual
        public void ShowFeedback(string message, Color color)
        {
            if (feedbackText == null) return;
            
            feedbackText.text = message;
            feedbackText.color = color;
            feedbackText.gameObject.SetActive(true);
            
            StopCoroutine(nameof(HideFeedbackCoroutine));
            StartCoroutine(HideFeedbackCoroutine());
        }
        
        private IEnumerator HideFeedbackCoroutine()
        {
            yield return new WaitForSeconds(feedbackDuration);
            
            // Fade out
            float elapsed = 0f;
            Color startColor = feedbackText.color;
            
            while (elapsed < 0.5f)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsed / 0.5f);
                feedbackText.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                yield return null;
            }
            
            feedbackText.gameObject.SetActive(false);
        }
        
        public void ShowBonusPopup(string text, Color color)
        {
            if (bonusPopupPrefab == null || popupContainer == null) return;
            
            GameObject popup = Instantiate(bonusPopupPrefab, popupContainer);
            TextMeshProUGUI popupText = popup.GetComponentInChildren<TextMeshProUGUI>();
            
            if (popupText != null)
            {
                popupText.text = text;
                popupText.color = color;
            }
            
            // Destruir após animação
            Destroy(popup, 1.5f);
        }
        
        private IEnumerator PunchScale(Transform target)
        {
            Vector3 originalScale = Vector3.one;
            Vector3 punchScale = Vector3.one * 1.2f;
            
            float elapsed = 0f;
            float duration = 0.1f;
            
            // Scale up
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                target.localScale = Vector3.Lerp(originalScale, punchScale, elapsed / duration);
                yield return null;
            }
            
            // Scale down
            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                target.localScale = Vector3.Lerp(punchScale, originalScale, elapsed / duration);
                yield return null;
            }
            
            target.localScale = originalScale;
        }
        
        // Botões
        public void OnStartButtonClicked()
        {
            gameManager?.StartGame();
        }
        
        public void OnResumeButtonClicked()
        {
            HidePause();
            gameManager?.TogglePause();
        }
        
        public void OnRestartButtonClicked()
        {
            gameManager?.RestartGame();
        }
        
        public void OnMainMenuButtonClicked()
        {
            gameManager?.ReturnToMenu();
        }
        
        public void OnQuitButtonClicked()
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
    }
}