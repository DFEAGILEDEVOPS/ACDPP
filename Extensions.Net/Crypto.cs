using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Extensions
{
    public static class Crypto
    {
        private static byte[] GenerateRandomBytes(int length)
        {
            var buffer = new byte[length];
            var rng = new RNGCryptoServiceProvider();
            rng.GetBytes(buffer);
            return buffer;
        }

        public static Guid GetMD5Guid(this string text)
        {
            var checksumData = Encoding.UTF8.GetBytes(text);
            var hash = MD5.Create().ComputeHash(checksumData);
            return new Guid(hash);
        }

        public static string GetMD5Checksum(this string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            var checksumData = Encoding.UTF8.GetBytes(text);
            var hash = MD5.Create().ComputeHash(checksumData);
            var calculatedChecksum = Convert.ToBase64String(hash);
            return calculatedChecksum;
        }

        public static byte[] GetMD5hash(this string text)
        {
            var checksumData = Encoding.UTF8.GetBytes(text);
            return MD5.Create().ComputeHash(checksumData);
        }

        public static string GetSHA512Checksum(this string text, bool base64encode = true)
        {
            var checksumData = Encoding.UTF8.GetBytes(text);
            var hash = SHA512.Create().ComputeHash(checksumData);
            var calculatedChecksum = base64encode ? Convert.ToBase64String(hash) : Encoding.UTF8.GetString(hash);
            return calculatedChecksum;
        }

        public static byte[] GetSHA512hash(this string text)
        {
            var checksumData = Encoding.UTF8.GetBytes(text);
            return SHA512.Create().ComputeHash(checksumData);
        }

        public static string GetSHA256Checksum(this string text)
        {
            var checksumData = Encoding.UTF8.GetBytes(text);
            var hash = SHA256.Create().ComputeHash(checksumData);
            var calculatedChecksum = Convert.ToBase64String(hash);
            return calculatedChecksum;
        }

        public static byte[] GetSHA256hash(this string text)
        {
            var checksumData = Encoding.UTF8.GetBytes(text);
            return SHA256.Create().ComputeHash(checksumData);
        }

        public static string GetSHA512Checksum(this byte[] checksumData)
        {
            var hash = SHA512.Create().ComputeHash(checksumData);
            var calculatedChecksum = Convert.ToBase64String(hash);
            return calculatedChecksum;
        }

        public static string GeneratePassword(int bitLength = 256)
        {
            return GeneratePassword(Text.AlphaNumericChars.ToArray(), bitLength);
        }

        public static string GeneratePassword(char[] charset, int bitLength)
        {
            //Ensure characters are distict and mixed up
            charset = charset.Distinct().ToList().Randomise().ToArray();

            //Taken from wikipedia https://en.wikipedia.org/wiki/Password_strength
            var H = bitLength; //Entropy per symbol H
            double N = charset.Length; //Symbol count

            var passwordLength = (int) Math.Ceiling(H/N.Log2());

            var chars = new char[passwordLength];

            //Generate a load of random numbers
            var randomData = new byte[chars.Length];
            using (var generator = new RNGCryptoServiceProvider())
            {
                generator.GetBytes(randomData);
            }

            //use the randome number to pick from the character set
            Parallel.For(0, chars.Length, i => { chars[i] = charset[randomData[i]%charset.Length]; });

            return new string(chars);
        }

        public static string GeneratePasscode(int passcodeLength = 8)
        {
            return GeneratePassword(Text.AlphaNumericChars.ToArray(), passcodeLength);
        }

        public static string GeneratePasscode(char[] charset, int passcodeLength)
        {
            //Ensure characters are distict and mixed up
            charset = charset.Distinct().ToList().Randomise().ToArray();

            var chars = new char[passcodeLength];

            //Generate a load of random numbers
            var randomData = new byte[chars.Length];
            using (var generator = new RNGCryptoServiceProvider())
            {
                generator.GetBytes(randomData);
            }

            //use the randome number to pick from the character set
            Parallel.For(0, chars.Length, i => { chars[i] = charset[randomData[i]%charset.Length]; });

            return new string(chars);
        }

        /// <summary>
        ///     Encodes specified data with bas64 encoding.
        /// </summary>
        /// <param name="data">Data to to encode.</param>
        /// <param name="base64Chars">Custom base64 chars (64 chars) or null if default chars used.</param>
        /// <param name="padd">
        ///     Padd missing block chars. Normal base64 must be 4 bytes blocks, if not 4 bytes in block,
        ///     missing bytes must be padded with '='. Modified base64 just skips missing bytes.
        /// </param>
        /// <returns></returns>
        public static byte[] Base64Encode(this byte[] data, char[] base64Chars, bool padd)
        {
            /* RFC 2045 6.8.  Base64 Content-Transfer-Encoding
			
				Base64 is processed from left to right by 4 6-bit byte block, 4 6-bit byte block 
				are converted to 3 8-bit bytes.
				If base64 4 byte block doesn't have 3 8-bit bytes, missing bytes are marked with =. 
				
			
				Value Encoding  Value Encoding  Value Encoding  Value Encoding
					0 A            17 R            34 i            51 z
					1 B            18 S            35 j            52 0
					2 C            19 T            36 k            53 1
					3 D            20 U            37 l            54 2
					4 E            21 V            38 m            55 3
					5 F            22 W            39 n            56 4
					6 G            23 X            40 o            57 5
					7 H            24 Y            41 p            58 6
					8 I            25 Z            42 q            59 7
					9 J            26 a            43 r            60 8
					10 K           27 b            44 s            61 9
					11 L           28 c            45 t            62 +
					12 M           29 d            46 u            63 /
					13 N           30 e            47 v
					14 O           31 f            48 w         (pad) =
					15 P           32 g            49 x
					16 Q           33 h            50 y
					
				NOTE: 4 base64 6-bit bytes = 3 8-bit bytes				
					// |    6-bit    |    6-bit    |    6-bit    |    6-bit    |
					// | 1 2 3 4 5 6 | 1 2 3 4 5 6 | 1 2 3 4 5 6 | 1 2 3 4 5 6 |
					// |    8-bit         |    8-bit        |    8-bit         |
			*/

            if (base64Chars != null && base64Chars.Length != 64)
            {
                throw new Exception("There must be 64 chars in base64Chars char array !");
            }

            if (base64Chars == null)
            {
                base64Chars = new[]
                {
                    'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z',
                    'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',
                    '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '+', '/'
                };
            }

            // Convert chars to bytes
            var base64LoockUpTable = new byte[64];
            for (var i = 0; i < 64; i++)
            {
                base64LoockUpTable[i] = (byte) base64Chars[i];
            }

            var encodedDataLength = (int) Math.Ceiling(data.Length*8/(double) 6);
            // Retrun value won't be interegral 4 block, but has less. Padding requested, padd missing with '='
            if (padd && (encodedDataLength/(double) 4 != Math.Ceiling(encodedDataLength/(double) 4)))
            {
                encodedDataLength += (int) (Math.Ceiling(encodedDataLength/(double) 4)*4) - encodedDataLength;
            }

            // See how many line brakes we need
            var numberOfLineBreaks = 0;
            if (encodedDataLength > 76)
            {
                numberOfLineBreaks = (int) Math.Ceiling(encodedDataLength/(double) 76) - 1;
            }

            // Construc return valu buffer
            var retVal = new byte[encodedDataLength + numberOfLineBreaks*2]; // * 2 - CRLF

            var lineBytes = 0;
            // Loop all 3 bye blocks
            var position = 0;
            for (var i = 0; i < data.Length; i += 3)
            {
                // Do line splitting
                if (lineBytes >= 76)
                {
                    retVal[position + 0] = (byte) '\r';
                    retVal[position + 1] = (byte) '\n';
                    position += 2;
                    lineBytes = 0;
                }

                // Full 3 bytes data block
                if (data.Length - i >= 3)
                {
                    retVal[position + 0] = base64LoockUpTable[data[i + 0] >> 2];
                    retVal[position + 1] = base64LoockUpTable[(data[i + 0] & 0x3) << 4 | data[i + 1] >> 4];
                    retVal[position + 2] = base64LoockUpTable[(data[i + 1] & 0xF) << 2 | data[i + 2] >> 6];
                    retVal[position + 3] = base64LoockUpTable[data[i + 2] & 0x3F];
                    position += 4;
                    lineBytes += 4;
                }
                // 2 bytes data block, left (last block)
                else if (data.Length - i == 2)
                {
                    retVal[position + 0] = base64LoockUpTable[data[i + 0] >> 2];
                    retVal[position + 1] = base64LoockUpTable[(data[i + 0] & 0x3) << 4 | data[i + 1] >> 4];
                    retVal[position + 2] = base64LoockUpTable[(data[i + 1] & 0xF) << 2];
                    if (padd)
                    {
                        retVal[position + 3] = (byte) '=';
                    }
                }
                // 1 bytes data block, left (last block)
                else if (data.Length - i == 1)
                {
                    retVal[position + 0] = base64LoockUpTable[data[i + 0] >> 2];
                    retVal[position + 1] = base64LoockUpTable[(data[i + 0] & 0x3) << 4];
                    if (padd)
                    {
                        retVal[position + 2] = (byte) '=';
                        retVal[position + 3] = (byte) '=';
                    }
                }
            }

            return retVal;
        }

        /// <summary>
        ///     Decodes base64 data. Defined in RFC 2045 6.8.  Base64 Content-Transfer-Encoding.
        /// </summary>
        /// <param name="base64Data">Base64 decoded data.</param>
        /// <param name="base64Chars">Custom base64 chars (64 chars) or null if default chars used.</param>
        /// <returns></returns>
        public static byte[] Base64Decode(this byte[] base64Data, char[] base64Chars = null)
        {
            /* RFC 2045 6.8.  Base64 Content-Transfer-Encoding
			
				Base64 is processed from left to right by 4 6-bit byte block, 4 6-bit byte block 
				are converted to 3 8-bit bytes.
				If base64 4 byte block doesn't have 3 8-bit bytes, missing bytes are marked with =. 
				
			
				Value Encoding  Value Encoding  Value Encoding  Value Encoding
					0 A            17 R            34 i            51 z
					1 B            18 S            35 j            52 0
					2 C            19 T            36 k            53 1
					3 D            20 U            37 l            54 2
					4 E            21 V            38 m            55 3
					5 F            22 W            39 n            56 4
					6 G            23 X            40 o            57 5
					7 H            24 Y            41 p            58 6
					8 I            25 Z            42 q            59 7
					9 J            26 a            43 r            60 8
					10 K           27 b            44 s            61 9
					11 L           28 c            45 t            62 +
					12 M           29 d            46 u            63 /
					13 N           30 e            47 v
					14 O           31 f            48 w         (pad) =
					15 P           32 g            49 x
					16 Q           33 h            50 y
					
				NOTE: 4 base64 6-bit bytes = 3 8-bit bytes				
					// |    6-bit    |    6-bit    |    6-bit    |    6-bit    |
					// | 1 2 3 4 5 6 | 1 2 3 4 5 6 | 1 2 3 4 5 6 | 1 2 3 4 5 6 |
					// |    8-bit         |    8-bit        |    8-bit         |
			*/

            if (base64Chars != null && base64Chars.Length != 64)
            {
                throw new Exception("There must be 64 chars in base64Chars char array !");
            }

            if (base64Chars == null)
            {
                base64Chars = new[]
                {
                    'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z',
                    'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',
                    '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '+', '/'
                };
            }

            //--- Create decode table ---------------------//
            var decodeTable = new byte[128];
            for (var i = 0; i < 128; i++)
            {
                var mappingIndex = -1;
                for (var bc = 0; bc < base64Chars.Length; bc++)
                {
                    if (i == base64Chars[bc])
                    {
                        mappingIndex = bc;
                        break;
                    }
                }

                if (mappingIndex > -1)
                {
                    decodeTable[i] = (byte) mappingIndex;
                }
                else
                {
                    decodeTable[i] = 0xFF;
                }
            }
            //---------------------------------------------//

            var decodedDataBuffer = new byte[base64Data.Length*6/8 + 4];
            var decodedBytesCount = 0;
            var nByteInBase64Block = 0;
            var decodedBlock = new byte[3];
            var base64Block = new byte[4];

            for (var i = 0; i < base64Data.Length; i++)
            {
                var b = base64Data[i];

                // Read 4 byte base64 block and process it 			
                // Any characters outside of the base64 alphabet are to be ignored in base64-encoded data.

                // Padding char
                if (b == '=')
                {
                    base64Block[nByteInBase64Block] = 0xFF;
                }
                else
                {
                    var decodeByte = decodeTable[b & 0x7F];
                    if (decodeByte != 0xFF)
                    {
                        base64Block[nByteInBase64Block] = decodeByte;
                        nByteInBase64Block++;
                    }
                }

                /* Check if we can decode some bytes. 
                 * We must have full 4 byte base64 block or reached at the end of data.
                 */
                var encodedBytesCount = -1;
                // We have full 4 byte base64 block
                if (nByteInBase64Block == 4)
                {
                    encodedBytesCount = 3;
                }
                // We have reached at the end of base64 data, there may be some bytes left
                else if (i == base64Data.Length - 1)
                {
                    // Invalid value, we can't have only 6 bit, just skip 
                    if (nByteInBase64Block == 1)
                    {
                        encodedBytesCount = 0;
                    }
                    // There is 1 byte in two base64 bytes (6 + 2 bit)
                    else if (nByteInBase64Block == 2)
                    {
                        encodedBytesCount = 1;
                    }
                    // There are 2 bytes in two base64 bytes ([6 + 2],[4 + 4] bit)
                    else if (nByteInBase64Block == 3)
                    {
                        encodedBytesCount = 2;
                    }
                }

                // We have some bytes available to decode, decode them
                if (encodedBytesCount > -1)
                {
                    decodedDataBuffer[decodedBytesCount + 0] = (byte) (base64Block[0] << 2 | base64Block[1] >> 4);
                    decodedDataBuffer[decodedBytesCount + 1] = (byte) ((base64Block[1] & 0xF) << 4 | base64Block[2] >> 2);
                    decodedDataBuffer[decodedBytesCount + 2] = (byte) ((base64Block[2] & 0x3) << 6 | base64Block[3] >> 0);

                    // Increase decoded bytes count
                    decodedBytesCount += encodedBytesCount;

                    // Reset this block, reade next if there is any
                    nByteInBase64Block = 0;
                }
            }

            // There is some decoded bytes, construct return value
            if (decodedBytesCount > -1)
            {
                var retVal = new byte[decodedBytesCount];
                Array.Copy(decodedDataBuffer, 0, retVal, 0, decodedBytesCount);
                return retVal;
            }
                // There is no decoded bytes
            return new byte[0];
        }
    }
}