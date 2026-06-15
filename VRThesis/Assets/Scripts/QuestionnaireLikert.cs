using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestionnaireLikert : MonoBehaviour
{
    public GameObject root;

    [Header("UI")]
    public TextMeshProUGUI questionText;
    public TextMeshProUGUI valueText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI progressText;
    public Slider slider;
    public Button nextButton;
    public Button previousButton;

    [Header("Questions")]
    [TextArea] public string[] questions;

    [Header("Placement")]
    public Transform playerHead;
    public float distanceFromPlayer = 0.4f;
    public float verticalOffset = 0f;

    [Header("Nav Cooldown")]
    public float navCooldown = 0.5f;

    private int _questionIndex;
    private int[] _answers;
    private Action<int[]> _onFinished;
    private float _lastNavTime = -999f;

    private readonly string[] descriptions =
    {
        "",
        "Strongly disagree",
        "Disagree",
        "Somewhat disagree",
        "Neutral",
        "Somewhat agree",
        "Agree",
        "Strongly agree"
    };

    private void Awake()
    {
        if (root) root.SetActive(false);

        slider.minValue = 1;
        slider.maxValue = 7;
        slider.wholeNumbers = true;

        slider.onValueChanged.RemoveAllListeners();
        nextButton.onClick.RemoveAllListeners();
        previousButton.onClick.RemoveAllListeners();

        slider.onValueChanged.AddListener(OnSliderChanged);
        nextButton.onClick.AddListener(NextQuestion);
        previousButton.onClick.AddListener(PreviousQuestion);
    }

    public void Show(Action<int[]> onFinished)
    {
        _onFinished = onFinished;

        _questionIndex = 0;
        _answers = new int[questions.Length];

        for (int i = 0; i < _answers.Length; i++)
            _answers[i] = 4;

        PlaceInFrontOfPlayer();

        if (root) root.SetActive(true);

        LoadQuestion();
    }

    private void OnSliderChanged(float value)
    {
        int intVal = Mathf.RoundToInt(value);

        if (_answers != null && _answers.Length > 0)
            _answers[_questionIndex] = intVal;

        if (valueText)
            valueText.text = intVal.ToString();

        if (descriptionText)
            descriptionText.text = descriptions[intVal];
    }

    public void NextQuestion()
    {
        if (!CanNavigate()) return;

        Debug.Log($"BEFORE Next: index={_questionIndex}, question={questions[_questionIndex]}");

        _answers[_questionIndex] = Mathf.RoundToInt(slider.value);

        if (_questionIndex >= questions.Length - 1)
        {
            FinishQuestionnaire();
            return;
        }

        _questionIndex++;
        LoadQuestion();

        Debug.Log($"AFTER Next: index={_questionIndex}, question={questions[_questionIndex]}");
    }

    public void PreviousQuestion()
    {
        if (!CanNavigate()) return;

        _answers[_questionIndex] = Mathf.RoundToInt(slider.value);

        if (_questionIndex <= 0)
            return;

        _questionIndex--;
        LoadQuestion();
    }

    private void LoadQuestion()
    {
        if (questionText)
            questionText.text = questions[_questionIndex];

        if (progressText)
            progressText.text = $"Question {_questionIndex + 1} / {questions.Length}";

        if (previousButton)
            previousButton.interactable = _questionIndex > 0;

        slider.SetValueWithoutNotify(_answers[_questionIndex]);
        OnSliderChanged(_answers[_questionIndex]);
    }

    private void FinishQuestionnaire()
    {
        if (root) root.SetActive(false);

        _onFinished?.Invoke(_answers);
        _onFinished = null;
    }

    public void PlaceInFrontOfPlayer()
    {
        if (!playerHead) return;

        Vector3 forward = playerHead.forward;
        forward.y = 0f;
        forward.Normalize();

        transform.position =
            playerHead.position +
            forward * distanceFromPlayer +
            Vector3.up * verticalOffset;

        transform.rotation = Quaternion.LookRotation(forward);
    }

    private bool CanNavigate()
    {
        if (Time.time - _lastNavTime < navCooldown)
            return false;

        _lastNavTime = Time.time;
        return true;
    }
}