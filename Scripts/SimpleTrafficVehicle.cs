using UnityEngine;

namespace CrazyTaxi.Traffic
{
    public class SimpleTrafficVehicle : MonoBehaviour
    {
        [Header("Configuração")]
        [SerializeField] private float speed = 8f;
        [SerializeField] private Transform pointA;
        [SerializeField] private Transform pointB;
        
        private Transform currentTarget;
        
        private void Start()
        {
            currentTarget = pointB;
            
            // Posicionar no ponto A
            if (pointA != null)
            {
                transform.position = pointA.position;
            }
        }
        
        private void Update()
        {
            if (currentTarget == null) return;
            
            // Mover em direção ao alvo
            Vector3 direction = (currentTarget.position - transform.position).normalized;
            direction.y = 0;
            
            // Rotacionar
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
            
            // Mover
            transform.position += direction * speed * Time.deltaTime;
            
            // Verificar chegada
            float distance = Vector3.Distance(transform.position, currentTarget.position);
            if (distance < 1f)
            {
                // Trocar alvo
                currentTarget = (currentTarget == pointA) ? pointB : pointA;
            }
        }
        
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;
            if (pointA != null) Gizmos.DrawWireSphere(pointA.position, 0.5f);
            if (pointB != null) Gizmos.DrawWireSphere(pointB.position, 0.5f);
            if (pointA != null && pointB != null)
            {
                Gizmos.DrawLine(pointA.position, pointB.position);
            }
        }
    }
}