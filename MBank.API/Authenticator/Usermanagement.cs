using System.Runtime.CompilerServices;

namespace MBank.API.Authenticator
{
    record TheUserDto(string username, string password);
    internal class Authenticator
    {
        public static async Task<bool> ValidateUser(TheUserDto user)
        {
            await Task.Delay(1);
            if(user.username.Trim().ToUpper() == "DKADMIN" && user.password.Trim() == "Admin@123") return true;
            return false;
        }
    }
}
