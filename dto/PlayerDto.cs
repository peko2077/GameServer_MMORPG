namespace GameServer.dto
{
    public class PlayerDto
    {
        public int PlayerId { get; set; }
        public string PlayerName { get; set; }
        public bool Gender { get; set; }
        public string Pic { get; set; }
        public int Level { get; set; }
        public int Experience { get; set; }
        public DateTime? Birthday { get; set; }
        public string Remark { get; set; }
        public int UserId { get; set; }
    }
}

