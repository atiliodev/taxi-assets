using UnityEngine;

namespace CrazyTaxi.Vehicle
{
    public class TaxiController : MonoBehaviour
    {
        [Header("Referências das Rodas")]
        [SerializeField] private WheelCollider wheelFL;
        [SerializeField] private WheelCollider wheelFR;
        [SerializeField] private WheelCollider wheelRL;
        [SerializeField] private WheelCollider wheelRR;
        
        [Header("Transforms Visuais das Rodas")]
        [SerializeField] private Transform wheelTransformFL;
        [SerializeField] private Transform wheelTransformFR;
        [SerializeField] private Transform wheelTransformRL;
        [SerializeField] private Transform wheelTransformRR;
        
        [Header("Configurações do Motor")]
        [SerializeField] private float maxMotorTorque = 1500f;
        [SerializeField] private float maxSpeed = 30f;
        [SerializeField] private float boostMultiplier = 1.5f;
        
        [Header("Configurações de Direção")]
        [SerializeField] private float maxSteerAngle = 35f;
        [SerializeField] private float steerSpeed = 5f;
        
        [Header("Configurações de Freio")]
        [SerializeField] private float brakeTorque = 3000f;
        [SerializeField] private float handbrakeTorque = 5000f;
        
        [Header("Centro de Massa")]
        [SerializeField] private Vector3 centerOfMass = new Vector3(0, -0.5f, 0);
        
        // Componentes
        private Rigidbody rb;
        
        // Input
        private float horizontalInput;
        private float verticalInput;
        private bool isBraking;
        private bool isBoosting;
        
        // Estado
        private float currentSteerAngle;
        private float currentSpeed;
        
        public float CurrentSpeed => currentSpeed;
        public float MaxSpeed => maxSpeed;
        public bool IsBoosting => isBoosting;
        
        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.centerOfMass = centerOfMass;
        }
        
        private void Update()
        {
            GetInput();
            UpdateWheelVisuals();
            currentSpeed = rb.linearVelocity.magnitude;
        }
        
        private void FixedUpdate()
        {
            HandleMotor();
            HandleSteering();
            HandleBraking();
        }
        
        private void GetInput()
        {
            horizontalInput = Input.GetAxis("Horizontal");
            verticalInput = Input.GetAxis("Vertical");
            isBraking = Input.GetKey(KeyCode.Space);
            isBoosting = Input.GetKey(KeyCode.LeftShift) && verticalInput > 0;
        }
        
        private void HandleMotor()
        {
            // Limitar velocidade máxima
            float effectiveMaxSpeed = isBoosting ? maxSpeed * boostMultiplier : maxSpeed;
            
            if (currentSpeed < effectiveMaxSpeed)
            {
                float torque = maxMotorTorque * verticalInput;
                if (isBoosting) torque *= boostMultiplier;
                
                // Tração traseira (pode mudar para 4x4)
                wheelRL.motorTorque = torque;
                wheelRR.motorTorque = torque;
            }
            else
            {
                wheelRL.motorTorque = 0;
                wheelRR.motorTorque = 0;
            }
        }
        
        private void HandleSteering()
        {
            float targetSteerAngle = maxSteerAngle * horizontalInput;
            currentSteerAngle = Mathf.Lerp(currentSteerAngle, targetSteerAngle, Time.deltaTime * steerSpeed);
            
            wheelFL.steerAngle = currentSteerAngle;
            wheelFR.steerAngle = currentSteerAngle;
        }
        
        private void HandleBraking()
        {
            float brake = 0f;
            
            if (isBraking)
            {
                brake = handbrakeTorque;
                // Aplicar drift nas rodas traseiras
                wheelRL.brakeTorque = brake;
                wheelRR.brakeTorque = brake;
                wheelFL.brakeTorque = brake * 0.3f;
                wheelFR.brakeTorque = brake * 0.3f;
            }
            else if (verticalInput < 0 && currentSpeed > 1f)
            {
                // Freio normal ao dar ré em movimento
                brake = brakeTorque;
                wheelFL.brakeTorque = brake;
                wheelFR.brakeTorque = brake;
                wheelRL.brakeTorque = brake;
                wheelRR.brakeTorque = brake;
            }
            else
            {
                wheelFL.brakeTorque = 0;
                wheelFR.brakeTorque = 0;
                wheelRL.brakeTorque = 0;
                wheelRR.brakeTorque = 0;
            }
        }
        
        private void UpdateWheelVisuals()
        {
            UpdateWheelTransform(wheelFL, wheelTransformFL);
            UpdateWheelTransform(wheelFR, wheelTransformFR);
            UpdateWheelTransform(wheelRL, wheelTransformRL);
            UpdateWheelTransform(wheelRR, wheelTransformRR);
        }
        
        private void UpdateWheelTransform(WheelCollider collider, Transform transform)
        {
            if (transform == null) return;
            
            Vector3 pos;
            Quaternion rot;
            collider.GetWorldPose(out pos, out rot);
            transform.position = pos;
            transform.rotation = rot;
        }
        
        // Método público para verificar se está em movimento
        public bool IsMoving()
        {
            return currentSpeed > 0.5f;
        }
        
        // Método para parar o carro (usado em cutscenes, etc)
        public void StopCar()
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            wheelFL.brakeTorque = handbrakeTorque;
            wheelFR.brakeTorque = handbrakeTorque;
            wheelRL.brakeTorque = handbrakeTorque;
            wheelRR.brakeTorque = handbrakeTorque;
        }
    }
}