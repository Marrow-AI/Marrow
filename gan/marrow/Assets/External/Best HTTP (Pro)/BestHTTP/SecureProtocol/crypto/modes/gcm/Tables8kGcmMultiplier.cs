#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)

using System;

using Org.BouncyCastle.Crypto.Utilities;
using Org.BouncyCastle.Utilities;

namespace Org.BouncyCastle.Crypto.Modes.Gcm
{
    public sealed class Tables8kGcmMultiplier
        : IGcmMultiplier
    {
        private byte[] H;
        private uint[][][] M;

        public void Init(byte[] H)
        {
            if (M == null)
            {
                M = new uint[32][][];
            }
            else if (Arrays.AreEqual(this.H, H))
            {
                return;
            }

            this.H = Arrays.Clone(H);

            M[0] = new uint[16][];
            M[1] = new uint[16][];
            M[0][0] = new uint[4];
            M[1][0] = new uint[4];
            M[1][8] = GcmUtilities.AsUints(H);

            for (int j = 4; j >= 1; j >>= 1)
            {
                uint[] tmp = (uint[])M[1][j + j].Clone();
                GcmUtilities.MultiplyP(tmp);
                M[1][j] = tmp;
            }

            {
                uint[] tmp = (uint[])M[1][1].Clone();
                GcmUtilities.MultiplyP(tmp);
                M[0][8] = tmp;
            }

            for (int j = 4; j >= 1; j >>= 1)
            {
                uint[] tmp = (uint[])M[0][j + j].Clone();
                GcmUtilities.MultiplyP(tmp);
                M[0][j] = tmp;
            }

            for (int i = 0; ; )
            {
                for (int j = 2; j < 16; j += j)
                {
                    for (int k = 1; k < j; ++k)
                    {
                        uint[] tmp = (uint[])M[i][j].Clone();
                        GcmUtilities.Xor(tmp, M[i][k]);
                        M[i][j + k] = tmp;
                    }
                }

                if (++i == 32) return;

                if (i > 1)
                {
                    M[i] = new uint[16][];
                    M[i][0] = new uint[4];
                    for (int j = 8; j > 0; j >>= 1)
                    {
                        uint[] tmp = (uint[])M[i - 2][j].Clone();
                        GcmUtilities.MultiplyP8(tmp);
                        M[i][j] = tmp;
                    }
                }
            }
        }

#if true //!ENABLE_IL2CPP || UNITY_WEBGL
        public void MultiplyH(byte[] x)
        {
            uint[] z = new uint[4];
            for (int i = 15; i >= 0; --i)
            {
                //GcmUtilities.Xor(z, M[i + i][x[i] & 0x0f]);
                uint[] m = M[i + i][x[i] & 0x0f];
                z[0] ^= m[0];
                z[1] ^= m[1];
                z[2] ^= m[2];
                z[3] ^= m[3];
                //GcmUtilities.Xor(z, M[i + i + 1][(x[i] & 0xf0) >> 4]);
                m = M[i + i + 1][(x[i] & 0xf0) >> 4];
                z[0] ^= m[0];
                z[1] ^= m[1];
                z[2] ^= m[2];
                z[3] ^= m[3];
            }

            Pack.UInt32_To_BE(z, x, 0);
        }
#else
        uint[] z = new uint[4];
        public unsafe void MultiplyH(byte[] x)
        {
            //Array.Clear(z, 0, z.Length);

            fixed (byte* px = x)
            fixed (uint* pz = z)
            {
                pz[0] = pz[1] = pz[2] = pz[3] = 0;

                for (int i = 15; i >= 0; --i)
                {
                    //GcmUtilities.Xor(z, M[i + i][x[i] & 0x0f]);
                    //uint[] m = M[i + i][x[i] & 0x0f];
                    //z[0] ^= m[0];
                    //z[1] ^= m[1];
                    //z[2] ^= m[2];
                    //z[3] ^= m[3];

                    fixed (uint* pm = M[i + i][px[i] & 0x0f])
                    {
                        pz[0] ^= pm[0];
                        pz[1] ^= pm[1];
                        pz[2] ^= pm[2];
                        pz[3] ^= pm[3];
                    }

                    //GcmUtilities.Xor(z, M[i + i + 1][(x[i] & 0xf0) >> 4]);
                    //m = M[i + i + 1][(x[i] & 0xf0) >> 4];
                    //z[0] ^= m[0];
                    //z[1] ^= m[1];
                    //z[2] ^= m[2];
                    //z[3] ^= m[3];

                    fixed (uint* pm = M[i + i + 1][(px[i] & 0xf0) >> 4])
                    {
                        pz[0] ^= pm[0];
                        pz[1] ^= pm[1];
                        pz[2] ^= pm[2];
                        pz[3] ^= pm[3];
                    }
                }

                //int off = 0;
                //for (int i = 0; i < 4; ++i)
                //{
                //    uint n = pz[i];
                //
                //    px[off + 0] = (byte)(n >> 24);
                //    px[off + 1] = (byte)(n >> 16);
                //    px[off + 2] = (byte)(n >> 8);
                //    px[off + 3] = (byte)(n);
                //
                //    off += 4;
                //}
                // i = 0
                uint n = pz[0];
                
                px[0] = (byte)(n >> 24);
                px[1] = (byte)(n >> 16);
                px[2] = (byte)(n >> 8);
                px[3] = (byte)(n);

                // i = 1
                n = pz[1];

                px[4] = (byte)(n >> 24);
                px[5] = (byte)(n >> 16);
                px[6] = (byte)(n >> 8);
                px[7] = (byte)(n);

                // i = 2
                n = pz[2];

                px[8] = (byte)(n >> 24);
                px[9] = (byte)(n >> 16);
                px[10] = (byte)(n >> 8);
                px[11] = (byte)(n);

                // i = 3
                n = pz[3];

                px[12] = (byte)(n >> 24);
                px[13] = (byte)(n >> 16);
                px[14] = (byte)(n >> 8);
                px[15] = (byte)(n);
            }

            //Pack.UInt32_To_BE(z, x, 0);
        }
#endif
    }
}

#endif
