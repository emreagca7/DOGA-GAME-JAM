using UnityEngine;
using TMPro;
using DG.Tweening;
using HappyHarvest;
using UnityEngine.UI;

public class LetterManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject letterPanel;
    public CanvasGroup contentGroup; // Metinlerin olduğu CanvasGroup
    public TextMeshProUGUI contentText;
    public Button actionButton; // "Devam Et" / "Başla" Butonu
    public TextMeshProUGUI buttonText;

    [Header("Settings")]
    public float typingSpeed = 0.04f;
    public float fadeDuration = 0.5f;

    [Header("Content")]
    [TextArea(5, 10)]
    public string page1Message = "Canım torunum...\nBu köy senin mirasın. Eskiden burada imece vardı, sevgi vardı. Şimdi her şey gri. Bu mektup senin anahtarın...";
    
    [TextArea(5, 10)]
    public string page2Message = "Şimdi görev başındasın!\nSol alt kısımdaki joystick ile hareket et. Köylülerin sorunlarını çözerek kültürel değerleri topla. Köyün kaderi senin ellerinde!";

    private int currentPage = 1;
    private bool isTyping = false;
    private Tween typingTween;

    void Start()
    {
        CloseLetter(); // Başlangıçta mektup kapalı olsun
    }

    void StartPage(string message)
    {
        isTyping = true;
        actionButton.gameObject.SetActive(false);
        
        contentText.text = message;
        contentText.maxVisibleCharacters = 0;
        contentGroup.alpha = 1;

        float duration = message.Length * typingSpeed;
        
        typingTween = DOTween.To(() => contentText.maxVisibleCharacters, 
                   x => contentText.maxVisibleCharacters = x, 
                   message.Length, 
                   duration)
               .SetEase(Ease.Linear)
               .OnComplete(() => {
                   isTyping = false;
                   ShowButton();
               });
    }

    void ShowButton()
    {
        buttonText.text = (currentPage == 1) ? "Devam Et" : "Başla";
        actionButton.gameObject.SetActive(true);
        // Butonu ufak bir animasyonla göster
        actionButton.transform.localScale = Vector3.zero;
        actionButton.transform.DOScale(1, 0.3f).SetEase(Ease.OutBack);
    }

    // Butona basıldığında bu fonksiyon çalışacak
    public void OnButtonClick()
    {
        if (isTyping) return; // Yazım sürerken basılmasın

        if (currentPage == 1)
        {
            NextPage();
        }
        else
        {
            CloseLetter();
        }
    }

    void NextPage()
    {
        currentPage = 2;
        // 1. Sayfayı karartarak gönder
        contentGroup.DOFade(0, fadeDuration).OnComplete(() => {
            StartPage(page2Message);
        });
    }

    public void CloseLetter()
    {
        // Paneli yavaşça kapat
        letterPanel.GetComponent<CanvasGroup>().DOFade(0, fadeDuration).OnComplete(() => {
            letterPanel.SetActive(false);
            GameManager.Instance.Resume();
        });
    }
}
