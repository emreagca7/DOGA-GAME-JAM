using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI; // Normal Text kullanmak için ekledik (TMPro silindi)
using Lean.Touch;
using DG.Tweening; 

namespace HappyHarvest
{
    public class StreetLightInteract : InteractiveObject
    {
        private Light2D[] childLights; 
        private bool isLit = false; 

        private void Awake()
        {
            childLights = GetComponentsInChildren<Light2D>(true);
            
            foreach (var light in childLights)
            {
                light.enabled = false;
            }
        }

        private void OnEnable()
        {
            LeanTouch.OnFingerTap += HandleFingerTap; 
        }

        private void OnDisable()
        {
            LeanTouch.OnFingerTap -= HandleFingerTap; 
        }

        private void HandleFingerTap(LeanFinger finger)
        {
            if (finger.IsOverGui) return;

            Vector2 touchWorldPos = Camera.main.ScreenToWorldPoint(finger.ScreenPosition);

            Collider2D hit = Physics2D.OverlapPoint(touchWorldPos, 1 << 31);
            
            if (hit != null && hit.gameObject == this.gameObject)
            {
                InteractedWith(); 
            }
        }

        public override void InteractedWith()
        {
            if (isLit)
            {
                ShowFloatingText("Bu lamba zaten umutla parlıyor...", Color.white);
                return;
            }

            if (GameManager.Instance != null && GameManager.Instance.CandleCount > 0)
            {
                GameManager.Instance.UseCandle();

                foreach (var light in childLights)
                {
                    light.enabled = true;
                }

                isLit = true; 
                ShowFloatingText("Bir karanlık köşe daha geçmişin ışığıyla aydınlandı...", Color.white);

                // --- YENİ EKLENEN SATIR: GameManager'a ışığın yandığını haber ver ---
                GameManager.Instance.LightTurnedOn();
            }
            else
            {
                ShowFloatingText("Karanlığı aydınlatmak için önce dedenin mektuplarından bir Mum kazanmalısın...", Color.white); 
            }
        }

        // --- YÜZEN YAZI (FLOATING TEXT) SİSTEMİ (Standart Normal Text İle) ---
        private void ShowFloatingText(string message, Color textColor)
        {
            // 1. Normal Text'in oyun dünyasında süzülebilmesi için World Space bir Canvas yaratıyoruz
            GameObject canvasObj = new GameObject("FloatingCanvas_" + gameObject.name);
            canvasObj.transform.position = transform.position + new Vector3(0, 1.5f, 0); 
            
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            
            // Hatanın çözümü burada: Sıralamayı doğrudan Canvas üzerinden yapıyoruz
            canvas.sortingLayerName = "ObjectsFront"; // Arkada kalmasını engeller
            canvas.sortingOrder = 32000;    // Her şeyin önüne geçirir

            // Canvas'ı sahneye uygun bir boyuta küçültüyoruz
            RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(25, 5); 
            canvasRect.localScale = new Vector3(0.01f, 0.01f, 0.01f); 

            // 2. Normal Text objesini oluştur ve Canvas'ın içine (Child olarak) koy
            GameObject textObj = new GameObject("NormalText");
            textObj.transform.SetParent(canvasObj.transform, false);

            Text normalText = textObj.AddComponent<Text>();
            normalText.text = message;
            normalText.color = textColor;
            normalText.fontSize = 20; 
            normalText.alignment = TextAnchor.MiddleCenter;
            
            // Yazı kutuya sığmazsa taşmasına izin ver (Kırpılmayı önler)
            normalText.horizontalOverflow = HorizontalWrapMode.Overflow;
            normalText.verticalOverflow = VerticalWrapMode.Overflow;

            // Kodu her bilgisayarda çalışsın diye Unity'nin standart fontunu (Arial) dinamik olarak atıyoruz
            normalText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (normalText.font == null) normalText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // 3. DOTween Animasyonları (Canvas'ı havaya kaldırıp, normalText'i saydamlaştır)
            canvasObj.transform.DOMoveY(canvasObj.transform.position.y + 1.5f, 2.5f).SetEase(Ease.OutCirc);

            DOTween.To(() => normalText.color, x => normalText.color = x, new Color(textColor.r, textColor.g, textColor.b, 0f), 2.5f)
                .SetEase(Ease.InExpo)
                .OnComplete(() => {
                    Destroy(canvasObj); // İşlem bitince yarattığımız her şeyi sil
                });
        }
    }
}