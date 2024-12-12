namespace ElinModTemplate;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class SettingDependentPatchAttribute(string section, string key) : Attribute {
    public string Section => section;
    public string Key => key;
}