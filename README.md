# text-encoding-detect
This is a C++ and C# library for detecting UTF8 and UTF16 text encoding. I recently had to upgrade the text file handling feature of [AutoIt](https://www.autoitscript.com/site/autoit/) to better handle text files where no [byte order mark](http://en.wikipedia.org/wiki/Byte_order_mark) (BOM) was present. The older version of code I was using worked fine for UTF-8 files (with or without BOM) but it wasn't able to detect UTF-16 files without a BOM. I tried to use the [IsTextUnicode](http://msdn.microsoft.com/en-us/library/windows/desktop/dd318672(v=vs.85).aspx) Win32 API function but this seemed extremely unreliable and wouldn't detect UTF-16 Big-Endian text in my tests. 

With UTF-16 detection, there is always an element of ambiguity. [This post](http://blogs.msdn.com/b/oldnewthing/archive/2007/04/17/2158334.aspx) by Raymond shows that however you try and detect encoding there will always be some sequence of bytes that will make your guesses look stupid. That said, here are the detection methods I'm currently using for the various types of text file. The order of the checks I perform are:

*   BOM
*   UTF-8
*   UTF-16 (newline)
*   UTF-16 (null distribution)

## BOM Detection 
I assume that if I find a BOM at the start of the file that it is valid. Although it's possible that the BOM could just be ANSI text, it's highly unlikely. The BOMs are as follows:

|Encoding              |BOM               |
|----------------------|------------------|
| UTF-8                | 0xEF, 0xBB, 0xBF |
| UTF-16 Little Endian | 0xFF, 0xFE       |
| UTF-16 Big Endian    | 0xFE, 0xFF       |


## UTF-8 Detection
UTF-8 checking is reliable with a very low chance of false positives, so this is done first. If the text is valid UTF-8 but all the characters are in the range **0-127** then this is essentially ASCII text and can be treated as such - in this case I don't continue to check for UTF-16. If a character is in the range of **0-127** then it is a single character and nothing more needs to be done. Values **above 127** indicate multibyte encoding using the next 1, 2 or 3 bytes.

|First Byte |Number of Bytes in Sequence|
|-----------|---------|
| 0-127     | 1 byte  |
| 194-223   | 2 bytes |
| 224-239   | 3 bytes |
| 240-244   | 4 bytes |

These additional bytes are in the range **128-191**. This scheme means that if we decode the text stream based on this method and no unexpected sequences occur then this is almost certainly UTF-8 text.   

## UTF-16 Detection 
UTF-16 text is generally made up of 2-byte sequences (technically, there can be a 4-byte sequence with surrogate pairs). Depending on the endianness of the file the unicode character 0x1234 could be represented in the character stream as "0x12 0x34" or "0x34 0x12".  The BOM is usually used to easily determine if the file is in big or little endian mode. Without a BOM this is a little more tricky to determine. I use two methods to try and determine if the text is UTF-16 and the endianness. The first is the newline characters 0x0a and 0x0d. Depending on the endianness they will be sequenced as "0x0a 0x00" or "0x00 0x0a". If every instance of these characters in a text file is encoded the same way then that is a good sign that the text is UTF-16 and if it is big or little endian. The drawback of this method is that it won't work for very small amounts of text, or files that don't contain newlines. The second method relies on the fact that many files may contain large amounts of pure ASCII text in the range 0-127. This applies especially to files generally used in IT like scripts and logs. When encoded in UTF-16 these are represented as the ASCII character and a null character. For example, space, 0x20 would be encoded as "0x00 0x20" or "0x20 0x00". Depending on the endianness this will result in a large amount of nulls in the odd or even byte positions. We just need to scan the file for these odd and even nulls and if there is a significant percentage in the expected position then we can assume the text is UTF-16.   

## The Library 
There are separate libraries for C++ and C#. The notes here are for the C# version. The two main public functions are:

	public Encoding CheckBOM(byte[] buffer, int size)
	public Encoding DetectEncoding(byte[] buffer, int size)

These functions return the Encoding which is the following enum:

	public enum Encoding
	{
		None,               // Unknown or binary
		ANSI,               // 0-255
		ASCII,              // 0-127
		UTF8_BOM,           // UTF8 with BOM
		UTF8_NOBOM,         // UTF8 without BOM
		UTF16_LE_BOM,       // UTF16 LE with BOM
		UTF16_LE_NOBOM,     // UTF16 LE without BOM
		UTF16_BE_BOM,       // UTF16-BE with BOM
		UTF16_BE_NOBOM      // UTF16-BE without BOM
	}

The DetectEncoding function takes a byte buffer and a size parameter. The larger the buffer that is used, the more accurate the result will be. I'd recommend at least 4KB. Here is an example of passing a buffer to the DetectEncoding function:

	// Detect encoding
	var textDetect = new TextEncodingDetect();
	TextEncodingDetect.Encoding encoding = textDetect.DetectEncoding(buffer, buffer.Length);
	
	Console.Write("Encoding: ");
	if (encoding == TextEncodingDetect.Encoding.None)
	{
		Console.WriteLine("Binary");
	}
	else if (encoding == TextEncodingDetect.Encoding.ASCII)
	{
		Console.WriteLine("ASCII (chars in the 0-127 range)");
	}
	else if (encoding == TextEncodingDetect.Encoding.ANSI)
	{
		Console.WriteLine("ANSI (chars in the range 0-255 range)");
	}
	else if (encoding == TextEncodingDetect.Encoding.UTF8_BOM || encoding == TextEncodingDetect.Encoding.UTF8_NOBOM)
	{
		Console.WriteLine("UTF-8");
	}
	else if (encoding == TextEncodingDetect.Encoding.UTF16_LE_BOM || encoding == TextEncodingDetect.Encoding.UTF16_LE_NOBOM)
	{
		Console.WriteLine("UTF-16 Little Endian");
	}
	else if (encoding == TextEncodingDetect.Encoding.UTF16_BE_BOM || encoding == TextEncodingDetect.Encoding.UTF16_BE_NOBOM)
	{
		Console.WriteLine("UTF-16 Big Endian");
	}

## Nulls and Binary
One quirk of the library is how I chose to handle nulls (0x00). These are technically valid in UTF-8 sequences, but I've assumed that any file that contains a null is not ANSI/ASCII/UTF-8. Allowing nulls for UTF-8 can cause a false return where UTF-16 text containing just ASCII can appear to be valid UTF-8. To disable this behaviour just set the **NullSuggestsBinary** property on the library to **false** before calling **DetectEncoding**. In practice, most text files don't contain nulls and the defaults are valid.