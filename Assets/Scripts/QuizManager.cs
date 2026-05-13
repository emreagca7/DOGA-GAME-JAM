using UnityEngine;
using TMPro; 
using UnityEngine.UI; // Normal Text kullanmak için bu kütüphane şart
using System;
using Lean.Gui;

namespace HappyHarvest
{
    public class QuizManager : MonoBehaviour
    {
        public static QuizManager Instance;

        [Header("UI References")]
        public GameObject quizPanel; 
        public TextMeshProUGUI questionText; // Ana soru metni (Eğer bu da normal Text ise 'public Text questionText;' yap)
        public LeanButton[] answerButtons; 
        
        // DEĞİŞİKLİK: TextMeshProUGUI yerine normal Text kullanıyoruz
        public Text[] answerTexts; 

        private QuestionData currentQuestion;
        
        private Action onCorrectAnswer; 
        private Action onWrongAnswer;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            quizPanel.SetActive(false); 
        }

        public void ShowQuestion(QuestionData qData, Action onCorrect, Action onWrong)
        {
            currentQuestion = qData;
            onCorrectAnswer = onCorrect;
            onWrongAnswer = onWrong;

            if (GameManager.Instance != null) GameManager.Instance.Pause();

            questionText.text = currentQuestion.questionText;

            // Döngü artık buton sayısı (4) kadar çalışacak
            for (int i = 0; i < answerButtons.Length; i++)
            {
                answerTexts[i].text = currentQuestion.answers[i];
                
                int index = i; 
                
                answerButtons[i].OnClick.RemoveAllListeners(); 
                answerButtons[i].OnClick.AddListener(() => OnAnswerSelected(index));
            }

            quizPanel.SetActive(true);
        }

        private void OnAnswerSelected(int selectedIndex)
        {
            quizPanel.SetActive(false); 
            
            if (GameManager.Instance != null) GameManager.Instance.Resume();

            if (selectedIndex == currentQuestion.correctAnswerIndex)
            {
                Debug.Log("DOĞRU CEVAP! Mektup ve Mum kazanıldı.");
                onCorrectAnswer?.Invoke(); 
            }
            else
            {
                Debug.Log("YANLIŞ CEVAP! Dede mirası bekliyor...");
                onWrongAnswer?.Invoke(); 
            }
        }
    }
}