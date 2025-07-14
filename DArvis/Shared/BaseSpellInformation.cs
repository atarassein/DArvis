namespace DArvis.Shared;

public class BaseSpellInformation
{
    public string Name { get; set; }
    public string Class { get; set; }
    public string Group { get; set; }
    public byte BaseLines { get; set; }
    public byte Lines { get; set; }
    public int Mana { get; set; }
    public int Cooldown { get; set; }

    public BaseSpellInformation(string name, string @class, string group, int mana, byte baselines = 0, int cooldown = 0)
    {
        Name = name;
        Class = @class;
        Group = group;
        BaseLines = baselines;
        Mana = mana;
        Cooldown = cooldown;
        Lines = 0;
    }
}