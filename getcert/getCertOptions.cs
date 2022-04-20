using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using System.IO;

namespace getcert
{
    public class getCertOptions
    {        
        [Value(0, Required = true, MetaName = "Url", HelpText = "Url to get certificates from")]
        public string Url{ get; set; }

        [Option('c', "chain", Default = false, Required = false, HelpText = "Get all certificates in chain")]
        public bool Chain { get; set; }

        [Option('i', "info", Default = false, Required = false, HelpText = "Get certificate info only")]
        public bool Info { get; set; }

        [Option('d', "dir", Default = "", Required = false, HelpText = "Directory to save certificates to")]
        public string Directory 
        {
            get; set;
        }

        [Option('a', "alias", Default = "", Required = false, HelpText = "Filename to save certificate(s)")]
        public string Alias { get; set; }

        private static bool checkAndValidateDirectory(string dir)
        {
             if (dir != "" && !System.IO.Directory.Exists(dir))
                return false;

            return true;
        }

    }
}
