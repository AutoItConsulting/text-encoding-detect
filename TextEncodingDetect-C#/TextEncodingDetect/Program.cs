// Copyright 2015-2016 Jonathan Bennett <jon@autoitscript.com>
// 
// https://www.autoitscript.com 
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.IO;
using AutoIt.Common;

public class Program
{
    public static int Main(string[] args)
    {
        Console.WriteLine();

        if (args.Length != 1)
        {
            Console.WriteLine("Usage: TextEncodingDetect.exe <filename>");
            return 1;
        }

        // Read in the file in binary
        byte[] buffer;

        try
        {
            buffer = File.ReadAllBytes(args[0]);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return 1;
        }

        // Detect encoding
        var textDetect = new TextEncodingDetect();
        TextEncodingDetect.Encoding encoding = textDetect.DetectEncoding(buffer, buffer.Length);

        Console.Write("Encoding: ");
        if (encoding == TextEncodingDetect.Encoding.None)
        {
            Console.WriteLine("Binary");
        }
        else if (encoding == TextEncodingDetect.Encoding.Ascii)
        {
            Console.WriteLine("ASCII (chars in the 0-127 range)");
        }
        else if (encoding == TextEncodingDetect.Encoding.Ansi)
        {
            Console.WriteLine("ANSI (chars in the range 0-255 range)");
        }
        else if (encoding == TextEncodingDetect.Encoding.Utf8Bom || encoding == TextEncodingDetect.Encoding.Utf8Nobom)
        {
            Console.WriteLine("UTF-8");
        }
        else if (encoding == TextEncodingDetect.Encoding.Utf16LeBom || encoding == TextEncodingDetect.Encoding.Utf16LeNoBom)
        {
            Console.WriteLine("UTF-16 Little Endian");
        }
        else if (encoding == TextEncodingDetect.Encoding.Utf16BeBom || encoding == TextEncodingDetect.Encoding.Utf16BeNoBom)
        {
            Console.WriteLine("UTF-16 Big Endian");
        }

        return 0;
    }
}