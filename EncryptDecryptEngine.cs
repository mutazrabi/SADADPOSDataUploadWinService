using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.IO;


namespace SADADPOSDataUploadWinService
{
    public class EncryptDecryptEngine
    {
        #region Member
        private static string Salt = "Test@$%^&$%@!";
        private static string _SharedSecret = "NCCAM@Passw@ord";
        #endregion


        /// <summary> 
        /// Encrypt the given string using AES.  The string can be decrypted using  
        /// DecryptStringAES().  The sharedSecret parameters must match. 
        /// </summary> 
        /// <param name="pPlainText">The text to encrypt.</param> 
        public static string EncryptAES(string pPlainText)
        {
            // Return the encrypted bytes from the memory stream. 
            return EncryptStringAES(pPlainText);
        }
        //public static string DecryptAES(string pCipherText)
        //{
        //    // Return the Decrypted bytes from the memory stream. 

        //    return DecryptStringAES(pCipherText);
        //}
        /// <summary>
        /// Create URL with encrypt parameters
        /// </summary>
        /// <param name="URL">Page URL</param>
        /// <param name="parameters">List of parameter</param>
        /// <returns>Encrypted URL</returns>
        public static string EncryptStringRijndael(String planiText)
        {
            String encodedPlaintext = String.Empty;

            RijndaelManaged rij = new RijndaelManaged();

            byte[] _salt = Encoding.ASCII.GetBytes(Salt);

            Rfc2898DeriveBytes key = new Rfc2898DeriveBytes(_SharedSecret, _salt);
            rij.Key = key.GetBytes(rij.KeySize / 8);
            rij.IV = key.GetBytes(rij.BlockSize / 8);

            ICryptoTransform encryptor = rij.CreateEncryptor(rij.Key, rij.IV);
            using (MemoryStream msEncrypt = new MemoryStream())
            {
                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                {
                    using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                    {
                        swEncrypt.Write(planiText);
                    }
                }
                encodedPlaintext = GetString(msEncrypt.ToArray());
            }
            rij.Clear();
            return encodedPlaintext;
        }
        private static string GetString(byte[] data)
        {
            StringBuilder result = new StringBuilder();

            foreach (Byte b in data)
                result.Append(b.ToString("X2"));
            return result.ToString();
        }
        private static byte[] GetBytes(string data)
        {
            byte[] result = new byte[data.Length / 2];

            for (int i = 0; i < data.Length; i += 2)
                result[i / 2] = Convert.ToByte(data.Substring(i, 2), 16);

            return result;
        }
        /// <summary>
        /// Get parameter with decrypt
        /// </summary>
        /// <param name="EncodedParameter">Encoded Parameter</param>
        /// <returns>Decrypted parameter</returns>
        public static string DecryptStringRijndael(string EncodedParameter)
        {
            string parameterValue = string.Empty;
            try
            {

                RijndaelManaged rij = new RijndaelManaged();

                byte[] _salt = Encoding.ASCII.GetBytes(Salt);

                Rfc2898DeriveBytes key = new Rfc2898DeriveBytes(_SharedSecret, _salt);
                rij.Key = key.GetBytes(rij.KeySize / 8);
                rij.IV = key.GetBytes(rij.BlockSize / 8);

                ICryptoTransform decryptor = rij.CreateDecryptor(rij.Key, rij.IV);

                Byte[] bytes = GetBytes(EncodedParameter);
                using (MemoryStream msDecrypt = new MemoryStream(bytes))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader swDecrypt = new StreamReader(csDecrypt))
                        {
                            parameterValue = swDecrypt.ReadToEnd();
                        }
                    }
                }
            }
            catch (Exception)
            {
                parameterValue = string.Empty;
            }

            return parameterValue;
        }

        //public static string DecryptStringAES(string cipherText)
        //{
        //    try
        //    {
        //        byte[] _salt = Encoding.ASCII.GetBytes(Salt);
        //        Aes myAes = Aes.Create();
        //        Rfc2898DeriveBytes key = new Rfc2898DeriveBytes(_SharedSecret, _salt);
        //        myAes.Key = key.GetBytes(myAes.KeySize / 8);
        //        myAes.IV = key.GetBytes(myAes.BlockSize / 8);
        //        // Check arguments.
        //        if (cipherText == null || cipherText.Length <= 0)
        //            throw new ArgumentNullException("cipherText");
        //        if (myAes.Key == null || myAes.Key.Length <= 0)
        //            throw new ArgumentNullException("Key");
        //        if (myAes.IV == null || myAes.IV.Length <= 0)
        //            throw new ArgumentNullException("Key");

        //        // Declare the string used to hold
        //        // the decrypted text.
        //        string plaintext = null;

        //        // Create an Aes object
        //        // with the specified key and IV.
        //        // Create a decrytor to perform the stream transform.
        //        ICryptoTransform decryptor = myAes.CreateDecryptor(myAes.Key
        //                , myAes.IV);
        //        Byte[] bytes = GetBytes(cipherText);
        //        // Create the streams used for decryption.
        //        using (MemoryStream msDecrypt = new MemoryStream(bytes))
        //        {
        //            using (CryptoStream csDecrypt = new CryptoStream(msDecrypt
        //                , decryptor, CryptoStreamMode.Read))
        //            {
        //                using (StreamReader srDecrypt = new StreamReader(csDecrypt))
        //                {

        //                    // Read the decrypted bytes from the decrypting 
        //                    plaintext = srDecrypt.ReadToEnd();
        //                }
        //            }
        //        }

        //        return plaintext;
        //    }
        //    catch
        //    {
        //        return null;

        //    }
        //}

        public static string EncryptStringAES(String plainText)
        {
            Aes myAes = Aes.Create();
            byte[] _salt = Encoding.ASCII.GetBytes(Salt);
            Rfc2898DeriveBytes key = new Rfc2898DeriveBytes(_SharedSecret, _salt);
            myAes.Key = key.GetBytes(myAes.KeySize / 8);
            myAes.IV = key.GetBytes(myAes.BlockSize / 8);
            // Check arguments.
            if (plainText == null || plainText.Length <= 0)
                throw new ArgumentNullException("plainText");
            if (myAes.Key == null || myAes.Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (myAes.IV == null || myAes.IV.Length <= 0)
                throw new ArgumentNullException("Key");
            byte[] encrypted;
            // Create an Aes object
            // with the specified key and IV.
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = myAes.Key;
                aesAlg.IV = myAes.IV;

                // Create a decrytor to perform the stream transform.
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key
                                                                        , aesAlg.IV);

                // Create the streams used for encryption.
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt
                                                           , encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(
                                                                        csEncrypt))
                        {

                            //Write all data to the stream.
                            swEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }

            // Return the encrypted bytes from the memory stream.
            return GetString(encrypted);

        }

    }
}
