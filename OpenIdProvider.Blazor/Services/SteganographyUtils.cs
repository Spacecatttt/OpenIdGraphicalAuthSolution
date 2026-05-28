using System.Security.Cryptography;
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace OpenIdProvider.Blazor.Services;

public static class ImageSteganographyUtility
{
    private const int SaltSize = 16;
    private const int Pbkdf2Iterations = 10000;
    // Size of blocks for complexity analysis
    private const int BlockSize = 8;
    // Threshold for standard deviation to consider a block "complex" enough
    private const double ComplexityThreshold = 10.0;

    public static Image<Rgba32> EmbedText(Image<Rgba32> image, string textToEncrypt, string key)
    {
        byte[] encryptedData = Encrypt(textToEncrypt, key);

        // Generate a map of safe, shuffled locations for embedding
        List<Point> embeddingMap = GetEmbeddingMap(image, key);

        // Check if the image has enough capacity in its complex regions
        // We need space for the message length (32 bits) + the message itself
        int requiredBits = 32 + encryptedData.Length * 8;
        if (embeddingMap.Count * 3 < requiredBits)
        {
            throw new ArgumentException("Image is not complex enough or too small to hold this text.");
        }

        Image<Rgba32> newImage = image.Clone();

        // Embed Data Length (32 bits)
        int length = encryptedData.Length;
        byte[] dataLengthBytes = BitConverter.GetBytes(length);

        for (int i = 0; i < 32; i++)
        {
            Point location = embeddingMap[i / 3];
            int channel = i % 3;
            bool bit = GetBit(dataLengthBytes[i / 8], i % 8);
            EmbedBitInPixel(newImage, location.X, location.Y, channel, bit);
        }

        // Embed Encrypted Message
        for (int i = 0; i < encryptedData.Length * 8; i++)
        {
            int mapIndex = 32 + i;
            Point location = embeddingMap[mapIndex / 3];
            int channel = mapIndex % 3;
            bool bit = GetBit(encryptedData[i / 8], i % 8);
            EmbedBitInPixel(newImage, location.X, location.Y, channel, bit);
        }

        return newImage;
    }

    public static string ExtractText(Image<Rgba32> image, string key)
    {
        List<Point> embeddingMap = GetEmbeddingMap(image, key);
        if (embeddingMap.Count * 3 < 32)
        {
            throw new InvalidOperationException("Invalid data or corrupted image: not enough capacity for length.");
        }

        // Extract Data Length
        byte[] dataLengthBytes = new byte[4];
        for (int i = 0; i < 32; i++)
        {
            Point location = embeddingMap[i / 3];
            int channel = i % 3;
            bool bit = ExtractBitFromPixel(image, location.X, location.Y, channel);
            SetBit(ref dataLengthBytes[i / 8], i % 8, bit);
        }

        int dataLength = BitConverter.ToInt32(dataLengthBytes, 0);
        if (dataLength <= 0 || (dataLength * 8 + 32) > embeddingMap.Count * 3)
        {
            throw new InvalidOperationException("Invalid data or corrupted image.");
        }

        // Extract Encrypted Message
        byte[] encryptedData = new byte[dataLength];
        for (int i = 0; i < dataLength * 8; i++)
        {
            int mapIndex = 32 + i;
            Point location = embeddingMap[mapIndex / 3];
            int channel = mapIndex % 3;
            bool bit = ExtractBitFromPixel(image, location.X, location.Y, channel);
            SetBit(ref encryptedData[i / 8], i % 8, bit);
        }

        return Decrypt(encryptedData, key);
    }

    /// <summary>
    /// Generates a shuffled, reproducible list of "safe" pixel coordinates for embedding.
    /// </summary>
    private static List<Point> GetEmbeddingMap(Image<Rgba32> image, string key)
    {
        var safeLocations = new List<Point>();

        for (int y = 0; y < image.Height - BlockSize; y += BlockSize)
        {
            for (int x = 0; x < image.Width - BlockSize; x += BlockSize)
            {
                var luminances = new List<double>();
                for (int blockY = 0; blockY < BlockSize; blockY++)
                {
                    for (int blockX = 0; blockX < BlockSize; blockX++)
                    {
                        Rgba32 pixel = image[x + blockX, y + blockY];
                        // Standard luminance calculation
                        byte stableR = (byte)(pixel.R & 0xF0);
                        byte stableG = (byte)(pixel.G & 0xF0);
                        byte stableB = (byte)(pixel.B & 0xF0);
                        luminances.Add(0.299 * stableR + 0.587 * stableG + 0.114 * stableB);
                    }
                }

                double stdDev = CalculateStdDev(luminances);
                if (stdDev > ComplexityThreshold)
                {
                    // If block is complex enough, add all its pixels to the list
                    for (int blockY = 0; blockY < BlockSize; blockY++)
                    {
                        for (int blockX = 0; blockX < BlockSize; blockX++)
                        {
                            safeLocations.Add(new Point(x + blockX, y + blockY));
                        }
                    }
                }
            }
        }

        // Shuffle the list of safe locations using the key as a seed
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        int seed = BitConverter.ToInt32(hash, 0);
        var random = new Random(seed);

        // Fisher-Yates shuffle algorithm
        for (int i = safeLocations.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (safeLocations[i], safeLocations[j]) = (safeLocations[j], safeLocations[i]);
        }

        return safeLocations;
    }

    /// <summary>
    /// Calculates the sample standard deviation for a list of values.
    /// </summary>
    private static double CalculateStdDev(List<double> values)
    {
        if (values.Count < 2) return 0;
        double avg = values.Average();
        double sum = values.Sum(d => Math.Pow(d - avg, 2));
        return Math.Sqrt(sum / (values.Count - 1));
    }

    private static void EmbedBitInPixel(Image<Rgba32> img, int x, int y, int channel, bool bit)
    {
        Rgba32 pixel = img[x, y];
        byte r = pixel.R, g = pixel.G, b = pixel.B;

        switch (channel)
        {
            case 0: r = SetLsb(r, bit); break;
            case 1: g = SetLsb(g, bit); break;
            case 2: b = SetLsb(b, bit); break;
        }

        img[x, y] = new Rgba32(r, g, b, pixel.A);
    }

    private static bool ExtractBitFromPixel(Image<Rgba32> img, int x, int y, int channel)
    {
        Rgba32 pixel = img[x, y];
        return channel switch
        {
            0 => (pixel.R & 1) == 1,
            1 => (pixel.G & 1) == 1,
            _ => (pixel.B & 1) == 1,
        };
    }

    private static byte SetLsb(byte value, bool bit) => bit ? (byte)(value | 1) : (byte)(value & 0xFE);
    private static bool GetBit(byte b, int bitNumber) => ((b >> bitNumber) & 1) == 1;
    private static void SetBit(ref byte b, int bitNumber, bool value)
    {
        if (value) b = (byte)(b | (1 << bitNumber));
        else b = (byte)(b & ~(1 << bitNumber));
    }

    // Encryption and Decryption methods
    private static byte[] Encrypt(string plainText, string key)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        using var aes = Aes.Create();
        aes.Key = new Rfc2898DeriveBytes(key, salt, Pbkdf2Iterations, HashAlgorithmName.SHA256).GetBytes(32);
        byte[] iv = aes.IV;

        using var memoryStream = new MemoryStream();
        memoryStream.Write(salt, 0, salt.Length);
        memoryStream.Write(iv, 0, iv.Length);

        using (var cryptoStream = new CryptoStream(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write, leaveOpen: true))
        {
            using (var streamWriter = new StreamWriter(cryptoStream, Encoding.UTF8))
            {
                streamWriter.Write(plainText);
            }
        }
        return memoryStream.ToArray();
    }

    private static string Decrypt(byte[] cipherTextWithSaltAndIv, string key)
    {
        using var memoryStream = new MemoryStream(cipherTextWithSaltAndIv);

        byte[] salt = new byte[SaltSize];
        memoryStream.Read(salt, 0, salt.Length);

        using var aes = Aes.Create();
        byte[] iv = new byte[aes.BlockSize / 8];
        memoryStream.Read(iv, 0, iv.Length);
        aes.IV = iv;

        aes.Key = new Rfc2898DeriveBytes(key, salt, Pbkdf2Iterations, HashAlgorithmName.SHA256).GetBytes(32);

        using var cryptoStream = new CryptoStream(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Read);
        using var streamReader = new StreamReader(cryptoStream, Encoding.UTF8);
        return streamReader.ReadToEnd();
    }
}
