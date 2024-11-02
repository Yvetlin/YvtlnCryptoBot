using System;
using System.Text;

namespace TelegramEncryptionBot.Crypto
{
    public class IdeaCipher
    {
        private Idea _idea;

        public IdeaCipher(string charKey, bool encrypt)
        {
            // Инициализация объекта шифрования/дешифрования IDEA
            _idea = new Idea(charKey, encrypt);
        }

        public string Encrypt(string plainText)
        {
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] encryptedBytes = Process(plainBytes);
            return Convert.ToBase64String(encryptedBytes);
        }

        public string Decrypt(string encryptedText)
        {
            byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
            byte[] decryptedBytes = Process(encryptedBytes);
            return Encoding.UTF8.GetString(decryptedBytes);
        }

        private byte[] Process(byte[] inputBytes)
        {
            // Рассчитываем количество блоков по 8 байт
            int paddedLength = (inputBytes.Length + 7) / 8 * 8; // Округляем до ближайшего большего кратного 8
            byte[] result = new byte[paddedLength];
            byte[] paddedInput = new byte[paddedLength];

            // Копируем исходные данные и дополняем нулями
            Array.Copy(inputBytes, paddedInput, inputBytes.Length);
    
            byte[] block = new byte[8];

            // Обрабатываем каждый 8-байтовый блок
            for (int i = 0; i < paddedLength; i += 8)
            {
                Array.Copy(paddedInput, i, block, 0, 8);
                _idea.crypt(block);
                Array.Copy(block, 0, result, i, 8);
            }

            return result;
        }

    }

    public class Idea
    {
        internal static int rounds = 8;
        internal int[] subKey;

        public Idea(string charKey, bool encrypt)
        {
            byte[] key = generateUserKeyFromCharKey(charKey);
            int[] tempSubKey = expandUserKey(key);
            subKey = encrypt ? tempSubKey : invertSubKey(tempSubKey);
        }

        public void crypt(byte[] data)
        {
            crypt(data, 0);
        }

        public void crypt(byte[] data, int dataPos)
        {
            int x0 = ((data[dataPos + 0] & 0xFF) << 8) | (data[dataPos + 1] & 0xFF);
            int x1 = ((data[dataPos + 2] & 0xFF) << 8) | (data[dataPos + 3] & 0xFF);
            int x2 = ((data[dataPos + 4] & 0xFF) << 8) | (data[dataPos + 5] & 0xFF);
            int x3 = ((data[dataPos + 6] & 0xFF) << 8) | (data[dataPos + 7] & 0xFF);

            int p = 0;
            for (int round = 0; round < rounds; round++)
            {
                int y0 = mul(x0, subKey[p++]);
                int y1 = add(x1, subKey[p++]);
                int y2 = add(x2, subKey[p++]);
                int y3 = mul(x3, subKey[p++]);

                int t0 = mul(y0 ^ y2, subKey[p++]);
                int t1 = add(y1 ^ y3, t0);
                int t2 = mul(t1, subKey[p++]);
                int t3 = add(t0, t2);

                x0 = y0 ^ t2;
                x1 = y2 ^ t2;
                x2 = y1 ^ t3;
                x3 = y3 ^ t3;
            }

            int r0 = mul(x0, subKey[p++]);
            int r1 = add(x2, subKey[p++]);
            int r2 = add(x1, subKey[p++]);
            int r3 = mul(x3, subKey[p++]);

            data[dataPos + 0] = (byte)(r0 >> 8);
            data[dataPos + 1] = (byte)r0;
            data[dataPos + 2] = (byte)(r1 >> 8);
            data[dataPos + 3] = (byte)r1;
            data[dataPos + 4] = (byte)(r2 >> 8);
            data[dataPos + 5] = (byte)r2;
            data[dataPos + 6] = (byte)(r3 >> 8);
            data[dataPos + 7] = (byte)r3;
        }

        private static int[] expandUserKey(byte[] userKey)
        {
            if (userKey.Length != 16)
                throw new ArgumentException("Key length must be 128 bits", nameof(userKey));

            int[] key = new int[rounds * 6 + 4];
            for (int i = 0; i < userKey.Length / 2; i++)
                key[i] = ((userKey[2 * i] & 0xFF) << 8) | (userKey[2 * i + 1] & 0xFF);

            for (int i = userKey.Length / 2; i < key.Length; i++)
                key[i] = ((key[(i + 1) % 8 != 0 ? i - 7 : i - 15] << 9) | (key[(i + 2) % 8 < 2 ? i - 14 : i - 6] >> 7)) & 0xFFFF;

            return key;
        }

        private static int[] invertSubKey(int[] key)
        {
            int[] invKey = new int[key.Length];
            int p = 0;
            int i = rounds * 6;

            invKey[i + 0] = mulInv(key[p++]);
            invKey[i + 1] = addInv(key[p++]);
            invKey[i + 2] = addInv(key[p++]);
            invKey[i + 3] = mulInv(key[p++]);

            for (int r = rounds - 1; r >= 0; r--)
            {
                i = r * 6;
                int m = r > 0 ? 2 : 1;
                int n = r > 0 ? 1 : 2;
                invKey[i + 4] = key[p++];
                invKey[i + 5] = key[p++];
                invKey[i + 0] = mulInv(key[p++]);
                invKey[i + m] = addInv(key[p++]);
                invKey[i + n] = addInv(key[p++]);
                invKey[i + 3] = mulInv(key[p++]);
            }

            return invKey;
        }

        private static int add(int a, int b) => (a + b) & 0xFFFF;

        private static int mul(int a, int b)
        {
            long r = (long)a * b;
            return r != 0 ? (int)(r % 0x10001) & 0xFFFF : (1 - a - b) & 0xFFFF;
        }

        private static int addInv(int x) => (0x10000 - x) & 0xFFFF;

        private static int mulInv(int x)
        {
            if (x <= 1) return x;
            int y = 0x10001, t0 = 1, t1 = 0;
            while (true)
            {
                t1 += y / x * t0; y %= x;
                if (y == 1) return 0x10001 - t1;
                t0 += x / y * t1; x %= y;
                if (x == 1) return t0;
            }
        }

        private static byte[] generateUserKeyFromCharKey(string charKey)
        {
            int nofChar = 0x7E - 0x21 + 1;
            int[] a = new int[8];
            foreach (char c in charKey)
            {
                int code = c;
                for (int i = a.Length - 1; i >= 0; i--)
                {
                    code += a[i] * nofChar;
                    a[i] = code & 0xFFFF;
                    code >>= 16;
                }
            }

            byte[] key = new byte[16];
            for (int i = 0; i < 8; i++)
            {
                key[i * 2] = (byte)(a[i] >> 8);
                key[i * 2 + 1] = (byte)a[i];
            }
            return key;
        }
    }
}
