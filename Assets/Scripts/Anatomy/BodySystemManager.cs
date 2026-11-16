using System;
using System.Collections.Generic;
using UnityEngine;

public class BodySystemManager : MonoBehaviour
{
    public static BodySystemManager Instance { get; private set; }

    [System.Serializable]
    public class BodySystemEntry
    {
        public string systemName;
        public GameObject rootObject;
        public bool isVisible = true;
    }

    [Header("Anatomical Systems")]
    public List<BodySystemEntry> systems = new List<BodySystemEntry>();

    public event Action<string, bool> OnSystemVisibilityChanged;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void RegisterSystem(string name, GameObject root)
    {
        var existing = systems.Find(s => s.systemName.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            existing.rootObject = root;
        }
        else
        {
            systems.Add(new BodySystemEntry { systemName = name, rootObject = root, isVisible = true });
        }
    }

    public void SetSystemVisibility(string systemName, bool visible)
    {
        var entry = systems.Find(s => s.systemName.Equals(systemName, StringComparison.OrdinalIgnoreCase));
        if (entry != null)
        {
            entry.isVisible = visible;
            if (entry.rootObject != null)
            {
                entry.rootObject.SetActive(visible);
            }
            OnSystemVisibilityChanged?.Invoke(systemName, visible);
        }
    }

    public void ToggleSystem(string systemName)
    {
        var entry = systems.Find(s => s.systemName.Equals(systemName, StringComparison.OrdinalIgnoreCase));
        if (entry != null)
        {
            SetSystemVisibility(systemName, !entry.isVisible);
        }
    }

    public bool IsSystemVisible(string systemName)
    {
        var entry = systems.Find(s => s.systemName.Equals(systemName, StringComparison.OrdinalIgnoreCase));
        return entry != null && entry.isVisible;
    }

    public void ShowAllSystems()
    {
        foreach (var entry in systems)
        {
            entry.isVisible = true;
            if (entry.rootObject != null) entry.rootObject.SetActive(true);
            OnSystemVisibilityChanged?.Invoke(entry.systemName, true);
        }
    }

    public void HideAllSystems()
    {
        foreach (var entry in systems)
        {
            entry.isVisible = false;
            if (entry.rootObject != null) entry.rootObject.SetActive(false);
            OnSystemVisibilityChanged?.Invoke(entry.systemName, false);
        }
    }

    public void IsolateSystem(string systemNameToKeep)
    {
        foreach (var entry in systems)
        {
            bool shouldShow = entry.systemName.Equals(systemNameToKeep, StringComparison.OrdinalIgnoreCase);
            entry.isVisible = shouldShow;
            if (entry.rootObject != null) entry.rootObject.SetActive(shouldShow);
            OnSystemVisibilityChanged?.Invoke(entry.systemName, shouldShow);
        }
    }
}
