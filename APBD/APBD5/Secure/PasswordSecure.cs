using System;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;


namespace APBD5.Secure
{

    public class PasswordSecure
    {
        public static string CodePassword(string pass,string coder)
        {
            var byeVal= KeyDerivation.Pbkdf2(password: pass, coder: Encoding.UTF8.GetBytes(coder),prf: KeyDerivationPrf.HMACSHA512, iterationCount: 10000,numBytesRequested: 256 / 8);

            return Convert.ToBase64String(byeVal);
        }

        public static bool Check(string val, string coder, string hash)
          => Create(val, coder) == hash;

        public static string CoderCreator()
        {
            byte[] byteArr = new byte[128 / 8];

            using (var generate = RandomNumberGenerator.Create())
            {
                generate.GetBytes(byteArr);

                return Convert.ToBase64String(byteArr);
            }
        }
        
       




    }
}
