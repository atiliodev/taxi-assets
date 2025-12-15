using UnityEngine;

namespace CrazyTaxi.Core
{
    public class Passenger : MonoBehaviour
    {
        [Header("Configurações")]
        [SerializeField] private float maxPatience = 30f;
        [SerializeField] private int baseValue = 100;
        [SerializeField] private float pickupRadius = 5f;
        
        [Header("Referências Visuais")]
        [SerializeField] private GameObject visualModel;
        [SerializeField] private GameObject markerObject;
        [SerializeField] private ParticleSystem pickupEffect;
        
        [Header("Cores do Marcador")]
        [SerializeField] private Color normalColor = Color.green;
        [SerializeField] private Color urgentColor = Color.red;
        [SerializeField] private float urgentThreshold = 0.3f;
        
        // Estado
        private bool isActive;
        private bool isPickedUp;
        private float currentPatience;
        private Destination assignedDestination;
        
        // Componentes
        private Renderer markerRenderer;
        private SphereCollider triggerCollider;
        
        // Propriedades
        public bool IsActive => isActive;
        public bool IsPickedUp => isPickedUp;
        public float RemainingPatience => currentPatience;
        public float MaxPatience => maxPatience;
        public int BaseValue => baseValue;
        public Destination AssignedDestination => assignedDestination;
        
        private void Awake()
        {
            // Configurar collider
            triggerCollider = GetComponent<SphereCollider>();
            if (triggerCollider == null)
            {
                triggerCollider = gameObject.AddComponent<SphereCollider>();
            }
            triggerCollider.isTrigger = true;
            triggerCollider.radius = pickupRadius;
            
            // Obter renderer do marcador
            if (markerObject != null)
            {
                markerRenderer = markerObject.GetComponent<Renderer>();
            }
        }
        
        private void Update()
        {
            if (!isActive || isPickedUp) return;
            
            // Atualizar paciência
            currentPatience -= Time.deltaTime;
            
            // Atualizar cor do marcador
            UpdateMarkerColor();
            
            // Animar marcador (flutuação)
            AnimateMarker();
            
            // Se paciência acabou, desativar e spawnar outro
            if (currentPatience <= 0)
            {
                Deactivate();
                // GameManager irá spawnar outro
            }
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (!isActive || isPickedUp) return;
            
            if (other.CompareTag("Player"))
            {
                GameManager.Instance?.PickUpPassenger(this);
            }
        }
        
        public void Activate()
        {
            isActive = true;
            isPickedUp = false;
            currentPatience = maxPatience;
            
            if (visualModel != null) visualModel.SetActive(true);
            if (markerObject != null) markerObject.SetActive(true);
            
            gameObject.SetActive(true);
        }
        
        public void Deactivate()
        {
            isActive = false;
            isPickedUp = false;
            
            if (visualModel != null) visualModel.SetActive(false);
            if (markerObject != null) markerObject.SetActive(false);
        }
        
        public void OnPickedUp()
        {
            isPickedUp = true;
            
            if (visualModel != null) visualModel.SetActive(false);
            if (markerObject != null) markerObject.SetActive(false);
            
            // Efeito de partículas
            if (pickupEffect != null)
            {
                pickupEffect.Play();
            }
        }
        
        public void AssignDestination(Destination destination)
        {
            assignedDestination = destination;
        }
        
        public void Reset()
        {
            isActive = false;
            isPickedUp = false;
            currentPatience = maxPatience;
            assignedDestination = null;
            
            if (visualModel != null) visualModel.SetActive(false);
            if (markerObject != null) markerObject.SetActive(false);
        }
        
        private void UpdateMarkerColor()
        {
            if (markerRenderer == null) return;
            
            float patiencePercent = currentPatience / maxPatience;
            Color targetColor = patiencePercent <= urgentThreshold ? urgentColor : normalColor;
            
            // Piscar quando urgente
            if (patiencePercent <= urgentThreshold)
            {
                float flash = Mathf.PingPong(Time.time * 4f, 1f);
                targetColor = Color.Lerp(normalColor, urgentColor, flash);
            }
            
            markerRenderer.material.color = targetColor;
        }
        
        private void AnimateMarker()
        {
            if (markerObject == null) return;
            
            // Flutuação suave
            float yOffset = Mathf.Sin(Time.time * 2f) * 0.2f;
            Vector3 basePos = transform.position + Vector3.up * 2.5f;
            markerObject.transform.position = basePos + Vector3.up * yOffset;
            
            // Rotação
            markerObject.transform.Rotate(Vector3.up, 90f * Time.deltaTime);
        }
        
        private void OnDrawGizmosSelected()
        {
            // Visualizar raio de pickup
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, pickupRadius);
        }
    }
}