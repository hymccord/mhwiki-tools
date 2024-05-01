using System.Diagnostics;

namespace mhwiki.cli;

[DebuggerDisplay("{ToString()}")]
class Mouse
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string AbbreviatedName { get; set; }
    public string Group { get; set; }

    public override string ToString()
    {
        return $"{Id}: {Name} {AbbreviatedName}";
    }
}
