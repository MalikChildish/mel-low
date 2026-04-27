using UnityEngine;

public class BeatReactions : MonoBehaviour
{
    [Header("References")]
    public BeatDetector  beatDetector;
    [SerializeField] AvatarSpeech _avatarSpeech;

    [Header("Drop Detection")]
    [Range(1.5f, 6f)]  public float dropMultiplier   = 3f;
    [Range(3f, 30f)]   public float dropCooldown     = 6f;
    [Range(0f, 1f)]    public float minVibeEnergy    = 0.55f;
    [Range(0f, 0.1f)]  public float minBassThreshold = 0.05f;

    float _dropCooldownTimer;
    float _smoothedBass;

    void Update()
    {
        if (!beatDetector) return;

        float bass    = beatDetector.BassEnergy;
        _smoothedBass = Mathf.Lerp(_smoothedBass, bass, Time.deltaTime * 3f);
        _dropCooldownTimer -= Time.deltaTime;

        if (_dropCooldownTimer <= 0f
            && bass > _smoothedBass * dropMultiplier
            && bass > minBassThreshold
            && beatDetector.VibeEnergy > minVibeEnergy)
        {
            _dropCooldownTimer = dropCooldown;
            _avatarSpeech?.TriggerDrop();
        }
    }
}
