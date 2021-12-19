using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace TQ
{
    public static class CompressionAlgorithms
    {
        public static List<byte> RLE_Encode_TI(byte[] raw_data)
        {
            List<byte> encoded = new List<byte>();
            byte D = (byte)'-';
            encoded.Add(D);
            int idx = 0;
            byte B = 0;
            byte C = 0;
            int cnt = 0;
            
            while (idx < raw_data.Length)
            {
                B = raw_data[idx++];
                cnt = 1;
                if (B == D)
                {
                    for (int i = 2; i <= 3 && idx < raw_data.Length; ++i)
                    {
                        C = raw_data[idx++];
                        if (C == D)
                        {
                            cnt++;
                        }
                        else
                        {
                            idx--;
                            break;
                        }
                    }
                    encoded.Add(D);
                    encoded.Add((byte)cnt);
                    cnt = 0;
                }
                else
                {
                    while (idx < raw_data.Length)
                    {
                        C = raw_data[idx++];
                        if (B == C)
                        {
                            cnt++;
                        }
                        else
                        {
                            idx--;
                            break;
                        }
                    }
                    if (cnt <= 3)
                    {
                        for (int i = 0; i < cnt; ++i)
                            encoded.Add(B);
                    }
                    else if (cnt < (1 << 8))
                    {
                        encoded.Add(D);
                        encoded.Add((byte)cnt);
                        encoded.Add(B);
                    }
                    else if (cnt < (1 << 16))
                    {
                        encoded.Add(D);
                        encoded.Add(0);
                        encoded.Add((byte)cnt);
                        encoded.Add(B);
                    }
                    else if (cnt < (1 << 24))
                    {
                        encoded.Add(D);
                        encoded.Add(0);
                        encoded.Add(0);
                        encoded.Add((byte)cnt);
                        encoded.Add(B);
                    }
                    else
                    {
                        return null;
                    }
                    cnt = 0;
                }
            }

            // end of data
            encoded.Add(D);
            encoded.Add(0);
            encoded.Add(0);
            encoded.Add(0);

            return encoded;
        }

        public static List<byte> RLE_Decode_TI(byte[] encoded)
        {
            List<byte> decoded = new List<byte>();
            int idx = 0;
            byte D = encoded[idx++];
            while (true)
            {
                byte B = 0;
                while (true)
                {
                    B = encoded[idx++];
                    if (B != D)
                    {
                        decoded.Add(B);
                    }
                    else
                    {
                        break;
                    }
                }

                int L = 0;
                L = encoded[idx++];
                if (L == 0)
                {
                    L = encoded[idx++];
                    if (L == 0)
                    {
                        L = encoded[idx++];
                        if (L == 0)
                        {
                            return decoded;
                        }
                        else
                        {
                            int L1 = encoded[idx++];
                            int L2 = encoded[idx++];
                            L = L << 16 | L1 << 8 | L2;
                        }
                    }
                    else
                    {
                        int L1 = encoded[idx++];
                        L = L << 8 | L1;
                    }
                }
                else if (0 < L && L < 4)
                {
                    while (L-- > 0)
                    {
                        decoded.Add(D);
                    }
                }
                else
                {
                    byte C = encoded[idx++];
                    while (L-- > 0)
                    {
                        decoded.Add(C);
                    }
                }
            }

            return null;
        }

        public static byte[] RLE_Encode_1Byte(byte[] raw_data)
        {
            List<byte> res = new List<byte>();

            bool emp = true;
            byte val = 0;
            int cnt = 0;
            for (int i = 0; i < raw_data.Length * 2; ++i)
            {
                bool is_odd = (i % 2 == 1);
                byte c_val = 0;
                if (!is_odd)
                    c_val = (byte)(raw_data[i / 2] >> 4);
                else
                    c_val = (byte)(raw_data[i / 2] & 0x0F);

                if (emp)
                {
                    val = c_val;
                    cnt = 1;
                    emp = false;
                }
                else
                {
                    if (cnt < 16 && c_val == val)
                    {
                        cnt++;
                    }
                    else
                    {
                        res.Add((byte)((val << 4) | (cnt - 1)));
                        emp = true;
                        val = 0;
                        cnt = 0;
                        i--;
                    }
                }
            }

            if (!emp)
                res.Add((byte)((val << 4) | (cnt - 1)));

            return res.ToArray();
        }

        public static byte[] RLE_Decode_1Byte(byte[] data)
        {
            List<byte> res = new List<byte>();

            int idx = 0;
            for (int i = 0; i < data.Length; ++i)
            {
                byte value = (byte)(data[i] >> 4);
                int times = (data[i] & 0x0F) + 1;
                for (int t = 0; t < times; ++t)
                {
                    if (idx % 2 == 1)
                    {
                        res[res.Count - 1] = (byte)((res[res.Count - 1] << 4) | value);
                    }
                    else
                    {
                        res.Add(value);
                    }
                    idx++;
                }
            }

            return res.ToArray();
        }
    }
}
