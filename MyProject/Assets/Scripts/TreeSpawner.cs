using System.Collections.Generic;
using UnityEngine;

public class TreeSpawner : MonoBehaviour
{
    [SerializeField] private GameObject _treePrefab;
    [SerializeField] private int _numberOfTrees = 50;
    [SerializeField] private Vector2 _areaCoord1 = new Vector2(-2, 22);
    [SerializeField] private Vector2 _areaCoord2 = new Vector2(62, -22);
    [SerializeField] private float _pathExclusion = 3;

    public void SpawnTrees()
    {
        List<GameObject> spawnedTrees = new List<GameObject>(_numberOfTrees);

        for (int i = 0; i < _numberOfTrees; i++)
        {
            float x = Random.Range(_areaCoord2.x, _areaCoord1.x);
            float z;
            do
            {
                z = Random.Range(_areaCoord1.y, _areaCoord2.y);
            } while (z > -_pathExclusion && z < _pathExclusion);
            Vector3 position = new Vector3(x, transform.position.y, z);

            GameObject newTree = Instantiate(_treePrefab, position, Quaternion.identity, transform);
            newTree.transform.Rotate(0, Random.Range(0, 360), 0);

            Vector3 scale = newTree.transform.localScale;
            float radiusScale = Random.Range(0.8f, 1.2f);
            scale.x *= radiusScale;
            scale.y *= Random.Range(0.8f, 1.2f);
            scale.z *= radiusScale;
            newTree.transform.localScale = scale;
        }

        if (spawnedTrees.Count > 0)
        {
            // Marquer le parent statique : utile pour que Unity prenne en compte le batching.
            // Idéalement, marquez les objets statiques dans l'éditeur pour de meilleures performances.
            try
            {
                gameObject.isStatic = true;
                foreach (GameObject t in spawnedTrees)
                {
                    t.isStatic = true;
                }

                // Combine tous les arbres sous ce parent pour réduire les draw calls.
                StaticBatchingUtility.Combine(spawnedTrees.ToArray(), gameObject);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("Static batching failed: " + ex.Message);
            }
        }
    }
}
