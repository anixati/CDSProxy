using System;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;

namespace CrmWebApiProxy
{
    public class CredentialBuilder
    {
        private readonly string _credFile;
        private readonly string _decCode;

        public CredentialBuilder()
        {
            _credFile = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\cred.crmprxy";
            _decCode = GetProcessorId();
        }

        public (string, SecureString) GetCredentials()
        {
            var unStr = string.Empty;
            var pdStr = new SecureString();
            if (File.Exists(_credFile))
            {
                var b64Str = File.ReadAllText(_credFile);
                if (string.IsNullOrEmpty(b64Str)) throw new Exception($"Failed to read content of cred file");
                var db64Str = Decrypt(b64Str, _decCode);
                if (string.IsNullOrEmpty(db64Str)) throw new Exception($"Failed to decrypt content of cred file");
                var parts = db64Str.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 1) throw new Exception($"Failed to split of cred file");
                unStr = parts[0];
                foreach (char c in parts[1])
                    pdStr.AppendChar(c);
            }
            else
            {
                Log.Msg("Enter User name :");
                unStr = Console.ReadLine();
                Log.Msg("Enter password :");
                while (true)
                {
                    ConsoleKeyInfo c = Console.ReadKey(true);
                    if (c.Key == ConsoleKey.Enter)
                        break;
                    pdStr.AppendChar(c.KeyChar);
                    Console.Write("#");
                }
                var flStr = Encrypt($"{unStr}|{SStoStr(pdStr)}", _decCode);
                File.WriteAllText(_credFile, flStr);
            }
            return (unStr, pdStr);
        }

        private static string SStoStr(SecureString value)
        {
            IntPtr valuePtr = IntPtr.Zero;
            try
            {
                valuePtr = Marshal.SecureStringToGlobalAllocUnicode(value);
                return Marshal.PtrToStringUni(valuePtr);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
            }
        }

        public static string GetProcessorId()
        {
            using (var mc = new ManagementClass("win32_processor"))
            {
                var moc = mc.GetInstances();
                var Id = string.Empty;
                foreach (ManagementObject mo in moc)
                {
                    Id = mo.Properties["processorID"].Value.ToString();
                    break;
                }
                return Id;
            }
        }

        private static byte[] Encrypt(byte[] clearData, byte[] Key, byte[] IV)
        {
            using (var ms = new MemoryStream())
            {
                using (var alg = Rijndael.Create())
                {
                    alg.Key = Key;
                    alg.IV = IV;
                    using (var cs = new CryptoStream(ms, alg.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(clearData, 0, clearData.Length);
                        cs.Close();
                    }
                }
                var encryptedData = ms.ToArray();
                return encryptedData;
            }
        }

        private static string Encrypt(string clearText, string codePassword)
        {
            var clearBytes = Encoding.Unicode.GetBytes(clearText);
            using (var pdb = new PasswordDeriveBytes(codePassword, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 }))
            {
                var encryptedData = Encrypt(clearBytes, pdb.GetBytes(32), pdb.GetBytes(16));
                return Convert.ToBase64String(encryptedData);
            }
        }

        private static byte[] Decrypt(byte[] cipherData, byte[] Key, byte[] IV)
        {
            using (var ms = new MemoryStream())
            {
                using (var alg = Rijndael.Create())
                {
                    alg.Key = Key;
                    alg.IV = IV;
                    using (var cs = new CryptoStream(ms, alg.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(cipherData, 0, cipherData.Length);
                        cs.Close();
                    }
                }
                byte[] decryptedData = ms.ToArray();
                return decryptedData;
            }
        }

        private static string Decrypt(string cipherText, string codePassword)
        {
            var cipherBytes = Convert.FromBase64String(cipherText);
            using (var pdb = new PasswordDeriveBytes(codePassword, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 }))
            {
                var decryptedData = Decrypt(cipherBytes, pdb.GetBytes(32), pdb.GetBytes(16));
                return Encoding.Unicode.GetString(decryptedData);
            }
        }
    }
}