namespace GameServer.dto
{
    public class CharacterDto
    {
        // characterId characterName picPath prefabPath characterGender characterBirthday campName characterRemark characterClassId characterSkillsId
        public int characterId { get; set; }
        public string? characterName { get; set; }
        public string? picPath { get; set; }
        public string? prefabPath { get; set; }
        public bool characterGender { get; set; }
        public string? characterBirthday { get; set; }
        public string? characterRemark { get; set; }
        public int campId { get; set; }
        public int characterClassId { get; set; }
        public int characterSkillsId { get; set; }

    }
}

