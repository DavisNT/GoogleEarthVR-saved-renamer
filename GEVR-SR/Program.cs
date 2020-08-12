using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace GEVR_SR
{
    class Program
    {
        private static readonly byte[] XMPXMLStart = Encoding.GetEncoding("us-ascii").GetBytes("\n<x:xmpmeta xmlns:x=\"adobe:ns:meta/\" x:xmptk=\"Adobe XMP\">\n  <rdf:RDF xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\">\n    <rdf:Description xmlns:EarthVR=\"http://ns.google.com/photos/1.0/earthvr/\" rdf:about=\"\" EarthVR:SerializedMetadata=\"");
        private static readonly byte[] XMPXMLEnd = Encoding.GetEncoding("us-ascii").GetBytes("\" EarthVR:Version=\"1.0\"/>\n  </rdf:RDF>\n</x:xmpmeta>\n");
        private static readonly int XMPHeadersLength = 77;
        private static byte[] FileBytes, GEMetadataDecoded;
        private static int GEMetadataB64Start, GEMetadataB64Length, XMPBodyLength, XMPBodyLengthPointer, XMPFullLength, XMPFullLengthPointer, OffsetTitle, OffsetSubtitle;

        static int Main(string[] args)
        {
            Console.WriteLine(string.Format("{0} {1}\r\n{2}\r\nhttps://github.com/DavisNT/GoogleEarthVR-saved-renamer\r\n", ((AssemblyProductAttribute)typeof(Program).Assembly.GetCustomAttributes(typeof(AssemblyProductAttribute), false)[0]).Product, typeof(Program).Assembly.GetName().Version.ToString(3), ((AssemblyDescriptionAttribute)typeof(Program).Assembly.GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false)[0]).Description));
            if (args.Length == 0 || args.Length > 3)
            {
                Console.WriteLine("Usage: gevr-sr file.jpg [title[ subtitle]]");
                Console.WriteLine(string.Format("\r\nThe files might be in: {0}", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Google Earth VR")));
                return args.Length == 0 ? 0 : 1;
            }
            else
            {
                var loaded = LoadFileAndDisplayInfo(args[0]);
                if (loaded == 0 && args.Length > 1) {
                    return UpdateInfoAndSaveFile(args[0], args[1], args.Length==3 ? args[2] : null);
                }
                return loaded;
            }
        }

        private static int LoadFileAndDisplayInfo(string fileName)
        {
            if (!File.Exists(fileName))
            {
                Console.WriteLine(string.Format("ERROR: Cannot find file: {0}", fileName));
                return 1;
            }

            Console.WriteLine(string.Format("Processing file {0}...", fileName));
            FileBytes = File.ReadAllBytes(fileName);

            // find Base64 encoded metadata
            GEMetadataB64Start = ByteSubarrayIndexOf(FileBytes, XMPXMLStart, 0) + XMPXMLStart.Length;
            GEMetadataB64Length = ByteSubarrayIndexOf(FileBytes, XMPXMLEnd, GEMetadataB64Start) - GEMetadataB64Start;
            if (GEMetadataB64Start < XMPXMLStart.Length + 128 || GEMetadataB64Start > FileBytes.Length - XMPXMLStart.Length - 128 || GEMetadataB64Length < 0 || GEMetadataB64Length > 20480)
            {
                throw new FormatException("Cannot find Base64 encoded metadata in the file");
            }

            // obtain XMP XML length from JPEG file headers
            XMPBodyLengthPointer = GEMetadataB64Start - XMPXMLStart.Length - 6;
            XMPFullLengthPointer = GEMetadataB64Start - XMPXMLStart.Length - 77;
            XMPBodyLength = (FileBytes[XMPBodyLengthPointer] << 8) + FileBytes[XMPBodyLengthPointer + 1];
            XMPFullLength = (FileBytes[XMPFullLengthPointer] << 8) + FileBytes[XMPFullLengthPointer + 1];
            if (XMPXMLStart.Length + GEMetadataB64Length + XMPXMLEnd.Length != XMPBodyLength || XMPFullLength != XMPBodyLength + XMPHeadersLength)
            {
                throw new FormatException("Unable to find XMP length in file headers");
            }

            // pad and decode metadata
            GEMetadataDecoded = Convert.FromBase64String(Encoding.GetEncoding("us-ascii").GetString(FileBytes, GEMetadataB64Start, GEMetadataB64Length) + new string('=', (4 - GEMetadataB64Length % 4) % 4));
            OffsetTitle = Array.IndexOf(GEMetadataDecoded, (byte)10, 2) + 1;
            OffsetSubtitle = OffsetTitle + GEMetadataDecoded[OffsetTitle] + 2;

            // verification of some pointer
            if (GEMetadataDecoded[OffsetTitle] + GEMetadataDecoded[OffsetSubtitle] + GEMetadataDecoded[OffsetSubtitle + GEMetadataDecoded[OffsetSubtitle] + 2] + 17 != GEMetadataDecoded[1])
            {
                throw new FormatException("Unable to parse Google Earth VR metadata");
            }

            // display info
            Console.WriteLine(string.Format("  Title ({0} bytes): {1}", GEMetadataDecoded[OffsetTitle], Encoding.GetEncoding("utf-8").GetString(GEMetadataDecoded, OffsetTitle + 1, GEMetadataDecoded[OffsetTitle])));
            Console.WriteLine(string.Format("  Subtitle ({0} bytes): {1}", GEMetadataDecoded[OffsetSubtitle], Encoding.GetEncoding("utf-8").GetString(GEMetadataDecoded, OffsetSubtitle + 1, GEMetadataDecoded[OffsetSubtitle])));
            Console.WriteLine(string.Format("  Available extra space: {0} bytes", 127 - GEMetadataDecoded[1]));
            Console.WriteLine(string.Format("  Total space for title and subtitle: {0} bytes", 127 - GEMetadataDecoded[1] + GEMetadataDecoded[OffsetTitle] + GEMetadataDecoded[OffsetSubtitle]));

            return 0;
        }

        private static int UpdateInfoAndSaveFile(string fileName, string newTitle, string newSubtitle)
        {
            if (newTitle.Length > 64 || newSubtitle?.Length > 64) {
                Console.WriteLine("ERROR: Title and subtitle cannot be longer than 64 characters.");
                return 1;
            }

            // title and subtitle strings
            byte[] newSubtitleBytes, newTitleBytes = Encoding.GetEncoding("utf-8").GetBytes(newTitle);
            if (newSubtitle == null)
            {
                newSubtitleBytes = new byte[GEMetadataDecoded[OffsetSubtitle]];
                Array.Copy(GEMetadataDecoded, OffsetSubtitle + 1, newSubtitleBytes, 0, GEMetadataDecoded[OffsetSubtitle]);
            } else
            {
                newSubtitleBytes = Encoding.GetEncoding("utf-8").GetBytes(newSubtitle);
            }

            // display new info
            Console.WriteLine(string.Format("  New Title ({0} bytes): {1}", newTitleBytes.Length, Encoding.GetEncoding("utf-8").GetString(newTitleBytes, 0, newTitleBytes.Length)));
            Console.WriteLine(string.Format("  New Subtitle ({0} bytes): {1}", newSubtitleBytes.Length, Encoding.GetEncoding("utf-8").GetString(newSubtitleBytes, 0, newSubtitleBytes.Length)));

            // length verifications
            var newFirstPartLength = GEMetadataDecoded[1] - GEMetadataDecoded[OffsetTitle] - GEMetadataDecoded[OffsetSubtitle] + newTitleBytes.Length + newSubtitleBytes.Length;
            if (newFirstPartLength > 127)
            {
                Console.WriteLine(string.Format("ERROR: Not enough space for new title and subtitle ({0} byte(s) insufficient)", newFirstPartLength - 127));
                return 1;
            }
            if (newTitleBytes.Length > 127 || newSubtitleBytes.Length > 127)
            {
                throw new ArgumentException("Title or subtitle longer than 127 bytes");
            }

            // concatenate metadata
            var newGEMetadata = new byte[GEMetadataDecoded.Length - GEMetadataDecoded[OffsetTitle] - GEMetadataDecoded[OffsetSubtitle] + newTitleBytes.Length + newSubtitleBytes.Length];
            Array.Copy(GEMetadataDecoded, 0, newGEMetadata, 0, OffsetTitle);
            newGEMetadata[1] = (byte)(newFirstPartLength);
            newGEMetadata[OffsetTitle] = (byte)newTitleBytes.Length;
            Array.Copy(newTitleBytes, 0, newGEMetadata, OffsetTitle + 1, newTitleBytes.Length);
            newGEMetadata[OffsetTitle + newTitleBytes.Length + 1] = (byte)10;
            newGEMetadata[OffsetTitle + newTitleBytes.Length + 2] = (byte)newSubtitleBytes.Length;
            Array.Copy(newSubtitleBytes, 0, newGEMetadata, OffsetTitle + newTitleBytes.Length + 3, newSubtitleBytes.Length);
            Array.Copy(GEMetadataDecoded, OffsetSubtitle + GEMetadataDecoded[OffsetSubtitle] + 1, newGEMetadata, OffsetTitle + newTitleBytes.Length + newSubtitleBytes.Length + 3, GEMetadataDecoded.Length - (OffsetSubtitle + GEMetadataDecoded[OffsetSubtitle] + 1));

            // encode and trim metadata
            var newGEMetadataB64 = Encoding.GetEncoding("us-ascii").GetBytes(Convert.ToBase64String(newGEMetadata).Replace("=", ""));

            // concatenate new file and update XMP length
            var newXMPBodyLength = XMPXMLStart.Length + newGEMetadataB64.Length + XMPXMLEnd.Length;
            var newXMPFullLength = newXMPBodyLength + XMPHeadersLength;
            var newFileBytes = new byte[FileBytes.Length - GEMetadataB64Length + newGEMetadataB64.Length];
            Array.Copy(FileBytes, 0, newFileBytes, 0, GEMetadataB64Start);
            newFileBytes[XMPBodyLengthPointer] = (byte)(newXMPBodyLength >> 8);
            newFileBytes[XMPBodyLengthPointer + 1] = (byte)(newXMPBodyLength);
            newFileBytes[XMPFullLengthPointer] = (byte)(newXMPFullLength >> 8);
            newFileBytes[XMPFullLengthPointer + 1] = (byte)(newXMPFullLength);
            Array.Copy(newGEMetadataB64, 0, newFileBytes, GEMetadataB64Start, newGEMetadataB64.Length);
            Array.Copy(FileBytes, GEMetadataB64Start + GEMetadataB64Length, newFileBytes, GEMetadataB64Start + newGEMetadataB64.Length, FileBytes.Length - (GEMetadataB64Start + GEMetadataB64Length));

            // backup file on first time
            if (!File.Exists(fileName + ".bak"))
            {
                File.Copy(fileName, fileName + ".bak", false);
            }

            // write the new file
            File.WriteAllBytes(fileName, newFileBytes);
            Console.WriteLine("File updated successfully.");

            return 0;
        }

        private static int ByteSubarrayIndexOf(byte[] haystack, byte[] needle, int startOffset)
        {
            if (needle.Length < 1 || needle.Length > haystack.Length)
            {
                return -1;
            }
            for (int j, i = startOffset; i < haystack.Length - needle.Length; i++)
            {
                for (j = 0; j < needle.Length && haystack[i + j] == needle[j]; j++) ;
                if (j == needle.Length)
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
