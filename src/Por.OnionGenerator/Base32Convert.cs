using System;
using System.IO;
using System.Text;

namespace Por.OnionGenerator
{
	static class Base32Convert
	{
		private const string base32chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
		private static readonly byte[] boundaries = System.Text.ASCIIEncoding.Default.GetBytes("=27AZaz");
		private const int PADDING_BOUND = 0;
		private const int DIGIT_LOW_BOUND = 1;
		private const int DIGIT_HIGH_BOUND = 2;
		private const int CAPITAL_LOW_BOUND = 3;
		private const int CAPITAL_HIGH_BOUND = 4;
		private const int LETTER_LOW_BOUND = 5;
		private const int LETTER_HIGH_BOUND = 6;
		private const byte VALUE_OF_2 = 26;
		private const byte INVALID_CHAR = 255;
		private static readonly byte PADDING_CHAR = boundaries[0];

		public static string ToString(byte[] plain)
		{
			short buffer = 0;
			int hi = 0;
			int currentByte = 0;
			StringBuilder sb = new StringBuilder();
			byte index = 0;
			
			Action chomp = () => {
				index = (byte) (buffer >> (hi - 5));
				sb.Append(base32chars[index]);
				buffer = (short) (buffer ^ index << (hi - 5));
				hi -= 5;
			};
			
			while (currentByte < plain.Length || hi > 5)
			{
				if (hi < 5)
				{
					buffer = (short) (buffer << 8 | plain[currentByte++]);
					hi += 8;
				}
				chomp();
			}
			
			if (hi > 0)
			{
				index = (byte) (buffer << (5 - hi));
				sb.Append(base32chars[index]);
			}
			
			switch (hi)
			{
			case 1:
				sb.Append("====");
				break;
			case 2:
				sb.Append("=");
				break;
			case 3:
				sb.Append("===");
				break;
			case 4:
				sb.Append("======");
				break;
			}
			
			return sb.ToString();
		}
		
		public static byte[] ToBytes(string enc)
		{
			short buffer = 0;
			int hi = 0;
			int currentChar = 0;
			byte[] encBuf = ASCIIEncoding.Default.GetBytes(enc);
			
			Func<byte,byte> normalize = (byte b) => {
				if (b == boundaries[PADDING_BOUND])
					return PADDING_CHAR;
				
				if (b  < boundaries[DIGIT_LOW_BOUND])
					return INVALID_CHAR;
				if (b <= boundaries[DIGIT_HIGH_BOUND])
					return (byte) (b - boundaries[DIGIT_LOW_BOUND] + VALUE_OF_2);
				if (b  < boundaries[CAPITAL_LOW_BOUND])
					return INVALID_CHAR;
				if (b <= boundaries[CAPITAL_HIGH_BOUND])
					return (byte) (b - boundaries[CAPITAL_LOW_BOUND]);
				if (b  < boundaries[LETTER_LOW_BOUND])
					return INVALID_CHAR;
				if (b <= boundaries[LETTER_HIGH_BOUND])
					return (byte) (b - boundaries[LETTER_LOW_BOUND]);
				return INVALID_CHAR;
			};
			
			using (MemoryStream decBuf = new MemoryStream(enc.Length * 5 / 8 + 1))
			{
				while (currentChar < encBuf.Length)
				{
					byte temp = normalize(encBuf[currentChar++]);
					if (temp == INVALID_CHAR)
						continue;
					if (temp == PADDING_CHAR)
						break;
					buffer = (short) (buffer << 5 | temp);
					hi += 5;
					if (hi > 8)
					{
						byte o = (byte) (buffer >> (hi - 8));
						decBuf.WriteByte(o);
						buffer = (short) (buffer ^ o << (hi - 8));
						hi -= 8;
					}
				}

				if (hi > 0)
				{
					buffer = (short) (buffer << (8 - hi));
					decBuf.WriteByte((byte) buffer);
				}

				return decBuf.ToArray();
			}
		}
	}
}
