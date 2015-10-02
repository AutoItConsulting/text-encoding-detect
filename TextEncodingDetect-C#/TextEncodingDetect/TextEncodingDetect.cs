//
// Copyright 2015 Jonathan Bennett <jon@autoitscript.com>
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
// 

namespace AutoIt.Text
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class TextEncodingDetect
    {
        #region Fields

        private readonly byte[] utf16LEBOM = { 0xFF, 0xFE };
        private readonly byte[] utf16BEBOM = { 0xFE, 0xFF };
        private readonly byte[] utf8BOM = { 0xEF, 0xBB, 0xBF };

        private bool nullSuggestsBinary = true;
        private double utf16ExpectedNullPercent = 70;
        private double utf16UnexpectedNullPercent = 10;

        #endregion

        #region Enums

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

        #endregion

        #region Properties

        public bool NullSuggestsBinary
        {
            set
            {
                this.nullSuggestsBinary = value;
            }
        }

        public double Utf16ExpectedNullPercent
        {
            set
            {
                if (value > 0 && value < 100)
                {
                    this.utf16ExpectedNullPercent = value;
                }
            }
        }

        public double Utf16UnexpectedNullPercent
        {
            set
            {
                if (value > 0 && value < 100)
                {
                    this.utf16UnexpectedNullPercent = value;
                }
            }
        }

        #endregion

        public static int GetBOMLengthFromEncodingMode(Encoding encoding)
        {
            int length = 0;

            if (encoding == Encoding.UTF16_BE_BOM || encoding == Encoding.UTF16_LE_BOM)
            {
                length = 2;
            }
            else if (encoding == Encoding.UTF8_BOM)
            {
                length = 3;
            }

            return length;
        }

        public Encoding CheckBOM(byte[] buffer, int size)
        {
            // Check for BOM
            if (size >= 2 && buffer[0] == this.utf16LEBOM[0] && buffer[1] == this.utf16LEBOM[1])
            {
                return Encoding.UTF16_LE_BOM;
            }
            else if (size >= 2 && buffer[0] == this.utf16BEBOM[0] && buffer[1] == this.utf16BEBOM[1])
            {
                return Encoding.UTF16_BE_BOM;
            }
            else if (size >= 3 && buffer[0] == this.utf8BOM[0] && buffer[1] == this.utf8BOM[1] && buffer[2] == this.utf8BOM[2])
            {
                return Encoding.UTF8_BOM;
            }
            else
            {
                return Encoding.None;
            }
        }

        public Encoding DetectEncoding(byte[] buffer, int size)
        {
            // First check if we have a BOM and return that if so
            Encoding encoding = this.CheckBOM(buffer, size);
            if (encoding != Encoding.None)
            {
                return encoding;
            }

            // Now check for valid UTF8
            encoding = this.CheckUTF8(buffer, size);
            if (encoding != Encoding.None)
            {
                return encoding;
            }

            // Now try UTF16 
            encoding = this.CheckUTF16NewlineChars(buffer, size);
            if (encoding != Encoding.None)
            {
                return encoding;
            }

            encoding = this.CheckUTF16ASCII(buffer, size);
            if (encoding != Encoding.None)
            {
                return encoding;
            }

            // ANSI or None (binary) then
            if (!this.DoesContainNulls(buffer, size))
            {
                return Encoding.ANSI;
            }
            else
            {
                // Found a null, return based on the preference in null_suggests_binary_
                if (this.nullSuggestsBinary)
                {
                    return Encoding.None;
                }
                else
                {
                    return Encoding.ANSI;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////
        // Checks if a buffer contains valid utf8. Returns:
        // None - not valid utf8
        // UTF8_NOBOM - valid utf8 encodings and multibyte sequences
        // ASCII - Only data in the 0-127 range. 
        ///////////////////////////////////////////////////////////////////////////////

        private Encoding CheckUTF8(byte[] buffer, int size)
        {
            // UTF8 Valid sequences
            // 0xxxxxxx  ASCII
            // 110xxxxx 10xxxxxx  2-byte
            // 1110xxxx 10xxxxxx 10xxxxxx  3-byte
            // 11110xxx 10xxxxxx 10xxxxxx 10xxxxxx  4-byte
            //
            // Width in UTF8
            // Decimal      Width
            // 0-127        1 byte
            // 194-223      2 bytes
            // 224-239      3 bytes
            // 240-244      4 bytes
            //
            // Subsequent chars are in the range 128-191
            bool only_saw_ascii_range = true;
            uint pos = 0;
            int more_chars;

            while (pos < size)
            {
                byte ch = buffer[pos++];

                if (ch == 0 && this.nullSuggestsBinary)
                {
                    return Encoding.None;
                }
                else if (ch <= 127)
                {
                    // 1 byte
                    more_chars = 0;
                }
                else if (ch >= 194 && ch <= 223)
                {
                    // 2 Byte
                    more_chars = 1;
                }
                else if (ch >= 224 && ch <= 239)
                {
                    // 3 Byte
                    more_chars = 2;
                }
                else if (ch >= 240 && ch <= 244)
                {
                    // 4 Byte
                    more_chars = 3;
                }
                else
                {
                    return Encoding.None;               // Not utf8
                }

                // Check secondary chars are in range if we are expecting any
                while (more_chars > 0 && pos < size)
                {
                    only_saw_ascii_range = false;       // Seen non-ascii chars now

                    ch = buffer[pos++];
                    if (ch < 128 || ch > 191)
                    {
                        return Encoding.None;           // Not utf8
                    }

                    --more_chars;
                }
            }

            // If we get to here then only valid UTF-8 sequences have been processed

            // If we only saw chars in the range 0-127 then we can't assume UTF8 (the caller will need to decide)
            if (only_saw_ascii_range)
            {
                return Encoding.ASCII;
            }
            else
            {
                return Encoding.UTF8_NOBOM;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////
        // Checks if a buffer contains text that looks like utf16 by scanning for 
        // newline chars that would be present even in non-english text.
        // Returns:
        // None - not valid utf16
        // UTF16_LE_NOBOM - looks like utf16 le
        // UTF16_BE_NOBOM - looks like utf16 be
        ///////////////////////////////////////////////////////////////////////////////

        private Encoding CheckUTF16NewlineChars(byte[] buffer, int size)
        {
            if (size < 2)
            {
                return Encoding.None;
            }

            // Reduce size by 1 so we don't need to worry about bounds checking for pairs of bytes
            size--;

            int le_control_chars = 0;
            int be_control_chars = 0;
            byte ch1, ch2;

            uint pos = 0;
            while (pos < size)
            {
                ch1 = buffer[pos++];
                ch2 = buffer[pos++];

                if (ch1 == 0)
                {
                    if (ch2 == 0x0a || ch2 == 0x0d)
                    {
                        ++be_control_chars;
                    }
                }
                else if (ch2 == 0)
                {
                    if (ch1 == 0x0a || ch1 == 0x0d)
                    {
                        ++le_control_chars;
                    }
                }

                // If we are getting both LE and BE control chars then this file is not utf16
                if (le_control_chars > 0 && be_control_chars > 0)
                {
                    return Encoding.None;
                }
            }

            if (le_control_chars > 0)
            {
                return Encoding.UTF16_LE_NOBOM;
            }
            else if (be_control_chars > 0)
            {
                return Encoding.UTF16_BE_NOBOM;
            }
            else
            {
                return Encoding.None;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////
        // Checks if a buffer contains text that looks like utf16. This is done based
        // the use of nulls which in ASCII/script like text can be useful to identify.
        // Returns:
        // None - not valid utf16
        // UTF16_LE_NOBOM - looks like utf16 le
        // UTF16_BE_NOBOM - looks like utf16 be
        ///////////////////////////////////////////////////////////////////////////////

        private Encoding CheckUTF16ASCII(byte[] buffer, int size)
        {
            int num_odd_nulls = 0;
            int num_even_nulls = 0;

            // Get even nulls
            uint pos = 0;
            while (pos < size)
            {
                if (buffer[pos] == 0)
                {
                    num_even_nulls++;
                }

                pos += 2;
            }

            // Get odd nulls
            pos = 1;
            while (pos < size)
            {
                if (buffer[pos] == 0)
                {
                    num_odd_nulls++;
                }

                pos += 2;
            }

            double even_null_threshold = (num_even_nulls * 2.0) / size;
            double odd_null_threshold = (num_odd_nulls * 2.0) / size;
            double expected_null_threshold = this.utf16ExpectedNullPercent / 100.0;
            double unexpected_null_threshold = this.utf16UnexpectedNullPercent / 100.0;

            // Lots of odd nulls, low number of even nulls
            if (even_null_threshold < unexpected_null_threshold && odd_null_threshold > expected_null_threshold)
            {
                return Encoding.UTF16_LE_NOBOM;
            }

            // Lots of even nulls, low number of odd nulls
            if (odd_null_threshold < unexpected_null_threshold && even_null_threshold > expected_null_threshold)
            {
                return Encoding.UTF16_BE_NOBOM;
            }

            // Don't know
            return Encoding.None;
        }

        ///////////////////////////////////////////////////////////////////////////////
        // Checks if a buffer contains any nulls. Used to check for binary vs text data.
        ///////////////////////////////////////////////////////////////////////////////

        private bool DoesContainNulls(byte[] buffer, int size)
        {
            uint pos = 0;
            while (pos < size)
            {
                if (buffer[pos++] == 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
