using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;

namespace getcert
{
    internal class Program
    {
        private static bool getChain;
        private static bool infoOnly;
        private static string savePath = string.Empty;
        private static string alias = "certificate";
        private const string GetCommand = "get";
        private const string ViewCommand = "view";
        private const string PemFormat = "pem";
        private const string DerFormat = "der";
        private const string Pkcs12Format = "pkcs12";
        private const string MissingCommandErrorMessage = "Command is required.";

        private static void Main(string[] args)
        {
            if (!TryParseRootCommand(args, out var command, out var commandArgs, out var parseError))
            {
                PrintRootUsage(parseError);
#if DEBUG
                Console.ReadLine();
#endif
                return;
            }

            if (!TryParseCommandArgs(command, commandArgs, out var options, out parseError))
            {
                PrintCommandUsage(command, parseError);
#if DEBUG
                Console.ReadLine();
#endif
                return;
            }

            CheckAndRun(command, options);

#if DEBUG
            Console.ReadLine();
#endif
        }

        private static bool TryParseRootCommand(string[] args, out string command, out string[] commandArgs, out string error)
        {
            command = string.Empty;
            commandArgs = Array.Empty<string>();
            error = string.Empty;

            if (args.Length == 0)
            {
                error = MissingCommandErrorMessage;
                return false;
            }

            var candidate = args[0];
            if (candidate == "-h" || candidate == "--help")
            {
                return false;
            }

            if (!string.Equals(candidate, GetCommand, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(candidate, ViewCommand, StringComparison.OrdinalIgnoreCase))
            {
                error = $"Unknown command '{candidate}'.";
                return false;
            }

            command = candidate;
            commandArgs = args.Skip(1).ToArray();
            return true;
        }

        private static bool TryParseCommandArgs(string command, string[] args, out getCertOptions options, out string error)
        {
            if (string.Equals(command, GetCommand, StringComparison.OrdinalIgnoreCase))
            {
                return TryParseGetArgs(args, out options, out error);
            }

            if (string.Equals(command, ViewCommand, StringComparison.OrdinalIgnoreCase))
            {
                return TryParseViewArgs(args, out options, out error);
            }

            options = new getCertOptions();
            error = $"Unknown command '{command}'.";
            return false;
        }

        private static bool TryParseGetArgs(string[] args, out getCertOptions options, out string error)
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

                if (!arg.StartsWith("-", StringComparison.Ordinal))
                {
                    if (!string.IsNullOrWhiteSpace(options.Url))
                    {
                        error = $"Unexpected argument '{arg}'.";
                        return false;
                    }

                    options.Url = arg;
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
                error = "URL argument is required.";
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

        private static bool TryParseViewArgs(string[] args, out getCertOptions options, out string error)
        {
            options = new getCertOptions();
            error = string.Empty;

            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];

                if (arg == "-h" || arg == "--help")
                {
                    error = string.Empty;
                    return false;
                }

                if (!arg.StartsWith("-", StringComparison.Ordinal))
                {
                    if (!string.IsNullOrWhiteSpace(options.FilePath))
                    {
                        error = $"Unexpected argument '{arg}'.";
                        return false;
                    }

                    options.FilePath = arg;
                    continue;
                }

                if (!TryGetOptionValue(args, ref i, out var value))
                {
                    error = $"Missing value for option '{arg}'.";
                    return false;
                }

                if (arg == "-f" || arg == "--format")
                {
                    options.Format = value;
                }
                else
                {
                    error = $"Unknown option '{arg}'.";
                    return false;
                }
            }

            if (string.IsNullOrWhiteSpace(options.FilePath))
            {
                error = "File argument is required.";
                return false;
            }

            if (!IsSupportedFormat(options.Format))
            {
                error = $"Invalid format '{options.Format}'. Supported values: {PemFormat}, {DerFormat}, {Pkcs12Format}.";
                return false;
            }

            options.Format = NormalizeFormat(options.Format);
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

        private static void PrintRootUsage(string error)
        {
            Console.WriteLine("getcert - Export TLS certificate(s) from an HTTPS endpoint.");
            Console.WriteLine("Version {0}", GetProgramVersion());
            Console.WriteLine();
            Console.WriteLine("Usage: getcert <command> [options]");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("  get           Fetch certificate(s) from an HTTPS endpoint.");
            Console.WriteLine("  view          Display certificate info from a file.");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  -h, --help    Show help.");

            if (!string.IsNullOrWhiteSpace(error))
            {
                Console.WriteLine();
                Console.WriteLine($"ERROR: {error}");
                if (string.Equals(error, MissingCommandErrorMessage, StringComparison.Ordinal))
                {
                    Console.WriteLine("Try 'getcert get -h' or 'getcert view -h' for command-specific help.");
                }
            }
        }

        private static void PrintCommandUsage(string command, string error)
        {
            if (string.Equals(command, ViewCommand, StringComparison.OrdinalIgnoreCase))
            {
                PrintViewUsage(error);
                return;
            }

            if (!string.Equals(command, GetCommand, StringComparison.OrdinalIgnoreCase))
            {
                PrintRootUsage(error);
                return;
            }

            Console.WriteLine("getcert get - Fetch TLS certificate(s) from an HTTPS endpoint.");
            Console.WriteLine("Version {0}", GetProgramVersion());
            Console.WriteLine();
            Console.WriteLine("Usage: getcert get <url> [-c|--chain] [-i|--info] [-d|--dir <path>] [-a|--alias <name>]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  <url>         Required HTTPS URL or host.");
            Console.WriteLine("  -c, --chain   Export full certificate chain.");
            Console.WriteLine("  -i, --info    Print certificate info only.");
            Console.WriteLine("  -d, --dir     Output directory for certificate files (ignored with -i|--info).");
            Console.WriteLine("  -a, --alias   Output file base name. Default: [certificate] (ignored with -i|--info).");
            Console.WriteLine("  -h, --help    Show help.");
            Console.WriteLine();
            Console.WriteLine("Compatibility:");
            Console.WriteLine("  -u, --url     Accepted as a legacy alternative for <url>.");

            if (!string.IsNullOrWhiteSpace(error))
            {
                Console.WriteLine();
                Console.WriteLine($"ERROR: {error}");
                if (error.StartsWith("Unknown option", StringComparison.Ordinal))
                {
                    Console.WriteLine("Use 'getcert -h' to list available commands.");
                    Console.WriteLine($"Use 'getcert {command} -h' to list available options.");
                }
            }
        }

        private static void PrintViewUsage(string error)
        {
            Console.WriteLine("getcert view - Display certificate info from a file.");
            Console.WriteLine("Version {0}", GetProgramVersion());
            Console.WriteLine();
            Console.WriteLine("Usage: getcert view <file> [-f|--format <format>]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  <file>            Required certificate file path.");
            Console.WriteLine("  -f, --format      Certificate format. Supported values: pem, der, pkcs12.");
            Console.WriteLine("                    Default: pem. pkcs12 is reserved and not supported yet.");
            Console.WriteLine("  -h, --help        Show help.");

            if (!string.IsNullOrWhiteSpace(error))
            {
                Console.WriteLine();
                Console.WriteLine($"ERROR: {error}");
                if (error.StartsWith("Unknown option", StringComparison.Ordinal))
                {
                    Console.WriteLine("Use 'getcert -h' to list available commands.");
                    Console.WriteLine($"Use 'getcert {ViewCommand} -h' to list available options.");
                }
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

        private static void CheckAndRun(string command, getCertOptions options)
        {
            try
            {
                if (string.Equals(command, ViewCommand, StringComparison.OrdinalIgnoreCase))
                {
                    ViewCertificates(options);
                    return;
                }

                if (!CheckAndValidateUrl(options.Url, out var newUri))
                {
                    throw new Exception("Url provided seems to be invalid");
                }

                getChain = options.Chain;
                infoOnly = options.Info;
                alias = string.IsNullOrWhiteSpace(options.Alias) ? "certificate" : options.Alias;
                savePath = string.Empty;

                if (!infoOnly && !string.IsNullOrWhiteSpace(options.Directory))
                {
                    if (!CheckAndValidatePath(options.Directory))
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
                PrintCertificateInfo(counter, cer.Certificate);

                if (!infoOnly)
                {
                    var cert = ExportToPEM(cer.Certificate);

                    Console.WriteLine(cert);

                    if (!string.IsNullOrWhiteSpace(savePath))
                    {
                        var fullName = SaveCertificate(counter, cert);

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

        private static void ViewCertificates(getCertOptions options)
        {
            if (!CheckAndValidateFile(options.FilePath))
            {
                throw new Exception("File provided is not valid or doesn't exist");
            }

            if (string.Equals(options.Format, Pkcs12Format, StringComparison.OrdinalIgnoreCase))
            {
                throw new NotSupportedException("Format 'pkcs12' is not supported yet.");
            }

            var certificates = LoadCertificates(options.FilePath, options.Format).ToList();
            if (certificates.Count == 0)
            {
                throw new Exception("No certificate was found in the provided file.");
            }

            for (var i = 0; i < certificates.Count; i++)
            {
                PrintCertificateInfo(i, certificates[i]);
                Console.WriteLine();
            }
        }

        private static IEnumerable<X509Certificate2> LoadCertificates(string filePath, string format)
        {
            if (string.Equals(format, DerFormat, StringComparison.OrdinalIgnoreCase))
            {
                return new[] { LoadDerCertificate(filePath) };
            }

            if (string.Equals(format, PemFormat, StringComparison.OrdinalIgnoreCase))
            {
                return LoadPemCertificates(filePath);
            }

            throw new NotSupportedException($"Format '{format}' is not supported.");
        }

        private static X509Certificate2 LoadDerCertificate(string filePath)
        {
            try
            {
                return new X509Certificate2(File.ReadAllBytes(filePath));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to read DER certificate from the provided file.", ex);
            }
        }

        private static IEnumerable<X509Certificate2> LoadPemCertificates(string filePath)
        {
            string content;
            MatchCollection matches;

            try
            {
                content = File.ReadAllText(filePath);
                matches = Regex.Matches(
                    content,
                    "-----BEGIN ([^-]+)-----(.*?)-----END \\1-----",
                    RegexOptions.Singleline | RegexOptions.CultureInvariant);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to read PEM certificate from the provided file.", ex);
            }
            var certificates = new List<X509Certificate2>();
            var ignoredBlockTypes = new List<string>();

            foreach (Match match in matches)
            {
                var blockType = match.Groups[1].Value.Trim();
                var blockBody = match.Groups[2].Value;

                if (!string.Equals(blockType, "CERTIFICATE", StringComparison.OrdinalIgnoreCase))
                {
                    ignoredBlockTypes.Add(blockType);
                    continue;
                }

                try
                {
                    var sanitizedBody = RemovePemWhitespace(blockBody);
                    certificates.Add(new X509Certificate2(Convert.FromBase64String(sanitizedBody)));
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("Failed to read PEM certificate from the provided file.", ex);
                }
            }

            if (ignoredBlockTypes.Count > 0)
            {
                Console.WriteLine(
                    $"WARNING: Ignoring unsupported PEM block(s): {string.Join(", ", ignoredBlockTypes.Distinct(StringComparer.OrdinalIgnoreCase))}.");
                Console.WriteLine();
            }

            return certificates;
        }

        private static bool CheckAndValidateUrl(string url, out Uri newUri)
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

        private static bool CheckAndValidatePath(string path)
        {
            return Directory.Exists(path);
        }

        private static bool CheckAndValidateFile(string path)
        {
            return !string.IsNullOrWhiteSpace(path) && File.Exists(path) && !Directory.Exists(path);
        }

        private static bool IsSupportedFormat(string format)
        {
            var normalizedFormat = NormalizeFormat(format);
            return string.Equals(normalizedFormat, PemFormat, StringComparison.Ordinal)
                || string.Equals(normalizedFormat, DerFormat, StringComparison.Ordinal)
                || string.Equals(normalizedFormat, Pkcs12Format, StringComparison.Ordinal);
        }

        private static string NormalizeFormat(string format)
        {
            return string.IsNullOrWhiteSpace(format) ? PemFormat : format.Trim().ToLowerInvariant();
        }

        private static string RemovePemWhitespace(string value)
        {
            return new string(value.Where(c => !char.IsWhiteSpace(c)).ToArray());
        }

        private static bool IsFileNameCorrect(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return false;
            }

            var invalidChars = Path.GetInvalidFileNameChars();
            if (fileName.IndexOfAny(invalidChars) >= 0)
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
                "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
                "CONIN$", "CONOUT$", "CLOCK$"
            };

            return reservedNames.Contains(normalizedName, StringComparer.OrdinalIgnoreCase);
        }

        private static void PrintCertificateInfo(int chainOrder, X509Certificate2 certificate)
        {
            Console.WriteLine($"Chain: {chainOrder}");
            Console.WriteLine($"Subject: {certificate.SubjectName.Name}");
            Console.WriteLine($"Issuer: {certificate.IssuerName.Name}");
            Console.WriteLine($"Valid from:{certificate.GetEffectiveDateString()}");
            Console.WriteLine($"Valid to:{certificate.GetExpirationDateString()}");
            Console.WriteLine($"Serial No:{certificate.SerialNumber}");
        }

        private static string SaveCertificate(int chainOrder, string pemString)
        {
            var fullName = Path.Combine(savePath, string.Format("{0}-{1}.crt", alias, chainOrder));
            File.WriteAllText(fullName, pemString);

            return fullName;
        }
    }
}
