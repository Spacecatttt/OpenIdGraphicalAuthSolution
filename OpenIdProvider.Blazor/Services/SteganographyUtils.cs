using System.Security.Cryptography;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

public static class ImageSteganographyUtility
{
    private const int SaltSize = 16;

    public static Image<Rgba32> EmbedText(Image<Rgba32> image, string textToEncrypt, string key)
    {
        byte[] encryptedData = Encrypt(textToEncrypt, key);

        if (encryptedData.Length * 8 + 32 > image.Width * image.Height * 3)
        {
            throw new ArgumentException("Image is too small to hold this text.");
        }

        Image<Rgba32> newImage = image.Clone();

        // Manual byte conversion for endianness safety
        int length = encryptedData.Length;
        byte[] dataLengthBytes =
        [
            (byte)length,
            (byte)(length >> 8),
            (byte)(length >> 16),
            (byte)(length >> 24),
        ];
        for (int i = 0; i < 32; i++)
        {
            EmbedBit(newImage, i, GetBit(dataLengthBytes[i / 8], i % 8));
        }

        for (int i = 0; i < encryptedData.Length * 8; i++)
        {
            int pixelIndex = i + 32;
            EmbedBit(newImage, pixelIndex, GetBit(encryptedData[i / 8], i % 8));
        }

        return newImage;
    }

    public static string ExtractText(Image<Rgba32> image, string key)
    {
        byte[] dataLengthBytes = new byte[4];
        for (int i = 0; i < 32; i++)
        {
            SetBit(ref dataLengthBytes[i / 8], i % 8, ExtractBit(image, i));
        }

        // Manual byte conversion for endianness safety
        int dataLength = dataLengthBytes[0] |
                        (dataLengthBytes[1] << 8) |
                        (dataLengthBytes[2] << 16) |
                        (dataLengthBytes[3] << 24);

        // Capacity check based on 3 channels (RGB)
        if (dataLength <= 0 || dataLength * 8 + 32 > image.Width * image.Height * 3)
        {
            throw new InvalidOperationException("Invalid data or corrupted image.");
        }

        byte[] encryptedData = new byte[dataLength];
        for (int i = 0; i < dataLength * 8; i++)
        {
            int pixelIndex = i + 32;
            SetBit(ref encryptedData[i / 8], i % 8, ExtractBit(image, pixelIndex));
        }

        return Decrypt(encryptedData, key);
    }

    // Helper methods for bit manipulation
    private static void EmbedBit(Image<Rgba32> img, int index, bool bit)
    {
        // Calculation is based on 3 channels (RGB)
        int pixelX = (index / 3) % img.Width;
        int pixelY = (index / 3) / img.Width;
        int channel = index % 3; // 0=R, 1=G, 2=B

        Rgba32 pixel = img[pixelX, pixelY];
        byte r = pixel.R, g = pixel.G, b = pixel.B;

        if (channel == 0) r = SetLsb(r, bit);
        else if (channel == 1) g = SetLsb(g, bit);
        else b = SetLsb(b, bit);
        // The alpha channel (pixel.A) is not modified

        img[pixelX, pixelY] = new Rgba32(r, g, b, pixel.A);
    }
    private static bool ExtractBit(Image<Rgba32> img, int index)
    {
        // Calculation is based on 3 channels (RGB)
        int pixelX = (index / 3) % img.Width;
        int pixelY = (index / 3) / img.Width;
        int channel = index % 3; // 0=R, 1=G, 2=B

        Rgba32 pixel = img[pixelX, pixelY];

        if (channel == 0) return (pixel.R & 1) == 1;
        if (channel == 1) return (pixel.G & 1) == 1;
        // else
        return (pixel.B & 1) == 1;
    }
    private static byte SetLsb(byte value, bool bit) => bit ? (byte)(value | 1) : (byte)(value & ~1);
    private static bool GetBit(byte b, int bitNumber) => ((b >> bitNumber) & 1) == 1;
    private static void SetBit(ref byte b, int bitNumber, bool value)
    {
        if (value)
            b = (byte)(b | (1 << bitNumber));
        else
            b = (byte)(b & ~(1 << bitNumber));
    }

    // Encryption and Decryption methods
    private static byte[] Encrypt(string plainText, string key)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] iv;
        byte[] encrypted;

        using (var aes = Aes.Create())
        {
            iv = aes.IV;
            using (var keyDerivation = new Rfc2898DeriveBytes(key, salt, 10000, HashAlgorithmName.SHA256))
            {
                aes.Key = keyDerivation.GetBytes(32); // 256-bit key
            }
            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var memoryStream = new MemoryStream();
            using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
            using (var streamWriter = new StreamWriter(cryptoStream))
            {
                streamWriter.Write(plainText);
            }
            encrypted = memoryStream.ToArray();
        }
        // Combine Salt, IV, and the ciphertext into a single array
        var result = new byte[salt.Length + iv.Length + encrypted.Length];
        Buffer.BlockCopy(salt, 0, result, 0, salt.Length);
        Buffer.BlockCopy(iv, 0, result, salt.Length, iv.Length);
        Buffer.BlockCopy(encrypted, 0, result, salt.Length + iv.Length, encrypted.Length);
        return result;
    }

    private static string Decrypt(byte[] cipherTextWithSaltAndIv, string key)
    {
        string plaintext;
        using (var aes = Aes.Create())
        {
            // Extract the salt from the beginning of the array
            byte[] salt = new byte[SaltSize];
            Array.Copy(cipherTextWithSaltAndIv, 0, salt, 0, SaltSize);

            // Extract the IV, which comes right after the salt
            int ivSize = aes.BlockSize / 8;
            byte[] iv = new byte[ivSize];
            Array.Copy(cipherTextWithSaltAndIv, SaltSize, iv, 0, ivSize);
            aes.IV = iv;

            // Extract the actual encrypted data
            int encryptedDataSize = cipherTextWithSaltAndIv.Length - SaltSize - ivSize;
            byte[] encryptedData = new byte[encryptedDataSize];
            Array.Copy(cipherTextWithSaltAndIv, SaltSize + ivSize, encryptedData, 0, encryptedDataSize);

            using (var keyDerivation = new Rfc2898DeriveBytes(key, salt, 10000, HashAlgorithmName.SHA256))
            {
                aes.Key = keyDerivation.GetBytes(32);
            }

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var memoryStream = new MemoryStream(encryptedData);
            using var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
            using var streamReader = new StreamReader(cryptoStream);
            plaintext = streamReader.ReadToEnd();
        }
        return plaintext;
    }
}
