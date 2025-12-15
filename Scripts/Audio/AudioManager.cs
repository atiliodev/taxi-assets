using UnityEngine;
using System.Collections.Generic;

namespace CrazyTaxi.Audio
{
    [System.Serializable]
    public class SoundEffect
    {
        public string name;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0.1f, 3f)] public float pitch = 1f;
        public bool randomizePitch = false;
        [Range(0f, 0.5f)] public float pitchVariation = 0.1f;
    }
    
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }
        
        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource engineSource;
        [SerializeField] private AudioSource ambientSource;
        
        [Header("Música")]
        [SerializeField] private AudioClip gameplayMusic;
        [SerializeField] private AudioClip menuMusic;
        [SerializeField] private float musicVolume = 0.3f;
        
        [Header("Som do Motor")]
        [SerializeField] private AudioClip engineLoop;
        [SerializeField] private float engineMinPitch = 0.8f;
        [SerializeField] private float engineMaxPitch = 1.6f;
        [SerializeField] private float engineVolume = 0.5f;
        
        [Header("Efeitos Sonoros")]
        [SerializeField] private List<SoundEffect> soundEffects = new List<SoundEffect>();
        
        [Header("Ambiente")]
        [SerializeField] private AudioClip cityAmbient;
        [SerializeField] private float ambientVolume = 0.2f;
        
        // Dicionário para acesso rápido
        private Dictionary<string, SoundEffect> sfxDictionary;
        
        // Referência ao veículo
        private Vehicle.TaxiController taxiController;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
            
            InitializeSFXDictionary();
            SetupAudioSources();
        }
        
        private void Start()
        {
            // Encontrar taxi
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                taxiController = player.GetComponent<Vehicle.TaxiController>();
            }
            
            // Iniciar áudio ambiente
            PlayAmbient();
        }
        
        private void Update()
        {
            UpdateEngineSound();
        }
        
        private void InitializeSFXDictionary()
        {
            sfxDictionary = new Dictionary<string, SoundEffect>();
            foreach (var sfx in soundEffects)
            {
                if (!sfxDictionary.ContainsKey(sfx.name))
                {
                    sfxDictionary.Add(sfx.name, sfx);
                }
            }
        }
        
        private void SetupAudioSources()
        {
            // Criar sources se não existirem
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.playOnAwake = false;
            }
            
            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
            }
            
            if (engineSource == null)
            {
                engineSource = gameObject.AddComponent<AudioSource>();
                engineSource.loop = true;
                engineSource.playOnAwake = false;
            }
            
            if (ambientSource == null)
            {
                ambientSource = gameObject.AddComponent<AudioSource>();
                ambientSource.loop = true;
                ambientSource.playOnAwake = false;
            }
        }
        
        // Música
        public void PlayGameplayMusic()
        {
            PlayMusic(gameplayMusic);
        }
        
        public void PlayMenuMusic()
        {
            PlayMusic(menuMusic);
        }
        
        public void PlayMusic(AudioClip clip)
        {
            if (musicSource == null || clip == null) return;
            
            musicSource.clip = clip;
            musicSource.volume = musicVolume;
            musicSource.Play();
        }
        
        public void StopMusic()
        {
            musicSource?.Stop();
        }
        
        public void SetMusicVolume(float volume)
        {
            musicVolume = volume;
            if (musicSource != null)
            {
                musicSource.volume = volume;
            }
        }
        
        // Efeitos Sonoros
        public void PlaySFX(string name)
        {
            if (sfxSource == null) return;
            
            if (sfxDictionary.TryGetValue(name, out SoundEffect sfx))
            {
                float pitch = sfx.pitch;
                if (sfx.randomizePitch)
                {
                    pitch += Random.Range(-sfx.pitchVariation, sfx.pitchVariation);
                }
                
                sfxSource.pitch = pitch;
                sfxSource.PlayOneShot(sfx.clip, sfx.volume);
            }
            else
            {
                Debug.LogWarning($"SFX não encontrado: {name}");
            }
        }
        
        public void PlaySFX(AudioClip clip, float volume = 1f)
        {
            if (sfxSource == null || clip == null) return;
            sfxSource.PlayOneShot(clip, volume);
        }
        
        // Motor
        public void StartEngine()
        {
            if (engineSource == null || engineLoop == null) return;
            
            engineSource.clip = engineLoop;
            engineSource.volume = engineVolume;
            engineSource.Play();
        }
        
        public void StopEngine()
        {
            engineSource?.Stop();
        }
        
        private void UpdateEngineSound()
        {
            if (engineSource == null || taxiController == null) return;
            
            if (!engineSource.isPlaying && engineLoop != null)
            {
                StartEngine();
            }
            
            // Ajustar pitch baseado na velocidade
            float speedPercent = taxiController.CurrentSpeed / taxiController.MaxSpeed;
            float targetPitch = Mathf.Lerp(engineMinPitch, engineMaxPitch, speedPercent);
            
            // Boost aumenta o pitch
            if (taxiController.IsBoosting)
            {
                targetPitch *= 1.2f;
            }
            
            engineSource.pitch = Mathf.Lerp(engineSource.pitch, targetPitch, Time.deltaTime * 5f);
        }
        
        // Ambiente
        public void PlayAmbient()
        {
            if (ambientSource == null || cityAmbient == null) return;
            
            ambientSource.clip = cityAmbient;
            ambientSource.volume = ambientVolume;
            ambientSource.Play();
        }
        
        public void StopAmbient()
        {
            ambientSource?.Stop();
        }
        
        // Volume Geral
        public void SetMasterVolume(float volume)
        {
            AudioListener.volume = volume;
        }
        
        // Métodos de conveniência para sons comuns
        public void PlayPickupSound()
        {
            PlaySFX("PickUp");
        }
        
        public void PlayDeliverySound()
        {
            PlaySFX("Delivery");
        }
        
        public void PlayHornSound()
        {
            PlaySFX("Horn");
        }
        
        public void PlaySkidSound()
        {
            PlaySFX("Skid");
        }
        
        public void PlayCrashSound()
        {
            PlaySFX("Crash");
        }
        
        public void PlayTimerWarningSound()
        {
            PlaySFX("TimerWarning");
        }
        
        public void PlayGameOverSound()
        {
            PlaySFX("GameOver");
        }
    }
}