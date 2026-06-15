using UnityEngine;

public class RandomObjectSpawner : MonoBehaviour
{
    [Header("Spawn Setup")]
    public GameObject interactablePrefab;
    public SwapTarget[] possibleTargets;
    public HandSwapManager swapManager;

    [Header("Reachable Area")]
    public float radiusMin = 0.15f;
    public float radiusMax = 0.35f;
    public float minHeightOffset = -0.05f;
    public float maxHeightOffset = 0.15f;

    public AutonomyHandAssistTarget CurrentAssistTarget { get; private set; }

    private GameObject _spawnedObject;
    private SwapTarget _currentTarget;
    private Vector3? _lastLocalOffset;

    public SwapTarget CurrentTarget => _currentTarget;
    public bool HasActiveObject => _spawnedObject != null;

    public void SpawnObject()
    {
        if (_spawnedObject != null || interactablePrefab == null || possibleTargets == null || possibleTargets.Length == 0)
            return;

        _currentTarget = ChooseRandomTarget();

        if (_currentTarget == null || _currentTarget.handSpawnCenter == null)
        {
            Debug.LogWarning("RandomObjectSpawner: chosen target or handSpawnCenter is missing.");
            return;
        }

        Vector3 localOffset = _lastLocalOffset ?? GetRandomLocalOffset();
        _lastLocalOffset = localOffset;

        Vector3 spawnPos = _currentTarget.handSpawnCenter.TransformPoint(localOffset);
        Quaternion spawnRot = _currentTarget.handSpawnCenter.rotation;

        _spawnedObject = Instantiate(interactablePrefab, spawnPos, spawnRot);
        _spawnedObject.name = interactablePrefab.name + "_Spawned";

        CurrentAssistTarget = _spawnedObject.GetComponentInChildren<AutonomyHandAssistTarget>(true);

        if (CurrentAssistTarget != null && _currentTarget != null)
        {
            CurrentAssistTarget.leftAssistPositionOffset = _currentTarget.leftHandPositionOffset;
            CurrentAssistTarget.rightAssistPositionOffset = _currentTarget.rightHandPositionOffset;
            CurrentAssistTarget.leftAssistRotationOffset = _currentTarget.leftHandRotationOffset;
            CurrentAssistTarget.rightAssistRotationOffset = _currentTarget.rightHandRotationOffset;
            CurrentAssistTarget.ResetAutoPress();
        }

        var trigger = _spawnedObject.GetComponentInChildren<ReturnTriggerObject>(true);
        if (trigger != null)
            trigger.Initialize(swapManager);

        Debug.Log($"RandomObjectSpawner: spawned object for target '{_currentTarget.name}'.");
    }

    public void SpawnObjectForTarget(SwapTarget target)
    {
        if (_spawnedObject != null || interactablePrefab == null || target == null || target.handSpawnCenter == null)
            return;

        _currentTarget = target;

        Vector3 localOffset = GetRandomLocalOffset();
        Vector3 spawnPos = _currentTarget.handSpawnCenter.TransformPoint(localOffset);
        Quaternion spawnRot = _currentTarget.handSpawnCenter.rotation;

        _spawnedObject = Instantiate(interactablePrefab, spawnPos, spawnRot);
        _spawnedObject.name = interactablePrefab.name + "_Spawned";

        CurrentAssistTarget = _spawnedObject.GetComponentInChildren<AutonomyHandAssistTarget>(true);

        if (CurrentAssistTarget != null)
        {
            CurrentAssistTarget.leftAssistPositionOffset = _currentTarget.leftHandPositionOffset;
            CurrentAssistTarget.rightAssistPositionOffset = _currentTarget.rightHandPositionOffset;
            CurrentAssistTarget.leftAssistRotationOffset = _currentTarget.leftHandRotationOffset;
            CurrentAssistTarget.rightAssistRotationOffset = _currentTarget.rightHandRotationOffset;
            CurrentAssistTarget.ResetAutoPress();
        }

        var trigger = _spawnedObject.GetComponentInChildren<ReturnTriggerObject>(true);
        if (trigger != null)
            trigger.Initialize(swapManager);

        Debug.Log($"Spawner: spawned object for target '{_currentTarget.name}'.");
    }

    public void HideSpawnedObjectImmediately()
    {
        if (_spawnedObject != null)
        {
            Destroy(_spawnedObject);
            _spawnedObject = null;
        }
        
        CurrentAssistTarget = null;
    }

    public void DespawnObject()
    {
        if (_spawnedObject != null)
        {
            Destroy(_spawnedObject);
            _spawnedObject = null;
        }
        
        CurrentAssistTarget = null;
    }

    public void ClearCurrentTarget()
    {
        _currentTarget = null;
    }

    public void ResetSpawnLocation()
    {
        _lastLocalOffset = null;
    }

    private SwapTarget ChooseRandomTarget()
    {
        if (possibleTargets == null || possibleTargets.Length == 0)
            return null;

        int index = Random.Range(0, possibleTargets.Length);
        return possibleTargets[index];
    }

    private Vector3 GetRandomLocalOffset()
    {
        Vector2 circle = Random.insideUnitCircle.normalized * Random.Range(radiusMin, radiusMax);
        float y = Random.Range(minHeightOffset, maxHeightOffset);
        return new Vector3(circle.x, y, circle.y);
    }
}