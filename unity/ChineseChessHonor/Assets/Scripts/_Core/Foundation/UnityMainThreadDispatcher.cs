// 文件路径: Assets/Scripts/_Core/Foundation/UnityMainThreadDispatcher.cs

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 一个确保任务在Unity主线程上执行的辅助工具。
/// 在处理来自网络或其他线程的回调时非常有用，因为Unity的API（如UI操作）只能在主线程调用。
/// </summary>
public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static readonly Queue<Action> _executionQueue = new Queue<Action>();
    private static UnityMainThreadDispatcher _instance = null;

    public static UnityMainThreadDispatcher Instance()
    {
        if (_instance == null)
        {
            // 尝试在场景中寻找实例
            _instance = FindObjectOfType<UnityMainThreadDispatcher>();
            if (_instance == null)
            {
                // 如果找不到，则动态创建一个
                var go = new GameObject("[UnityMainThreadDispatcher]");
                _instance = go.AddComponent<UnityMainThreadDispatcher>();
                // 确保它在场景切换时不被销毁
                DontDestroyOnLoad(go);
            }
        }
        return _instance;
    }

    /// <summary>
    /// 将一个Action（方法）排入队列，等待在下一帧的主线程Update中执行。
    /// </summary>
    /// <param name="action">要执行的方法</param>
    public void Enqueue(Action action)
    {
        // 使用锁确保线程安全
        lock (_executionQueue)
        {
            _executionQueue.Enqueue(action);
        }
    }

    private void Update()
    {
        // 在主线程的Update循环中，不断检查队列并执行任务
        lock (_executionQueue)
        {
            while (_executionQueue.Count > 0)
            {
                _executionQueue.Dequeue().Invoke();
            }
        }
    }
}