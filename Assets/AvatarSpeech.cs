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

    [Header("Drop Reaction")]
    public string[] dropPhrases =
    {
        "THE DROP!!", "OH WOW", "THERE IT IS", "yooo", "that's the one!!",
        "felt that", "ok ok OK", "bro...", "THE BASS", "I felt my soul leave",
    };

    [Header("Timing")]
    [Range(1f, 10f)]   public float showDuration      = 3.5f;
    [Range(5f, 120f)]  public float minCooldown       = 20f;
    [Range(10f, 300f)] public float maxCooldown       = 60f;
    [Range(1, 10)]     public int   maxShowsPerSession = 2;
    [Range(30f, 600f)] public float sessionDuration   = 180f;

    [Header("Positioning")]
    [Range(0f, 300f)] public float topDistance    = 60f;
    [Range(0f, 300f)] public float bottomDistance = 120f;
    [Range(0.1f, 3f)] public float bubbleScale    = 1f;

    // ── State ─────────────────────────────────────────────────────────────────

    RectTransform _bubbleRect;
    float         _showTimer;
    float         _cooldown;
    bool          _isShowing;
    int           _sessionShowCount;
    float         _sessionTimer;

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
    }

    void Update()
    {
        UpdatePosition();

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

        _cooldown -= Time.deltaTime;
        if (_cooldown > 0f) return;
        if (_sessionShowCount >= maxShowsPerSession) return;

        Mood mood = GetCurrentMood();

        // Silence fires only 25% of the time
        if (mood == Mood.Silence && Random.value > 0.25f)
        {
            _cooldown = Random.Range(minCooldown, maxCooldown);
            return;
        }

        ShowBubble(mood);
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
