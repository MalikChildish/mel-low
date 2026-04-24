using SpeechBubble;
using UnityEngine;

public class AvatarSpeech : MonoBehaviour
{
    // ── Per-mood configuration ────────────────────────────────────────────────

    [System.Serializable]
    public class MoodConfig
    {
        public string           label;
        public string[]         phrases;
        public SpeechBubbleType bubbleType;
        public Color            fillColor;
    }

    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("References")]
    public BeatDetector    beatDetector;
    public Transform       avatarHead;
    [SerializeField] SpeechBubble_TMP _bubble;

    [Header("Moods")]
    public MoodConfig[] moods =
    {
        new()
        {
            label      = "Silence",
            bubbleType = SpeechBubbleType.Think,
            fillColor  = new Color(0.85f, 0.80f, 0.95f),
            phrases    = new[]
            {
                "hm...", "...", "what's next?", "zoning out...", "just vibing",
                "needs more music", "the silence is loud", "put something on",
                "waiting...", "ears hungry", "pick a vibe", "loading...",
                "where's the beat?", "queue something", "thinking...",
            },
        },
        new()
        {
            label      = "Quiet",
            bubbleType = SpeechBubbleType.Whisper,
            fillColor  = new Color(0.80f, 0.90f, 0.98f),
            phrases    = new[]
            {
                "nice and easy", "soft vibes~", "chill", "mellow", "low key",
                "late night energy", "smooth", "sunday morning feel", "no rush",
                "float with it", "real gentle", "good for the soul",
                "bedroom vibes", "this calms me", "mmm~",
            },
        },
        new()
        {
            label      = "Vibing",
            bubbleType = SpeechBubbleType.Note,
            fillColor  = new Color(0.80f, 0.95f, 0.80f),
            phrases    = new[]
            {
                "ok I feel this", "this slaps", "vibing rn", "good groove", "head bobbin'",
                "oh this is nice", "the groove is real", "this is that one",
                "rhythm is everything", "yep yep yep", "locked in",
                "the vibe is immaculate", "this producer ate", "chef's kiss",
                "in my element",
            },
        },
        new()
        {
            label      = "Hype",
            bubbleType = SpeechBubbleType.Yell,
            fillColor  = new Color(1.00f, 0.92f, 0.40f),
            phrases    = new[]
            {
                "LET'S GO!", "this is a BOP", "!!!", "FIRE", "ok ok ok",
                "YO", "ABSOLUTE BANGER", "the drop!!", "BOP BOP BOP",
                "I am NOT okay", "losing my mind rn", "THE BASS",
                "pure energy", "ok I need to sit down", "TOO GOOD",
            },
        },
        new()
        {
            label      = "Intense",
            bubbleType = SpeechBubbleType.Stress,
            fillColor  = new Color(1.00f, 0.60f, 0.50f),
            phrases    = new[]
            {
                "OHHHHH!!", "INTENSE", "can't stop", "MAX POWER", "WOOOO",
                "TOO MUCH", "my neck hurts", "THE BPM", "going too fast",
                "this is unhinged", "absolutely unreal", "I can't",
                "SEND HELP", "maximum overdrive", "ears on fire",
            },
        },
    };

    [Header("Session End")]
    [Range(2f, 15f)] public float sessionEndDelay    = 4f;
    [Range(10f, 120f)] public float minPlayTimeForEnd = 30f;
    public string[] sessionEndPhrases =
    {
        "that was a vibe~", "good session", "nice jam~", "that was it right there",
        "needed that", "ok that was good", "solid session~",
        "that playlist did not miss", "well played", "coming back to that one",
        "ok I fw that", "that one fed my soul", "chef's kiss session~",
    };

    [Header("Drop Reaction")]
    public string[] dropPhrases =
    {
        "THE DROP!!", "OH WOW", "THERE IT IS", "yooo", "that's the one!!",
        "felt that", "ok ok OK", "bro...", "THE BASS", "I felt my soul leave",
    };

    [Header("Timing")]
    [Range(1f, 10f)]   public float showDuration      = 3.5f;
    [Range(1f, 15f)]   public float greetingDuration  = 6f;
    [Range(5f, 120f)]  public float minCooldown       = 20f;
    [Range(10f, 300f)] public float maxCooldown       = 60f;
    [Range(1, 10)]     public int   maxShowsPerSession  = 2;
    [Range(30f, 600f)] public float sessionDuration    = 180f;
    [Range(0f, 1f)]    public float silenceProbability = 0.1f;
    [Range(1f, 24f)]   public float newSessionHours   = 6f;

    [Header("Positioning")]
    [Range(0f, 300f)] public float topDistance    = 60f;
    [Range(0f, 300f)] public float bottomDistance = 120f;
    [Range(0.1f, 3f)] public float bubbleScale    = 1f;

    [Header("Been a While")]
    [Range(1, 30)] public int beenAWhileDays = 3;
    public string[] beenAWhilePhrases =
    {
        "oh you're back~", "thought you forgot about me", "it's been a minute!",
        "where have you been??", "finally! I missed the music", "you came back~",
        "long time no vibe", "was starting to worry ngl",
    };

    [Header("Holidays")]
    public string[] halloweenPhrases =
    {
        "spooky season energy~", "happy halloween!!", "october 31st hits different",
        "all hallows eve~", "trick or treat??", "this playlist is scary good",
    };
    public string[] christmasPhrases =
    {
        "happy holidays~", "it's giving christmas energy", "festive vibes~",
        "tis the season!!", "christmas playlist activated", "holiday mode on~",
    };
    public string[] newYearPhrases =
    {
        "HAPPY NEW YEAR!!", "new year new vibes~", "we made it!!",
        "new year energy hits different", "fresh start fresh playlist",
        "365 days of music ahead~",
    };
    public string[] valentinePhrases =
    {
        "happy valentine's day~", "love is in the air~",
        "valentines day vibes", "spreading love through music~",
        "the most romantic playlist~",
    };

    [Header("Long Session")]
    [Range(30f, 300f)] public float longSessionMinutes = 120f;
    public string[] longSessionPhrases =
    {
        "we've been at this for a while~", "hours of music fr", "still going strong",
        "dedication~", "we don't stop", "this is a marathon session",
        "still here still vibing", "no skip button today huh",
    };

    [Header("Time Awareness")]
    public string[] morningPhrases =
    {
        "morning vibes~", "rise and grind I guess", "good morning~",
        "starting the day right", "morning session let's go", "early bird energy",
    };
    public string[] afternoonPhrases =
    {
        "afternoon session", "midday mood~", "keeping the energy up",
        "good afternoon~", "perfect time for music", "afternoon reset",
    };
    public string[] eveningPhrases =
    {
        "evening vibes~", "winding down nicely", "evening session~",
        "the evening playlist hits different", "good evening~", "end of day energy",
    };
    public string[] lateNightPhrases =
    {
        "late night session~", "it's giving midnight energy", "who else is up rn",
        "the late night playlist hits different", "night owl hours",
        "past my bedtime but the music is too good", "late night mood~",
    };
    public string[] fridayPhrases =
    {
        "HAPPY FRIDAY!!", "it's fridayyy~", "friday energy is unmatched",
        "weekend is almost here!!", "TGIF fr", "friday playlist activated",
    };
    public string[] weekendPhrases =
    {
        "weekend mode activated~", "no alarms today~", "this is the life",
        "weekend energy different", "saturdays are for the music",
        "sunday funday~", "living my best life rn",
    };
    public string[] mondayPhrases =
    {
        "monday... at least there's music", "music makes mondays survivable",
        "it's giving monday but make it bearable", "monday reset~",
        "new week new playlist", "mondays hit different with good music",
    };

    // ── State ─────────────────────────────────────────────────────────────────

    RectTransform _bubbleRect;
    float         _showTimer;
    float         _cooldown;
    bool          _isShowing;
    int           _sessionShowCount;
    float         _sessionTimer;
    bool          _hasGreeted;
    float         _musicStartTimer;
    bool          _beenAWhile;
    float         _totalPlayTime;
    bool          _longSessionNotified;
    bool          _musicWasPlaying;
    bool          _prevMusicPlaying;
    float         _musicStopTimer;
    bool          _sessionEndFired;
    float         _continuousPlayTime;

    enum Mood { Silence, Quiet, Vibing, Hype, Intense }

    // ── Unity ─────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (_bubble)
        {
            _bubbleRect           = _bubble.GetComponent<RectTransform>();
            _bubbleRect.anchorMin = new Vector2(0f,   0.5f);
            _bubbleRect.anchorMax = new Vector2(1f,   0.5f);
            _bubbleRect.pivot     = new Vector2(0.5f, 0.5f);
            _bubbleRect.sizeDelta = new Vector2(0f,   300f);
            _bubble.transform.localScale = Vector3.zero;
        }
    }

    void Start()
    {
        _cooldown = Random.Range(minCooldown * 0.5f, minCooldown);

        string lastDate = PlayerPrefs.GetString("lastLaunchDate", "");
        if (!string.IsNullOrEmpty(lastDate) &&
            System.DateTime.TryParse(lastDate, out System.DateTime last))
            _beenAWhile = (System.DateTime.Now - last).TotalDays >= beenAWhileDays;

        PlayerPrefs.SetString("lastLaunchDate", System.DateTime.Now.ToString("yyyy-MM-dd"));
        PlayerPrefs.Save();
    }

    void Update()
    {
        UpdatePosition();
        UpdateTimeGreeting();

        if (_isShowing)
        {
            _showTimer -= Time.deltaTime;
            if (_showTimer <= 0f) HideBubble();
            return;
        }

        _sessionTimer += Time.deltaTime;
        if (_sessionTimer >= sessionDuration)
        {
            _sessionTimer     = 0f;
            _sessionShowCount = 0;
        }

        if (!_hasGreeted) return;

        bool musicPlaying = beatDetector && beatDetector.Energy > 0.01f;

        if (musicPlaying && !_prevMusicPlaying)
            CheckNewSession();
        _prevMusicPlaying = musicPlaying;

        if (musicPlaying)
        {
            _totalPlayTime       += Time.deltaTime;
            _continuousPlayTime  += Time.deltaTime;
            _musicWasPlaying      = true;
            _musicStopTimer       = 0f;
            _sessionEndFired      = false;
        }
        else if (_musicWasPlaying)
        {
            _musicStopTimer     += Time.deltaTime;
            _continuousPlayTime  = 0f;

            if (!_sessionEndFired && _musicStopTimer >= sessionEndDelay
                && _totalPlayTime >= minPlayTimeForEnd)
            {
                _sessionEndFired  = true;
                _musicWasPlaying  = false;
                ShowSessionEndBubble();
                return;
            }
        }

        if (!_longSessionNotified && _totalPlayTime >= longSessionMinutes * 60f)
        {
            _longSessionNotified = true;
            ShowLongSessionBubble();
            return;
        }

        _cooldown -= Time.deltaTime;
        if (_cooldown > 0f) return;
        if (_sessionShowCount >= maxShowsPerSession) return;

        Mood mood = GetCurrentMood();

        if (mood == Mood.Silence && Random.value > silenceProbability)
        {
            _cooldown = Random.Range(minCooldown, maxCooldown);
            return;
        }

        ShowBubble(mood);
    }

    void UpdateTimeGreeting()
    {
        if (_hasGreeted || !_bubble) return;

        bool musicPlaying = beatDetector && beatDetector.Energy > 0.01f;
        if (musicPlaying)
        {
            _musicStartTimer += Time.deltaTime;
            if (_musicStartTimer >= 1.5f)
            {
                _hasGreeted = true;
                ShowTimeGreeting();
            }
        }
        else
        {
            _musicStartTimer = 0f;
        }
    }

    void ShowTimeGreeting()
    {
        string[] phrases = GetTimePhrases();
        if (phrases == null || phrases.Length == 0) return;

        MoodConfig config = moods[(int)Mood.Quiet];
        if (_isShowing) HideBubble();

        _bubble.setBubbleType(config.bubbleType);
        _bubble.setDialogueText(phrases[Random.Range(0, phrases.Length)]);
        _bubble.setFillColor(config.fillColor);
        _bubble.transform.localScale = Vector3.one * bubbleScale;

        PlayerPrefs.SetString("lastGreetingTime", System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        PlayerPrefs.Save();

        _showTimer = greetingDuration;
        _cooldown  = Random.Range(minCooldown, maxCooldown);
        _isShowing = true;
        _sessionShowCount++;
    }

    void CheckNewSession()
    {
        if (!_hasGreeted) return;

        string saved = PlayerPrefs.GetString("lastGreetingTime", "");
        if (string.IsNullOrEmpty(saved)) return;
        if (!System.DateTime.TryParse(saved, out System.DateTime last)) return;

        if ((System.DateTime.Now - last).TotalHours >= newSessionHours)
        {
            _hasGreeted          = false;
            _musicStartTimer     = 0f;
            _sessionShowCount    = 0;
            _longSessionNotified = false;
        }
    }

    string[] GetTimePhrases()
    {
        var now  = System.DateTime.Now;
        int hour = now.Hour;

        if (_beenAWhile) return beenAWhilePhrases;

        string[] holiday = GetHolidayPhrases(now);
        if (holiday != null) return holiday;

        if (now.DayOfWeek == System.DayOfWeek.Friday)   return fridayPhrases;
        if (now.DayOfWeek == System.DayOfWeek.Saturday ||
            now.DayOfWeek == System.DayOfWeek.Sunday)   return weekendPhrases;
        if (now.DayOfWeek == System.DayOfWeek.Monday)   return mondayPhrases;

        if (hour >= 5  && hour < 12) return morningPhrases;
        if (hour >= 12 && hour < 18) return afternoonPhrases;
        if (hour >= 18 && hour < 22) return eveningPhrases;
        return lateNightPhrases;
    }

    string[] GetHolidayPhrases(System.DateTime now)
    {
        int m = now.Month, d = now.Day;
        if (m == 10 && d == 31)              return halloweenPhrases;
        if (m == 12 && (d == 24 || d == 25)) return christmasPhrases;
        if ((m == 12 && d == 31) || (m == 1 && d == 1)) return newYearPhrases;
        if (m == 2  && d == 14)              return valentinePhrases;
        return null;
    }

    void ShowSessionEndBubble()
    {
        if (!_bubble || sessionEndPhrases.Length == 0) return;

        MoodConfig config = moods[(int)Mood.Vibing];
        if (_isShowing) HideBubble();

        _bubble.setBubbleType(config.bubbleType);
        _bubble.setDialogueText(sessionEndPhrases[Random.Range(0, sessionEndPhrases.Length)]);
        _bubble.setFillColor(config.fillColor);
        _bubble.transform.localScale = Vector3.one * bubbleScale;

        _showTimer = greetingDuration;
        _cooldown  = Random.Range(minCooldown, maxCooldown);
        _isShowing = true;
        _sessionShowCount++;
    }

    void ShowLongSessionBubble()
    {
        if (!_bubble || longSessionPhrases.Length == 0) return;

        MoodConfig config = moods[(int)Mood.Quiet];
        if (_isShowing) HideBubble();

        _bubble.setBubbleType(config.bubbleType);
        _bubble.setDialogueText(longSessionPhrases[Random.Range(0, longSessionPhrases.Length)]);
        _bubble.setFillColor(config.fillColor);
        _bubble.transform.localScale = Vector3.one * bubbleScale;

        _showTimer = greetingDuration;
        _cooldown  = Random.Range(minCooldown, maxCooldown);
        _isShowing = true;
        _sessionShowCount++;
    }

    // ── Logic ─────────────────────────────────────────────────────────────────

    Mood GetCurrentMood()
    {
        if (!beatDetector || beatDetector.Energy < 0.001f) return Mood.Silence;

        float vibe = beatDetector.VibeEnergy;
        float bpm  = beatDetector.BPM;

        if (vibe > 0.65f && bpm > 130f) return Mood.Intense;
        if (vibe > 0.45f)               return Mood.Hype;
        if (vibe > 0.20f)               return Mood.Vibing;
        return Mood.Quiet;
    }

    void ShowBubble(Mood mood)
    {
        if (!_bubble) return;

        int        idx    = (int)mood;
        MoodConfig config = idx < moods.Length ? moods[idx] : moods[0];

        _bubble.setBubbleType(config.bubbleType);
        _bubble.setDialogueText(config.phrases[Random.Range(0, config.phrases.Length)]);
        _bubble.setFillColor(config.fillColor);
        _bubble.transform.localScale = Vector3.one * bubbleScale;

        _showTimer = showDuration;
        _cooldown  = Random.Range(minCooldown, maxCooldown);
        _isShowing = true;
        _sessionShowCount++;
    }

    public void TriggerDrop()
    {
        if (!_bubble || !enabled || dropPhrases.Length == 0) return;
        if (!_hasGreeted) return;
        if (_sessionShowCount >= maxShowsPerSession) return;

        MoodConfig config = moods[(int)Mood.Hype];
        if (_isShowing) HideBubble();

        _bubble.setBubbleType(config.bubbleType);
        _bubble.setDialogueText(dropPhrases[Random.Range(0, dropPhrases.Length)]);
        _bubble.setFillColor(config.fillColor);
        _bubble.transform.localScale = Vector3.one * bubbleScale;

        _showTimer = showDuration;
        _cooldown  = Random.Range(minCooldown, maxCooldown);
        _isShowing = true;
        _sessionShowCount++;
    }

    void HideBubble()
    {
        if (_bubble) _bubble.transform.localScale = Vector3.zero;
        _isShowing = false;
    }

    void UpdatePosition()
    {
        if (!_bubbleRect || !avatarHead || !Camera.main) return;
        if (!_isShowing) return;

        Vector3 sp    = Camera.main.WorldToScreenPoint(avatarHead.position);
        float   halfW = Screen.width  * 0.5f;
        float   halfH = Screen.height * 0.5f;
        float   halfBH = 150f * bubbleScale;
        float   halfBW = 570f * bubbleScale;
        float   yDir  = (Screen.height - sp.y) >= sp.y ? 1f : -1f;
        float   dist  = yDir > 0f ? topDistance : bottomDistance;

        float x = Mathf.Clamp(sp.x - halfW, -halfW + halfBW, halfW - halfBW);
        float y = Mathf.Clamp(
            sp.y - halfH + (halfBH + dist) * yDir,
            -halfH + halfBH + 10f,
             halfH - halfBH - 10f);

        _bubbleRect.anchoredPosition = new Vector2(x, y);
    }
}
