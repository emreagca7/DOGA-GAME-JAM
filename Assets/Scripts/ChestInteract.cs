using UnityEngine;
using Lean.Touch;
using UnityEngine.UI;
using DG.Tweening;

namespace HappyHarvest
{
    public class ChestInteract : InteractiveObject
    {
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
            
            // PlayerController'da kullandığın 31. Katman (Interactable)
            Collider2D hit = Physics2D.OverlapPoint(touchWorldPos, 1 << 31); 
            
            if (hit != null && hit.gameObject == this.gameObject)
            {
                InteractedWith(); 
            }
        }
        

        public override void InteractedWith()
        {
            // Eğer oyuncunun elinde en az 1 mektup varsa
            if (GameManager.Instance != null && GameManager.Instance.LetterCount > 0)
            {
                // Mektupları teslim et ve kaç tane teslim edildiğini al
                int depositedCount = GameManager.Instance.DepositLetters();
                
                // Mavi tonlarında (Cyan) bir başarı yazısı göster
                ShowFloatingText($"{depositedCount} mektup Ata Sandığı'na emanet edildi...", Color.cyan);
            }
            else
            {
                // Elinde mektup yoksa uyarı ver
                ShowFloatingText("Sandık bekliyor... Önce etraftaki mektupları bulmalısın.", new Color(1f, 0.5f, 0f));
            }
        }

        // --- YÜZEN YAZI (FLOATING TEXT) ---
        private void ShowFloatingText(string message, Color textColor)
        {
            GameObject canvasObj = new GameObject("FloatingCanvas_Chest");
            canvasObj.transform.position = transform.position + new Vector3(0, 1.5f, -2f); 
            
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingLayerName = "ObjectsFront"; // Arkada kalmasını engeller
            canvas.sortingOrder = 32000;    

            RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(25, 5); 
            
            // TALEBİN ÜZERİNE SCALE 0.01 OLARAK AYARLANDI
            canvasRect.localScale = new Vector3(0.001f, 0.001f, 0.001f); 

            GameObject textObj = new GameObject("NormalText");
            textObj.transform.SetParent(canvasObj.transform, false);

            Text normalText = textObj.AddComponent<Text>();
            normalText.text = message;
            normalText.color = textColor;
            
            // Scale'i 10 kat küçülttüğümüz için yazının okunabilmesi adına fontu 20'den 200'e çıkardık
            normalText.fontSize = 200; 
            
            normalText.alignment = TextAnchor.MiddleCenter;
            normalText.horizontalOverflow = HorizontalWrapMode.Overflow;
            normalText.verticalOverflow = VerticalWrapMode.Overflow;

            normalText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (normalText.font == null) normalText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // Animasyonlar
            canvasObj.transform.DOMoveY(canvasObj.transform.position.y + 1.5f, 2.5f).SetEase(Ease.OutCirc);

            DOTween.To(() => normalText.color, x => normalText.color = x, new Color(textColor.r, textColor.g, textColor.b, 0f), 5f)
                .SetEase(Ease.InExpo)
                .OnComplete(() => {
                    Destroy(canvasObj); 
                });
        }
    }
}