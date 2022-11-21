using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using UtilsApi.Models;

namespace UtilsApi.Controllers;

[ApiController]
[Route("[controller]/[action]")]
public class EncryptionController : ControllerBase
{
    private readonly ILogger<EncryptionController> _logger;

    public EncryptionController(ILogger<EncryptionController> logger)
    {
        _logger = logger;
    }

    [HttpPost(Name = "Encrypt")]
    public ActionResult<string> Encrypt([FromBody] EncryptionRequest request)
    {
        try
        {
            string encryptionKey = request.Key;

            if (string.IsNullOrEmpty(encryptionKey))
            {
                return BadRequest("Missing key");
            }

            byte[] cipherData;

            Aes aes = Aes.Create();
            aes.Key = Convert.FromBase64String(encryptionKey);
            aes.GenerateIV();
            aes.Mode = CipherMode.CBC;
            ICryptoTransform cipher = aes.CreateEncryptor(aes.Key, aes.IV);
            using (MemoryStream ms = new())
            {
                using (CryptoStream cs = new(ms, cipher, CryptoStreamMode.Write))
                {
                    using StreamWriter sw = new(cs);
                    sw.Write(request.PlainText);
                }
                cipherData = ms.ToArray();
            }
            byte[] combinedData = new byte[aes.IV.Length + cipherData.Length];
            Array.Copy(aes.IV, 0, combinedData, 0, aes.IV.Length);
            Array.Copy(cipherData, 0, combinedData, aes.IV.Length, cipherData.Length);
            return Convert.ToBase64String(combinedData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Encrypt Exception");
            return "";
        }
    }

    [HttpPost(Name = "Decrypt")]
    public ActionResult<string> Decrypt([FromBody] DecryptionRequest request)
    {
        try
        {
            string decryptionKey = request.Key;

            if (string.IsNullOrEmpty(decryptionKey))
            {
                return BadRequest("Missing key");
            }

            string decrypted;
            byte[] combinedData;
            try
            {
                combinedData = Convert.FromBase64String(request.CombinedString);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error occured while decrypting");
                combinedData = Encoding.ASCII.GetBytes(request.CombinedString);
            }

            Aes aes = Aes.Create();
            aes.Key = Convert.FromBase64String(decryptionKey);
            byte[] iv = new byte[aes.BlockSize / 8];
            byte[] cipherText = new byte[combinedData.Length - iv.Length];
            Array.Copy(combinedData, iv, iv.Length);
            Array.Copy(combinedData, iv.Length, cipherText, 0, cipherText.Length);
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            ICryptoTransform decipher = aes.CreateDecryptor(aes.Key, aes.IV);
            using MemoryStream ms = new(cipherText);
            using (CryptoStream cs = new(ms, decipher, CryptoStreamMode.Read))
            {
                using StreamReader sr = new(cs);
                decrypted = sr.ReadToEnd();
            }
            return decrypted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Decrypt Exception on {request.CombinedString}");
            return "";
        }
    }
}