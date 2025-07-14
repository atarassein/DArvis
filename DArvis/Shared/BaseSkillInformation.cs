namespace DArvis.Shared;

public class BaseSkillInformation
{
    public string sName { get; set; }
    public string sClass { get; set; }
    public string sGroup { get; set; }
    public int sCooldown { get; set; }

    public BaseSkillInformation(string name, string @class, string group, int cooldown = 0)
    {
        sName = name;
        sClass = @class;
        sGroup = group;
        sCooldown = cooldown;
    }

}