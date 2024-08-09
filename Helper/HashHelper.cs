using System.Security.Cryptography;
using System.Text;
using System;
using System.Linq;

namespace NuevaLuz.Fonoteca.Helper
{
    public class HashHelper
    {
        public static string GenerateHash(string pass)
        {
            using (MD5 md5 = MD5.Create())
            {
                UnicodeEncoding ue = new UnicodeEncoding();
                byte[] byteSourceText = ue.GetBytes(pass);
                byte[] byteHash = md5.ComputeHash(byteSourceText);
                return Convert.ToBase64String(byteHash);
            }
        }

        public static string GenerateRandomPassword()
        {
            const string chars = "ABCDEFGH0123456789";
            Random random = new Random();

            // Generate a random password of 6 characters
            return new string(Enumerable.Repeat(chars, 6)
                                        .Select(s => s[random.Next(s.Length)])
                                        .ToArray());
        }
    }
}
