using System;
using UnityEngine;

public class BucketContentScript : MonoBehaviour
{
    [SerializeField] private MapleWaterVisual _mapleWaterVisual;
    [SerializeField] private AudioSource _audioSource;
    public AudioSource AudioSource => _audioSource;
    [SerializeField] private AudioClip[] _pouringSounds;
    private bool pouring = false;

    public float maxFillAmount = 10.0f;
    public float currentFillAmount = 0.0f; 
    public float transferRatePerSecond = 4f;

    private float upsideDownAngleThreshold = 90f;
    private float transferCheckDistance = 1.5f;
    private LayerMask bucketLayer = ~6;

    [SerializeField] private bool _debugMode = false;

    public void Awake()
    {
        UpdateVisual();
    }

    public void Update()
    {
        if (IsUpsideDown() && currentFillAmount > 0)
        {
            BucketContentScript target = FindBucketBelow();
            if (target != null)
            {
                if (_debugMode)
                    Debug.Log("Bucket found, transferring content.");
                TransferTo(target);
            }
            else
            {
                if (_debugMode)
                    Debug.Log("No bucket found below, spilling content.");
                float amountThisFrame = transferRatePerSecond * Time.deltaTime;
                float transferable = Mathf.Min(currentFillAmount, amountThisFrame);
                currentFillAmount -= transferable;
            }
            UpdateVisual();

            if (!pouring && _pouringSounds.Length > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, _pouringSounds.Length);
                AudioClip clipToPlay = _pouringSounds[randomIndex];
                _audioSource.PlayOneShot(clipToPlay);
            }
            pouring = true;
        }
        else
        {
            pouring = false;
        }
    }

    private bool IsUpsideDown()
    {
        float angle = Vector3.Angle(transform.up, Vector3.up);
        return angle > upsideDownAngleThreshold;
    }

    private BucketContentScript FindBucketBelow()
    {
        RaycastHit[] hits = Physics.RaycastAll(transform.position, Vector3.down, transferCheckDistance, bucketLayer.value);
        if (hits == null || hits.Length == 0) return null;

        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.transform.IsChildOf(transform)) continue;

            BucketContentScript other = hit.collider.GetComponentInParent<BucketContentScript>();
            if (other != null && other != this)
            {
                if (hit.point.y < transform.position.y - 0.01f)
                    return other;
            }
        }
        return null;
    }

    private void TransferTo(BucketContentScript target)
    {
        if (target == null) return;

        float amountThisFrame = transferRatePerSecond * Time.deltaTime;

        float transferable = Mathf.Min(currentFillAmount, amountThisFrame);
        float space = Mathf.Max(0f, target.maxFillAmount - target.currentFillAmount);
        float transfer = Mathf.Min(transferable, space);

        target.currentFillAmount += transfer;
        target.UpdateVisual();
        currentFillAmount -= transferable;
    }

    public void SetFillAmount(float amount)
    {
        currentFillAmount = Mathf.Clamp(amount, 0, maxFillAmount);
        UpdateVisual();
    }

    public void UpdateVisual()
    {
        if (_mapleWaterVisual != null)
            _mapleWaterVisual.UpdateVisual(currentFillAmount, maxFillAmount);
    }

    public float GetWeight()
    {
        return currentFillAmount / 2;
    }
}
