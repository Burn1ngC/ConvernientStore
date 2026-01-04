using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemInspectUI : MonoBehaviour
{
    public static ItemInspectUI Instance;

    [Header("UI Refs")]
    public GameObject root;
    public Image packImage;
    public TMP_Text titleText;
    public Button putButton;
    public Button closeButton;

    [Header("Chase Trigger")]
    public ChaseSpawner chaseSpawner; // 拖场景里的 ChaseSystem（挂了ChaseSpawner的物体）

    ItemPickup current;

    // ✅ 只触发一次追逐
    private bool chaseTriggeredOnce = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        CloseImmediate();
    }

    public void Open(ItemPickup pickup)
    {
        if (pickup == null || pickup.data == null)
        {
            Debug.LogWarning("[ItemInspectUI] Open failed: pickup or pickup.data is null.");
            return;
        }

        current = pickup;

        if (root != null) root.SetActive(true);
        InputLock.Locked = true;

        if (packImage != null) packImage.sprite = pickup.data.icon;
        if (titleText != null) titleText.text = pickup.data.displayName;

        if (putButton != null)
        {
            putButton.onClick.RemoveAllListeners();
            putButton.onClick.AddListener(OnPut);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Close);
        }
    }

    void OnPut()
    {
        if (current == null || current.data == null)
        {
            Close();
            return;
        }

        current.collected = true;

        if (GameManager.Instance != null)
            GameManager.Instance.OnItemCollected(current.data);

        // 先关UI解锁输入（否则你还在Locked状态）
        Close();

        // ✅ 只触发一次追逐
        if (!chaseTriggeredOnce)
        {
            chaseTriggeredOnce = true;

            if (chaseSpawner != null)
                chaseSpawner.TriggerChaseOnce();
            else
                Debug.LogWarning("[ItemInspectUI] chaseSpawner 没有拖引用，无法触发追逐。");
        }
    }

    public void Close()
    {
        current = null;
        if (root != null) root.SetActive(false);
        InputLock.Locked = false;
    }

    void CloseImmediate()
    {
        current = null;
        if (root != null) root.SetActive(false);
        InputLock.Locked = false;
    }
}
