# text-encoding-detect

![alt text](https://www.autoitconsulting.com/site/wp-content/uploads/2014/08/charmap_330x220.png "text-encoding-detect")

This is a C++ and C# library for detecting UTF8 and UTF16 text encoding. I recently had to upgrade the text file handling feature of [AutoIt](https://www.autoitscript.com/site/autoit/) to better handle text files where no [byte order mark](http://en.wikipedia.org/wiki/Byte_order_mark) (BOM) was present. The older version of code I was using worked fine for UTF-8 files (with or without BOM) but it wasn't able to detect UTF-16 files without a BOM. I tried to use the [IsTextUnicode](http://msdn.microsoft.com/en-us/library/windows/desktop/dd318672(v=vs.85).aspx) Win32 API function but this seemed extremely unreliable and wouldn't detect UTF-16 Big-Endian text in my tests. 

Please see the project page for details on how the library works and how to use it: [https://www.autoitconsulting.com/site/development/utf-8-utf-16-text-encoding-detection-library/](https://www.autoitconsulting.com/site/development/utf-8-utf-16-text-encoding-detection-library/)

