using Nethereum.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using XSystem.Security.Cryptography;

namespace WalletAddressValidator;

public static class AddressValidator
{
    public static bool IsValidEthAddress(string address)
    {
        if (AddressUtil.Current.IsValidAddressLength(address) &&
            AddressUtil.Current.IsValidEthereumAddressHexFormat(address) &&
            (AddressUtil.Current.IsChecksumAddress(address) || 
            address.Equals(address.ToLower()) ||
            address.Substring(2).Equals(address.Substring(2).ToUpper())))
        {
            return true;
        }

        return false;
    }

    // Bech32 human readable parts
    private const string BitcoinBech32MainnetHrp = "bc1";

    // Base58 prefixes
    private static readonly int[] BitcoinBase58MainnetPrefixes = { 0, 5 };

    public static bool IsValidBtcAddress(string address)
    {
        if (string.IsNullOrEmpty(address) || string.IsNullOrWhiteSpace(address))
        {
            return false;
        }

        if (address.ToLower().StartsWith(BitcoinBech32MainnetHrp))
        {
            return ValidateBech32Address(address);
        }

        return ValidateBase58Address(address, BitcoinBase58MainnetPrefixes);
    }

    private static bool ValidateBech32Address(string address)
    {
        return Bech32.Decode(address) is not null;
    }

    private static bool ValidateBase58Address(string address, int[] prefixes)
    {
        return Base58.Decode(address, prefixes) is not null;
    }

    private static class Bech32
    {
        private static readonly uint[] PolymodGenerator = { 0x3b6a57b2U, 0x26508e6dU, 0x1ea119faU, 0x3d4233ddU, 0x2a1462b3U };

        internal static byte[]? Decode(string address)
        {
            if (address.Any(char.IsUpper) && address.Any(char.IsLower))
            {
                return null;
            }

            var base32Array = ToBase32ArrayCheckAndStripChecksum(address.ToLower());

            if (base32Array is null)
            {
                return null;
            }

            return DecodeSegwitDataPart(base32Array);
        }

        private static byte[]? DecodeSegwitDataPart(byte[] base32Array)
        {
            if (base32Array[0] > 16)
            {
                return null;
            }

            var bitArraySource = new BitArray(base32Array.SubArray(1, base32Array.Length - 1));
            var destinationBitCountWithZeroBits = (bitArraySource.Length / 8 + (bitArraySource.Length % 8 == 0 ? 0 : 1)) * 5;
            var destinationBitCountWithOutZeroBits = destinationBitCountWithZeroBits / 8 * 8;
            
            if (destinationBitCountWithOutZeroBits - destinationBitCountWithZeroBits > 4)
            {
                return null;
            }

            for (var i = 0; i < destinationBitCountWithOutZeroBits - destinationBitCountWithZeroBits; i++)
            {
                if (bitArraySource[bitArraySource.Length - i])
                {
                    return null;
                }
            }

            var bitArrayDestination = new BitArray(destinationBitCountWithOutZeroBits);
            for (int i = 0, j = 0; i < bitArraySource.Length && j < destinationBitCountWithOutZeroBits; i++)
            {
                if (i % 8 < 3)
                {
                    continue;
                }

                bitArrayDestination[j] = bitArraySource[i];
                j++;
            }
            var byteArray = new byte[bitArrayDestination.Length / 8];
            bitArrayDestination.CopyTo(byteArray, 0);
            if (byteArray.Length < 2 || byteArray.Length > 40)
            {
                return null;
            }

            if (base32Array[0] == 0 && byteArray.Length != 20 && byteArray.Length != 32)
            {
                return null;
            }
            
            return byteArray.Prepend(base32Array[0]).ToArray();
        }

        private static byte[]? ToBase32ArrayCheckAndStripChecksum(string inputBech32)
        {
            var expandedHrp = ExpandHrp(inputBech32.Substring(0, inputBech32.LastIndexOf('1')));
            
            var data = ToBase32Array(inputBech32.Substring(inputBech32.LastIndexOf('1') + 1,
                inputBech32.Length - (inputBech32.LastIndexOf('1') + 1)));

            if (data is null)
            {
                return null;
            }

            return Polymod(expandedHrp.Concat(data)) != 1
                ? null
                : data.SubArray(0, data.Length - 6);
        }

        private static uint Polymod(IEnumerable<byte> inputArray)
        {
            uint chk = 1;
            foreach (var value in inputArray)
            {
                var top = chk >> 25;
                chk = (value ^ ((chk & 0x1ffffff) << 5));
                chk = Enumerable
                    .Range(0, 5)
                    .Aggregate(chk, (current, i) => current ^ (((top >> i) & 1) == 1 ? PolymodGenerator[i] : 0));
            }
            return chk;
        }

        private static byte[]? ToBase32Array(string inputBase32)
        {
            var outputArray = new byte[inputBase32.Length];
            const string alphabet = "qpzry9x8gf2tvdw0s3jn54khce6mua7l";

            for (var i = 0; i < inputBase32.Length; i++)
            {
                if (alphabet.Contains(inputBase32[i]))
                {
                    outputArray[i] = (byte)alphabet.IndexOf(inputBase32[i]);
                }
                else
                {
                    return null;
                }
            }

            return outputArray;
        }

        private static IEnumerable<byte> ExpandHrp(string inputHrp)
        {
            var hrpLength = inputHrp.Length;
            var expandedHrp = new byte[2 * hrpLength + 1];
            for (var i = 0; i < hrpLength; i++)
            {
                expandedHrp[i] = (byte)(inputHrp[i] >> 5);
                expandedHrp[i + hrpLength + 1] = (byte)(inputHrp[i] & 31);
            }
            return expandedHrp;
        }
    }

    private static class Base58
    {
        internal static byte[]? Decode(string address, int[] prefixes)
        {
            if (string.IsNullOrEmpty(address) || prefixes is null || prefixes.Length.Equals(0))
            {
                return null;
            }
            
            var decoded = ToByteArrayCheckAndStripChecksum(address);
            
            if (decoded is null || !decoded.Length.Equals(21) || !prefixes.Contains(decoded[0]))
            {
                return null;
            }
            
            return decoded;
        }

        private static byte[]? ToByteArrayCheckAndStripChecksum(string inputBase58)
        {
            var inputArray = ToByteArray(inputBase58);
            
            if (inputArray is null || inputArray.Length < 4)
            {
                return null;
            }

            var hasher = new SHA256Managed();
            var hash = hasher.ComputeHash(inputArray.SubArray(0, inputArray.Length - 4));
            hash = hasher.ComputeHash(hash);

            if (!inputArray.SubArray(21, 4).SequenceEqual(hash.SubArray(0, 4)))
            {
                return null;
            }

            return inputArray.SubArray(0, inputArray.Length - 4);
        }

        private static byte[]? ToByteArray(string inputBase58)
        {
            var outputValue = new BigInteger(0);
            const string alphabet = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
            foreach (var character in inputBase58)
            {
                if (alphabet.Contains(character))
                {
                    outputValue = BigInteger.Add(new BigInteger(alphabet.IndexOf(character)),
                        BigInteger.Multiply(outputValue, new BigInteger(58)));
                }
                else
                {
                    return null;
                }
            }

            var outputArray = outputValue.ToByteArray(true, true);
            
            foreach (var character in inputBase58)
            {
                if (character != '1') break;
                var extendedArray = new byte[outputArray.Length + 1];
                Array.Copy(outputArray, 0, extendedArray, 1, outputArray.Length);
                outputArray = extendedArray;
            }
            return outputArray;
        }
    }
}

internal static class ArrayExtensions
{
    public static T[] SubArray<T>(this T[] data, int index, int length)
    {
        var result = new T[length];
        Array.Copy(data, index, result, 0, length);
        return result;
    }
}
