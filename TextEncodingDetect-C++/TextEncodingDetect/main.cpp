//
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
// 

#include <stdio.h>
#include <tchar.h>

#include "text_encoding_detect.h"
using namespace AutoIt::Common;


int wmain(int argc, wchar_t* argv[])
{
	if (argc != 2)
	{
		wprintf(L"\nUsage: %s filename.", argv[0]);
		return 1;
	}
	
	// Open file in binary mode
	FILE *file = _wfopen(argv[1], L"rb");

	if (file == NULL)
	{
		wprintf(L"\nCould not open file.\n");
		return 1;
	}

	// Get file size
	fseek(file, 0, SEEK_END);
	long fsize = ftell(file);
	fseek(file, 0, SEEK_SET);

	// Read it all in
	unsigned char *buffer = new unsigned char[fsize];
	fread(buffer, fsize, 1, file);
	fclose(file);

	// Detect the encoding
	TextEncodingDetect textDetect;
	TextEncodingDetect::Encoding encoding = textDetect.DetectEncoding(buffer, fsize);

	wprintf(L"\nEncoding: ");
	if (encoding == TextEncodingDetect::None)
		wprintf(L"Binary");
	else if (encoding == TextEncodingDetect::ASCII)
		wprintf(L"ASCII (chars in the 0-127 range)");
	else if (encoding == TextEncodingDetect::ANSI)
		wprintf(L"ANSI (chars in the range 0-255 range)");
	else if (encoding == TextEncodingDetect::UTF8_BOM || encoding == TextEncodingDetect::UTF8_NOBOM)
		wprintf(L"UTF-8");
	else if (encoding == TextEncodingDetect::UTF16_LE_BOM || encoding == TextEncodingDetect::UTF16_LE_NOBOM)
		wprintf(L"UTF-16 Little Endian");
	else if (encoding == TextEncodingDetect::UTF16_BE_BOM || encoding == TextEncodingDetect::UTF16_BE_NOBOM)
		wprintf(L"UTF-16 Big Endian");

	// Free up
	delete[] buffer;

	return 0;
}

