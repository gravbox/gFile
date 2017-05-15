using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gravitybox.gFileSystem.Service
{
    internal class Utilities
    {
        public static byte[] ConvertHexStringToByteArray(string hexString)
        {
            if (string.IsNullOrEmpty(hexString))
                return null;
            if (hexString.Length % 2 != 0)
                return null;
            try
            {
                byte[] hexAsBytes = new byte[hexString.Length / 2];
                for (int index = 0; index < hexAsBytes.Length; index++)
                {
                    string byteValue = hexString.Substring(index * 2, 2);
                    hexAsBytes[index] = byte.Parse(byteValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                }
                return hexAsBytes;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}
