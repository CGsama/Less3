﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using XmlToPox;

namespace Less3
{
    /// <summary>
    /// Common static methods.
    /// </summary>
    internal static class Common
    {
        #region Environment

        public static void ExitApplication(string method, string text, int returnCode)
        {
            Console.WriteLine("---");
            Console.WriteLine("");
            Console.WriteLine("The application has exited.");
            Console.WriteLine("");
            Console.WriteLine("  Requested by : " + method);
            Console.WriteLine("  Reason text  : " + text);
            Console.WriteLine("");
            Console.WriteLine("---");
            Environment.Exit(returnCode);
            return;
        }

        #endregion

        #region Directory

        public static List<string> GetSubdirectoryList(string directory, bool recursive)
        {
            try
            {
                /*
                 * Prepends the 'directory' variable to the name of each directory already
                 * so each is immediately usable from the resultant list
                 * 
                 * Does NOT append a slash
                 * Does NOT include the original directory in the list
                 * Does NOT include child files
                 * 
                 * i.e. 
                 * C:\code\kvpbase
                 * C:\code\kvpbase\src
                 * C:\code\kvpbase\test
                 * 
                 */

                string[] folders;

                if (recursive)
                {
                    folders = Directory.GetDirectories(@directory, "*", SearchOption.AllDirectories);
                }
                else
                {
                    folders = Directory.GetDirectories(@directory, "*", SearchOption.TopDirectoryOnly);
                }

                List<string> folderList = new List<string>();

                foreach (string folder in folders)
                {
                    folderList.Add(folder);
                }

                return folderList;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static bool DeleteDirectory(string dir, bool recursive)
        {
            try
            {
                Directory.Delete(dir, recursive);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool RenameDirectory(string from, string to)
        {
            try
            {
                if (String.IsNullOrEmpty(from)) return false;
                if (String.IsNullOrEmpty(to)) return false;
                if (String.Compare(from, to) == 0) return true;
                Directory.Move(from, to);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool MoveDirectory(string from, string to)
        {
            try
            {
                if (String.IsNullOrEmpty(from)) return false;
                if (String.IsNullOrEmpty(to)) return false;
                if (String.Compare(from, to) == 0) return true;
                Directory.Move(from, to);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool WalkDirectory(
            string environment,
            int depth,
            string directory,
            bool prependFilename,
            out List<string> subdirectories,
            out List<string> files,
            out long bytes,
            bool recursive)
        {
            subdirectories = new List<string>();
            files = new List<string>();
            bytes = 0;

            try
            {
                subdirectories = Common.GetSubdirectoryList(directory, false);
                files = Common.GetFileList(environment, directory, prependFilename);

                if (files != null && files.Count > 0)
                {
                    foreach (String currFile in files)
                    {
                        FileInfo fi = new FileInfo(currFile);
                        bytes += fi.Length;
                    }
                }

                List<string> queueSubdirectories = new List<string>();
                List<string> queueFiles = new List<string>();
                long queueBytes = 0;

                if (recursive)
                {
                    if (subdirectories == null || subdirectories.Count < 1) return true;
                    depth += 2;

                    foreach (string curr in subdirectories)
                    {
                        List<string> childSubdirectories = new List<string>();
                        List<string> childFiles = new List<string>();
                        long childBytes = 0;

                        WalkDirectory(
                            environment,
                            depth,
                            curr,
                            prependFilename,
                            out childSubdirectories,
                            out childFiles,
                            out childBytes,
                            true);

                        if (childSubdirectories != null)
                            foreach (string childSubdir in childSubdirectories)
                                queueSubdirectories.Add(childSubdir);

                        if (childFiles != null)
                            foreach (string childFile in childFiles)
                                queueFiles.Add(childFile);

                        queueBytes += childBytes;
                    }
                }

                if (queueSubdirectories != null)
                    foreach (string queueSubdir in queueSubdirectories)
                        subdirectories.Add(queueSubdir);

                if (queueFiles != null)
                    foreach (string queueFile in queueFiles)
                        files.Add(queueFile);

                bytes += queueBytes;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool DirectoryStatistics(
            DirectoryInfo dirinfo,
            bool recursive,
            out long bytes,
            out int files,
            out int subdirs)
        {
            bytes = 0;
            files = 0;
            subdirs = 0;

            try
            {
                FileInfo[] fis = dirinfo.GetFiles();
                files = fis.Length;

                foreach (FileInfo fi in fis)
                {
                    bytes += fi.Length;
                }

                // Add subdirectory sizes
                DirectoryInfo[] subdirinfos = dirinfo.GetDirectories();

                if (recursive)
                {
                    foreach (DirectoryInfo subdirinfo in subdirinfos)
                    {
                        subdirs++;
                        long subdirBytes = 0;
                        int subdirFiles = 0;
                        int subdirSubdirectories = 0;

                        if (Common.DirectoryStatistics(subdirinfo, recursive, out subdirBytes, out subdirFiles, out subdirSubdirectories))
                        {
                            bytes += subdirBytes;
                            files += subdirFiles;
                            subdirs += subdirSubdirectories;
                        }
                    }
                }
                else
                {
                    subdirs = subdirinfos.Length;
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion

        #region File

        public static bool DeleteFile(string filename)
        {
            try
            {
                File.Delete(@filename);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool FileExists(string filename)
        {
            return File.Exists(filename);
        }

        public static bool VerifyFileReadAccess(string filename)
        {
            try
            {
                using (FileStream stream = File.Open(filename, System.IO.FileMode.Open, FileAccess.Read))
                {
                    return true;
                }
            }
            catch (IOException)
            {
                return false;
            }
        }

        public static List<string> GetFileList(string environment, string directory, bool prependFilename)
        {
            try
            {
                /*
                 * 
                 * Returns only the filename unless prepend_filename is set
                 * If prepend_filename is set, directory is prepended
                 * 
                 */

                DirectoryInfo info = new DirectoryInfo(directory);
                FileInfo[] files = info.GetFiles().OrderBy(p => p.CreationTime).ToArray();
                List<string> fileList = new List<string>();

                foreach (FileInfo file in files)
                {
                    if (prependFilename) fileList.Add(directory + "/" + file.Name);
                    else fileList.Add(file.Name);
                }

                return fileList;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static bool WriteFile(string filename, string content, bool append)
        {
            using (StreamWriter writer = new StreamWriter(filename, append))
            {
                writer.WriteLine(content);
            }
            return true;
        }

        public static bool WriteFile(string filename, byte[] content)
        {
            if (content != null && content.Length > 0)
            {
                File.WriteAllBytes(filename, content);
            }
            else
            {
                File.Create(filename).Close();
            }

            return true;
        }

        public static bool WriteFile(string filename, byte[] content, int pos)
        {
            using (Stream stream = new FileStream(filename, System.IO.FileMode.OpenOrCreate))
            {
                stream.Seek(pos, SeekOrigin.Begin);
                stream.Write(content, 0, content.Length);
            }
            return true;
        }

        public static string ReadTextFile(string filename)
        {
            try
            {
                return File.ReadAllText(@filename);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static byte[] ReadBinaryFile(string filename, int from, int len)
        {
            try
            {
                if (len < 1) return null;
                if (from < 0) return null;

                byte[] ret = new byte[len];
                using (BinaryReader reader = new BinaryReader(new FileStream(filename, System.IO.FileMode.Open)))
                {
                    reader.BaseStream.Seek(from, SeekOrigin.Begin);
                    reader.Read(ret, 0, len);
                }

                return ret;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static byte[] ReadBinaryFile(string filename)
        {
            try
            {
                return File.ReadAllBytes(@filename);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static string GetFileExtension(string filename)
        {
            try
            {
                if (String.IsNullOrEmpty(filename)) return null;
                return Path.GetExtension(filename);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static bool RenameFile(string from, string to)
        {
            try
            {
                if (String.IsNullOrEmpty(from)) return false;
                if (String.IsNullOrEmpty(to)) return false;

                if (String.Compare(from, to) == 0) return true;
                File.Move(from, to);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool MoveFile(string from, string to)
        {
            try
            {
                if (String.IsNullOrEmpty(from)) return false;
                if (String.IsNullOrEmpty(to)) return false;

                if (String.Compare(from, to) == 0) return true;
                File.Move(from, to);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion

        #region IsTrue

        public static bool IsTrue(bool val)
        {
            return val;
        }

        public static bool IsTrue(bool? val)
        {
            if (val == null) return false;
            return Convert.ToBoolean(val);
        }

        public static bool IsTrue(string val)
        {
            if (String.IsNullOrEmpty(val)) return false;
            val = val.ToLower().Trim();
            int valInt = 0;
            if (Int32.TryParse(val, out valInt)) if (valInt == 1) return true;
            if (String.Compare(val, "true") == 0) return true;
            return false;
        }

        #endregion
         
        #region Compress

        public static byte[] GzipCompress(byte[] input)
        {
            if (input == null) return null;
            if (input.Length < 1) return null;

            using (MemoryStream memory = new MemoryStream())
            {
                using (GZipStream gzip = new GZipStream(memory, CompressionMode.Compress, true))
                {
                    gzip.Write(input, 0, input.Length);
                }
                return memory.ToArray();
            }
        }

        public static byte[] GzipDecompress(byte[] input)
        {
            if (input == null) return null;
            if (input.Length < 1) return null;

            using (GZipStream stream = new GZipStream(new MemoryStream(input), CompressionMode.Decompress))
            {
                const int size = 4096;
                byte[] buffer = new byte[size];
                using (MemoryStream memory = new MemoryStream())
                {
                    int count = 0;
                    do
                    {
                        count = stream.Read(buffer, 0, size);
                        if (count > 0)
                        {
                            memory.Write(buffer, 0, count);
                        }
                    }
                    while (count > 0);
                    return memory.ToArray();
                }
            }
        }

        #endregion

        #region Misc

        public static string StringRemove(string original, string remove)
        {
            if (String.IsNullOrEmpty(original)) return null;
            if (String.IsNullOrEmpty(remove)) return original;

            int index = original.IndexOf(remove);
            string ret = (index < 0)
                ? original
                : original.Remove(index, remove.Length);

            return ret;
        }

        public static string Line(int count, string fill)
        {
            if (count < 1) return "";

            string ret = "";
            for (int i = 0; i < count; i++)
            {
                ret += fill;
            }

            return ret;
        }

        public static string RandomString(int numChar)
        {
            string ret = "";
            if (numChar < 1) return null;
            int valid = 0;
            Random random = new Random((int)DateTime.Now.Ticks);
            int num = 0;

            for (int i = 0; i < numChar; i++)
            {
                num = 0;
                valid = 0;
                while (valid == 0)
                {
                    num = random.Next(126);
                    if (((num > 47) && (num < 58)) ||
                        ((num > 64) && (num < 91)) ||
                        ((num > 96) && (num < 123)))
                    {
                        valid = 1;
                    }
                }
                ret += (char)num;
            }

            return ret;
        }

        public static double TotalMsFrom(DateTime startTime)
        {
            try
            {
                DateTime endTime = DateTime.Now;
                TimeSpan totalTime = (endTime - startTime);
                return totalTime.TotalMilliseconds;
            }
            catch (Exception)
            {
                return -1;
            }
        }

        public static bool IsLaterThanNow(DateTime? dt)
        {
            try
            {
                DateTime curr = Convert.ToDateTime(dt);
                return Common.IsLaterThanNow(curr);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool IsLaterThanNow(DateTime dt)
        {
            if (DateTime.Compare(dt, DateTime.Now) > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool ContainsUnsafeCharacters(string data)
        {
            /*
             * 
             * Returns true if unsafe characters exist
             * 
             * 
             */

            // see https://kb.acronis.com/content/39790

            if (String.IsNullOrEmpty(data)) return false;
            if (data.Equals(".")) return true;
            if (data.Equals("..")) return true;

            if (
                (String.Compare(data.ToLower(), "com1") == 0) ||
                (String.Compare(data.ToLower(), "com2") == 0) ||
                (String.Compare(data.ToLower(), "com3") == 0) ||
                (String.Compare(data.ToLower(), "com4") == 0) ||
                (String.Compare(data.ToLower(), "com5") == 0) ||
                (String.Compare(data.ToLower(), "com6") == 0) ||
                (String.Compare(data.ToLower(), "com7") == 0) ||
                (String.Compare(data.ToLower(), "com8") == 0) ||
                (String.Compare(data.ToLower(), "com9") == 0) ||
                (String.Compare(data.ToLower(), "lpt1") == 0) ||
                (String.Compare(data.ToLower(), "lpt2") == 0) ||
                (String.Compare(data.ToLower(), "lpt3") == 0) ||
                (String.Compare(data.ToLower(), "lpt4") == 0) ||
                (String.Compare(data.ToLower(), "lpt5") == 0) ||
                (String.Compare(data.ToLower(), "lpt6") == 0) ||
                (String.Compare(data.ToLower(), "lpt7") == 0) ||
                (String.Compare(data.ToLower(), "lpt8") == 0) ||
                (String.Compare(data.ToLower(), "lpt9") == 0) ||
                (String.Compare(data.ToLower(), "con") == 0) ||
                (String.Compare(data.ToLower(), "nul") == 0) ||
                (String.Compare(data.ToLower(), "prn") == 0) ||
                (String.Compare(data.ToLower(), "con") == 0)
                )
            {
                return true;
            }

            for (int i = 0; i < data.Length; i++)
            {
                if (
                    ((int)(data[i]) < 32) ||    // below range
                    ((int)(data[i]) > 126) ||   // above range
                    ((int)(data[i]) == 47) ||   // slash /
                    ((int)(data[i]) == 92) ||   // backslash \
                    ((int)(data[i]) == 63) ||   // question mark ?
                    ((int)(data[i]) == 60) ||   // less than < 
                    ((int)(data[i]) == 62) ||   // greater than >
                    ((int)(data[i]) == 58) ||   // colon :
                    ((int)(data[i]) == 42) ||   // asterisk *
                    ((int)(data[i]) == 124) ||  // pipe |
                    ((int)(data[i]) == 34) ||   // double quote "
                    ((int)(data[i]) == 39) ||   // single quote '
                    ((int)(data[i]) == 94)      // caret ^
                    )
                {
                    return true;
                }
            }

            return false;
        }

        public static bool ContainsUnsafeCharacters(List<string> data)
        {
            if (data == null || data.Count < 1) return true;
            foreach (string curr in data)
            {
                if (ContainsUnsafeCharacters(curr)) return true;
            }
            return false;
        }

        public static int GuidToInt(string guid)
        {
            if (String.IsNullOrEmpty(guid)) return 0;
            byte[] bytes = Encoding.UTF8.GetBytes(guid);
            int ret = 0;

            foreach (byte currByte in bytes)
            {
                ret += (int)currByte;
            }

            return ret;
        }

        public static byte[] AppendBytes(byte[] head, byte[] tail)
        {
            byte[] arrayCombined = new byte[head.Length + tail.Length];
            Array.Copy(head, 0, arrayCombined, 0, head.Length);
            Array.Copy(tail, 0, arrayCombined, head.Length, tail.Length);
            return arrayCombined;
        }

        public static string ByteArrayToHex(byte[] data)
        {
            StringBuilder hex = new StringBuilder(data.Length * 2);
            foreach (byte b in data) hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

        public static dynamic DataTableToDynamic(DataTable dt)
        {
            dynamic ret = new ExpandoObject();
            if (dt == null || dt.Rows.Count < 1) return ret;

            foreach (DataRow curr in dt.Rows)
            {
                foreach (DataColumn col in dt.Columns)
                {
                    var dic = (IDictionary<string, object>)ret;
                    dic[col.ColumnName] = curr[col];
                }

                return ret;
            }

            return ret;
        }
        
        public static List<dynamic> DataTableToListDynamic(DataTable dt)
        {
            List<dynamic> ret = new List<dynamic>();
            if (dt == null || dt.Rows.Count < 1) return ret;

            foreach (DataRow curr in dt.Rows)
            {
                dynamic dyn = new ExpandoObject();
                foreach (DataColumn col in dt.Columns)
                {
                    var dic = (IDictionary<string, object>)dyn;
                    dic[col.ColumnName] = curr[col];
                }
                ret.Add(dyn);
            }

            return ret;
        }

        #endregion

        #region Dictionary

        public static Dictionary<string, string> AddToDictionary(string key, string val, Dictionary<string, string> existing)
        {
            Dictionary<string, string> ret = new Dictionary<string, string>();

            if (existing == null)
            {
                ret.Add(key, val);
                return ret;
            }
            else
            {
                existing.Add(key, val);
                return existing;
            }
        }

        #endregion

        #region Crypto

        public static byte[] Md5(byte[] data)
        {
            if (data == null) return null;
            return MD5.Create().ComputeHash(data);
        }

        public static byte[] Md5(string data)
        {
            if (String.IsNullOrEmpty(data)) return null;
            return Md5(Encoding.UTF8.GetBytes(data));
        }

        public static byte[] Md5(Stream stream)
        {
            if (stream == null || !stream.CanRead) return null;

            MD5 md5 = MD5.Create();
            return md5.ComputeHash(stream);
        }

        public static async Task<byte[]> Md5Async(Stream stream, int bufferSize)
        {
            using (var md5 = MD5.Create())
            {
                byte[] buffer = new byte[bufferSize];
                int read = 0;

                do
                {
                    read = await stream.ReadAsync(buffer, 0, bufferSize);
                    if (read > 0)
                    {
                        md5.TransformBlock(buffer, 0, read, null, 0);
                    }
                } while (read > 0);

                md5.TransformFinalBlock(buffer, 0, 0);
                return md5.Hash; 
            }
        }
        
        public static byte[] Sha512(byte[] data)
        {
            if (data == null) return null;
            return SHA512.Create().ComputeHash(data);
        }

        public static byte[] Sha512(string data)
        {
            if (String.IsNullOrEmpty(data)) return null;
            return Sha512(Encoding.UTF8.GetBytes(data));
        }

        public static byte[] Sha512(Stream stream)
        {
            if (stream == null || !stream.CanRead) return null;

            SHA512 sha512 = SHA512.Create();
            return sha512.ComputeHash(stream);
        }

        public static async Task<byte[]> Sha512Async(Stream stream, int bufferSize)
        {
            using (var sha512 = SHA512.Create())
            {
                byte[] buffer = new byte[bufferSize];
                int read = 0;

                do
                {
                    read = await stream.ReadAsync(buffer, 0, bufferSize);
                    if (read > 0)
                    {
                        sha512.TransformBlock(buffer, 0, read, null, 0);
                    }
                } while (read > 0);

                sha512.TransformFinalBlock(buffer, 0, 0);
                return sha512.Hash; 
            }
        }

        public static byte[] HmacSha1(byte[] data, byte[] key)
        {
            if (data == null) return null;
            if (key == null || key.Length < 1) return null;
            return new HMACSHA1(key).ComputeHash(data);
        }

        public static byte[] HmacSha256(byte[] data, byte[] key)
        {
            if (data == null) return null;
            if (key == null || key.Length < 1) return null;
            return new HMACSHA256(key).ComputeHash(data);
        }

        public static byte[] Sha1(byte[] data)
        {
            if (data == null) return null;
            using (SHA1 hash = SHA1.Create())
            {
                return hash.ComputeHash(data);
            }
        }

        public static byte[] Sha256(byte[] data)
        {
            if (data == null) return null;
            using (SHA256 hash = SHA256.Create())
            {
                return hash.ComputeHash(data);
            }
        }

        #endregion

        #region Encoding

        public static byte[] StreamToBytes(Stream input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (!input.CanRead) throw new InvalidOperationException("Input stream is not readable");

            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;

                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }

                return ms.ToArray();
            }
        }

        public static byte[] Base64ToBytes(string data)
        {
            return Convert.FromBase64String(data);
        }

        public static string Base64ToString(string data)
        {
            if (String.IsNullOrEmpty(data)) return null;
            byte[] bytes = System.Convert.FromBase64String(data);
            return System.Text.UTF8Encoding.UTF8.GetString(bytes);
        }

        public static string BytesToBase64(byte[] data)
        {
            if (data == null) return null;
            if (data.Length < 1) return null;
            return System.Convert.ToBase64String(data);
        }

        public static string StringToBase64(string data)
        {
            if (String.IsNullOrEmpty(data)) return null;
            byte[] bytes = System.Text.UTF8Encoding.UTF8.GetBytes(data);
            return System.Convert.ToBase64String(bytes);
        }

        public static List<string> CsvToStringList(string csv)
        {
            if (String.IsNullOrEmpty(csv)) return null;

            List<string> ret = new List<string>();

            string[] array = csv.Split(',');

            if (array != null)
            {
                if (array.Length > 0)
                {
                    foreach (string curr in array)
                    {
                        if (String.IsNullOrEmpty(curr)) continue;
                        ret.Add(curr.Trim());
                    }

                    return ret;
                }
            }

            return null;
        }

        public static string StringListToCsv(List<string> strings)
        {
            if (strings == null || strings.Count < 1) return null;

            int added = 0;
            string ret = "";

            foreach (string curr in strings)
            {
                if (added == 0)
                {
                    ret += curr;
                }
                else
                {
                    ret += "," + curr;
                }

                added++;
            }

            return ret;
        }

        public static string BytesToHexString(byte[] ba)
        {
            return BitConverter.ToString(ba).Replace("-", "");
        }

        public static byte[] HexStringToBytes(String hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }

        #endregion 
    }
}
