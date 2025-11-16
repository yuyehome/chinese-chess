// 文件路径: Assets/Scripts/_App/UI/UIManager.cs

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    private static UIManager _instance;
    public static UIManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<UIManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("[UIManager]");
                    _instance = go.AddComponent<UIManager>();
                }
            }
            return _instance;
        }
    }

    [Header("UI 配置")]
    [SerializeField] private List<UIPanel> panelPrefabs;
    [SerializeField] private Transform panelsParent;

    private Dictionary<Type, UIPanel> _panelInstances = new Dictionary<Type, UIPanel>();

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    public T ShowPanel<T>() where T : UIPanel
    {
        Type panelType = typeof(T);
        Debug.Log($"[UIManager] 请求显示面板: {panelType.Name}");

        if (_panelInstances.TryGetValue(panelType, out UIPanel panel) && panel != null)
        {
            Debug.Log($"[UIManager] 面板 '{panelType.Name}' 已存在于缓存中，直接调用Show()。");
            panel.Show();
            return panel as T;
        }
        else
        {
            Debug.Log($"[UIManager] 面板 '{panelType.Name}' 不存在，尝试从Prefab创建。");
            T panelPrefab = GetPanelPrefab<T>();
            if (panelPrefab != null)
            {
                Debug.Log($"[UIManager] 成功找到 '{panelType.Name}' 的Prefab，正在实例化...");
                T newPanelInstance = Instantiate(panelPrefab, panelsParent);
                newPanelInstance.gameObject.name = panelType.Name;
                _panelInstances[panelType] = newPanelInstance;

                Debug.Log($"[UIManager] 面板 '{panelType.Name}' 实例化成功，调用Setup()。");
                newPanelInstance.Setup();

                Debug.Log($"[UIManager] 面板 '{panelType.Name}' Setup完毕，调用Show()。");
                newPanelInstance.Show();
                return newPanelInstance;
            }
        }
        Debug.LogError($"[UIManager] 显示面板 '{panelType.Name}' 失败，流程中断。");
        return null;
    }

    public void HidePanel<T>() where T : UIPanel
    {
        Type panelType = typeof(T);
        Debug.Log($"[UIManager] 请求隐藏面板: {panelType.Name}");
        if (_panelInstances.TryGetValue(panelType, out UIPanel panel) && panel != null)
        {
            panel.Hide();
        }
        else
        {
            Debug.LogWarning($"[UIManager] 尝试隐藏一个从未被创建的面板: {panelType.Name}");
        }
    }

    private T GetPanelPrefab<T>() where T : UIPanel
    {
        if (panelPrefabs == null || panelPrefabs.Count == 0)
        {
            Debug.LogError("[UIManager] 'panelPrefabs' 列表为空！请在Inspector中配置！");
            return null;
        }

        T prefab = panelPrefabs.OfType<T>().FirstOrDefault();
        if (prefab == null)
        {
            Debug.LogError($"[UIManager] 未能在panelPrefabs列表中找到类型为 {typeof(T).Name} 的Prefab！请检查UIManager的配置。");
        }
        return prefab;
    }
}