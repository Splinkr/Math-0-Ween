using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

[Serializable]
public sealed class MatchMetadata
{
    public int schemaVersion;
    public string seed;
    public string rngVersion;
    public string mathRulesVersion;
    public string scoringRulesVersion;
    public string balanceVersion;
    public string configurationSignature;
    public double durationSeconds = -1;
    public string gameVersion;

    internal const string BalanceVersion = "balance-v1";
    internal MatchMetadata Copy() => (MatchMetadata)MemberwiseClone();

    internal static void Validate(XmlElement element)
    {
        if (element.GetAttribute("type") == "null") return;
        if (element.GetAttribute("type") != "object") throw new FormatException("Match metadata must be an object or null.");
        foreach (string field in new[] { "seed", "rngVersion", "mathRulesVersion", "scoringRulesVersion", "balanceVersion", "configurationSignature", "gameVersion" })
        {
            var nodes = element.SelectNodes(field);
            if (nodes.Count > 1) throw new FormatException("Duplicate match field: " + field);
            if (nodes.Count == 1 && ((XmlElement)nodes[0]).GetAttribute("type") != "string" && ((XmlElement)nodes[0]).GetAttribute("type") != "null")
                throw new FormatException("Match field must be a string: " + field);
        }
        if (element.SelectNodes("schemaVersion").Count > 1 || element.SelectNodes("durationSeconds").Count > 1)
            throw new FormatException("Duplicate numeric match field.");
        var version = element["schemaVersion"];
        if (version != null && (version.GetAttribute("type") != "number" || !int.TryParse(version.InnerText, NumberStyles.None, CultureInfo.InvariantCulture, out int parsed) || parsed < 0 || parsed > 1))
            throw new FormatException("Unsupported match schema version.");
        var duration = element["durationSeconds"];
        if (duration != null && (duration.GetAttribute("type") != "number" || !double.TryParse(duration.InnerText, NumberStyles.Float, CultureInfo.InvariantCulture, out double seconds)
            || double.IsNaN(seconds) || double.IsInfinity(seconds) || (seconds < 0 && seconds != -1)))
            throw new FormatException("Invalid active match duration.");
        string seed = element["seed"]?.InnerText;
        if (!string.IsNullOrEmpty(seed) && !ulong.TryParse(seed, NumberStyles.None, CultureInfo.InvariantCulture, out _))
            throw new FormatException("Invalid unsigned match seed.");
        string signature = element["configurationSignature"]?.InnerText;
        if (!string.IsNullOrEmpty(signature) && !Regex.IsMatch(signature, @"\Aeffective-config-v1:[0-9a-f]{64}\z"))
            throw new FormatException("Invalid effective configuration signature.");
    }
}

// Canonical description of already captured values, never an asset serializer.
internal sealed class MatchConfiguration
{
    private readonly StringBuilder text = new StringBuilder();
    private bool valid = true;
    internal MatchConfiguration Add(string key, int value) => AddText(key, value.ToString(CultureInfo.InvariantCulture));
    internal MatchConfiguration Add(string key, bool value) => AddText(key, value ? "1" : "0");
    internal MatchConfiguration Add(string key, float value)
    {
        valid &= !float.IsNaN(value) && !float.IsInfinity(value);
        return AddText(key, (value == 0 ? 0f : value).ToString("R", CultureInfo.InvariantCulture));
    }
    internal MatchConfiguration Add(string key, double value)
    {
        valid &= !double.IsNaN(value) && !double.IsInfinity(value);
        return AddText(key, (value == 0 ? 0d : value).ToString("R", CultureInfo.InvariantCulture));
    }
    private MatchConfiguration AddText(string key, string value)
    {
        text.Append(key).Append('=').Append(value).Append('\n');
        return this;
    }
    internal string Description => valid ? text.ToString() : null;
    internal static string Signature(params string[] sections)
    {
        var input = new StringBuilder("effective-config-v1\n" + MathGeneration.RulesVersion + "\n" + ScoreRules.RulesVersion + "\n" + MatchMetadata.BalanceVersion + "\n");
        foreach (string section in sections)
        {
            if (section == null) return null;
            input.Append(section.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(section);
        }
        using (var sha = SHA256.Create())
        {
            var digest = sha.ComputeHash(Encoding.UTF8.GetBytes(input.ToString()));
            var hex = new StringBuilder("effective-config-v1:");
            foreach (byte value in digest) hex.Append(value.ToString("x2", CultureInfo.InvariantCulture));
            return hex.ToString();
        }
    }
}

// Completed metadata crosses scene boundaries; score remains solely in LastScore.
internal sealed class CompletedMatch
{
    internal readonly GradePhase Grade;
    private readonly MatchMetadata metadata;
    internal CompletedMatch(GradePhase grade, MatchMetadata metadata) { Grade = grade; this.metadata = metadata.Copy(); }
    internal MatchMetadata Metadata => metadata.Copy();
}
