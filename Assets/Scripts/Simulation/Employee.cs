using System.Collections.Generic;

public class Employee
{
    public int energy = 100;      // 0–100, the scarce resource
    public int career = 0;         // progress toward the promotion
    public int toilet = 100;       // 0-100
    public Dictionary<SkillType, int> skills = new Dictionary<SkillType, int>
    {
        { SkillType.Charisma, 0 },
        { SkillType.Programming, 0 },
        { SkillType.Writing, 0 },
        { SkillType.Administration, 0 },
        { SkillType.Science, 0 },
    };
    
    public int GetSkill(SkillType type) => skills[type];
    public void ChangeSkill(SkillType type, int amount) => skills[type] += amount;

}

