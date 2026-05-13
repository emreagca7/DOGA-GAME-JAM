using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using TMPro; 

namespace HappyHarvest
{
    [DefaultExecutionOrder(-9999)]
    public class GameManager : MonoBehaviour
    {
        private static GameManager s_Instance;
        
#if UNITY_EDITOR
        private static bool s_IsQuitting = false;
#endif
        public static GameManager Instance 
        {
            get
            {
#if UNITY_EDITOR
                if (!Application.isPlaying || s_IsQuitting)
                    return null;
                
                if (s_Instance == null)
                {
                    Debug.LogWarning("GameManager bulunamadı! Sahneye eklendiğinden emin olun.");
                }
#endif
                return s_Instance;
            }
        }

        [Header("Spawn Settings (BUNU INSPECTOR'DAN ATA)")] 
        public PlayerController PlayerPrefab;

        [Header("Envanter Sistemi (UI)")]
        public TextMeshProUGUI letterCountText; 
        public TextMeshProUGUI candleCountText; 
        
        public int LetterCount { get; private set; } 
        public int CandleCount { get; private set; }

        public int DeliveredLetterCount { get; private set; }
        // --- YENİ EKLENENLER (OYUN SONU TAKİBİ İÇİN) ---
        public int LitLightCount { get; private set; } // Yanan ışık sayısı
        public int TargetCount = 10; // Kazanmak için gereken mektup ve ışık sayısı
        

        public TerrainManager Terrain { get; set; }
        public PlayerController Player { get; set; }
        public DayCycleHandler DayCycleHandler { get; set; }
        public WeatherSystem WeatherSystem { get; set; }
        public CinemachineCamera MainCamera { get; set; }
        public Tilemap WalkSurfaceTilemap { get; set; }
        
        public SceneData LoadedSceneData { get; set; }
        
        public float CurrentDayRatio => m_CurrentTimeOfTheDay / DayDurationInSeconds;

        [Header("Market")] 
        public Item[] MarketEntries;
        
        [Header("Time settings")]
        [Min(1.0f)] 
        public float DayDurationInSeconds;
        public float StartingTime = 0.0f;

        [Header("Data")] 
        public ItemDatabase ItemDatabase;
        public CropDatabase CropDatabase;

        public Storage Storage;

        private bool m_IsTicking;
        
        private List<DayEventHandler> m_EventHandlers = new();
        private List<SpawnPoint> m_ActiveTransitions = new List<SpawnPoint>();
        
        private float m_CurrentTimeOfTheDay;

        private void Awake()
        {
            if (s_Instance != null && s_Instance != this)
            {
                Destroy(this.gameObject);
                return;
            }

            s_Instance = this;
            
            m_IsTicking = true;
            
            ItemDatabase.Init();
            CropDatabase.Init();
            
            Storage = new Storage();
            
            m_CurrentTimeOfTheDay = StartingTime;
            
            if (DayDurationInSeconds <= 0.0f)
            {
                DayDurationInSeconds = 1.0f;
            }
        }

        private void Start()
        {
            m_CurrentTimeOfTheDay = StartingTime;
            
            LetterCount = 0;
            CandleCount = 0;
            UpdateInventoryUI();
            
            UIHandler.SceneLoaded();
        }

#if UNITY_EDITOR
        private void OnDestroy()
        {
            if (s_Instance == this)
            {
                s_IsQuitting = true;
            }
        }
#endif

        private void Update()
        {
            if (m_IsTicking)
            {
                float previousRatio = CurrentDayRatio;
                m_CurrentTimeOfTheDay += Time.deltaTime;

                while (m_CurrentTimeOfTheDay > DayDurationInSeconds)
                    m_CurrentTimeOfTheDay -= DayDurationInSeconds;

                foreach (var handler in m_EventHandlers)
                {
                    foreach (var evt in handler.Events)
                    {
                        bool prev = evt.IsInRange(previousRatio);
                        bool current = evt.IsInRange(CurrentDayRatio);
                    
                        if (prev && !current)
                        {
                            evt.OffEvent.Invoke();
                        }
                        else if (!prev && current)
                        {
                            evt.OnEvents.Invoke();
                        }
                    }
                }
                
                if(DayCycleHandler != null)
                    DayCycleHandler.Tick();
            }
        }

        public void AddRewards(int letterAmount, int candleAmount)
        {
            LetterCount += letterAmount;
            CandleCount += candleAmount;
            
            UpdateInventoryUI();
            Player?.UpdateCandleVisual(); // Oyuncunun elindeki mumu güncelle
        }

        public bool UseCandle()
        {
            if (CandleCount > 0)
            {
                CandleCount--;
                UpdateInventoryUI();
                Player?.UpdateCandleVisual(); // Mumu harcadıktan sonra elindeki görseli güncelle
                return true;
            }
            return false;
        }

        // YENİ FONKSİYON: Eldeki mektupları sandığa atar
        public int DepositLetters()
        {
            int amount = LetterCount;
            if (amount > 0)
            {
                DeliveredLetterCount += amount; 
                LetterCount = 0; 
                UpdateInventoryUI(); 
                
                CheckWinCondition(); // Sandığa her mektup atıldığında kazanıp kazanmadığını kontrol et
                
                return amount; 
            }
            return 0;
        }

        public void LightTurnedOn()
        {
            LitLightCount++;
            CheckWinCondition(); // Lamba her yandığında kazanıp kazanmadığını kontrol et
        }

        // Kazanma şartlarının sağlanıp sağlanmadığına bakar
        private void CheckWinCondition()
        {
            // Eğer 10 mektup teslim edildiyse VE 10 ışık yandıysa OYUN BİTER
            if (DeliveredLetterCount >= TargetCount && LitLightCount >= TargetCount)
            {
                Debug.Log("KAZANDIN! Tüm şartlar sağlandı. Final ekranı geliyor...");
                
                // Oyuncu sandığa atma yazısını (floating text) okuyabilsin diye final ekranını 2.5 saniye gecikmeli açıyoruz
                Invoke(nameof(TriggerEndGame), 2.5f);
            }
        }

        // Gecikmeli olarak final mektubunu çağıran yardımcı fonksiyon
        private void TriggerEndGame()
        {
            if (LetterManager.Instance != null)
            {
                LetterManager.Instance.ShowEndGameLetter();
            }
        }

        private void UpdateInventoryUI()
        {
            if (letterCountText != null)
                letterCountText.text = LetterCount.ToString();
                
            if (candleCountText != null)
                candleCountText.text = CandleCount.ToString();
        }

        public void Pause()
        {
            m_IsTicking = false;
            Player?.ToggleControl(false);
        }

        public void Resume()
        {
            m_IsTicking = true;
            Player?.ToggleControl(true);
        }

        public void RegisterSpawn(SpawnPoint spawn)
        {
            // KAMERA SORUNUNUN ÇÖZÜMÜ: Oyuncu null olmasa bile bu blok çalışmalı
            if (spawn.SpawnIndex == 0)
            { 
                if (Player == null)
                {
                    PlayerController scenePlayer = FindObjectOfType<PlayerController>();
                    
                    if (scenePlayer != null)
                    {
                        Player = scenePlayer; 
                    }
                    else if (PlayerPrefab != null) 
                    {
                        Player = Instantiate(PlayerPrefab);
                    }
                    else
                    {
                        Debug.LogError("KARAKTER DOĞAMADI! GameManager içindeki PlayerPrefab alanına karakterini sürüklemedin.");
                    }
                }

                // Kamera ayarlarının yapılması için SpawnHere() mutlaka çağrılmalı
                if (Player != null)
                {
                    spawn.SpawnHere(); 
                }
            }
            
            m_ActiveTransitions.Add(spawn);
        }

        public void UnregisterSpawn(SpawnPoint spawn)
        {
            m_ActiveTransitions.Remove(spawn);
        }

        public void MoveTo(int targetScene, int targetSpawn)
        {
            Pause();
            SaveSystem.SaveSceneData();
            UIHandler.FadeToBlack(() =>
            {
                var asyncop = SceneManager.LoadSceneAsync(targetScene, LoadSceneMode.Single);
                asyncop.completed += operation =>
                {
                    m_IsTicking = true;
                    
                    foreach (var active in m_ActiveTransitions)
                    {
                        if (active.SpawnIndex == targetSpawn)
                        {
                            active.SpawnHere();
                            SaveSystem.LoadSceneData();
                        }
                    }
                    
                    UIHandler.SceneLoaded();
                    UIHandler.FadeFromBlack(() =>
                    {
                        Player?.ToggleControl(true);
                    });
                };
            });
        }
        
        public string CurrentTimeAsString()
        {
            return GetTimeAsString(CurrentDayRatio);
        }

        public static string GetTimeAsString(float ratio)
        {
            var hour = GetHourFromRatio(ratio);
            var minute = GetMinuteFromRatio(ratio);

            return $"{hour}:{minute:00}";
        }

        public static int GetHourFromRatio(float ratio)
        {
            var time = ratio * 24.0f;
            var hour = Mathf.FloorToInt(time);

            return hour;
        }

        public static int GetMinuteFromRatio(float ratio)
        {
            var time = ratio * 24.0f;
            var minute = Mathf.FloorToInt((time - Mathf.FloorToInt(time)) * 60.0f);

            return minute;
        }
        
        public static void RegisterEventHandler(DayEventHandler handler)
        {
            foreach (var evt in handler.Events)
            {
                if (evt.IsInRange(GameManager.Instance.CurrentDayRatio))
                {
                    evt.OnEvents.Invoke();
                }
                else
                {
                    evt.OffEvent.Invoke();
                }
            }
            
            Instance.m_EventHandlers.Add(handler);
        }

        public static void RemoveEventHandler(DayEventHandler handler)
        {
            Instance?.m_EventHandlers.Remove(handler);
        }
    }
}