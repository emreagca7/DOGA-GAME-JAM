using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

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
        public PlayerController PlayerPrefab; // KARAKTER PREFABINI BURAYA SÜRÜKLEYECEKSİN

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
            // SINGLETON KORUMASI: Çift GameManager oluşmasını engeller
            if (s_Instance != null && s_Instance != this)
            {
                Destroy(this.gameObject);
                return;
            }

            s_Instance = this;
            DontDestroyOnLoad(gameObject);
            
            m_IsTicking = true;
            
            ItemDatabase.Init();
            CropDatabase.Init();
            
            Storage = new Storage();
            
            m_CurrentTimeOfTheDay = StartingTime;
            
            if (DayDurationInSeconds <= 0.0f)
            {
                DayDurationInSeconds = 1.0f;
                Debug.LogError("The day length on the GameManager is set to 0, the length need to be set to a positive value");
            }
        }

        private void Start()
        {
            m_CurrentTimeOfTheDay = StartingTime;
            
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
            if (Player == null && spawn.SpawnIndex == 0)
            { 
                // YENİ SPAWN SİSTEMİ: Resources klasörü yerine Inspector referansını kullanır
                if (PlayerPrefab != null)
                {
                    Instantiate(PlayerPrefab);
                    spawn.SpawnHere();
                }
                else
                {
                    Debug.LogError("KARAKTER DOĞAMADI! GameManager içindeki PlayerPrefab alanına karakterini sürüklemedin.");
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