using System;
using UnityEngine;

// No session state or configuration asset ownership: advances the facade's existing fields.
internal static class DifficultyProgression
{
    internal readonly struct Curve
    {
        internal readonly double Start, RatePerMinute, Maximum;
        internal Curve(double start, double ratePerMinute, double maximum)
        {
            Start = start; RatePerMinute = ratePerMinute; Maximum = maximum;
        }
        internal static Curve Default => new Curve(180, 0.25, 2);
        internal static Curve Unaccelerated => new Curve(180, 0, 1);

        internal double Multiplier(double seconds)
        {
            if (double.IsNaN(seconds)) return 1;
            if (RatePerMinute == 0 || Maximum == 1) return 1;
            return 1 + Math.Min(Math.Max(0, seconds - Start) / 60 * RatePerMinute, Maximum - 1);
        }

        // Integrate the baseline, the clipped linear ramp, and the capped tail.
        // Work over the requested span instead of subtracting two large primitives.
        internal double Integral(double from, double to)
        {
            if (to <= from) return 0;
            double area = to - from;
            if (RatePerMinute == 0 || Maximum == 1) return area;
            double slope = RatePerMinute / 60;
            double capTime = Start + (Maximum - 1) / slope;
            double rampFrom = Math.Max(from, Start), rampTo = Math.Min(to, capTime);
            if (rampTo > rampFrom)
                area += (rampTo - rampFrom) * (((rampFrom - Start) + (rampTo - Start)) * slope / 2);
            area += Math.Max(0, to - Math.Max(from, capTime)) * (Maximum - 1);
            return area;
        }
    }

    // Owned by the facade. Double working values avoid float rounding per render frame/batch.
    internal struct State
    {
        internal double Fraction, Speed, Interval;
        internal bool Initialized;
    }

    internal static Curve ReadCurve(GameBalanceConfig.DifficultySettings settings, out bool valid)
    {
        valid = settings != null
            && Finite(settings.accelerationStartSeconds) && settings.accelerationStartSeconds >= 0
            && Finite(settings.rateIncreasePerMinute) && settings.rateIncreasePerMinute >= 0
            && Finite(settings.maximumRateMultiplier) && settings.maximumRateMultiplier >= 1;
        return valid ? new Curve(settings.accelerationStartSeconds, settings.rateIncreasePerMinute, settings.maximumRateMultiplier) : Curve.Default;
    }

    private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    internal static double EffectivePeriod(double configured) => Finite(configured) && configured > 0 ? Math.Max(1, configured) : 20;

    internal static bool TryBatch(Curve curve, double from, double now, double configuredPeriod, out double end, out double growth)
    {
        end = from; growth = 0;
        if (!Finite(from) || !Finite(now) || now <= from) return false;
        double period = EffectivePeriod(configuredPeriod);
        // Tolerance covers only numerical error at a logical tick boundary.
        double ticks = Math.Floor((now - from) / period + 1e-9);
        if (ticks < 1) return false;
        end = from + ticks * period;
        growth = (curve.Integral(from, end) / period) * 2;
        return true;
    }

    internal readonly struct Parameters
    {
        internal readonly float SpeedIncrease, MaxSpeed, IntervalDecrease, MinInterval;
        internal Parameters(float speedIncrease, float maxSpeed, float intervalDecrease, float minInterval)
        {
            SpeedIncrease = speedIncrease;
            MaxSpeed = maxSpeed;
            IntervalDecrease = intervalDecrease;
            MinInterval = minInterval;
        }
    }

    internal static bool IsDue(float timer, float interval, float lastIncreaseTime)
    {
        return timer >= interval && Time.time - lastIncreaseTime >= 1f;
    }

    internal static void Advance(double growth, ref State state, ref int difficultyLevel,
        ref float fallSpeed, ref float spawnInterval, Func<Parameters> readParameters)
    {
        if (double.IsNaN(growth) || growth <= 0) return;
        if (double.IsPositiveInfinity(growth)) growth = double.MaxValue;
        double pending = state.Fraction + growth;
        int previous = difficultyLevel;
        if (pending >= (long)int.MaxValue - difficultyLevel)
        {
            difficultyLevel = int.MaxValue;
            state.Fraction = 0;
        }
        else
        {
            long whole = (long)Math.Floor(pending + 1e-9);
            difficultyLevel = (int)(difficultyLevel + whole);
            state.Fraction = Math.Max(0, pending - whole);
        }
        Debug.Log("Difficulty increased by " + ((long)difficultyLevel - previous) + " to " + difficultyLevel);
        // Preserve parameter edits made by synchronous difficulty-log callbacks.
        Parameters parameters = readParameters();
        if (!state.Initialized || (float)state.Speed != fallSpeed) state.Speed = fallSpeed;
        if (!state.Initialized || (float)state.Interval != spawnInterval) state.Interval = spawnInterval;
        state.Initialized = true;
        state.Speed = Math.Min(parameters.MaxSpeed, state.Speed + parameters.SpeedIncrease * growth);
        state.Interval = Math.Max(parameters.MinInterval, state.Interval - parameters.IntervalDecrease * growth);
        fallSpeed = (float)state.Speed;
        spawnInterval = (float)state.Interval;
        Debug.Log("Fall speed: " + fallSpeed + " | Spawn interval: " + spawnInterval);
    }
}
