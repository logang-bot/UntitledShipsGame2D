// One ability line on a role card: its kind (ABILITY or PASSIVE), its name
// and a one-sentence description.
public struct RoleAbilityLine
{
    public string kind;
    public string name;
    public string description;

    public RoleAbilityLine(string kind, string name, string description)
    {
        this.kind = kind;
        this.name = name;
        this.description = description;
    }
}
