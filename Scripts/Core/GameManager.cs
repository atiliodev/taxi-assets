using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using CrazyTaxi.Audio;

namespace CrazyTaxi.Core
{
    public enum GameState
    {
        Menu,
        Playing,
        Paused,
        GameOver
    }
    
    public enum PassengerState
    {
        WaitingForPassenger,
        DeliveringPassenger
    }
    
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        
        [Header("Configurações de Tempo")]
        [SerializeField] private float startingTime = 60f;
        [SerializeField] private float maxTime = 120f;
        [SerializeField] private float timeWarningThreshold = 15f;
        
        [Header("Configurações de Pontuação")]
        [SerializeField] private int baseDeliveryScore = 100;
        [SerializeField] private float bonusTimeMultiplier = 2f;
        
        [Header("=== CONFIGURAÇÕES DE PASSAGEIROS ===")]
        [Tooltip("Quantos passageiros podem estar ativos ao mesmo tempo")]
        [SerializeField] private int maxActivePassengers = 5;
        
        [Tooltip("Mínimo de passageiros que devem estar sempre disponíveis")]
        [SerializeField] private int minActivePassengers = 3;
        
        [Tooltip("Intervalo em segundos para verificar e spawnar novos passageiros")]
        [SerializeField] private float spawnCheckInterval = 2f;
        
        [Tooltip("Spawnar passageiro imediatamente quando um é coletado")]
        [SerializeField] private bool instantRespawn = true;
        
        [Header("Referências")]
        [SerializeField] private Transform playerTransform;
        [SerializeField] private List<Passenger> allPassengers = new List<Passenger>();
        [SerializeField] private List<Destination> allDestinations = new List<Destination>();
        
        // Estado do Jogo
        private GameState currentGameState = GameState.Menu;
        private PassengerState passengerState = PassengerState.WaitingForPassenger;
        
        // Variáveis de Jogo
        private float currentTime;
        private int currentScore;
        private int deliveriesCompleted;
        private Passenger currentPassenger; // Passageiro que está NO CARRO
        private Destination currentDestination;
        
        // Lista de passageiros atualmente ESPERANDO na rua
        private List<Passenger> activeWaitingPassengers = new List<Passenger>();
        
        // Timer para spawn
        private float spawnTimer;
        
        // Eventos
        public System.Action<float> OnTimeChanged;
        public System.Action<int> OnScoreChanged;
        public System.Action<Passenger> OnPassengerPickedUp;
        public System.Action<int, float> OnPassengerDelivered;
        public System.Action OnGameOver;
        public System.Action OnGameStarted;
        public System.Action<float> OnTimeWarning;
        
        // Propriedades públicas
        public GameState CurrentGameState => currentGameState;
        public PassengerState CurrentPassengerState => passengerState;
        public float CurrentTime => currentTime;
        public int CurrentScore => currentScore;
        public int DeliveriesCompleted => deliveriesCompleted;
        public Passenger CurrentPassenger => currentPassenger;
        public Destination CurrentDestination => currentDestination;
        public Transform PlayerTransform => playerTransform;
        public int ActivePassengersCount => activeWaitingPassengers.Count;
        
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
            
            if (playerTransform == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    playerTransform = player.transform;
                }
            }
        }
        
        private void Start()
        {
            // Auto-descobrir passageiros e destinos
            if (allPassengers.Count == 0)
            {
                allPassengers.AddRange(FindObjectsOfType<Passenger>());
            }
            if (allDestinations.Count == 0)
            {
                allDestinations.AddRange(FindObjectsOfType<Destination>());
            }
            
            // Desativar todos no início
            DeactivateAllPassengersAndDestinations();
            
            Debug.Log($"[GameManager] Encontrados {allPassengers.Count} passageiros e {allDestinations.Count} destinos");
        }
        
        private void Update()
        {
            if (currentGameState != GameState.Playing) return;
            
            // Atualizar timer do jogo
            currentTime -= Time.deltaTime;
            OnTimeChanged?.Invoke(currentTime);
            
            // Verificar aviso de tempo
            if (currentTime <= timeWarningThreshold && currentTime > timeWarningThreshold - 0.1f)
            {
                OnTimeWarning?.Invoke(currentTime);
            }
            
            // Verificar game over
            if (currentTime <= 0)
            {
                currentTime = 0;
                EndGame();
            }
            
            // === SISTEMA DE SPAWN FREQUENTE ===
            UpdatePassengerSpawning();
            
            // Verificar passageiros que expiraram (paciência acabou)
            CheckExpiredPassengers();
            
            // Pausar
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                TogglePause();
            }
        }
        
        /// <summary>
        /// Verifica periodicamente se precisa spawnar mais passageiros
        /// </summary>
        private void UpdatePassengerSpawning()
        {
            spawnTimer += Time.deltaTime;
            
            if (spawnTimer >= spawnCheckInterval)
            {
                spawnTimer = 0f;
                
                // Se há menos passageiros ativos que o mínimo, spawnar mais
                int currentActive = activeWaitingPassengers.Count;
                
                if (currentActive < minActivePassengers)
                {
                    int toSpawn = minActivePassengers - currentActive;
                    
                    for (int i = 0; i < toSpawn; i++)
                    {
                        SpawnNewPassenger();
                    }
                    
                    Debug.Log($"[GameManager] Spawnados {toSpawn} passageiros. Total ativo: {activeWaitingPassengers.Count}");
                }
            }
        }
        
        /// <summary>
        /// Verifica se algum passageiro esperando perdeu a paciência
        /// </summary>
        private void CheckExpiredPassengers()
        {
            // Criar lista temporária para remover
            List<Passenger> expiredPassengers = new List<Passenger>();
            
            foreach (var passenger in activeWaitingPassengers)
            {
                if (passenger == null) continue;
                
                // Se a paciência acabou
                if (passenger.RemainingPatience <= 0)
                {
                    expiredPassengers.Add(passenger);
                }
            }
            
            // Remover expirados e spawnar novos
            foreach (var expired in expiredPassengers)
            {
                expired.Deactivate();
                activeWaitingPassengers.Remove(expired);
                
                Debug.Log($"[GameManager] Passageiro expirou. Restam: {activeWaitingPassengers.Count}");
                
                // Spawnar substituto imediatamente
                if (instantRespawn)
                {
                    SpawnNewPassenger();
                }
            }
        }
        
        public void StartGame()
        {
            currentGameState = GameState.Playing;
            currentTime = startingTime;
            currentScore = 0;
            deliveriesCompleted = 0;
            passengerState = PassengerState.WaitingForPassenger;
            currentPassenger = null;
            currentDestination = null;
            
            // Limpar lista de ativos
            activeWaitingPassengers.Clear();
            
            // Resetar todos os passageiros
            foreach (var p in allPassengers)
            {
                p.Reset();
            }
            foreach (var d in allDestinations)
            {
                d.Deactivate();
            }
            
            Time.timeScale = 1f;
            
            OnGameStarted?.Invoke();
            OnTimeChanged?.Invoke(currentTime);
            OnScoreChanged?.Invoke(currentScore);
            
            // === SPAWNAR MÚLTIPLOS PASSAGEIROS NO INÍCIO ===
            for (int i = 0; i < maxActivePassengers; i++)
            {
                SpawnNewPassenger();
            }
            
            Debug.Log($"[GameManager] Jogo iniciado com {activeWaitingPassengers.Count} passageiros ativos");
        }
        
        public void EndGame()
        {
            currentGameState = GameState.GameOver;
            OnGameOver?.Invoke();
            
            int highScore = PlayerPrefs.GetInt("HighScore", 0);
            if (currentScore > highScore)
            {
                PlayerPrefs.SetInt("HighScore", currentScore);
                PlayerPrefs.Save();
            }
        }
        
        public void TogglePause()
        {
            if (currentGameState == GameState.Playing)
            {
                currentGameState = GameState.Paused;
                Time.timeScale = 0f;
            }
            else if (currentGameState == GameState.Paused)
            {
                currentGameState = GameState.Playing;
                Time.timeScale = 1f;
            }
        }
        
        public void RestartGame()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        
        public void ReturnToMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        }
        
        // ==========================================
        // SISTEMA DE PASSAGEIROS MELHORADO
        // ==========================================
        
        /// <summary>
        /// Chamado quando o jogador coleta QUALQUER passageiro ativo
        /// </summary>
        public void PickUpPassenger(Passenger passenger)
        {
            // Só pode pegar se não está carregando ninguém
            if (passengerState != PassengerState.WaitingForPassenger) 
            {
                Debug.Log("[GameManager] Já está carregando um passageiro!");
                return;
            }
            
            // Verificar se o passageiro está na lista de ativos
            if (!activeWaitingPassengers.Contains(passenger))
            {
                Debug.Log("[GameManager] Este passageiro não está ativo!");
                return;
            }
            
            // Remover da lista de esperando
            activeWaitingPassengers.Remove(passenger);
            
            // Definir como passageiro atual (no carro)
            currentPassenger = passenger;
            passengerState = PassengerState.DeliveringPassenger;
            
            // Desativar visualmente
            passenger.OnPickedUp();
            
            // Ativar destino correspondente
            currentDestination = passenger.AssignedDestination;
            if (currentDestination != null)
            {
                currentDestination.Activate();
            }
            
            OnPassengerPickedUp?.Invoke(passenger);
            
            // Tocar som
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX("PickUp");
            }
            
            // === SPAWNAR NOVO PASSAGEIRO IMEDIATAMENTE ===
            if (instantRespawn && activeWaitingPassengers.Count < maxActivePassengers)
            {
                SpawnNewPassenger();
            }
            
            Debug.Log($"[GameManager] Passageiro coletado! Destino: {currentDestination?.LocationName}. Esperando: {activeWaitingPassengers.Count}");
        }
        
        /// <summary>
        /// Chamado quando o jogador entrega o passageiro no destino
        /// </summary>
        public void DeliverPassenger(Destination destination)
        {
            if (passengerState != PassengerState.DeliveringPassenger) return;
            if (destination != currentDestination) return;
            
            // Calcular pontuação
            int score = CalculateDeliveryScore();
            float bonusTime = CalculateBonusTime();
            
            // Aplicar recompensas
            currentScore += score;
            currentTime = Mathf.Min(currentTime + bonusTime, maxTime);
            deliveriesCompleted++;
            
            OnScoreChanged?.Invoke(currentScore);
            OnTimeChanged?.Invoke(currentTime);
            OnPassengerDelivered?.Invoke(score, bonusTime);
            
            // Desativar destino
            destination.Deactivate();
            
            // Resetar passageiro entregue para poder ser reutilizado
            if (currentPassenger != null)
            {
                currentPassenger.Reset();
            }
            
            currentDestination = null;
            currentPassenger = null;
            
            // Mudar estado - pronto para pegar outro
            passengerState = PassengerState.WaitingForPassenger;
            
            // Tocar som
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX("Delivery");
            }
            
            // === GARANTIR QUE HÁ PASSAGEIROS ESPERANDO ===
            if (activeWaitingPassengers.Count < minActivePassengers)
            {
                int toSpawn = minActivePassengers - activeWaitingPassengers.Count;
                for (int i = 0; i < toSpawn; i++)
                {
                    SpawnNewPassenger();
                }
            }
            
            Debug.Log($"[GameManager] Entrega completa! Score: {score}, Tempo: +{bonusTime}s. Esperando: {activeWaitingPassengers.Count}");
        }
        
        private int CalculateDeliveryScore()
        {
            int score = baseDeliveryScore;
            
            if (currentPassenger != null)
            {
                float timeBonus = currentPassenger.RemainingPatience / currentPassenger.MaxPatience;
                score += Mathf.RoundToInt(score * timeBonus);
            }
            
            return score;
        }
        
        private float CalculateBonusTime()
        {
            float bonus = 10f;
            
            if (currentPassenger != null)
            {
                float efficiencyBonus = currentPassenger.RemainingPatience / currentPassenger.MaxPatience;
                bonus += 5f * efficiencyBonus;
            }
            
            return bonus;
        }
        
        /// <summary>
        /// Spawna um novo passageiro em um local disponível
        /// </summary>
        private void SpawnNewPassenger()
        {
            // Verificar limite máximo
            if (activeWaitingPassengers.Count >= maxActivePassengers)
            {
                return;
            }
            
            // Encontrar passageiros não ativos e não sendo usados
            List<Passenger> availablePassengers = allPassengers.FindAll(p => 
                !p.IsActive && 
                !p.IsPickedUp && 
                !activeWaitingPassengers.Contains(p) &&
                p != currentPassenger
            );
            
            // Se não há passageiros disponíveis, resetar alguns
            if (availablePassengers.Count == 0)
            {
                // Pegar passageiros que não estão na lista ativa
                foreach (var p in allPassengers)
                {
                    if (!activeWaitingPassengers.Contains(p) && p != currentPassenger)
                    {
                        p.Reset();
                        availablePassengers.Add(p);
                    }
                }
            }
            
            if (availablePassengers.Count == 0)
            {
                Debug.LogWarning("[GameManager] Nenhum passageiro disponível para spawn!");
                return;
            }
            
            // Escolher passageiro aleatório
            int randomIndex = Random.Range(0, availablePassengers.Count);
            Passenger newPassenger = availablePassengers[randomIndex];
            
            // Encontrar destino disponível (não usado por outros passageiros ativos)
            List<Destination> usedDestinations = new List<Destination>();
            foreach (var activeP in activeWaitingPassengers)
            {
                if (activeP.AssignedDestination != null)
                {
                    usedDestinations.Add(activeP.AssignedDestination);
                }
            }
            if (currentDestination != null)
            {
                usedDestinations.Add(currentDestination);
            }
            
            List<Destination> availableDestinations = allDestinations.FindAll(d => !usedDestinations.Contains(d));
            
            // Se não há destinos únicos disponíveis, usar qualquer um
            if (availableDestinations.Count == 0)
            {
                availableDestinations = new List<Destination>(allDestinations);
            }
            
            if (availableDestinations.Count > 0)
            {
                int destIndex = Random.Range(0, availableDestinations.Count);
                newPassenger.AssignDestination(availableDestinations[destIndex]);
            }
            
            // Ativar passageiro
            newPassenger.Activate();
            
            // Adicionar à lista de ativos esperando
            activeWaitingPassengers.Add(newPassenger);
        }
        
        private void DeactivateAllPassengersAndDestinations()
        {
            foreach (var passenger in allPassengers)
            {
                passenger.Deactivate();
            }
            foreach (var destination in allDestinations)
            {
                destination.Deactivate();
            }
            activeWaitingPassengers.Clear();
        }
        
        public void AddTime(float seconds)
        {
            currentTime = Mathf.Min(currentTime + seconds, maxTime);
            OnTimeChanged?.Invoke(currentTime);
        }
        
        public void AddScore(int points)
        {
            currentScore += points;
            OnScoreChanged?.Invoke(currentScore);
        }
        
        /// <summary>
        /// Força spawn de passageiros até o máximo (pode ser chamado externamente)
        /// </summary>
        public void ForceSpawnPassengers()
        {
            int toSpawn = maxActivePassengers - activeWaitingPassengers.Count;
            for (int i = 0; i < toSpawn; i++)
            {
                SpawnNewPassenger();
            }
        }
        
        /// <summary>
        /// Retorna o passageiro mais próximo do jogador (para UI, etc)
        /// </summary>
        public Passenger GetNearestPassenger()
        {
            if (playerTransform == null || activeWaitingPassengers.Count == 0) return null;
            
            Passenger nearest = null;
            float nearestDistance = float.MaxValue;
            
            foreach (var passenger in activeWaitingPassengers)
            {
                if (passenger == null || !passenger.IsActive) continue;
                
                float distance = Vector3.Distance(playerTransform.position, passenger.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = passenger;
                }
            }
            
            return nearest;
        }
    }
}