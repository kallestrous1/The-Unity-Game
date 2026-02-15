using System.Collections.Generic;
using System.Xml;
using UnityEngine;

public abstract class EntityPersistenceBehaviour
    : DataPersistenceBehaviour, IEntityPersistence
{
    //public abstract string DebugID { get; }

    [SerializeField] private string uniqueID;
    public override string DebugID => uniqueID;


#if UNITY_EDITOR
    protected virtual void Reset()
    {
        GenerateID();
    }

    protected virtual void OnValidate()
    {
        if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(this))
            return;

        if (string.IsNullOrEmpty(uniqueID))
            GenerateID();
    }

    [ContextMenu("Regenerate Unique ID")]
    private void RegenerateID()
    {
        uniqueID = System.Guid.NewGuid().ToString();
    }
#endif

    protected override void OnEnable()
    {
        base.OnEnable();

        EntityIDRegistry.Register(uniqueID, this);
    }

    protected override void OnDisable()
    {
        EntityIDRegistry.Unregister(uniqueID, this);
        base.OnDisable();
    }

    protected void GenerateID()
    {
        uniqueID = System.Guid.NewGuid().ToString();
    }
}

public static class EntityIDRegistry
{
    private static readonly Dictionary<string, List<Object>> registry = new();

    public static void Register(string id, Object obj)
    {
        if (string.IsNullOrEmpty(id)) return;

        if (!registry.TryGetValue(id, out var list))
        {
            list = new List<Object>();
            registry[id] = list;
        }

        list.Add(obj);

        if (list.Count > 1)
        {
            Debug.LogWarning($"ENTITY ID '{id}' is used by multiple objects:" + obj.name, obj);
            Debug.LogError($"Duplicate ENTITY ID detected: {id}", obj);
        }
    }

    public static void Unregister(string id, Object obj)
    {
        if (!registry.TryGetValue(id, out var list)) return;

        list.Remove(obj);
        if (list.Count == 0)
            registry.Remove(id);
    }
}
