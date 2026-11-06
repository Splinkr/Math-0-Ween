using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;
using UnityEngine;

[Serializable]
public class ScoreEntry
{
    public string id;
    public string nome;
    public string dificuldade; // Changed from 'escola'
    public int pontos;
    public string date;
    public MatchMetadata match;
}

[Serializable]
public class ScoreCollection
{
    public List<ScoreEntry> entries = new List<ScoreEntry>();

    internal string ToJson()
    {
        // Unity serializes a null inline class as a default object. Omit that
        // synthesized object so rewriting legacy records cannot invent metadata.
        var document = new XmlDocument();
        string json = JsonUtility.ToJson(this, true);
        using (var reader = JsonReaderWriterFactory.CreateJsonReader(Encoding.UTF8.GetBytes(json), XmlDictionaryReaderQuotas.Max))
            document.Load(reader);
        var records = document.DocumentElement["entries"];
        for (int index = 0; index < entries.Count; index++)
        {
            var record = (XmlElement)records.ChildNodes[index];
            if (entries[index].match == null && record["match"] != null) record.RemoveChild(record["match"]);
        }
        using (var stream = new MemoryStream())
        {
            using (var writer = JsonReaderWriterFactory.CreateJsonWriter(stream, new UTF8Encoding(false), false, true, "  "))
            { document.WriteTo(writer); writer.Flush(); }
            return Encoding.UTF8.GetString(stream.ToArray());
        }
    }

    internal static ScoreCollection ParseChecked(string json)
    {
        // JsonUtility normalizes missing/null arrays and null records to empty
        // values. Validate the JSON types first so a later save cannot erase them.
        var document = new XmlDocument();
        using (var reader = JsonReaderWriterFactory.CreateJsonReader(Encoding.UTF8.GetBytes(json), XmlDictionaryReaderQuotas.Max))
            document.Load(reader);
        XmlElement root = document.DocumentElement;
        XmlElement entries = root?["entries"];
        if (root == null || root.GetAttribute("type") != "object" || root.SelectNodes("entries").Count != 1 || entries.GetAttribute("type") != "array")
            throw new FormatException("Score data must contain one non-null entries array.");
        foreach (XmlElement entry in entries.ChildNodes)
        {
            if (entry.GetAttribute("type") != "object")
                throw new FormatException("Score records must be JSON objects.");
            foreach (string field in new[] { "nome", "dificuldade", "date" })
            {
                if (entry.SelectNodes(field).Count != 1 || entry[field].GetAttribute("type") != "string")
                    throw new FormatException("Score record is missing a string field: " + field);
            }
            XmlElement points = entry["pontos"];
            if (entry.SelectNodes("pontos").Count != 1 || points.GetAttribute("type") != "number" || !int.TryParse(points.InnerText, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out _))
                throw new FormatException("Score points must be an integer.");
            XmlElement id = entry["id"];
            if (entry.SelectNodes("id").Count > 1 || (id != null && id.GetAttribute("type") != "string" && id.GetAttribute("type") != "null"))
                throw new FormatException("Score record ID must be a string when present.");
            if (entry.SelectNodes("match").Count > 1) throw new FormatException("Duplicate match metadata.");
            if (entry["match"] != null) MatchMetadata.Validate(entry["match"]);
        }
        var collection = JsonUtility.FromJson<ScoreCollection>(json);
        for (int index = 0; index < entries.ChildNodes.Count; index++)
        {
            var match = ((XmlElement)entries.ChildNodes[index])["match"];
            if (match == null || match.GetAttribute("type") == "null") collection.entries[index].match = null;
            else
            {
                if (collection.entries[index].match == null) collection.entries[index].match = new MatchMetadata();
                if (match["durationSeconds"] == null) collection.entries[index].match.durationSeconds = -1;
            }
        }
        return collection;
    }
}
