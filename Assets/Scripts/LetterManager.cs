using UnityEngine;
using TMPro;
using DG.Tweening;
using HappyHarvest;
using UnityEngine.UI;

public class LetterManager : MonoBehaviour
{
    public static LetterManager Instance; 

    [Header("UI References")]
    public GameObject letterPanel;
    public CanvasGroup contentGroup; 
    public TextMeshProUGUI contentText;
    public Button actionButton; 
    public TextMeshProUGUI buttonText;

    [Header("Settings")]
    public float typingSpeed = 0.04f;
    public float fadeDuration = 0.5f;

    [Header("Content - Başlangıç")]
    [TextArea(8, 12)]
    public string page1Message = "Canım torunum...\nEğer bu satırları okuyorsan, köyümüzün üzerindeki o soğuk ve gri örtüyü sen de fark etmişsin demektir. Eskiden bu sokaklarda kahkahalar yankılanır, kalbimizden 'imece' ruhu eksik olmazdı. İnsanlar birbirinden uzaklaştıkça, köyümüzün ışığı da yavaş yavaş söndü. Ben göçüp gitmeden önce, o eski güzel günlerin sıcaklığını mektuplarıma sakladım. Sana sadece bir toprak değil, karanlıkta kaybolan manevi mirasımızı bırakıyorum. Bu mektup, aydınlığa atacağın ilk adımdır...";
    
    [TextArea(8, 12)]
    public string page2Message = "Şimdi bu mirası ayağa kaldırma vakti!\nSol alt köşedeki yön çubuğu ile köyde dolaş. Etrafa gizlediğim mektupları bularak anılarımızı güvence altına al ve içlerinden çıkan mumlarla sönen sokak lambalarını tekrar yak. Tüm mektupları toplayıp 'Ata Sandığı'na emanet ettiğinde, köyümüz o eski parlak günlerine dönecek. Unutma; geçmişimizin ışığı ve bu köyün kaderi artık senin ellerinde...";

    [Header("Content - Final (Oyun Sonu)")]
    [TextArea(5, 10)]
    public string endGameMessage = "Gözüm artık arkada kalmayacak...\nKöyümüz senin sayende o eski, sıcak ışığına kavuştu. Sandıktaki mektuplar sadece geçmişi değil, geleceğimizi de aydınlatacak. Mirasıma sahip çıktığın için sana minnettarım.";

    // Yeni: Credits Sayfası (Otomatik ortalanmış ve takım ismi kalınlaştırılmış formatta)
    [TextArea(10, 15)]
    public string creditsMessage = "<align=center><b>Pinky UP</b>\n\nSefa Ağca - Computer Engineer & XR Developer\nGAMZE TOK - 2D ARTIST\nAYBUKE BETÜL KUŞAT - 2D ARTIST</align>";

    private int currentPage = 1;
    private bool isTyping = false;
    private bool isGameEnding = false; 
    private Tween typingTween;
    
    private Vector3 initialButtonScale; 

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;

        if (actionButton != null)
        {
            initialButtonScale = actionButton.transform.localScale;
        }
    }

    void Start()
    {
        letterPanel.SetActive(false); 
    }

    public void StartLetterSequence()
    {
        isGameEnding = false;
        GameManager.Instance.Pause(); 
        actionButton.gameObject.SetActive(false); 
        letterPanel.SetActive(true);
        currentPage = 1; 
        StartPage(page1Message); 
    }

    public void ShowEndGameLetter()
    {
        isGameEnding = true;
        GameManager.Instance.Pause();
        actionButton.gameObject.SetActive(false);
        letterPanel.SetActive(true);
        
        // Final mesajı 98. sayfa, Emeği Geçenler 99. sayfa olarak kodlandı
        currentPage = 98; 
        StartPage(endGameMessage);
    }


    void Update()
    {
        // Geliştirici Test Kısayolu: Klavyeden F12 tuşuna basılırsa oyunu bitir
        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log("HİLE AKTİF: Oyun sonu simüle ediliyor...");
            ShowEndGameLetter();
        }
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
        if (isGameEnding)
        {
            // Eğer 98. sayfadaysak "İleri" yazsın, 99. sayfadaysak (Credits) "Oyundan Çık" yazsın
            buttonText.text = (currentPage == 98) ? "İleri" : "Oyundan Çık";
        }
        else
        {
            buttonText.text = (currentPage == 1) ? "Devam Et" : "Başla";
        }

        actionButton.gameObject.SetActive(true);
        
        actionButton.transform.localScale = Vector3.zero;
        actionButton.transform.DOScale(initialButtonScale, 0.3f).SetEase(Ease.OutBack);
    }

    public void OnButtonClick()
    {
        if (isTyping) return; 

        if (isGameEnding)
        {
            if (currentPage == 98)
            {
                NextPageCredits(); // Dedenin mesajından Emeği Geçenler sayfasına geçiş yap
            }
            else if (currentPage == 99)
            {
                // Sayfa açık kalır, sadece oyun uygulaması tamamen kapanır
                Debug.Log("Oyun Bitti. Çıkış Yapılıyor...");
                Application.Quit();
            }
        }
        else 
        {
            if (currentPage == 1)
            {
                NextPage();
            }
            else
            {
                CloseLetter();
            }
        }
    }

    void NextPage()
    {
        currentPage = 2;
        contentGroup.DOFade(0, fadeDuration).OnComplete(() => {
            StartPage(page2Message);
        });
    }

    void NextPageCredits()
    {
        currentPage = 99; 
        contentGroup.DOFade(0, fadeDuration).OnComplete(() => {
            StartPage(creditsMessage);
        });
    }

    public void CloseLetter()
    {
        letterPanel.GetComponent<CanvasGroup>().DOFade(0, fadeDuration).OnComplete(() => {
            letterPanel.SetActive(false);
            GameManager.Instance.Resume();
        });
    }
}