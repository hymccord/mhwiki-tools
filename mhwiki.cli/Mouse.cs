using System.Diagnostics;
using System.Text.Json.Serialization;

namespace mhwiki.cli;

#nullable disable

[DebuggerDisplay("{ToString()}")]
public class Mouse
{
    public long Id { get; set; }
    public string Type { get; set; }
    public string Name { get; set; }
    public string AbbreviatedName { get; set; }
    public string Description { get; set; }
    public long Points { get; set; }
    public string PointsFormatted { get; set; }
    public long Gold { get; set; }
    public string GoldFormatted { get; set; }
    public string Group { get; set; }
    public string Subgroup { get; set; }
    public Images Images { get; set; }
    public Dictionary<Effectiveness, HashSet<PowerType>> Weaknesses { get; set; }
    public Dictionary<PowerType, int> Minlucks { get; set; }
    public long Wisdom { get; set; }
    public Dictionary<PowerType, double> Effectivenesses { get; set; }
    public string Location { get; set; }

    public override string ToString()
    {
        return $"{Id}: {Name}";
    }
}

[JsonConverter(typeof(JsonStringEnumConverter<Effectiveness>))]
public enum Effectiveness
{
    LessEffective,
    Effective,
    VeryEffective
}

[JsonConverter(typeof(JsonStringEnumConverter<PowerType>))]
public enum PowerType
{
    Power,
    Physical,
    Shadow,
    Tactical,
    Arcane,
    Forgotten,
    Hydro,
    Draconic,
    Law,
    Rift,
    Parental,
}

public partial class Images
{
    public Uri Thumbnail { get; set; }
    public Uri SilhouetteThumbnail { get; set; }
    public Uri Medium { get; set; }
    public Uri SilhouetteMedium { get; set; }
    public Uri Large { get; set; }
    public Uri SilhouetteLarge { get; set; }
    public Uri Square { get; set; }
    public bool IsLandscape { get; set; }
}
