using System;
using System.Collections.Generic;
using UnityEngine;

namespace Viture.XR.Samples.MarkerTrackingDemo
{
    public class TrackedMarkerVisualizer : MonoBehaviour
    {
        [Serializable]
        private struct MarkerPrefabMapping
        {
            public int objectId;
            public GameObject prefab;
        }

        [SerializeField] 
        private MarkerPrefabMapping[] m_PrefabMappings;

        private readonly Dictionary<int, GameObject> m_PrefabLookup = new();
        private readonly Dictionary<int, List<GameObject>> m_InstancePools = new();
        private readonly Dictionary<int, List<VitureTrackedMarker>> m_MarkersByObjectId = new();

        private void Awake()
        {
            foreach (var mapping in m_PrefabMappings)
            {
                if (mapping.prefab != null)
                {
                    m_PrefabLookup[mapping.objectId] = mapping.prefab;
                    m_InstancePools[mapping.objectId] = new List<GameObject>();   
                }
            }
        }
        
        public void OnTrackedMarkersChanged(List<VitureTrackedMarker> markers)
        {
            foreach (var list in m_MarkersByObjectId.Values)
                list.Clear();
            
            foreach (var marker in markers)
            {
                if (!m_MarkersByObjectId.ContainsKey(marker.objectId))
                    m_MarkersByObjectId[marker.objectId] = new List<VitureTrackedMarker>();
                
                m_MarkersByObjectId[marker.objectId].Add(marker);
            }

            foreach (var kvp in m_InstancePools)
            {
                int objectId = kvp.Key;
                List<GameObject> pool = kvp.Value;

                if (m_MarkersByObjectId.TryGetValue(objectId, out var markersForId))
                {
                    while (pool.Count < markersForId.Count)
                        pool.Add(SpawnInstance(objectId));

                    for (int i = 0; i < markersForId.Count; i++)
                    {
                        var instance = pool[i];
                        instance.SetActive(true);
                        instance.transform.SetPositionAndRotation(
                            markersForId[i].pose.position,
                            markersForId[i].pose.rotation);
                    }
                    
                    for (int i = markersForId.Count; i < pool.Count; i++)
                        pool[i].SetActive(false);
                }
                else
                {
                    foreach (var instance in pool)
                        instance.SetActive(false);
                }
            }
        }

        private GameObject SpawnInstance(int objectId)
        {
            if (!m_PrefabLookup.TryGetValue(objectId, out var prefab))
            {
                Debug.LogError($"No prefab assigned for objectId {objectId}");
                return new GameObject($"Missing_Prefab_{objectId}");
            }
            
            var instance = Instantiate(prefab, transform);
            instance.name = $"{prefab.name}_ObjectId_{objectId}";
            instance.SetActive(false);
            return instance;
        }
    }
}
