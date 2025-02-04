﻿#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
using System;
using System.Diagnostics;
using Org.BouncyCastle.Utilities;

namespace Org.BouncyCastle.Crypto.Digests
{
    /// <summary>
    ///     Implementation of SHAKE based on following KeccakNISTInterface.c from http://keccak.noekeon.org/
    /// </summary>
    /// <remarks>
    ///     Following the naming conventions used in the C source code to enable easy review of the implementation.
    /// </remarks>
    public class ShakeDigest
        : KeccakDigest, IXof
    {
        public ShakeDigest()
            : this(128)
        {
        }

        public ShakeDigest(int bitLength)
            : base(CheckBitLength(bitLength))
        {
        }

        public ShakeDigest(ShakeDigest source)
            : base(source)
        {
        }

        public override string AlgorithmName => "SHAKE" + fixedOutputLength;

        public override int DoFinal(byte[] output, int outOff)
        {
            return DoFinal(output, outOff, GetDigestSize());
        }

        public virtual int DoFinal(byte[] output, int outOff, int outLen)
        {
            DoOutput(output, outOff, outLen);

            Reset();

            return outLen;
        }

        public virtual int DoOutput(byte[] output, int outOff, int outLen)
        {
            if (!squeezing) Absorb(new byte[] { 0x0F }, 0, 4);

            Squeeze(output, outOff, (long)outLen * 8);

            return outLen;
        }

        private static int CheckBitLength(int bitLength)
        {
            switch (bitLength)
            {
                case 128:
                case 256:
                    return bitLength;
                default:
                    throw new ArgumentException(bitLength + " not supported for SHAKE", "bitLength");
            }
        }

        /*
         * TODO Possible API change to support partial-byte suffixes.
         */
        protected override int DoFinal(byte[] output, int outOff, byte partialByte, int partialBits)
        {
            return DoFinal(output, outOff, GetDigestSize(), partialByte, partialBits);
        }

        /*
         * TODO Possible API change to support partial-byte suffixes.
         */
        protected virtual int DoFinal(byte[] output, int outOff, int outLen, byte partialByte, int partialBits)
        {
            if (partialBits < 0 || partialBits > 7)
                throw new ArgumentException("must be in the range [0,7]", "partialBits");

            var finalInput = (partialByte & ((1 << partialBits) - 1)) | (0x0F << partialBits);
            Debug.Assert(finalInput >= 0);
            var finalBits = partialBits + 4;

            if (finalBits >= 8)
            {
                oneByte[0] = (byte)finalInput;
                Absorb(oneByte, 0, 8);
                finalBits -= 8;
                finalInput >>= 8;
            }

            if (finalBits > 0)
            {
                oneByte[0] = (byte)finalInput;
                Absorb(oneByte, 0, finalBits);
            }

            Squeeze(output, outOff, (long)outLen * 8);

            Reset();

            return outLen;
        }

        public override IMemoable Copy()
        {
            return new ShakeDigest(this);
        }
    }
}
#endif