using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repeater
{

    public static class StringTable
    {

        public const string setupEncrypt = 
                            @"Encryption setup requires a PASSWORD and a secret key
                            A typical example:
                            Password: ThisIsMyPassw0rd
                            Secret Key: 1234567890ABCDE

                            The password will be encrypted and the secure key (returned value) should be placed 
                            in a protected area. 
                            To decrypt, you can use the Secure key to help decrypt your password.

                            At the prompts, enter the following: PASSWORD and SECRET KEY
                            ";
        public const string setupEnterPassword = "Enter yopur password";
        public const string setupEnterEncryptionKey = "Enter your encryption key";

        public const string errInvalidCmdEncrypt =
                            @"Invalid command for encryping passwords.
                            Example usage: 
                                -cmd encrypt password=MyPassword key=123456789ABC
                                -cmd encrypt password ThisIsMyPassword Key 123456789AB
                            ";


    }

}
