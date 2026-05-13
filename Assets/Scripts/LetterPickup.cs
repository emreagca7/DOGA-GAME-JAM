using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using DG.Tweening;

namespace HappyHarvest
{
    public class LetterPickup : MonoBehaviour
    {
        [Header("Bu Mektubun Sorusu")]
        public QuestionData myQuestion; 
        
        [Header("Ayarlar")]
        public float cooldownTime = 5f; // Yanlış bilinirse kaç saniye beklenecek?

        private Collider2D myCollider;
        private SpriteRenderer mySprite;

        private void Awake()
        {
            myCollider = GetComponent<Collider2D>();
            mySprite = GetComponent<SpriteRenderer>(); // Mektubun görselini soluklaştırmak için
        }

        public void InteractWithLetter()
        {
            if (QuizManager.Instance != null && myQuestion != null)
            {
                QuizManager.Instance.ShowQuestion(myQuestion, OnCorrect, OnWrong);
            }
        }

        private void OnCorrect()
        {
            if(GameManager.Instance != null)
            {
                GameManager.Instance.AddRewards(1, 1);
            }
            
            // Havada yeşil bir başarı yazısı süzülsün ve mektup silinsin
            ShowFloatingText("Dedenin bir anısı daha koruma altına alındı...", Color.green);
            
            // Yazının havada süzülmesini görebilmek için objeyi hemen silmek yerine görünmez yapıp 3 saniye sonra siliyoruz
            myCollider.enabled = false;
            if (mySprite != null) mySprite.enabled = false;
            Destroy(gameObject, 3f); 
        }

        private void OnWrong()
        {
            // Yanlış cevap verildiğinde Cooldown başlatılır
            StartCoroutine(WrongAnswerCooldown());
        }

        private IEnumerator WrongAnswerCooldown()
        {
            ShowFloatingText("Anılar silikleşti... Hatırlamak için biraz bekle.", Color.red);

            // Mektubu etkileşime kapat ve rengini yarı saydam yap
            myCollider.enabled = false;
            if (mySprite != null) mySprite.color = new Color(1f, 1f, 1f, 0.4f);

            // Cooldown süresi kadar bekle
            yield return new WaitForSeconds(cooldownTime);

            // Süre dolunca mektubu tekrar canlandır
            if (mySprite != null) mySprite.color = new Color(1f, 1f, 1f, 1f);
            myCollider.enabled = true;
            
            ShowFloatingText("Mektup tekrar okunabilir durumda.", Color.white);
        }

        // --- SOKAK LAMBASINDAKİ YÜZEN YAZI SİSTEMİNİN AYNISI ---
        private void ShowFloatingText(string message, Color textColor)
        {
            GameObject canvasObj = new GameObject("FloatingCanvas_Letter");
            canvasObj.transform.position = transform.position + new Vector3(0, 1.5f, 0); 
            
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingLayerName = "ObjectsFront"; // Arkada kalmasını engeller
            canvas.sortingOrder = 32000;    

            RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(25, 5); 
            canvasRect.localScale = new Vector3(0.01f, 0.01f, 0.01f); 

            GameObject textObj = new GameObject("NormalText");
            textObj.transform.SetParent(canvasObj.transform, false);

            Text normalText = textObj.AddComponent<Text>();
            normalText.text = message;
            normalText.color = textColor;
            normalText.fontSize = 20; 
            normalText.alignment = TextAnchor.MiddleCenter;
            normalText.horizontalOverflow = HorizontalWrapMode.Overflow;
            normalText.verticalOverflow = VerticalWrapMode.Overflow;

            normalText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (normalText.font == null) normalText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            canvasObj.transform.DOMoveY(canvasObj.transform.position.y + 1.5f, 2.5f).SetEase(Ease.OutCirc);

            DOTween.To(() => normalText.color, x => normalText.color = x, new Color(textColor.r, textColor.g, textColor.b, 0f), 5f)
                .SetEase(Ease.InExpo)
                .OnComplete(() => {
                    Destroy(canvasObj); 
                });
        }
    }
}