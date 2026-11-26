using System;

// The controller samples a short window. Isolated stalls and long pauses do not
// repeatedly resize render targets or penalize the next match.
public sealed class OZGraphicsBudget
{
    public readonly struct Profile
    {
        public readonly float Minimum, Maximum, ShadowDistance, ViewDistance;
        public readonly int ShadowPixels, Samples;

        public Profile(float minimum, float maximum, float shadowDistance, float viewDistance,
                       int shadowPixels, int samples)
        {
            Minimum = minimum;
            Maximum = maximum;
            ShadowDistance = shadowDistance;
            ViewDistance = viewDistance;
            ShadowPixels = shadowPixels;
            Samples = samples;
        }
    }

    static readonly Profile[] Profiles = {
        new Profile(.62f, .80f, 24, 430, 512, 1),
        new Profile(.72f, .92f, 48, 560, 1024, 2),
        new Profile(.80f, 1f, 80, 700, 1024, 2),
        new Profile(.86f, 1.08f, 110, 900, 2048, 4)
    };

    public Profile Settings { get; private set; }
    public float Scale { get; private set; }
    float seconds;
    int frames, goodWindows;

    public void Reset(int quality)
    {
        Settings = Profiles[Math.Max(0, Math.Min(Profiles.Length - 1, quality))];
        Scale = Settings.Maximum;
        ResetWindow();
    }

    public void ResetWindow()
    {
        frames = goodWindows = 0;
        seconds = 0;
    }

    public bool Sample(float delta, int fps)
    {
        if (!(delta > 0 && delta < .5f) || fps <= 0)
        {
            ResetWindow();
            return false;
        }
        seconds += delta;
        frames++;
        if (seconds < 1.5f) return false;
        float load = seconds * fps / frames;
        seconds = 0;
        frames = 0;
        float before = Scale;
        if (load > 1.18f)
        {
            Scale = Math.Max(Settings.Minimum, Scale - .06f);
            goodWindows = 0;
        }
        else if (load <= 1.04f)
        {
            if (++goodWindows >= 3)
            {
                Scale = Math.Min(Settings.Maximum, Scale + .025f);
                goodWindows = 0;
            }
        }
        else goodWindows = 0;
        return Scale != before;
    }
}
