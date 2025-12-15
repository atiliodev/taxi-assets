using UnityEngine;

namespace CrazyTaxi.UI
{
    public class DirectionArrow : MonoBehaviour
    {
        [Header("Configurações")]
        [SerializeField] private float rotationSpeed = 5f;
        [SerializeField] private bool use3DArrow = false;
        
        [Header("Referências")]
        [SerializeField] private Transform playerTransform;
        [SerializeField] private RectTransform uiArrow; // Para seta 2D
        [SerializeField] private Transform worldArrow;  // Para seta 3D
        
        [Header("Offset 3D")]
        [SerializeField] private Vector3 arrowOffset = new Vector3(0, 3f, 2f);
        
        // Referências
        private Core.GameManager gameManager;
        private Transform currentTarget;
        
        private void Start()
        {
            gameManager = Core.GameManager.Instance;
            
            if (playerTransform == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    playerTransform = player.transform;
                }
            }
        }
        
        private void Update()
        {
            UpdateTarget();
            
            if (currentTarget == null || playerTransform == null)
            {
                HideArrow();
                return;
            }
            
            ShowArrow();
            
            if (use3DArrow)
            {
                Update3DArrow();
            }
            else
            {
                Update2DArrow();
            }
        }
        
        private void UpdateTarget()
        {
            if (gameManager == null) return;
            
            // Determinar alvo baseado no estado
            if (gameManager.CurrentPassengerState == Core.PassengerState.WaitingForPassenger)
            {
                // Apontar para passageiro atual
                currentTarget = gameManager.CurrentPassenger?.transform;
            }
            else if (gameManager.CurrentPassengerState == Core.PassengerState.DeliveringPassenger)
            {
                // Apontar para destino
                currentTarget = gameManager.CurrentDestination?.transform;
            }
            else
            {
                currentTarget = null;
            }
        }
        
        private void Update2DArrow()
        {
            if (uiArrow == null) return;
            
            // Calcular direção no plano XZ
            Vector3 directionToTarget = currentTarget.position - playerTransform.position;
            directionToTarget.y = 0;
            
            // Converter direção do mundo para direção relativa ao jogador
            Vector3 playerForward = playerTransform.forward;
            playerForward.y = 0;
            
            float angle = Vector3.SignedAngle(playerForward, directionToTarget, Vector3.up);
            
            // Aplicar rotação suave
            Quaternion targetRotation = Quaternion.Euler(0, 0, -angle);
            uiArrow.rotation = Quaternion.Slerp(
                uiArrow.rotation, 
                targetRotation, 
                Time.deltaTime * rotationSpeed
            );
        }
        
        private void Update3DArrow()
        {
            if (worldArrow == null) return;
            
            // Posicionar seta acima do jogador
            worldArrow.position = playerTransform.position + 
                                  playerTransform.TransformDirection(arrowOffset);
            
            // Calcular direção para o alvo
            Vector3 directionToTarget = currentTarget.position - worldArrow.position;
            directionToTarget.y = 0; // Manter no plano horizontal
            
            if (directionToTarget.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                worldArrow.rotation = Quaternion.Slerp(
                    worldArrow.rotation, 
                    targetRotation, 
                    Time.deltaTime * rotationSpeed
                );
            }
        }
        
        private void ShowArrow()
        {
            if (use3DArrow && worldArrow != null)
            {
                worldArrow.gameObject.SetActive(true);
            }
            else if (uiArrow != null)
            {
                uiArrow.gameObject.SetActive(true);
            }
        }
        
        private void HideArrow()
        {
            if (worldArrow != null) worldArrow.gameObject.SetActive(false);
            if (uiArrow != null) uiArrow.gameObject.SetActive(false);
        }
        
        // Método público para obter distância até o alvo
        public float GetDistanceToTarget()
        {
            if (currentTarget == null || playerTransform == null) return -1f;
            return Vector3.Distance(playerTransform.position, currentTarget.position);
        }
    }
}