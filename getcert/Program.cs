using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using CommandLine;
using CommandLine.Text;

using System.Text.RegularExpressions;

namespace getcert
{
    internal class Program
    {
        static bool getChain = false;
        static bool infoOnly = false;
        static string savePath = "";
        static string alias = "";

        static void Main(string[] args)
        {
                var parser = new CommandLine.Parser(with => with.HelpWriter = null);

            var parserResult = parser.ParseArguments<getCertOptions>(args);
            parserResult
                .WithParsed(getCertOptions => checkAndRun(getCertOptions))
                .WithNotParsed(errs => DisplayHelp(parserResult, errs));







#if DEBUG
            Console.ReadLine();
#endif
        }

        private static void SaveCertificate(Uri uri)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
                request.AllowAutoRedirect = false;
                request.ServerCertificateValidationCallback = ServerCertificateValidationCallback;

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                response.Close();

                //                X509Certificate2 cert = new X509Certificate2(request.ServicePoint.Certificate);

                //                File.WriteAllText(_path, ExportToPEM(cert));
            }
            catch (Exception)
            {
            }
        }

        private static void checkAndRun(getCertOptions options)
        {
            Uri newUri = null;

            try
            {

                if (!checkAndValidateUrl(options.Url, out newUri))
                {
                    throw new Exception("Url provided seems to be invalid");
                }

                getChain = options.Chain;
                infoOnly = options.Info;

                if (options.Directory != "")
                {
                    if (!checkAndValidatePath(options.Directory))
                    {
                        throw new Exception("Directory provided is not valid or doesn't exist");
                    }
                    else
                    {
                        savePath = options.Directory;
                    }
                }

                if (options.Alias != "")
                {
                    if (!IsFileNameCorrect(options.Alias))
                    {
                        throw new Exception("Filename provided seems to be not valid");
                    }
                    else
                    {
                        alias = options.Alias;
                    }
                }

                SaveCertificate(newUri);
            }
            catch(Exception x)
            {
                Console.WriteLine(x.Message);
            }
        }


        public static string ExportToPEM(X509Certificate2 _cert)
        {
            StringBuilder builder = new StringBuilder();

            try
            {
                builder.AppendLine("-----BEGIN CERTIFICATE-----");
                builder.AppendLine(Convert.ToBase64String(_cert.Export(X509ContentType.Cert), Base64FormattingOptions.InsertLineBreaks));
                builder.AppendLine("-----END CERTIFICATE-----");

            }
            catch (Exception)
            {
            }

            return builder.ToString();
        }

        private static bool ServerCertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            int counter = 0;

            foreach (var cer in chain.ChainElements)
            {
                Console.WriteLine($"Chain: {counter}");

                printCertificateInfo(cer);

                if (infoOnly == false)
                {
                    var cert = ExportToPEM(cer.Certificate);

                    Console.WriteLine(cert);

                    if (savePath != "")
                    {
                        string fullName = Path.Combine(savePath, string.Format("{0}-{1}.crt", alias, counter));
                        File.WriteAllText(fullName, cert);

                        Console.WriteLine($"Saved to file {fullName}");
                    }
                }
                Console.WriteLine();

                if (counter == 0 && getChain == false)
                    break;

                counter++;
            }

            return true;
        }

        private static bool checkAndValidateUrl(string url, out Uri newUri)
        {
            newUri = null;

            try
            {
                // Unfortunately System.Uri class seems to not parse and check always well => try to use regex first
                var patternUrl = @"^((https)\:\/\/)?([0-9a-zA-Z]([-.\w]*[0-9a-zA-Z])*)(:(0-9)*)*(\/?)([a-zA-Z0-9\-\.\?\,\'\/\\\+&amp;%\$#_]*)?$";

                //@"^(?:http(s)?:\/\/)?[\w.-]+(?:\.[\w\.-]+)+[\w\-\._~:/?#[\]@!\$&'\(\)\*\+,;=.]+$"

                Regex Rgx = new Regex(patternUrl, RegexOptions.Compiled | RegexOptions.IgnoreCase);

                var matches = Rgx.Matches(url);

                if (matches is null || matches.Count == 0)
                    return false;

                if (matches[0].Groups[2].Value == "")
                {
                    url = "https://" + url;
                }

                if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
                    return false;

                if (!Uri.TryCreate(url, UriKind.Absolute, out newUri) || newUri.Scheme != Uri.UriSchemeHttps)
                    return false;
            }
            catch (Exception x)
            {
                return false;
            }

            return true;
        }

        private static bool checkAndValidatePath(string path)
        {
            if (!Directory.Exists(path))
                return false;

            return true;
        }

        static bool IsFileNameCorrect(string fileName)
        {
            return !fileName.Any(f => Path.GetInvalidFileNameChars().Contains(f));
        }

        static void DisplayHelp<T>(ParserResult<T> result, IEnumerable<Error> errs)
        {
            HelpText helpText = null;
            if (errs.IsVersion())  //check if error is version request
                helpText = HelpText.AutoBuild(result);
            else
            {
                foreach( var err in errs)
                {
                    
                }
                
                helpText = HelpText.AutoBuild(result, h =>
                {                    
                    return HelpText.DefaultParsingErrorsHandler(result, h);
                }, e => e);
            }
            Console.WriteLine(helpText);
        }

        static void printCertificateInfo(X509ChainElement cer)
        {            
            Console.WriteLine($"Subject: {cer.Certificate.SubjectName.Name}");
            Console.WriteLine($"Issuer: {cer.Certificate.IssuerName.Name}");
            Console.WriteLine($"Valid from:{cer.Certificate.GetEffectiveDateString()}");
            Console.WriteLine($"Valid to:{cer.Certificate.GetExpirationDateString()}");
            Console.WriteLine($"Serial No:{cer.Certificate.SerialNumber}");
        }

    }


}
