// 文件路径: Assets/Scripts/_App/UI/UIManager.cs

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// UI系统核心管理器。
/// 负责所有UIPanel的生命周期管理（实例化、显示、隐藏）。
/// 采用单例模式，方便全局访问。
/// </summary>
public class UIManager : MonoBehaviour
{
    // --- 单例模式 ---
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
    [Tooltip("所有UI面板的Prefab列表，在此处注册")]
    [SerializeField] private List<UIPanel> panelPrefabs;

    [Tooltip("所有UI面板实例的父节点")]
    [SerializeField] private Transform panelsParent;

    // 运行时实例化的面板缓存池，通过类型快速查找
    private Dictionary<Type, UIPanel> _panelInstances = new Dictionary<Type, UIPanel>();

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        // DontDestroyOnLoad(gameObject); // 如果需要跨场景，则取消此行注释
    }

    /// <summary>
    /// 显示指定类型的UI面板。
    /// 如果面板从未被创建，则会从Prefab实例化。
    /// </summary>
    /// <typeparam name="T">要显示的UIPanel的子类类型</typeparam>
    /// <returns>显示出的面板实例</returns>
    public T ShowPanel<T>() where T : UIPanel
    {
        Type panelType = typeof(T);
        if (_panelInstances.TryGetValue(panelType, out UIPanel panel))
        {
            panel.Show();
            return panel as T;
        }
        else
        {
            T panelPrefab = GetPanelPrefab<T>();
            if (panelPrefab != null)
            {
                T newPanelInstance = Instantiate(panelPrefab, panelsParent);
                newPanelInstance.gameObject.name = panelType.Name; // 清理实例化的物体名称
                _panelInstances[panelType] = newPanelInstance;
                newPanelInstance.Setup(); // 调用一次性初始化
                newPanelInstance.Show();
                return newPanelInstance;
            }
        }
        return null;
    }

    /// <summary>
    /// 隐藏指定类型的UI面板。
    /// </summary>
    /// <typeparam name="T">要隐藏的UIPanel的子类类型</typeparam>
    public void HidePanel<T>() where T : UIPanel
    {
        Type panelType = typeof(T);
        if (_panelInstances.TryGetValue(panelType, out UIPanel panel))
        {
            panel.Hide();
        }
    }

    /// <summary>
    /// 从预设列表中查找指定类型的面板Prefab。
    /// </summary>
    private T GetPanelPrefab<T>() where T : UIPanel
    {
        T prefab = panelPrefabs.OfType<T>().FirstOrDefault();
        if (prefab == null)
        {
            Debug.LogError($"[UIManager] 未能在panelPrefabs列表中找到类型为 {typeof(T).Name} 的Prefab！请检查UIManager的配置。");
        }
        return prefab;
    }
}