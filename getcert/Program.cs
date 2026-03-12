using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace getcert
{
    internal class Program
    {
        private static bool getChain;
        private static bool infoOnly;
        private static string savePath = string.Empty;
        private static string alias = "certificate";

        private static void Main(string[] args)
        {
            if (!TryParseArgs(args, out var options, out var parseError))
            {
                PrintUsage(parseError);
#if DEBUG
                Console.ReadLine();
#endif
                return;
            }

            checkAndRun(options);

#if DEBUG
            Console.ReadLine();
#endif
        }

        private static bool TryParseArgs(string[] args, out getCertOptions options, out string error)
        {
            options = new getCertOptions();
            error = string.Empty;
            var directorySpecified = false;
            var aliasSpecified = false;

            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];

                if (arg == "-h" || arg == "--help")
                {
                    error = string.Empty;
                    return false;
                }

                if (arg == "-c" || arg == "--chain")
                {
                    options.Chain = true;
                    continue;
                }

                if (arg == "-i" || arg == "--info")
                {
                    options.Info = true;
                    continue;
                }

                if (!TryGetOptionValue(args, ref i, out var value))
                {
                    error = $"Missing value for option '{arg}'.";
                    return false;
                }

                if (arg == "-u" || arg == "--url")
                {
                    options.Url = value;
                }
                else if (arg == "-d" || arg == "--dir")
                {
                    options.Directory = value;
                    directorySpecified = true;
                }
                else if (arg == "-a" || arg == "--alias")
                {
                    options.Alias = value;
                    aliasSpecified = true;
                }
                else
                {
                    error = $"Unknown option '{arg}'.";
                    return false;
                }
            }

            if (string.IsNullOrWhiteSpace(options.Url))
            {
                error = "Option -u|--url is required.";
                return false;
            }

            if (options.Info && (directorySpecified || aliasSpecified))
            {
                Console.WriteLine("WARNING: -d|--dir and -a|--alias are ignored when -i|--info is used.\n");
                options.Directory = string.Empty;
                options.Alias = string.Empty;
            }

            return true;
        }

        private static bool TryGetOptionValue(string[] args, ref int index, out string value)
        {
            value = string.Empty;

            var nextIndex = index + 1;
            if (nextIndex >= args.Length)
            {
                return false;
            }

            value = args[nextIndex];
            index = nextIndex;
            return true;
        }

        private static void PrintUsage(string error)
        {
            Console.WriteLine("getcert - Export TLS certificate(s) from an HTTPS endpoint");
            Console.WriteLine("Version {0}", GetProgramVersion());
            Console.WriteLine();
            Console.WriteLine("Usage: getcert -u|--url <url> [-c|--chain] [-i|--info] [-d|--dir <path>] [-a|--alias <name>]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  -u, --url     Required HTTPS URL or host.");
            Console.WriteLine("  -c, --chain   Export full certificate chain.");
            Console.WriteLine("  -i, --info    Print certificate info only.");
            Console.WriteLine("  -d, --dir     Output directory for certificate files (ignored with -i|--info).");
            Console.WriteLine("  -a, --alias   Output file base name. Default: [certificate] (ignored with -i|--info).");
            Console.WriteLine("  -h, --help    Show help.");

            if (!string.IsNullOrWhiteSpace(error))
            {
                Console.WriteLine();
                Console.WriteLine($"ERROR: {error}");
            }
        }

        private static string GetProgramVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var fileVersion = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>();

            return fileVersion?.Version ?? assembly.GetName().Version?.ToString() ?? string.Empty;
        }

        private static void SaveCertificate(Uri uri)
        {
            var request = (HttpWebRequest)WebRequest.Create(uri);
            request.AllowAutoRedirect = false;
            request.ServerCertificateValidationCallback = ServerCertificateValidationCallback;

            using (var response = (HttpWebResponse)request.GetResponse())
            {
            }
        }

        private static void checkAndRun(getCertOptions options)
        {
            try
            {
                if (!checkAndValidateUrl(options.Url, out var newUri))
                {
                    throw new Exception("Url provided seems to be invalid");
                }

                getChain = options.Chain;
                infoOnly = options.Info;
                alias = string.IsNullOrWhiteSpace(options.Alias) ? "certificate" : options.Alias;
                savePath = string.Empty;

                if (!infoOnly && !string.IsNullOrWhiteSpace(options.Directory))
                {
                    if (!checkAndValidatePath(options.Directory))
                    {
                        throw new Exception("Directory provided is not valid or doesn't exist");
                    }

                    savePath = options.Directory;
                }

                if (!infoOnly && !IsFileNameCorrect(alias))
                {
                    throw new Exception("Filename provided seems to be not valid");
                }

                SaveCertificate(newUri);
            }
            catch (Exception x)
            {
                if (x is WebException webEx && webEx.Response is HttpWebResponse httpWebResponse)
                {
                    if (httpWebResponse.StatusCode == HttpStatusCode.NotFound)
                    {
                        return;
                    }
                }

                Console.WriteLine();
                Console.WriteLine($"ERROR(S): {x.Message}");
            }
        }

        public static string ExportToPEM(X509Certificate2 cert)
        {
            try
            {
                var builder = new StringBuilder();
                builder.AppendLine("-----BEGIN CERTIFICATE-----");
                builder.AppendLine(Convert.ToBase64String(cert.Export(X509ContentType.Cert), Base64FormattingOptions.InsertLineBreaks));
                builder.AppendLine("-----END CERTIFICATE-----");
                return builder.ToString();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to export certificate to PEM.", ex);
            }
        }

        private static bool ServerCertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            var counter = 0;

            foreach (var cer in chain.ChainElements)
            {
                printCertificateInfo(counter, cer);

                if (!infoOnly)
                {
                    var cert = ExportToPEM(cer.Certificate);

                    Console.WriteLine(cert);

                    if (!string.IsNullOrWhiteSpace(savePath))
                    {
                        var fullName = saveCertificate(counter, cert);

                        Console.WriteLine($"Certificate saved to file {fullName}");
                    }
                }

                Console.WriteLine();

                if (counter == 0 && !getChain)
                {
                    break;
                }

                counter++;
            }

            return true;
        }

        private static bool checkAndValidateUrl(string url, out Uri newUri)
        {
            newUri = null;

            if (string.IsNullOrWhiteSpace(url))
            {
                return false;
            }

            var candidate = url.Trim();

            if (Uri.TryCreate(candidate, UriKind.Absolute, out var absoluteUri))
            {
                if (!Uri.UriSchemeHttps.Equals(absoluteUri.Scheme, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(absoluteUri.Host))
                {
                    return false;
                }

                newUri = absoluteUri;
                return true;
            }

            candidate = $"https://{candidate}";

            if (!Uri.TryCreate(candidate, UriKind.Absolute, out var parsedUri))
            {
                return false;
            }

            if (!Uri.UriSchemeHttps.Equals(parsedUri.Scheme, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(parsedUri.Host))
            {
                return false;
            }

            newUri = parsedUri;
            return true;
        }

        private static bool checkAndValidatePath(string path)
        {
            return Directory.Exists(path);
        }

        private static bool IsFileNameCorrect(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return false;
            }

            if (fileName.Any(f => Path.GetInvalidFileNameChars().Contains(f)))
            {
                return false;
            }

            if (fileName.EndsWith(" ", StringComparison.Ordinal) || fileName.EndsWith(".", StringComparison.Ordinal))
            {
                return false;
            }

            return !IsReservedWindowsFileName(fileName);
        }

        private static bool IsReservedWindowsFileName(string fileName)
        {
            var normalizedName = Path.GetFileNameWithoutExtension(fileName.Trim());
            var reservedNames = new[]
            {
                "CON", "PRN", "AUX", "NUL",
                "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
                "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
            };

            return reservedNames.Contains(normalizedName, StringComparer.OrdinalIgnoreCase);
        }

        private static void printCertificateInfo(int chainOrder, X509ChainElement cer)
        {
            Console.WriteLine($"Chain: {chainOrder}");
            Console.WriteLine($"Subject: {cer.Certificate.SubjectName.Name}");
            Console.WriteLine($"Issuer: {cer.Certificate.IssuerName.Name}");
            Console.WriteLine($"Valid from:{cer.Certificate.GetEffectiveDateString()}");
            Console.WriteLine($"Valid to:{cer.Certificate.GetExpirationDateString()}");
            Console.WriteLine($"Serial No:{cer.Certificate.SerialNumber}");
        }

        private static string saveCertificate(int chainOrder, string pemString)
        {
            var fullName = Path.Combine(savePath, string.Format("{0}-{1}.crt", alias, chainOrder));
            File.WriteAllText(fullName, pemString);

            return fullName;
        }
    }
}
