using System.Collections;
using UnityEngine;

public class HandSwapManager : MonoBehaviour
{
    [Header("Core References")]
    public Transform rigRoot;
    public Transform playerReference;

    [Header("Targets")]
    public SwapTarget[] swapTargets;

    [Header("Player Hand Visuals")]
    public SkinnedMeshRenderer[] playerHandRenderers;

    [Header("Startup Setup")]
    public float initialSwapDelay = 0.1f;
    public float initialObjectSpawnDelay = 0.1f;

    [Header("Auto Swap")]
    public float objectSpawnDelay = 2f;
    public float autoSwapDelayAfterObjectSpawn = 3f;

    [Header("Fade")]
    public OVRScreenFade screenFader;
    public float blinkOutDuration = 0.08f;
    public float blinkHoldDuration = 0.03f;
    public float blinkInDuration = 0.10f;

    [Header("Options")]
    public bool yawOnlyRotation = true;

    [Header("Spawned Interaction Object")]
    public RandomObjectSpawner spawner;

    [Header("Autonomy Manager")]
    public SecondaryBehaviourManager assistantManager;

    [Header("Experiment Controller")]
    public ExperimentController experimentController;

    private bool _isSwapping;
    private SwapTarget _currentTarget;
    private SwapTarget _pendingTarget;
    private Coroutine _swapCycleRoutine;
    private SwapTarget _previousTarget;
    private bool _firstCycle = true;
    private bool _experimentPaused;

    private void Start()
    {
        if (!screenFader)
            screenFader = FindFirstObjectByType<OVRScreenFade>();

        if (swapTargets == null || swapTargets.Length == 0)
        {
            Debug.LogError("HandSwapManager: no swap targets assigned.");
            enabled = false;
            return;
        }

        _currentTarget = swapTargets[Random.Range(0, swapTargets.Length)];
        _previousTarget = null;

        if (_currentTarget == null || _currentTarget.reference == null)
        {
            Debug.LogError("HandSwapManager: starting target/reference missing.");
            enabled = false;
            return;
        }

        MoveRigToReference(_currentTarget.reference);
        ApplyTargetMaterial(_currentTarget.playerHandMaterial);
        UpdateWorldVisualState();

        _swapCycleRoutine = StartCoroutine(AutoSwapCycle());
    }

    public void NotifySpawnedObjectInteracted()
    {
        if (_isSwapping)
            return;

        if (spawner != null)
        {
            spawner.HideSpawnedObjectImmediately();

        if (assistantManager != null)
        {
            assistantManager.currentAssistTarget = null;
            assistantManager.DisableAllAssist();
            assistantManager.ApplyAutonomyModes(null);
        }

        if (experimentController != null)
            experimentController.OnObjectButtonPressed();
        }
        else
        {
            ContinueExperimentLoop();
        }
    }

    private IEnumerator AutoSwapCycle()
    {
        if (_experimentPaused)
            yield break;
            
        float spawnDelay = _firstCycle ? initialObjectSpawnDelay : objectSpawnDelay;
        float swapDelay = _firstCycle ? initialSwapDelay : autoSwapDelayAfterObjectSpawn;

        _firstCycle = false;

        yield return new WaitForSeconds(spawnDelay);

        _pendingTarget = PickRandomTarget();

        if (_pendingTarget == null)
        {
            Debug.LogWarning("HandSwapManager: No valid swap target left, waiting for questionnaire.");
            yield break;
        }

        if (spawner != null)
            spawner.SpawnObjectForTarget(_pendingTarget);

        yield return new WaitForSeconds(swapDelay);

        if (_pendingTarget != null)
            yield return StartCoroutine(SwapToTargetRoutine(_pendingTarget));

        _pendingTarget = null;
    }

    private IEnumerator SwapToTargetRoutine(SwapTarget target)
    {
        _isSwapping = true;

        yield return StartCoroutine(FadeOutOnly());
        yield return new WaitForSeconds(blinkHoldDuration);

        MoveRigToReference(target.reference);
        ApplyTargetMaterial(target.playerHandMaterial);

        _previousTarget = _currentTarget;
        _currentTarget = target;
        UpdateWorldVisualState();

        if (assistantManager != null && spawner != null)
        {
            assistantManager.currentAssistTarget = spawner.CurrentAssistTarget;
            assistantManager.ApplyAssistHandMaterial(target.playerHandMaterial);
            assistantManager.ApplyAutonomyModes(target);
        }

        yield return StartCoroutine(FadeInOnly());

        _isSwapping = false;

        experimentController?.OnAutoSwapCompleted(target);
    }

    private void MoveRigToReference(Transform targetReference)
    {
        Vector3 currentPos = playerReference.position;
        Vector3 targetPos = targetReference.position;

        Quaternion currentRot = playerReference.rotation;
        Quaternion targetRot = targetReference.rotation;

        Quaternion deltaRot;

        if (yawOnlyRotation)
        {
            float currentYaw = currentRot.eulerAngles.y;
            float targetYaw = targetRot.eulerAngles.y;
            deltaRot = Quaternion.Euler(0f, targetYaw - currentYaw, 0f);
        }
        else
        {
            deltaRot = targetRot * Quaternion.Inverse(currentRot);
        }

        Vector3 deltaPos = targetPos - (deltaRot * currentPos);

        rigRoot.SetPositionAndRotation(
            deltaRot * rigRoot.position + deltaPos,
            deltaRot * rigRoot.rotation
        );
    }

    private IEnumerator FadeOutOnly()
    {
        if (screenFader != null)
        {
            screenFader.fadeTime = blinkOutDuration;
            screenFader.FadeOut();
            yield return new WaitForSeconds(blinkOutDuration);
        }
    }

    private IEnumerator FadeInOnly()
    {
        if (screenFader != null)
        {
            screenFader.fadeTime = blinkInDuration;
            screenFader.FadeIn();
            yield return new WaitForSeconds(blinkInDuration);
        }
    }

    private void ApplyTargetMaterial(Material targetMaterial)
    {
        if (playerHandRenderers == null || targetMaterial == null)
            return;

        foreach (var renderer in playerHandRenderers)
        {
            if (!renderer) continue;

            var mats = renderer.materials;
            for (int i = 0; i < mats.Length; i++)
                mats[i] = targetMaterial;

            renderer.materials = mats;
        }
    }

    private void UpdateWorldVisualState()
    {
        if (swapTargets == null)
            return;

        foreach (var target in swapTargets)
        {
            if (target == null) continue;

            // Hide currently possessed hands/body, show all others
            target.SetVisible(target != _currentTarget);
        }
    }

    private SwapTarget PickRandomTarget()
    {
        if (swapTargets == null || swapTargets.Length == 0)
            return null;

        var validTargets = new System.Collections.Generic.List<SwapTarget>();

        foreach (var target in swapTargets)
        {
            if (target == null)
                continue;
            if (experimentController != null && !experimentController.CanUseTarget(target))
                continue;

            // Exclude current and previous target
            if (target == _currentTarget) continue;
            if (target == _previousTarget) continue;

            validTargets.Add(target);
        }

        // Fallback if you only have 1–2 targets or all were excluded
        if (validTargets.Count == 0)
        {
            foreach (var target in swapTargets)
            {
                if (target == null)
                    continue;
                if (experimentController != null && !experimentController.CanUseTarget(target))
                    continue;
                if (target == _currentTarget) continue;

                validTargets.Add(target);
            }
        }

        if (validTargets.Count == 0)
            return null;

        return validTargets[Random.Range(0, validTargets.Count)];
    }

    public void ContinueExperimentLoop()
    {
        if (_experimentPaused)
            return;

        if (_swapCycleRoutine != null)
            StopCoroutine(_swapCycleRoutine);

        _swapCycleRoutine = StartCoroutine(AutoSwapCycle());
    }

    public void PauseSwapLoop()
    {
        _experimentPaused = true;

        if (_swapCycleRoutine != null)
        {
            StopCoroutine(_swapCycleRoutine);
            _swapCycleRoutine = null;
        }

        if (spawner != null)
        {
            spawner.HideSpawnedObjectImmediately();
            spawner.ClearCurrentTarget();
        }

        if (assistantManager != null)
        {
            assistantManager.currentAssistTarget = null;
            assistantManager.DisableAllAssist();
        }
    }

    public void ResumeSwapLoop()
    {
        _experimentPaused = false;

        if (_swapCycleRoutine != null)
            StopCoroutine(_swapCycleRoutine);

        _swapCycleRoutine = StartCoroutine(AutoSwapCycle());
    }

    public void StartQuestionnaireSwap(SwapTarget target)
    {
        if (_swapCycleRoutine != null)
            StopCoroutine(_swapCycleRoutine);

        _swapCycleRoutine = StartCoroutine(QuestionnaireSwapRoutine(target));
    }

    private IEnumerator QuestionnaireSwapRoutine(SwapTarget target)
    {
        if (target == null)
            yield break;

        yield return new WaitForSeconds(objectSpawnDelay);

        if (spawner != null)
            spawner.SpawnObjectForTarget(target);

        yield return new WaitForSeconds(autoSwapDelayAfterObjectSpawn);

        yield return StartCoroutine(SwapToTargetRoutine(target));
    }
}