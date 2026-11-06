public static class GameSettings
{
    // Applied once when a match context is created. Null restores automatic seeds.
    public static ulong? FixedSeed { get; set; }
    public static int Difficulty { get; set; }
    // Compatibility alias: the selected school grade has one authoritative value.
    public static GradePhase GradePhase
    {
        get => (GradePhase)Difficulty;
        set => Difficulty = (int)value;
    }
    public static int LastScore { get; internal set; }
    internal static CompletedMatch LastMatch { get; set; }
}
