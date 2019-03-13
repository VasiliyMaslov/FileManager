
namespace FileManager.Entities
{
    public class User
    {
        public static object Identity { get; internal set; }

        // данные, которые будут вноситься в БД
        public int userId { get; set; }
        public string name { get; set; }
        public string secondName { get; set; }
        public string login { get; set; }
        public byte[] PasswordHash { get; set; }
        public byte[] HashKey { get; set; }
        public string Role { get; set; }
        public string Token { get; set; }
    }
}
