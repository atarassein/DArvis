namespace DArvis.Types;

public class StaffTable 
{
    public string Name { get; set; }
    public byte Level { get; set; }
    public string @Class { get; set; }
    public byte Id { get; set; }
    public SpellModifiers Modifer = new SpellModifiers();
}