using Oculus.Interaction.Locomotion;
using StarterAssets;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public float gameTime = 600f;
    private float _elapsedTime = 0f;
    private bool _gameOngoing = false;
    private bool _playedBeepSound = false;

    [SerializeField] private TreeSpawner _treeSpawner;
    [SerializeField] private TractorScript _tractorScript;
    [SerializeField] private BucketContentScript _tractorBucketContent;
    [SerializeField] private FirstPersonLocomotor _locomotor;
    [SerializeField] private FirstPersonController _firstPersonController;
    public bool isVrMode = true;

    [SerializeField] private float _scoreMultiplier = 10.0f;
    [SerializeField] private int _bucketNotPutBackPenalty = 25;
    [SerializeField] private List<BucketInteraction> _allBuckets;
    private int _NbBucketsNotPutBack
    {
        get
        {
            int count = 0;
            foreach (BucketInteraction bucket in _allBuckets)
            {
                if (!bucket.IsGrabbed && !bucket.IsHooked)
                {
                    count++;
                }
            }
            return count;
        }
    }

    [SerializeField] private GameObject _endGameUI;
    [SerializeField] private TMP_Text _mapleWaterPts;
    [SerializeField] private TMP_Text _bucketPenaltyPts;
    [SerializeField] private TMP_Text _finalScorePts;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("GameManager: instance existante détectée — destruction du duplicata.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void Start()
    {
        _treeSpawner.SpawnTrees();
        _tractorScript.Init();
        _gameOngoing = true;
        _endGameUI.SetActive(false);
    }

    public void Update()
    {
        if (!_gameOngoing)
            return;

        _elapsedTime += Time.deltaTime;

        if (!_playedBeepSound && _elapsedTime >= (gameTime - 30f))
        {
            _tractorScript.PlayBeepSound();
            _playedBeepSound = true;
        }

        if (_elapsedTime >= gameTime)
        {
            EndGame();
        }
    }

    private void EndGame()
    {
        _gameOngoing = false;

        if (isVrMode)
            _locomotor.DisableMovement();
        else
            _firstPersonController.MoveSpeed = 0;
        _tractorScript.enabled = false;

        int score = Mathf.RoundToInt(_tractorBucketContent.currentFillAmount * _scoreMultiplier);
        int penalty = _NbBucketsNotPutBack * _bucketNotPutBackPenalty;
        int finalScore = score - penalty;

        Debug.Log($"Score final : {finalScore}");

        _mapleWaterPts.text = $"{score} points";
        _bucketPenaltyPts.text = $"-{penalty} points";
        _finalScorePts.text = $"{finalScore} points";
        _endGameUI.SetActive(true);
    }

    public void RegisterBucket(BucketInteraction bucket)
    {
        _allBuckets.Add(bucket);
    }

    public void UpdatePlayerSpeed(float speedChange)
    {
        if (isVrMode)
        {
            _locomotor.SpeedFactor += speedChange;
            _locomotor.RunningSpeedFactor += speedChange;
        }
        else
        {
            _firstPersonController.MoveSpeed += speedChange / 5;
            _firstPersonController.SprintSpeed += speedChange / 5;
        }
    }
}
