using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExperimentController : MonoBehaviour
{
    private enum State
    {
        Init,
        WaitingForTrial,
        TrialActive,
        Questionnaire,
        End
    }

    [Header("Experiment")]
    public int userId = 0;
    public int trialsPerHand = 20;

    [Header("References")]
    public HandSwapManager handSwapManager;
    public RandomObjectSpawner spawner;
    public DataLogger dataLogger;
    public QuestionnaireLikert questionnaireUI;

    public InstructionPrompts instructionPrompts;
    public int initialInstructionTrials = 3;

    private State _state = State.Init;

    private int _trialId;
    private float _trialStartTime;

    private SwapTarget _currentTrialTarget;

    private Dictionary<SwapTarget, int> _trialCounts = new();
    private HashSet<SwapTarget> _questionnaireCompleted = new();
    private SwapTarget _currentQuestionnaireTarget;
    private bool _questionnaireActive;
    private bool _questionnaireStarted;
    private SwapTarget _pendingQuestionnaireTarget;

    private void Start()
    {
        dataLogger.StartLogging(
            ';',
            "HandSwapSession",
            new[]
            {
                "UserId",
                "TrialId",
                "HandName",
                "HandColour",
                "LeftBehaviour",
                "RightBehaviour",
                "TrialTime",
                "EventType",
                "QuestionnaireScore"
            }
        );

        foreach (var target in handSwapManager.swapTargets)
        {
            if (target != null)
                _trialCounts[target] = 0;
        }

        _state = State.WaitingForTrial;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            Debug.Log("DEBUG: Force questionnaire");
            StartNextQuestionnaireTarget();
        }

        if (Input.GetKeyDown(KeyCode.W))
        {
            Debug.Log("DEBUG: Continue loop");
            handSwapManager.ContinueExperimentLoop();
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("DEBUG: End experiment");
            EndExperiment();
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            Debug.Log("DEBUG: Force finish questionnaire");
            ForceFinishQuestionnaire();
        }

    }

    public bool CanUseTarget(SwapTarget target)
    {
        if (target == null) return false;
        if (!_trialCounts.ContainsKey(target)) return false;
        if (_questionnaireCompleted.Contains(target)) return false;

        // Discard target once it has completed 20 trials
        return _trialCounts[target] < trialsPerHand;
    }

    public void OnAutoSwapCompleted(SwapTarget target)
    {
        if (_state == State.End) return;

        _currentTrialTarget = target;
        _trialStartTime = Time.time;

        if (_pendingQuestionnaireTarget == target)
        {
            _state = State.Questionnaire;

            instructionPrompts?.Show(
                "Questionnaire coming up.\nPress the button to begin."
            );

            return;
        }

        _state = State.TrialActive;

        if (WillButtonPressStartQuestionnaires())
            instructionPrompts?.Show("Questionnaire coming up.\nPress the button to begin.");
        else if (_trialId < initialInstructionTrials)
            instructionPrompts?.Show("Press the button in front of you");
        else
            instructionPrompts?.Hide();
    }

    public void OnObjectButtonPressed()
    {
        Debug.Log("Button pressed, state = " + _state);
        instructionPrompts?.Hide();

        if (_pendingQuestionnaireTarget != null && _currentTrialTarget == _pendingQuestionnaireTarget)
        {
            SwapTarget target = _pendingQuestionnaireTarget;
            _pendingQuestionnaireTarget = null;

            if (spawner != null)
            {
                spawner.HideSpawnedObjectImmediately();
                spawner.ClearCurrentTarget();
            }

            StartQuestionnaire(target);
            return;
        }
        
        if (_state != State.TrialActive || _currentTrialTarget == null)
        {
            return;
        }

        float trialTime = Time.time - _trialStartTime;

        _trialCounts[_currentTrialTarget]++;

        bool shouldStartQuestionnaire = AllTargetsReachedTrialLimit();

        dataLogger.LogTrial(
            userId,
            _trialId,
            _currentTrialTarget.name,
            GetColourName(_currentTrialTarget),
            _currentTrialTarget.rightHandBehaviour.ToString(),
            trialTime,
            "TrialComplete"
        );

        _trialId++;

        instructionPrompts?.Hide();

        if (shouldStartQuestionnaire)
        {
            instructionPrompts?.Hide();
            if (spawner != null)
            {
                spawner.HideSpawnedObjectImmediately();
                spawner.ClearCurrentTarget();
            }

            StartQuestionnaire(_currentTrialTarget);
            return;
        }
        else
        {
            _state = State.WaitingForTrial;
            bool showCountdown = _trialId < initialInstructionTrials;

            if (showCountdown && instructionPrompts != null)
            {
                StartCoroutine(InstructionCountdownRoutine());
            }

            handSwapManager.ContinueExperimentLoop();
        }
    }

    public void StartQuestionnaire(SwapTarget target)
    {
        if (_questionnaireActive)
        {
            return;
        }

        if (_questionnaireCompleted.Contains(target))
        {
            return;
        }

        _questionnaireActive = true;
        _state = State.Questionnaire;
        _currentQuestionnaireTarget = target;

        handSwapManager.PauseSwapLoop();

        instructionPrompts?.Hide();

        questionnaireUI.PlaceInFrontOfPlayer();

        questionnaireUI.Show(scores =>
        {
            OnQuestionnaireFinished(scores);
        });
    }

    private bool AllQuestionnairesDone()
    {
        foreach (var target in handSwapManager.swapTargets)
        {
            if (target != null && !_questionnaireCompleted.Contains(target))
                return false;
        }

        return true;
    }

    private void EndExperiment()
    {
        _state = State.End;
        dataLogger.StopLogging();
        Debug.Log("Experiment finished.");
    }

    private string GetColourName(SwapTarget target)
    {
        return target.playerHandMaterial != null
            ? target.playerHandMaterial.name
            : "Unknown";
    }

    private bool AllTargetsReachedTrialLimit()
    {
        foreach (var target in handSwapManager.swapTargets)
        {
            if (target == null) continue;

            if (!_trialCounts.ContainsKey(target)) return false;
            if (_trialCounts[target] < trialsPerHand) return false;
        }

        return true;
    }

    private void StartNextQuestionnaireTarget()
    {
        foreach (var target in handSwapManager.swapTargets)
        {
            if (target == null) continue;
            if (_questionnaireCompleted.Contains(target)) continue;

            _pendingQuestionnaireTarget = target;
            _state = State.WaitingForTrial;

            handSwapManager.StartQuestionnaireSwap(target);
            return;
        }

        EndExperiment();
    }

    private void ForceFinishQuestionnaire()
    {
        if (_state != State.Questionnaire)
        {
            Debug.Log("Not currently in questionnaire state.");
            return;
        }

        // Use neutral score for debug
        int debugScore = 4;

        if (questionnaireUI != null)
            questionnaireUI.root.SetActive(false);

        // Call the same logic you normally run after questionnaire answer
        OnQuestionnaireFinished(new int[] { debugScore });
    }

    private void OnQuestionnaireFinished(int[] scores)
    {
        if (!_questionnaireActive)
        {
            Debug.LogWarning("Questionnaire not active.");
            return;
        }

        _questionnaireActive = false;

        var target = _currentQuestionnaireTarget;
        if (target == null) return;

        for (int i = 0; i < scores.Length; i++)
        {
            dataLogger.LogTrial(
                userId,
                _trialId,
                target.name,
                GetColourName(target),
                target.rightHandBehaviour.ToString(),
                0f,
                $"Questionnaire_Q{i + 1}",
                scores[i]
            );
        }

        _questionnaireCompleted.Add(target);
        _currentQuestionnaireTarget = null;

        if (AllQuestionnairesDone())
        {
            EndExperiment();
        }
        else
        {
            StartNextQuestionnaireTarget();
        }
    }

    private IEnumerator InstructionCountdownRoutine()
    {
        instructionPrompts.Hide();

        yield return new WaitForSeconds(handSwapManager.objectSpawnDelay);

        float total = handSwapManager.autoSwapDelayAfterObjectSpawn;
        float step = total / 3f;

        instructionPrompts.Show("Initiating swap in 3");
        yield return new WaitForSeconds(step);

        instructionPrompts.Show("Initiating swap in 2");
        yield return new WaitForSeconds(step);

        instructionPrompts.Show("Initiating swap in 1");
        yield return new WaitForSeconds(step);

        instructionPrompts.Hide();
    }

    private bool WillButtonPressStartQuestionnaires()
    {
        if (_currentTrialTarget == null)
            return false;

        foreach (var target in handSwapManager.swapTargets)
        {
            if (target == null) continue;

            int count = _trialCounts.ContainsKey(target) ? _trialCounts[target] : 0;

            // Pretend the current active trial has just been completed
            if (target == _currentTrialTarget)
                count++;

            if (count < trialsPerHand)
                return false;
        }

        return true;
    }
}