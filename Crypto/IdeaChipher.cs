using System;
using System.Numerics;

namespace YvetlinTgBot
{
    public class IdeaCipher
    {
        private readonly (ushort, ushort, ushort, ushort, ushort, ushort)[] _keys;

        public IdeaCipher(BigInteger key)
        {
            _keys = GenerateKeys(key);
        }

        private ushort MulMod(ushort a, ushort b)
        {
            if (a == 0) a = 0x10000;
            if (b == 0) b = 0x10000;

            // Выполняем умножение с использованием длинного целого типа, чтобы избежать переполнения
            uint result = (uint)a * b % 0x10001;
    
            return (ushort)(result == 0x10000 ? 0 : result);
        }


        private ushort AddMod(ushort a, ushort b)
        {
            return (ushort)((a + b) % 0x10000);
        }

        private ushort AddInv(ushort value)
        {
            return (ushort)(0x10000 - value);
        }

        private ushort MulInv(ushort value)
        {
            if (value == 0) return 0;
            int a = 0x10001, b = value, x0 = 1, x1 = 0;
            while (b > 0)
            {
                int q = a / b, r = a % b;
                a = b;
                b = r;
                int x = x0 - q * x1;
                x0 = x1;
                x1 = x;
            }
            return (ushort)((x0 < 0) ? x0 + 0x10001 : x0);
        }

        private (ushort, ushort, ushort, ushort, ushort, ushort)[] GenerateKeys(BigInteger key)
        {
            var subKeys = new ushort[52];
            for (int i = 0; i < 52; i++)
            {
                subKeys[i] = (ushort)((key >> (112 - (i % 8) * 16)) & 0xFFFF);
                if (i % 8 == 7)
                {
                    key = (key << 25) | (key >> 103);
                }
            }

            var keys = new (ushort, ushort, ushort, ushort, ushort, ushort)[9];
            for (int i = 0; i < 9; i++)
            {
                keys[i] = (
                    subKeys[6 * i],
                    subKeys[6 * i + 1],
                    subKeys[6 * i + 2],
                    subKeys[6 * i + 3],
                    subKeys[6 * i + 4],
                    subKeys[6 * i + 5]
                );
            }
            return keys;
        }

        public ulong Encrypt(ulong plainText)
        {
            ushort p1 = (ushort)(plainText >> 48);
            ushort p2 = (ushort)(plainText >> 32);
            ushort p3 = (ushort)(plainText >> 16);
            ushort p4 = (ushort)plainText;

            for (int i = 0; i < 8; i++)
            {
                var (k1, k2, k3, k4, k5, k6) = _keys[i];

                p1 = MulMod(p1, k1);
                p2 = AddMod(p2, k2);
                p3 = AddMod(p3, k3);
                p4 = MulMod(p4, k4);

                ushort x1 = (ushort)(p1 ^ p3);
                ushort t0 = MulMod(k5, x1);
                ushort x2 = (ushort)(p2 ^ p4);
                ushort x = AddMod(t0, x2);
                ushort t1 = MulMod(k6, x);
                ushort t2 = AddMod(t0, t1);

                p1 ^= t1;
                p4 ^= t2;
                ushort temp = p2;
                p2 = (ushort)(p3 ^ t1);
                p3 = (ushort)(temp ^ t2);
            }

            var (finalK1, finalK2, finalK3, finalK4, _, _) = _keys[8];
            p1 = MulMod(p1, finalK1);
            p2 = AddMod(p3, finalK2);
            p3 = AddMod(p2, finalK3);
            p4 = MulMod(p4, finalK4);

            return ((ulong)p1 << 48) | ((ulong)p2 << 32) | ((ulong)p3 << 16) | p4;
        }

        public ulong Decrypt(ulong cipherText)
        {
            ushort p1 = (ushort)(cipherText >> 48);
            ushort p2 = (ushort)(cipherText >> 32);
            ushort p3 = (ushort)(cipherText >> 16);
            ushort p4 = (ushort)cipherText;

            for (int i = 8; i > 0; i--)
            {
                var (k1, k2, k3, k4, k5, k6) = _keys[i];

                ushort invK1 = MulInv(k1);
                ushort invK4 = MulInv(k4);
                ushort invK2 = AddInv(k2);
                ushort invK3 = AddInv(k3);

                ushort x1 = (ushort)(p1 ^ p3);
                ushort t0 = MulMod(k5, x1);
                ushort x2 = (ushort)(p2 ^ p4);
                ushort x = AddMod(t0, x2);
                ushort t1 = MulMod(k6, x);
                ushort t2 = AddMod(t0, t1);

                p1 ^= t1;
                p4 ^= t2;
                ushort temp = p2;
                p2 = (ushort)(p3 ^ t1);
                p3 = (ushort)(temp ^ t2);
            }

            var (finalK1, finalK2, finalK3, finalK4, _, _) = _keys[0];
            p1 = MulMod(p1, MulInv(finalK1));
            p2 = AddMod(p3, AddInv(finalK2));
            p3 = AddMod(p2, AddInv(finalK3));
            p4 = MulMod(p4, MulInv(finalK4));

            return ((ulong)p1 << 48) | ((ulong)p2 << 32) | ((ulong)p3 << 16) | p4;
        }
    }
}
