using UnityEngine;

public class TreeBucketSpawner : MonoBehaviour
{
    [SerializeField] private GameObject _chalumeauPrefab;
    [SerializeField] private GameObject _bucketPrefab;
    [SerializeField] private float _treeRadius = 0.2f;
    [SerializeField] private float _bucketHeight = 0.9f;

    private int _numberOfBuckets;
    private float _treeRadiusScaled;

    public void Start()
    {
        _treeRadiusScaled = _treeRadius * transform.localScale.x;
        _numberOfBuckets = Mathf.CeilToInt(2 * Mathf.PI * _treeRadiusScaled / 0.7f);
        //Debug.Log("Spawning " + _numberOfBuckets + " buckets around tree with scaled radius " + _treeRadiusScaled);

        SpawnBucketsAndChalumeau();
    }

    private void SpawnBucketsAndChalumeau()
    {
        for (int i = 0; i < _numberOfBuckets; i++)
        {
            float angle = i * (360f / _numberOfBuckets);
            float radian = angle * Mathf.Deg2Rad;

            float chalumeauX = transform.position.x + _treeRadiusScaled * Mathf.Cos(radian);
            float chalumeauZ = transform.position.z + _treeRadiusScaled * Mathf.Sin(radian);
            float chalumeauY = transform.position.y + _bucketHeight + Random.Range(-0.05f, 0.15f);

            float bucketX = transform.position.x + (_treeRadiusScaled + 0.05f) * Mathf.Cos(radian);
            float bucketZ = transform.position.z + (_treeRadiusScaled + 0.05f) * Mathf.Sin(radian);
            float bucketY = chalumeauY - 0.047f;

            Vector3 chalumeauPosition = new Vector3(chalumeauX, chalumeauY, chalumeauZ);
            Vector3 bucketPosition = new Vector3(bucketX, bucketY, bucketZ);

            Vector3 center = new Vector3(transform.position.x, chalumeauY, transform.position.z);
            Vector3 directionToOutwards = chalumeauPosition - center;

            Quaternion chalumeauRotation = Quaternion.LookRotation(directionToOutwards, Vector3.up);
            chalumeauRotation.eulerAngles = new Vector3(0, chalumeauRotation.eulerAngles.y + 90f, 0);
            Quaternion bucketRotation = chalumeauRotation;
            bucketRotation.eulerAngles = new Vector3(0, bucketRotation.eulerAngles.y + 90f, 0);

            GameObject newChalumeau = Instantiate(_chalumeauPrefab, chalumeauPosition, chalumeauRotation);
            GameObject newBucket = Instantiate(_bucketPrefab, bucketPosition, bucketRotation);

            Rigidbody chalumeauRigidbody = newChalumeau.GetComponentInChildren<Rigidbody>();
            BucketInteraction bucketInteraction = newBucket.GetComponent<BucketInteraction>();
            bucketInteraction.connectedBody = chalumeauRigidbody;
            bucketInteraction.CreateJoint();

            bucketInteraction.BucketContent.SetFillAmount(Random.Range(0.1f, bucketInteraction.BucketContent.maxFillAmount));

            GameManager.Instance.RegisterBucket(bucketInteraction);
        }
    }
}
