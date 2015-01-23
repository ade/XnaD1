//
// App.cs
//
// Authors:
//		Weichhold (oliver@weichhold.com)
//
// (C) 2006 Weichhold (oliver@weichhold.com)
//
// - Uses code from the SCSharp project (http://scsharp.hungry.com/)
// - Uses SharpZipLib from IC#Code (http://www.icsharpcode.net/OpenSource/SharpZipLib/)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

// TODO:
// - Not sure what to do with blocks having Unknown_02000000 flag set

using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using MpqReader;

namespace MpqTool
{
    class App
    {
        #region private fields
        static Regex regexArgExpression = new Regex("-r\\s*(?<exp>.+)", RegexOptions.Singleline | RegexOptions.Compiled);
        static Regex listFileExpression = new Regex("-l\\s*(?<file>.+)", RegexOptions.Singleline | RegexOptions.Compiled);
        static Regex destDirExpression = new Regex("-d\\s*(?<file>.+)", RegexOptions.Singleline | RegexOptions.Compiled);

        Regex regex = null;
        string archiveFile = null;
        string listFile = null;
        string destDir = null;
        bool stripPath = false;
        bool quietOutput = false;
        #endregion

        /// <summary>
        /// Non-static entry point
        /// </summary>
        /// <param name="args">The args.</param>
        void Run(string[] args)
        {
            try
            {
                // Check mandatory args
                if(args.Length < 1)
                    throw new ArgumentException(BuildUsage());
                
                // handle args
                switch(args[0].ToLower())
                {
                    case "-h":
                    case "--help":
                    {
                        ShowHelp();
                        return;
                    }

                    case "-l":
                    case "--list":
                    {
                        if(args.Length < 2)
                            throw new ArgumentException("You must specify an archive!");
                        
                        archiveFile = args[1];
                        ParseAdditionalArgs(args, 2);
            
                        if(!quietOutput)
                            ShowBanner();

                        ListArchive();
                        return;
                    }

                    case "-e":
                    case "--extract":
                    {
                        if(args.Length < 2)
                            throw new ArgumentException("You must specify an archive!");

                        archiveFile = args[1];
                        ParseAdditionalArgs(args, 2);

                        if(!quietOutput)
                            ShowBanner();
                        
                        ExtractArchive();
                        return;
                    }
                }
            }

            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine("Aborted.");
                Environment.ExitCode = 10;
            }
        }

        /// <summary>
        /// Shows the banner.
        /// </summary>
        private static void ShowBanner()
        {
            Console.WriteLine("mpqtool v0.85");
        }

        /// <summary>
        /// Parses additional args.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <param name="StartArgIndex">Start index of the arg.</param>
        void ParseAdditionalArgs(string[] args, int StartArgIndex)
        {
            for(int i = StartArgIndex; i < args.Length; i++)
            {
                if(args[i].ToLower() == "-sp")
                {
                    stripPath = true;
                    continue;
                }
                
                if(args[i].ToLower() == "-q")
                {
                    quietOutput = true;
                    continue;
                }
                
                // try matching list file arg first
                Match m = listFileExpression.Match(args[i]);

                if(m.Success)
                {
                    // list file must be last argument
                    if(i < args.Length - 1)
                        throw new ArgumentException("The list file must be the last argument!");

                    listFile = m.Groups["file"].Value;
                    break;
                }

                // now try matching destination dir arg 
                m = destDirExpression.Match(args[i]);

                if(m.Success)
                {
                    destDir = m.Groups["file"].Value;
                    continue;
                }

                // now try matching regex arg 
                m = regexArgExpression.Match(args[i]);

                if(m.Success)
                {
                    if(regex != null)
                        throw new ArgumentException("Do not specify multiple wildcards/regex!");

                    string Pattern = m.Groups["exp"].Value;
                    regex = new Regex(Pattern, RegexOptions.Singleline | RegexOptions.Compiled);
                    continue;
                }

                else
                {
                    // must be wildcard
                    if(regex != null)
                        throw new ArgumentException("Do not specify multiple wildcards/regex!");

                    regex = new Regex(wildcardToRegexp(args[i]), RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
                    continue;
                }
            }
        }

        /// <summary>
        /// Displays the help message
        /// </summary>
        void ShowHelp()
        {
            Console.Write(BuildUsage());
        }

        /// <summary>
        /// Lists the contents of the archive.
        /// </summary>
        void ListArchive()
        {
            using(MpqArchive archive = new MpqArchive(archiveFile))
            {
                // setup external listfile if specified
                if(listFile != null && listFile != "")
                    archive.ExternalListFile = listFile;

                Console.WriteLine("ucmp. size   cmp. size   ratio   cmp. type   filename");
                Console.WriteLine("----------   ---------   -----   ---------   --------");

                for(int i = 0; i < archive.Files.Length; i++)
                {
                    MpqArchive.FileInfo fi = archive.Files[i];

                    // match pattern
                    if(regex != null && !regex.Match(fi.Name).Success)
                        continue;

                    string srcFile = fi.Name;
                    if(stripPath)
                        srcFile = Path.GetFileName(srcFile);
                    
                    // display info
                    Console.WriteLine("{0, 10}   {1, 9}   {2, 5}   {3, 9}   {4}",
                                      fi.UncompressedSize, fi.CompressedSize,
                                      CompressionRatioString(fi.UncompressedSize, fi.CompressedSize),
                                      CompressionTypeString(fi.Flags), srcFile);
                }
            }
        }

        /// <summary>
        /// Extracts files from the archive matching the pattern (if any)
        /// </summary>
        void ExtractArchive()
        {
            using(MpqArchive archive = new MpqArchive(archiveFile))
            {
                // destination directory
                if(destDir == null || destDir == "")
                    destDir = Directory.GetCurrentDirectory();    // default to current dir of not specified

                // setup external listfile if specified
                if(listFile != null && listFile != "")
                    archive.ExternalListFile = listFile;

                // buffers
                byte[] buf = new byte[0x40000];

                if(!quietOutput)
                    Console.WriteLine("Extracting to {0}", destDir);

                for(int i = 0; i < archive.Files.Length; i++)
                {
                    MpqArchive.FileInfo fi = archive.Files[i];

                    // WoW contains a lot of files with Flags & FileHasMetadata
                    //if((fi.Flags & MpqFileFlags.FileHasMetadata) != 0)
                    //    continue;
                    // WoW contains a lot of zero length files with Flags & Unknown_02000000
                    //if((fi.Flags & MpqFileFlags.Unknown_02000000) != 0)
                    //    continue;
                    
                    // match pattern
                    if(regex != null && !regex.Match(fi.Name).Success)
                        continue;

                    if(!quietOutput)
                        Console.Write(fi.Name + " .. ");

                    // create destination directory
                    string srcFile = fi.Name;
                    if(stripPath)
                        srcFile = Path.GetFileName(srcFile);
                   
                    string destFile = Path.Combine(destDir, srcFile);
                    string absDestDir = Path.GetDirectoryName(destFile);
                    CreateDirectory(absDestDir);

                    // copy to destination file
                    using(Stream stmIn = archive.OpenFile(fi.Name))
                    {
                        using(Stream stmOut = new FileStream(destFile, FileMode.Create))
                        {
                            while(true)
                            {
                                int cb = stmIn.Read(buf, 0, buf.Length);
                                if(cb == 0)
                                    break;

                                stmOut.Write(buf, 0, cb);
                            }

                            stmOut.Close();
                        }
                    }

                    if(!quietOutput)
                        Console.WriteLine("Done.");
                }
            }
        }

        /// <summary>
        /// Creates the directory.
        /// </summary>
        /// <param name="dir">The dir.</param>
        void CreateDirectory(string dir)
        {
            // Create destination directory
            string[] dirs = dir.Split(Path.DirectorySeparatorChar);

            if(dirs.Length > 1)
            {
                string absdir = dirs[0];

                for(int i = 1; i < dirs.Length; i++)
                {
                    absdir = absdir + Path.DirectorySeparatorChar + dirs[i];

                    if(!Directory.Exists(absdir))
                        Directory.CreateDirectory(absdir);
                }
            }
        }

        /// <summary>
        /// Builds a string with usage information
        /// </summary>
        /// <returns></returns>
        string BuildUsage()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Usage: mpqtool <command> <archive> [option]\n");
            sb.Append("\n");
            sb.Append("Commands:\n");
            sb.Append("\n");
            sb.Append("Extract files from the given mpq archive.\n");
            sb.Append("  -e, --extract [wildcard] [-d<destDir>] [-r<regex>] [-sp] [-q] [-l<listFile>]\n");
            sb.Append("\n");
            sb.Append("List the contents of a mpq archive.\n");
            sb.Append("  -l, --list [wildcard] [-r<regex>]  [-sp] [-q] [-l<listFile>]\n");
            sb.Append("\n");
            sb.Append("Display this help and exit\n");
            sb.Append("  -h, --help\n");
            sb.Append("\n");
            sb.Append("\n");
            sb.Append("Options:\n");
            sb.Append("\n");
            sb.Append("  <wildcard>\tStandard wildcard (*.mpq)\n");
            sb.Append("  -sp\t\tStrips path information from destination file.\n");
            sb.Append("  -d\t\tSpecifies the output directory.\n");
            sb.Append("  -l\t\tSpecifies a custom list file.\n");
            sb.Append("  -q\t\tSuppress console output.\n");
            
            return sb.ToString();
        }


        /// <summary>
        /// Builds a compressions ratio string.
        /// </summary>
        /// <param name="CompressedSize">Size compressed.</param>
        /// <param name="UncompressedSize">Size uncompressed.</param>
        /// <returns></returns>
        string CompressionRatioString(double CompressedSize, double UncompressedSize)
        {
            if(UncompressedSize == 0)
                return "0%";

            double Delta = CompressedSize - UncompressedSize;
            return string.Format("{0:#0}%", Delta / (UncompressedSize / 100));
        }

        /// <summary>
        /// Builds a compressions type string
        /// </summary>
        /// <param name="Flags">The flags.</param>
        /// <returns></returns>
        string CompressionTypeString(MpqFileFlags Flags)
        {
            if((Flags & MpqFileFlags.CompressedMulti) != 0)
                return "Multi";

            if((Flags & MpqFileFlags.CompressedPK) != 0)
                return "PKZip";

            return "";
        }

        /// <summary>
        /// Converts wildcards to Regex
        /// </summary>
        /// <param name="wildcard">wildcard.</param>
        /// <returns></returns>
        public string wildcardToRegexp(string wildcard)
        {
            StringBuilder s = new StringBuilder(wildcard.Length);

            //s.Append('^');
            for(int i = 0; i < wildcard.Length; i++)
            {
                char c = wildcard[i];
                switch(c)
                {
                    case '*':
                        s.Append('.');
                        s.Append('*');
                        break;
                    case '?':
                        s.Append('.');
                        break;

                    // escape special regexp-characters
                    case '(':
                    case ')':
                    case '[':
                    case ']':
                    case '$':
                    case '^':
                    case '.':
                    case '{':
                    case '}':
                    case '|':
                    case '\\':
                        s.Append('\\');
                        s.Append(c);
                        break;
                    default:
                        s.Append(c);
                        break;
                }
            }

            s.Append('$');
            return s.ToString();
        }
        
        /// <summary>
        /// Mains entry point
        /// </summary>
        /// <param name="args">The args.</param>
        static void Main(string[] args)
        {
            App app = new App();
            app.Run(args);
        }
    }
}
