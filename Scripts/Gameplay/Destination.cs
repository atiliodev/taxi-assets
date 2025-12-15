using UnityEngine;

namespace CrazyTaxi.Core
{
    public class Destination : MonoBehaviour
    {
        [Header("Configurações")]
        [SerializeField] private float deliveryRadius = 8f;
        [SerializeField] private string locationName = "Destino";
        
        [Header("Referências Visuais")]
        [SerializeField] private GameObject markerObject;
        [SerializeField] private GameObject areaIndicator;
        [SerializeField] private ParticleSystem deliveryEffect;
        
        [Header("Cores")]
        [SerializeField] private Color activeColor = Color.yellow;
        
        // Estado
        private bool isActive;
        
        // Componentes
        private SphereCollider triggerCollider;
        private Renderer markerRenderer;
        private Renderer areaRenderer;
        
        // Propriedades
        public bool IsActive => isActive;
        public string LocationName => locationName;
        
        private void Awake()
        {
            // Configurar collider
            triggerCollider = GetComponent<SphereCollider>();
            if (triggerCollider == null)
            {
                triggerCollider = gameObject.AddComponent<SphereCollider>();
            }
            triggerCollider.isTrigger = true;
            triggerCollider.radius = deliveryRadius;
            
            // Obter renderers
            if (markerObject != null)
            {
                markerRenderer = markerObject.GetComponent<Renderer>();
            }
            if (areaIndicator != null)
            {
                areaRenderer = areaIndicator.GetComponent<Renderer>();
            }
        }
        
        private void Update()
        {
            if (!isActive) return;
            
            AnimateMarker();
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (!isActive) return;
            
            if (other.CompareTag("Player"))
            {
                // Verificar se está em estado de entrega
                if (GameManager.Instance?.CurrentPassengerState == PassengerState.DeliveringPassenger)
                {
                    GameManager.Instance.DeliverPassenger(this);
                }
            }
        }
        
        public void Activate()
        {
            isActive = true;
            
            if (markerObject != null)
            {
                markerObject.SetActive(true);
                if (markerRenderer != null)
                {
                    markerRenderer.material.color = activeColor;
                }
            }
            
            if (areaIndicator != null)
            {
                areaIndicator.SetActive(true);
                // Escalar área para combinar com raio
                areaIndicator.transform.localScale = new Vector3(
                    deliveryRadius * 2f, 
                    0.1f, 
                    deliveryRadius * 2f
                );
            }
            
            gameObject.SetActive(true);
        }
        
        public void Deactivate()
        {
            isActive = false;
            
            // Efeito de partículas ao completar
            if (deliveryEffect != null)
            {
                deliveryEffect.Play();
            }
            
            if (markerObject != null) markerObject.SetActive(false);
            if (areaIndicator != null) areaIndicator.SetActive(false);
        }
        
        private void AnimateMarker()
        {
            if (markerObject == null) return;
            
            // Flutuação
            float yOffset = Mathf.Sin(Time.time * 3f) * 0.3f;
            Vector3 basePos = transform.position + Vector3.up * 4f;
            markerObject.transform.position = basePos + Vector3.up * yOffset;
            
            // Rotação
            markerObject.transform.Rotate(Vector3.up, 120f * Time.deltaTime);
            
            // Pulsar escala
            float scale = 1f + Mathf.Sin(Time.time * 2f) * 0.1f;
            markerObject.transform.localScale = Vector3.one * scale;
        }
        
        private void OnDrawGizmosSelected()
        {
            // Visualizar raio de entrega
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, deliveryRadius);
        }
    }
}