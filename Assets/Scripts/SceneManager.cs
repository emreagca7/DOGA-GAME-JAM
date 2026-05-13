using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

namespace HappyHarvest
{
    public class GameSceneManager : MonoBehaviour
    {
        public static GameSceneManager Instance;

        [Header("UI Ayarları")]
        public GameObject loadingScreen;     
        public CanvasGroup canvasGroup;      
        public Slider progressBar;           
        public TextMeshProUGUI loadingText;             

        [Header("Geçiş Ayarları")]
        public float fadeDuration = 0.5f;    

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject); 
        }

        private void Start()
        {
            if (loadingScreen != null)
            {
                loadingScreen.SetActive(false);
                if (canvasGroup != null) canvasGroup.alpha = 0;
            }
        }

        public void LoadScene(string sceneName)
        {
            StartCoroutine(LoadSceneAsync(sceneName));
        }

        private IEnumerator LoadSceneAsync(string sceneName)
        {
            // Yükleme ekranı varsa aç ve karart (Zırh eklendi)
            if (loadingScreen != null) loadingScreen.SetActive(true);
            
            if (canvasGroup != null)
            {
                Tween fadeIn = canvasGroup.DOFade(1, fadeDuration);
                yield return fadeIn.WaitForCompletion(); 
            }

            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
            operation.allowSceneActivation = false; 

            while (!operation.isDone)
            {
                float progress = Mathf.Clamp01(operation.progress / 0.9f);

                // Silinmiş olma ihtimaline karşı null check
                if (progressBar != null) progressBar.value = progress;
                if (loadingText != null) loadingText.text = "Köy Yükleniyor... %" + (progress * 100).ToString("F0");

                if (operation.progress >= 0.9f)
                {
                    operation.allowSceneActivation = true;
                }

                yield return null;
            }

            // Yeni sahne yüklendiğinde yükleme ekranını kapat (Zırh eklendi)
            if (canvasGroup != null)
            {
                Tween fadeOut = canvasGroup.DOFade(0, fadeDuration);
                yield return fadeOut.WaitForCompletion();
            }

            if (loadingScreen != null) loadingScreen.SetActive(false);
        }
    }
}