using System.Security.Cryptography;

// One context per match. Metadata is public; consumers receive only their own stream.
public sealed class MatchRandomSession
{
    public const string CurrentVersion = "pcg32-v1";
    private const ulong MathId = 0x4D415448UL;
    private const ulong GameplayId = 0x47414D45UL;

    public ulong RootSeed { get; }
    public string RngVersion => CurrentVersion;
    internal IRandomSource Math { get; }
    internal IRandomSource Gameplay { get; }

    public MatchRandomSession(ulong rootSeed)
    {
        RootSeed = rootSeed;
        Math = new Pcg32(rootSeed ^ MathId, MathId);
        Gameplay = new Pcg32(rootSeed ^ GameplayId, GameplayId);
    }

    internal static MatchRandomSession CreateConfigured()
    {
        ulong? fixedSeed = GameSettings.FixedSeed;
        return new MatchRandomSession(fixedSeed ?? CreateAutomaticSeed());
    }

    private static ulong CreateAutomaticSeed()
    {
        var bytes = new byte[8];
        using (var random = RandomNumberGenerator.Create())
            random.GetBytes(bytes);
        ulong seed = 0;
        for (int i = 0; i < bytes.Length; i++)
            seed |= (ulong)bytes[i] << (8 * i);
        return seed;
    }
}
