using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Lean.Gui;

namespace HappyHarvest
{
    public class PlayerController : MonoBehaviour
    {
        public InputActionAsset InputAction;
        public float Speed = 4.0f;

        [Header("Mobile UI (BUNU INSPECTOR'DAN ATA!)")]
        public LeanJoystick MovementJoystick; 

        public SpriteRenderer Target;
        public Transform ItemAttachBone;

        [Header("Visuals (BUNU INSPECTOR'DAN ATA)")]
        public GameObject HandCandleVisual; 

        public int Coins
        {
            get => m_Coins;
            set
            {
                m_Coins = value;
                UIHandler.UpdateCoins(Coins);
            }
        }

        public InventorySystem Inventory => m_Inventory;
        public Animator Animator => m_Animator;

        [SerializeField]
        private int m_Coins = 10;

        [SerializeField]
        private InventorySystem m_Inventory;
        
        private Rigidbody2D m_Rigidbody;

        private Vector3 m_SpawnPosition;
        
        private InputAction m_MoveAction;
        private InputAction m_NextItemAction;
        private InputAction m_PrevItemAction;
        private InputAction m_UseItemAction;

        private Vector3 m_CurrentWorldMousePos;
        private Vector2 m_CurrentLookDirection;
        private Vector3Int m_CurrentTarget;

        private TargetMarker m_TargetMarker;

        private bool m_HasTarget = false;
        private bool m_IsOverUI = false;

        private bool m_CanControl = true;

        private Animator m_Animator;

        private InteractiveObject m_CurrentInteractiveTarget = null;
        private Collider2D[] m_CollidersCache = new Collider2D[8];

        private Dictionary<Item, ItemInstance> m_ItemVisualInstance = new();

        private int m_DirXHash = Animator.StringToHash("DirX");
        private int m_DirYHash = Animator.StringToHash("DirY");
        private int m_SpeedHash = Animator.StringToHash("Speed");

        void Awake()
        {
            m_Rigidbody = GetComponent<Rigidbody2D>();
            m_Animator = GetComponentInChildren<Animator>();
            m_TargetMarker = Target.GetComponent<TargetMarker>();
            m_TargetMarker.Hide();
            
            gameObject.transform.SetParent(null);
            
            if (GameManager.Instance != null)
            {
                GameManager.Instance.Player = this;
            }
        }
        
        void Start()
        {
            m_MoveAction = InputAction.FindAction("Gameplay/Move");
            m_MoveAction.Enable();

            m_NextItemAction = InputAction.FindAction("Gameplay/EquipNext");
            m_PrevItemAction = InputAction.FindAction("Gameplay/EquipPrev");

            m_NextItemAction.Enable();
            m_NextItemAction.performed += context =>
            {
                ToggleToolVisual(false);
                m_Inventory.EquipNext();
                ToggleToolVisual(true);
            };
            
            m_PrevItemAction.Enable();
            m_PrevItemAction.performed += context =>
            {
                ToggleToolVisual(false);
                m_Inventory.EquipPrev();
                ToggleToolVisual(true);
            };

            m_UseItemAction = InputAction.FindAction("Gameplay/Use");
            m_UseItemAction.Enable();

            m_UseItemAction.performed += context => UseObject();
            
            m_CurrentLookDirection = Vector2.right;
            
            m_Inventory.Init();

            foreach (var entry in m_Inventory.Entries)
            {
                if (entry.Item != null)
                    CreateItemVisual(entry.Item);
            }
            ToggleToolVisual(true);
            
            UIHandler.UpdateInventory(m_Inventory);
            UIHandler.UpdateCoins(m_Coins);
        }

        private void Update()
        {
            m_IsOverUI = EventSystem.current.IsPointerOverGameObject();
            m_CurrentInteractiveTarget = null;
            m_HasTarget = false;
            
            if (!IsMouseOverGameWindow())
            {
                SafeChangeCursor(UIHandler.CursorType.System);
                return;
            }

            if (!m_CanControl)
            {
                return;
            }
            if (m_IsOverUI) 
            {
                SafeChangeCursor(UIHandler.CursorType.Interact);
            }
            
            m_CurrentWorldMousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            var overlapCol = Physics2D.OverlapPoint(m_CurrentWorldMousePos, 1 << 31);
                
            if (overlapCol != null)
            {
                m_CurrentInteractiveTarget = overlapCol.GetComponent<InteractiveObject>();
                m_HasTarget = false;
                SafeChangeCursor(UIHandler.CursorType.Interact);
                return;
            }
          
            SafeChangeCursor(UIHandler.CursorType.Normal);

            var grid = GameManager.Instance.Terrain?.Grid;

            if (grid != null)
            {
                var currentCell = grid.WorldToCell(transform.position);
                var pointedCell = grid.WorldToCell(m_CurrentWorldMousePos);

                currentCell.z = 0;
                pointedCell.z = 0;

                var toTarget = pointedCell - currentCell;

                if (Mathf.Abs(toTarget.x) > 1)
                {
                    toTarget.x = (int)Mathf.Sign(toTarget.x);
                }

                if (Mathf.Abs(toTarget.y) > 1)
                {
                    toTarget.y = (int)Mathf.Sign(toTarget.y);
                }

                m_CurrentTarget = currentCell + toTarget;
                Target.transform.position = GameManager.Instance.Terrain.Grid.GetCellCenterWorld(m_CurrentTarget);

                if (m_Inventory.EquippedItem != null
                    && m_Inventory.EquippedItem.CanUse(m_CurrentTarget))
                {
                    m_HasTarget = true;
                    m_TargetMarker.Activate();
                }
                else
                {
                    m_TargetMarker.Hide();
                }
            }

            if (Keyboard.current.f5Key.wasPressedThisFrame)
            {
                SaveSystem.Save();
            }
            else if (Keyboard.current.f9Key.wasPressedThisFrame)
            {
                SaveSystem.Load();
            }
        }

        // --- YENİ EKLENEN KORUMA KALKANI FONKSİYONU ---
        // NullReferenceException hatasını yutarak oyunun çökmesini engeller
        private void SafeChangeCursor(UIHandler.CursorType cursorType)
        {
            try
            {
                UIHandler.ChangeCursor(cursorType);
            }
            catch (System.Exception)
            {
                // Hata görmezden geliniyor. 
                // Oyun cursor değiştiremese bile tıkır tıkır çalışmaya devam edecek.
            }
        }

        public void UpdateCandleVisual()
        {
            if (HandCandleVisual != null)
            {
                bool hasCandle = GameManager.Instance.CandleCount > 0;
                HandCandleVisual.SetActive(hasCandle);
                
                Debug.Log("<color=green>Mum Görseli Güncellendi. Görünürlük: " + hasCandle + "</color>");
            }
            else
            {
                Debug.LogWarning("<color=red>DİKKAT: PlayerController içindeki Hand Candle Visual atanmamış!</color>");
            }
        }

        void UseObject()
        {
            if(m_IsOverUI)
                return;
            
            if (m_CurrentInteractiveTarget != null)
            {
                m_CurrentInteractiveTarget.InteractedWith();
                return;
            }
            
            if (m_Inventory.EquippedItem != null && m_Inventory.EquippedItem.NeedTarget() && !m_HasTarget) return;
            
            UseItem();
        }

        void FixedUpdate()
        {
            Vector2 move = Vector2.zero;

            if (MovementJoystick != null && MovementJoystick.ScaledValue != Vector2.zero)
            {
                move = MovementJoystick.ScaledValue;
            }
            else
            {
                move = m_MoveAction.ReadValue<Vector2>();
            }

            if (move != Vector2.zero)
            {
                SetLookDirectionFrom(move);
            }
            else
            {
                if (IsMouseOverGameWindow())
                {
                    Vector3 posToMouse = m_CurrentWorldMousePos - transform.position;
                    SetLookDirectionFrom(posToMouse);
                }
            }

            var movement = move * Speed;
            var speed = movement.sqrMagnitude;
            
            m_Animator.SetFloat(m_DirXHash, m_CurrentLookDirection.x);
            m_Animator.SetFloat(m_DirYHash, m_CurrentLookDirection.y);
            m_Animator.SetFloat(m_SpeedHash, speed);

            m_Rigidbody.MovePosition(m_Rigidbody.position + movement * Time.deltaTime);
        }

        bool IsMouseOverGameWindow()
        {
            return !(0 > Input.mousePosition.x || 0 > Input.mousePosition.y || Screen.width < Input.mousePosition.x || Screen.height < Input.mousePosition.y);
        } 

        void SetLookDirectionFrom(Vector2 direction)
        {
            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            {
                m_CurrentLookDirection = direction.x > 0 ? Vector2.right : Vector2.left;
            }
            else
            {
                m_CurrentLookDirection = direction.y > 0 ? Vector2.up : Vector2.down;
            }
        }

        public bool CanFitInInventory(Item item, int count)
        {
            return m_Inventory.CanFitItem(item, count);
        }
        
        public bool AddItem(Item newItem)
        {
            CreateItemVisual(newItem);
            return m_Inventory.AddItem(newItem);
        }

        public void SellItem(int inventoryIndex, int count)
        {
            if (inventoryIndex < 0 || inventoryIndex > Inventory.Entries.Length)
                return;

            var item = Inventory.Entries[inventoryIndex].Item;

            if (item == null || !(item is Product product))
                return;
            
            int actualCount = m_Inventory.Remove(inventoryIndex, count);

            m_Coins += actualCount * product.SellPrice;
            UIHandler.UpdateCoins(m_Coins);
            UIHandler.PlayBuySellSound(transform.position);
        }

        public bool BuyItem(Item item)
        {
            if (item.BuyPrice > m_Coins)
            {
                return false;
            }

            m_Coins -= item.BuyPrice;
            UIHandler.UpdateCoins(m_Coins);
            UIHandler.PlayBuySellSound(transform.position);
            AddItem(item);
            return true;
        }

        public void ChangeEquipItem(int index)
        {
            ToggleToolVisual(false);
            m_Inventory.EquipItem(index);
            ToggleToolVisual(true);
        }

        public void ToggleControl(bool canControl)
        {
            m_CanControl = canControl;
            if (canControl)
            {
                m_MoveAction.Enable();
                m_NextItemAction.Enable();
                m_PrevItemAction.Enable();
                m_UseItemAction.Enable();
            }
            else
            {
                m_MoveAction.Disable();
                m_NextItemAction.Disable();
                m_PrevItemAction.Disable();
                m_UseItemAction.Disable();
            }
        }

        public void UseItem()
        {
            if(m_Inventory.EquippedItem == null)
                return;
            
            var previousEquipped = m_Inventory.EquippedItem;
            
            m_Inventory.UseEquippedObject(m_CurrentTarget);

            if (m_ItemVisualInstance.ContainsKey(previousEquipped))
            {
                var visual = m_ItemVisualInstance[previousEquipped];

                m_Animator.SetTrigger(visual.AnimatorHash);

                if (visual.Animator != null)
                {
                    if (!visual.Instance.activeInHierarchy)
                    {
                        var current = visual.Instance.transform;
                        while (current != null)
                        {
                            current.gameObject.SetActive(true);
                            current = current.parent;
                        }
                    }
                    
                    visual.Animator.SetFloat(m_DirXHash, m_CurrentLookDirection.x);
                    visual.Animator.SetFloat(m_DirYHash, m_CurrentLookDirection.y);
                    visual.Animator.SetTrigger("Use");
                }
            }
            
            if (m_Inventory.EquippedItem == null)
            {
                if (previousEquipped != null)
                {
                    StartCoroutine(DelayedObjectDisable(previousEquipped));
                }
            }
        }

        IEnumerator DelayedObjectDisable(Item item)
        {
            yield return new WaitForSeconds(1.0f);
            ToggleVisualExplicit(false, item);
        }

        public void Save(ref PlayerSaveData data)
        {
            data.Position = m_Rigidbody.position;
            data.Coins = m_Coins;
            data.Inventory = new List<InventorySaveData>();
            m_Inventory.Save(ref data.Inventory);
        }

        public void Load(PlayerSaveData data)
        {
            m_Coins = data.Coins;
            m_Inventory.Load(data.Inventory);

            m_Rigidbody.position = data.Position;
        }

        void ToggleToolVisual(bool enable)
        {
            if (m_Inventory.EquippedItem != null && m_ItemVisualInstance.TryGetValue(m_Inventory.EquippedItem, out var itemVisual))
            {
                itemVisual.Instance.SetActive(enable);
            }
        }

        void ToggleVisualExplicit(bool enable, Item item)
        {
            if (item != null && m_ItemVisualInstance.TryGetValue(item, out var itemVisual))
            {
                itemVisual.Instance.SetActive(enable);
            }
        }

        void CreateItemVisual(Item item)
        {
            if (item.VisualPrefab != null && !m_ItemVisualInstance.ContainsKey(item))
            {
                var newVisual = Instantiate(item.VisualPrefab, ItemAttachBone, false);
                newVisual.SetActive(false);
                
                m_ItemVisualInstance[item] = new ItemInstance()
                {
                    Instance = newVisual,
                    Animator = newVisual.GetComponentInChildren<Animator>(),
                    AnimatorHash = Animator.StringToHash(item.PlayerAnimatorTriggerUse)
                };
            }
        }
    }

    class ItemInstance
    {
        public GameObject Instance;
        public Animator Animator;
        public int AnimatorHash;
    }

    [System.Serializable]
    public struct PlayerSaveData
    {
        public Vector3 Position;
        public int Coins;
        public List<InventorySaveData> Inventory;
    }
}