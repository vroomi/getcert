using System;

namespace getcert
{
    public class getCertOptions
    {
        public string Url { get; set; } = string.Empty;

        public string FilePath { get; set; } = string.Empty;

        public string Format { get; set; } = "pem";

        public bool Chain { get; set; }

        public bool Info { get; set; }

        public string Directory { get; set; } = string.Empty;

        public string Alias { get; set; } = "certificate";
    }
}
