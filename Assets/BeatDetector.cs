using System;
using System.Threading;
using UnityEngine;
using CSCore.SoundIn;

public class BeatDetector : MonoBehaviour
{
    [Header("Detection")]
    [Range(0.1f, 5f)]    public float sensitivity           = 1.5f;
    [Range(0.01f, 0.3f)] public float smoothing             = 0.08f;

    [Header("Body Wave")]
    public BoneLayer[] boneChain;
    [Range(0f, 1f)]      public float phaseOffset           = 0.3f;
    [Range(0.1f, 10f)]   public float amplitudeSmoothing    = 1.5f;
    [Range(1f, 200f)]    public float bassAmplitudeScale    = 60f;
    [Range(0.01f, 0.3f)] public float beatIntervalSmoothing = 0.05f;
    [Range(0.1f, 1f)]    public float amplitudeCurve        = 0.7f;
    [Range(0f, 0.3f)]    public float minAmplitude          = 0.05f;
    [Range(0.2f, 0.8f)]  public float nodAttack             = 0.4f;
    [Range(0f, 10f)]     public float spineScale            = 1f;
    [Range(0f, 10f)]     public float headNodScale          = 1f;

    [Header("Vibe")]
    [Range(0f, 1f)]      public float vibeInfluence         = 0.5f;

    [Header("Debug")]
    public bool showDebug     = true;
    public bool reactToMusic  = true;

    // Public readable state
    public float Energy        { get; private set; }
    public float BassEnergy    { get; private set; }
    public float SubBassEnergy { get; private set; }
    public float MidEnergy     { get; private set; }
    public float HighEnergy    { get; private set; }
    public float VibeWeight    { get; private set; }
    public float VibeEnergy    { get; private set; }
    public bool  IsBeat        { get; private set; }
    public float BPM           { get; private set; }
    public float HeadNodAngle  { get; private set; }
    public float HeadNodTurn   { get; private set; }

    WasapiLoopbackCapture _capture;
    float[]               _sampleBuffer;
    int                   _channels = 2;

    // Shared between threads (under _lock)
    float _latestEnergy, _latestBass, _latestSubBass, _latestMid, _latestHigh;
    float _latestFlux, _acWinFlux;
    readonly object _lock = new object();

    // FFT spectral flux — frequency-weighted to emphasize kick drum over hi-hats
    const int FftSize = 2048;
    const int HopSize = 512;
    float[]   _fftRing   = new float[FftSize];
    int       _fftRingHead;
    int       _hopCount;
    float[]   _re        = new float[FftSize];
    float[]   _im        = new float[FftSize];
    float[]   _hann      = new float[FftSize];
    float[]   _fftPrev   = new float[FftSize / 2];
    int       _kickBinMax;  // highest bin covering ~200 Hz (kick/sub-bass)
    int       _midBinMax;   // highest bin covering ~2000 Hz (snare/mid)

    // Autocorrelation beat tracking
    const int   AcBufSize = 256;   // ~8.5 seconds at 30 Hz
    const float AcHz      = 30f;
    const int   AcMin     = 9;     // lag for 200 BPM
    const int   AcMax     = 33;    // lag for ~55 BPM
    float[]     _acBuf    = new float[AcBufSize];
    int         _acHead;
    float       _acTimer;
    float       _acWinMax;
    float       _acPeak   = 0.001f;
    float       _acCorrTimer;

    // Median filter over recent autocorrelation results — outlier detections have zero influence
    const int AcHistorySize = 7;
    float[]   _acHistory     = new float[AcHistorySize];
    float[]   _acHistorySort = new float[AcHistorySize];
    int       _acHistoryHead;

    float _avgFlux;
    float _silenceTimer;
    bool  _detectionReset;

    // IIR band filters — background thread only, for VibeWeight/VibeEnergy/amplitude
    float _subBassAlpha, _bassAlpha, _lp2kAlpha;
    float _subBassState, _bassState, _lp2kState;

    float _beatCooldown;
    float _beatInterval = 0.75f;
    const float CooldownTime = 0.35f;

    Quaternion[] _initialRots;
    bool         _initialized;
    float        _phase;
    float        _nodDirDeg = 0f;
    float        _targetNodDirDeg = 0f;
    bool         _halfTime;
    float        _smoothedNodInterval = 0.5f;

    float        _smoothedAmplitude;
    float        _smoothedVibeWeight;
    float        _smoothedVibeEnergy;

    [Serializable]
    public class BoneLayer
    {
        public Transform bone;
        [Range(-20f, 20f)] public float angle = 8f;
    }

    void Start()
    {
        // Hann window computed on main thread before background thread starts
        for (int i = 0; i < FftSize; i++)
            _hann[i] = 0.5f * (1f - Mathf.Cos(2f * Mathf.PI * i / (FftSize - 1)));

        Thread t = new Thread(() =>
        {
            Thread.Sleep(300);
            int maxAttempts = 3;
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                try
                {
                    _capture = new WasapiLoopbackCapture();
                    _capture.Initialize();

                    int sr    = _capture.WaveFormat.SampleRate;
                    _channels = _capture.WaveFormat.Channels;

                    // Bin boundaries for frequency-weighted flux
                    _kickBinMax = Mathf.Max(1, (int)(200f  * FftSize / sr));
                    _midBinMax  = Mathf.Max(_kickBinMax + 1, (int)(2000f * FftSize / sr));

                    _subBassAlpha = 1f / (1f + sr / (2f * Mathf.PI * 60f));
                    _bassAlpha    = 1f / (1f + sr / (2f * Mathf.PI * 200f));
                    _lp2kAlpha    = 1f / (1f + sr / (2f * Mathf.PI * 2000f));

                    _capture.DataAvailable += OnData;
                    _capture.Start();
                    break;
                }
                catch (Exception ex)
                {
                    try { _capture?.Dispose(); } catch { }
                    _capture = null;
                    if (attempt == maxAttempts - 1)
                        Debug.LogError("BeatDetector failed after " + maxAttempts + " attempts: " + ex.Message);
                    else
                        Thread.Sleep(500);
                }
            }
        });
        t.SetApartmentState(ApartmentState.MTA);
        t.IsBackground = true;
        t.Start();
    }

    void OnData(object sender, DataAvailableEventArgs e)
    {
        int floatCount = e.ByteCount / 4;
        if (_sampleBuffer == null || _sampleBuffer.Length < floatCount)
            _sampleBuffer = new float[floatCount];

        Buffer.BlockCopy(e.Data, e.Offset, _sampleBuffer, 0, e.ByteCount);

        // IIR bands — run on all interleaved samples (energy calculation, not time-sensitive)
        float rmsSum = 0f, subBassSum = 0f, bassSum = 0f, midSum = 0f, highSum = 0f;
        for (int i = 0; i < floatCount; i++)
        {
            float s = _sampleBuffer[i];
            rmsSum += s * s;

            _subBassState = _subBassAlpha * s + (1f - _subBassAlpha) * _subBassState;
            _bassState    = _bassAlpha    * s + (1f - _bassAlpha)    * _bassState;
            _lp2kState    = _lp2kAlpha    * s + (1f - _lp2kAlpha)   * _lp2kState;

            float mid  = _lp2kState - _bassState;
            float high = s - _lp2kState;

            subBassSum += _subBassState * _subBassState;
            bassSum    += _bassState    * _bassState;
            midSum     += mid  * mid;
            highSum    += high * high;
        }

        float div = Mathf.Max(floatCount, 1);

        // FFT — mono mixdown, one sample per stereo frame, fed into ring buffer
        int frames = floatCount / _channels;
        for (int f = 0; f < frames; f++)
        {
            float mono = 0f;
            for (int ch = 0; ch < _channels; ch++)
                mono += _sampleBuffer[f * _channels + ch];
            mono /= _channels;

            _fftRing[_fftRingHead % FftSize] = mono;
            _fftRingHead++;
            _hopCount++;

            if (_hopCount >= HopSize)
            {
                _hopCount = 0;
                ComputeFluxFrame();
            }
        }

        lock (_lock)
        {
            _latestEnergy  = Mathf.Sqrt(rmsSum     / div);
            _latestSubBass = Mathf.Sqrt(subBassSum / div);
            _latestBass    = Mathf.Sqrt(bassSum    / div);
            _latestMid     = Mathf.Sqrt(midSum     / div);
            _latestHigh    = Mathf.Sqrt(highSum    / div);
        }
    }

    void ComputeFluxFrame()
    {
        // Copy FftSize samples from ring buffer with Hann window applied
        int start = _fftRingHead - FftSize;
        for (int i = 0; i < FftSize; i++)
        {
            int idx = ((start + i) % FftSize + FftSize) % FftSize;
            _re[i] = _fftRing[idx] * _hann[i];
            _im[i] = 0f;
        }

        FFT(_re, _im);

        // Frequency-weighted spectral flux: kick (4×), snare/mid (1×), hi-hat (0.1×)
        float flux = 0f;
        for (int k = 1; k < FftSize / 2; k++)
        {
            float mag      = (float)Math.Sqrt(_re[k] * _re[k] + _im[k] * _im[k]);
            float increase = Mathf.Max(0f, mag - _fftPrev[k]);
            _fftPrev[k] = mag;
            float w = k <= _kickBinMax ? 4f : (k <= _midBinMax ? 1f : 0.1f);
            flux += increase * w;
        }

        lock (_lock)
        {
            _latestFlux = flux;
            _acWinFlux  = Mathf.Max(_acWinFlux, flux);
        }
    }

    void FFT(float[] re, float[] im)
    {
        int n = re.Length;
        // Bit-reversal permutation
        for (int i = 1, j = 0; i < n; i++)
        {
            int bit = n >> 1;
            for (; (j & bit) != 0; bit >>= 1) j ^= bit;
            j ^= bit;
            if (i < j)
            {
                float t;
                t = re[i]; re[i] = re[j]; re[j] = t;
                t = im[i]; im[i] = im[j]; im[j] = t;
            }
        }
        // Cooley-Tukey butterfly passes
        for (int len = 2; len <= n; len <<= 1)
        {
            double ang = -2.0 * Math.PI / len;
            double wRe = Math.Cos(ang);
            double wIm = Math.Sin(ang);
            for (int i = 0; i < n; i += len)
            {
                double curRe = 1.0, curIm = 0.0;
                int    half  = len / 2;
                for (int j = 0; j < half; j++)
                {
                    float  uRe = re[i + j],           uIm = im[i + j];
                    double vRe = re[i + j + half] * curRe - im[i + j + half] * curIm;
                    double vIm = re[i + j + half] * curIm + im[i + j + half] * curRe;
                    re[i + j]        = uRe + (float)vRe;
                    im[i + j]        = uIm + (float)vIm;
                    re[i + j + half] = uRe - (float)vRe;
                    im[i + j + half] = uIm - (float)vIm;
                    double newRe = curRe * wRe - curIm * wIm;
                    curIm        = curRe * wIm + curIm * wRe;
                    curRe        = newRe;
                }
            }
        }
    }

    void Update()
    {
        float e, bass, subBass, mid, high, flux, winFlux;
        lock (_lock)
        {
            e        = _latestEnergy;
            bass     = _latestBass;
            subBass  = _latestSubBass;
            mid      = _latestMid;
            high     = _latestHigh;
            flux     = _latestFlux;
            winFlux  = _acWinFlux;
            _acWinFlux = 0f;
        }

        // Reset detection state after 3 s of silence so a new song re-learns BPM from scratch
        if (e < 0.001f)
        {
            _silenceTimer += Time.deltaTime;
            if (_silenceTimer >= 3f && !_detectionReset)
            {
                ResetDetection();
                _detectionReset = true;
            }
        }
        else
        {
            _silenceTimer     = 0f;
            _detectionReset   = false;
        }

        // IsBeat from spectral flux threshold
        _avgFlux = Mathf.Lerp(_avgFlux, flux, smoothing);
        IsBeat   = flux > _avgFlux * sensitivity && flux > 0.001f;

        Energy        = e;
        BassEnergy    = bass;
        SubBassEnergy = subBass;
        MidEnergy     = mid;
        HighEnergy    = high;

        // Vibe: how bass-dominant vs bright, and overall intensity
        float total            = bass + mid + high + 0.0001f;
        float targetVibeWeight = bass / total;
        float targetVibeEnergy = Mathf.Clamp01(e * 5f);
        _smoothedVibeWeight    = Mathf.Lerp(_smoothedVibeWeight, targetVibeWeight, Time.deltaTime * 0.5f);
        _smoothedVibeEnergy    = Mathf.Lerp(_smoothedVibeEnergy, targetVibeEnergy, Time.deltaTime * 1f);
        VibeWeight             = _smoothedVibeWeight;
        VibeEnergy             = _smoothedVibeEnergy;

        // Normalize flux by decaying peak, then downsample to 30 Hz (window max)
        if (winFlux > _acPeak) _acPeak = winFlux;
        else _acPeak = Mathf.Max(_acPeak * 0.9995f, 0.001f);
        float normFlux = winFlux / _acPeak;

        _acWinMax = Mathf.Max(_acWinMax, normFlux);
        _acTimer += Time.deltaTime;
        if (_acTimer >= 1f / AcHz)
        {
            _acBuf[_acHead % AcBufSize] = _acWinMax;
            _acHead++;
            _acWinMax = 0f;
            _acTimer -= 1f / AcHz;
        }

        // Every 2 s (once buffer full): run autocorrelation and push result into median filter
        _acCorrTimer += Time.deltaTime;
        if (_acHead >= AcBufSize && _acCorrTimer >= 2f)
        {
            _acCorrTimer = 0f;
            float detected = RunAutocorrelation();
            _acHistory[_acHistoryHead % AcHistorySize] = detected;
            _acHistoryHead++;
            // Median of last 7 detections — a single bad reading has zero influence
            float consensus = MedianInterval();
            _beatInterval = Mathf.Lerp(_beatInterval, consensus, beatIntervalSmoothing * 3f);
        }

        _beatCooldown -= Time.deltaTime;
        if (IsBeat && _beatCooldown <= 0f)
        {
            _beatCooldown = CooldownTime;

            // Gently nudge phase toward the nearest beat boundary
            float nearest = Mathf.Round(_phase / (Mathf.PI * 2f)) * Mathf.PI * 2f;
            _phase = Mathf.Lerp(_phase, nearest, 0.04f);

            // Each beat randomly picks a nod direction: forward (80%), right (10%), left (10%)
            float r = UnityEngine.Random.value;
            if (r < 0.8f)      _targetNodDirDeg =   0f;
            else if (r < 0.9f) _targetNodDirDeg =  90f;
            else               _targetNodDirDeg = -90f;
        }

        BPM = 60f / _beatInterval;
        UpdateBodyWave();

        if (showDebug)
        {
            _debugTimer -= Time.deltaTime;
            if (_debugTimer <= 0f)
            {
                _debugTimer = 0.5f;
                Debug.Log($"BPM: {BPM:F1} | Flux: {flux:F1} | AvgFlux: {_avgFlux:F1} | VibeWeight: {VibeWeight:F2} | VibeEnergy: {VibeEnergy:F2}");
            }
        }
    }

    float RunAutocorrelation()
    {
        float mean = 0f;
        for (int i = 0; i < AcBufSize; i++) mean += _acBuf[i];
        mean /= AcBufSize;

        float bestScore = float.MinValue;
        int   bestLag   = AcMin;

        for (int lag = AcMin; lag <= AcMax; lag++)
        {
            // Harmonic comb: R(L) + 0.5·R(2L) + 0.25·R(3L)
            // The true beat period scores much higher than octave errors because
            // all its multiples also align beats to beats in the same 8.5-second window.
            float score = NormCorrelate(lag, mean);
            int   lag2  = lag * 2;
            int   lag3  = lag * 3;
            if (lag2 < AcBufSize) score += 0.5f  * NormCorrelate(lag2, mean);
            if (lag3 < AcBufSize) score += 0.25f * NormCorrelate(lag3, mean);
            if (score > bestScore) { bestScore = score; bestLag = lag; }
        }

        return bestLag / AcHz;
    }

    float NormCorrelate(int lag, float mean)
    {
        float sum = 0f;
        int   n   = AcBufSize - lag;
        for (int i = 0; i < n; i++)
        {
            float xi  = _acBuf[(_acHead + i) % AcBufSize] - mean;
            float xil = _acBuf[(_acHead + i + lag) % AcBufSize] - mean;
            sum += xi * xil;
        }
        return n > 0 ? sum / n : 0f;
    }

    float MedianInterval()
    {
        int count = Mathf.Min(_acHistoryHead, AcHistorySize);
        if (count == 0) return _beatInterval;
        int start = _acHistoryHead - count;
        for (int i = 0; i < count; i++)
            _acHistorySort[i] = _acHistory[(start + i) % AcHistorySize];
        System.Array.Sort(_acHistorySort, 0, count);
        return _acHistorySort[count / 2];
    }

    void UpdateBodyWave()
    {
        if (boneChain == null || boneChain.Length == 0) return;

        if (!_initialized)
        {
            _initialRots = new Quaternion[boneChain.Length];
            for (int i = 0; i < boneChain.Length; i++)
                if (boneChain[i].bone)
                    _initialRots[i] = boneChain[i].bone.localRotation;
            _initialized = true;
            return;
        }

        if (!reactToMusic)
        {
            for (int i = 0; i < boneChain.Length; i++)
                if (boneChain[i].bone)
                    boneChain[i].bone.localRotation = _initialRots[i];
            HeadNodAngle       = 0f;
            HeadNodTurn        = 0f;
            _smoothedAmplitude = 0f;
            return;
        }

        float rawAmp = Mathf.Clamp01(BassEnergy * bassAmplitudeScale);
        float curved = Mathf.Pow(rawAmp, amplitudeCurve);
        float floor  = Energy > 0.001f ? minAmplitude * (0.5f + VibeEnergy * 0.5f) : 0f;
        float target = BPM > 0f ? Mathf.Max(floor, curved) : 0f;
        _smoothedAmplitude = Mathf.Lerp(_smoothedAmplitude, target, Time.deltaTime * amplitudeSmoothing);

        float vibeAngleScale = Mathf.Lerp(1f, Mathf.Lerp(0.7f, 1.4f, VibeWeight), vibeInfluence);

        // Enter half-time above ~140 BPM, exit below ~128 BPM
        // Tight gap is fine — MoveTowards means any boundary oscillation is slow/invisible
        if (!_halfTime && _beatInterval < 0.43f) _halfTime = true;
        if ( _halfTime && _beatInterval > 0.47f) _halfTime = false;

        float targetNodInterval = _halfTime ? _beatInterval * 2f : _beatInterval;
        _smoothedNodInterval = Mathf.MoveTowards(_smoothedNodInterval, targetNodInterval, Time.deltaTime * 0.1f);

        _phase += (1f / _smoothedNodInterval) * Time.deltaTime * Mathf.PI * 2f;
        _nodDirDeg = Mathf.MoveTowardsAngle(_nodDirDeg, _targetNodDirDeg, Time.deltaTime * 150f);

        for (int i = 0; i <= boneChain.Length; i++)
        {
            float warpedSin = AsymmetricSin(_phase - i * phaseOffset, nodAttack);
            float nodAngle  = warpedSin * _smoothedAmplitude * vibeAngleScale;

            if (i < boneChain.Length)
            {
                if (!boneChain[i].bone) continue;
                boneChain[i].bone.localRotation = _initialRots[i]
                    * Quaternion.Euler(nodAngle * boneChain[i].angle * spineScale, 0f, 0f);
            }
            else
            {
                float dirRad = _nodDirDeg * Mathf.Deg2Rad;
                HeadNodAngle = reactToMusic ? nodAngle * Mathf.Cos(dirRad) * headNodScale : 0f;
                HeadNodTurn  = reactToMusic ? nodAngle * Mathf.Sin(dirRad) * headNodScale : 0f;
            }
        }
    }

    float AsymmetricSin(float phase, float attack)
    {
        float t = (phase % (Mathf.PI * 2f) + Mathf.PI * 2f) % (Mathf.PI * 2f);
        t /= Mathf.PI * 2f;
        float warped = t < attack
            ? (t / attack) * 0.5f
            : 0.5f + ((t - attack) / (1f - attack)) * 0.5f;
        return Mathf.Sin(warped * Mathf.PI * 2f);
    }

    float _debugTimer;

    void ResetDetection()
    {
        System.Array.Clear(_acBuf,     0, AcBufSize);
        System.Array.Clear(_acHistory, 0, AcHistorySize);
        _acHead        = 0;
        _acHistoryHead = 0;
        _acCorrTimer   = 0f;
        _acTimer       = 0f;
        _acWinMax      = 0f;
        _acPeak        = 0.001f;
        _avgFlux       = 0f;
    }

    void OnDestroy()
    {
        _capture?.Stop();
        _capture?.Dispose();
    }
}
