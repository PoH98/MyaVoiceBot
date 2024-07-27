using LiteDB;

namespace MyaVoiceBot.Model
{
    public class Member
    {
        [BsonId]
        public ulong Id { get; set; }
        public Role Role { get; set; }
        public string Name { get; set; }
        public bool Banned { get; set; }
    }

    public enum Role
    {
        Owner,
        Member
    }
}
