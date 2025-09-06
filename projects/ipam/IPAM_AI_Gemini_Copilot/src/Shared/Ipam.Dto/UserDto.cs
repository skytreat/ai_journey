
namespace Ipam.Dto
{
    public class UserDto
    {
        public string Username { get; set; }
        public string Password { get; set; } // For creation/login, not for sending back
    }
}
