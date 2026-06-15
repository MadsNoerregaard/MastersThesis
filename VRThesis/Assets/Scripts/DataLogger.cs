using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class DataLogger : MonoBehaviour
{
    private bool isLogging;
    private readonly List<string> logData = new();
    private char separator;
    private string filenameBase;
    private string[] header;

    public void StartLogging(char separator, string filenameBase, string[] header)
    {
        this.separator = separator;
        this.filenameBase = filenameBase;
        this.header = header;
        isLogging = true;
        logData.Clear();
    }

    public void LogTrial(
        int userId,
        int trial,
        string handName,
        string handColour,
        string rightBehaviour,
        float trialTime,
        string eventType,
        int questionnaireScore = -1)
    {
        if (!isLogging) return;

        string row =
            $"{userId}{separator}{trial}{separator}{handName}{separator}{handColour}{separator}" +
            $"{rightBehaviour}{separator}{trialTime:F3}{separator}" +
            $"{eventType}{separator}{questionnaireScore}";

        Debug.Log(row);
        logData.Add(row);
    }

    public void StopLogging()
    {
        if (!isLogging) return;

        isLogging = false;

        string filepath = Path.Combine(
            Application.persistentDataPath,
            filenameBase + "_" + System.DateTime.Now.ToString("ddMMyyyy-HHmmss") + ".csv"
        );

        using StreamWriter writer = new(filepath);
        writer.WriteLine(string.Join(separator.ToString(), header));

        foreach (string row in logData)
            writer.WriteLine(row);

        Debug.Log("Saved log to: " + filepath);
    }
}