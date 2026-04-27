using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;

public class MoodHistory : MonoBehaviour
{
    [Serializable]
    public class SessionEntry
    {
        public string  timestamp;
        public float   duration;
        public int     dominantMood;
        public float[] moodPercents;
    }

    [Serializable]
    class SaveData
    {
        public List<SessionEntry> sessions = new List<SessionEntry>();
    }

    const int MaxSessions = 30;

    string   _filePath;
    SaveData _data = new SaveData();

    void Awake()
    {
        _filePath = Path.Combine(Application.persistentDataPath, "mood_history.json");
        Load();
    }

    void OnApplicationQuit() => Save();

    public bool HasHistory() => _data.sessions.Count >= 2;

    public void ClearHistory()
    {
        _data = new SaveData();
        Save();
    }

    public void RecordSession(float duration, int[] moodCounts)
    {
        if (duration < 30f) return;

        int total = 0;
        foreach (int c in moodCounts) total += c;

        float[] percents = new float[5];
        int     dominant = 0;
        if (total > 0)
        {
            for (int i = 0; i < moodCounts.Length; i++)
            {
                percents[i] = (float)moodCounts[i] / total;
                if (percents[i] > percents[dominant]) dominant = i;
            }
        }

        _data.sessions.Add(new SessionEntry
        {
            timestamp    = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            duration     = duration,
            dominantMood = dominant,
            moodPercents = percents,
        });

        while (_data.sessions.Count > MaxSessions)
            _data.sessions.RemoveAt(0);

        Save();
    }

    // Returns dominant mood index if consistent across last N sessions, -1 if no clear streak
    public int GetRecentStreak(int sessionCount = 3)
    {
        if (_data.sessions.Count < sessionCount) return -1;

        int[] counts = new int[5];
        int   start  = _data.sessions.Count - sessionCount;
        for (int i = start; i < _data.sessions.Count; i++)
            counts[_data.sessions[i].dominantMood]++;

        int best = 0;
        for (int i = 1; i < counts.Length; i++)
            if (counts[i] > counts[best]) best = i;

        return counts[best] > sessionCount / 2 ? best : -1;
    }

    public bool IsLongerThanUsual(float currentDuration)
    {
        if (_data.sessions.Count < 3) return false;
        float total = 0f;
        foreach (var s in _data.sessions) total += s.duration;
        return currentDuration > (total / _data.sessions.Count) * 1.5f;
    }

    void Load()
    {
        try
        {
            if (File.Exists(_filePath))
                _data = JsonUtility.FromJson<SaveData>(File.ReadAllText(_filePath)) ?? new SaveData();
        }
        catch { _data = new SaveData(); }
    }

    void Save()
    {
        try   { File.WriteAllText(_filePath, JsonUtility.ToJson(_data, true)); }
        catch { }
    }
}
