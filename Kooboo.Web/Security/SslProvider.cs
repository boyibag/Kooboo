//Copyright (c) 2018 Yardi Technology Limited. Http://www.kooboo.com 
//All rights reserved.
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Kooboo.HttpServer;

namespace Kooboo.Web.Security
{
    public class SslProvider : Kooboo.HttpServer.ISslCertificateProvider
    {  
        public SslProvider()
        {
            diskcache = LoadDisk(); 
        }

        private Dictionary<string, X509Certificate2> diskcache = new Dictionary<string, X509Certificate2>(StringComparer.OrdinalIgnoreCase);


        private Dictionary<string, X509Certificate2> cache = new Dictionary<string, X509Certificate2>(StringComparer.OrdinalIgnoreCase);

        X509Certificate ISslCertificateProvider.SelectCertificate(string hostName)
        {
            if (hostName != null)
            {
                if (cache.ContainsKey(hostName))
                {
                    var item = cache[hostName];
                    if (item.NotAfter > DateTime.Now)
                    {
                        return item;
                    }
                }

                var cert = Kooboo.Data.GlobalDb.SslCertificate.GetByDomain(hostName);

                if (cert != null)
                {
                    var certificate = TryGetCert(cert.Content);  
                    if (certificate != null)
                    {
                        cache[hostName] = certificate;
                    }

                    return certificate;
                }

                if (diskcache.ContainsKey(hostName))
                {
                    var diskcert = diskcache[hostName]; 
                    if (diskcert !=null)
                    {
                        cache[hostName] = diskcert;
                    }   
                    return diskcert; 
                }    
            }

            return Kooboo.Data.Server.SslCertificate.DefaultCert;
            // return null;    
        }

        public Dictionary<string, X509Certificate2> LoadDisk()
        {  
            Dictionary<string, X509Certificate2> result = new Dictionary<string, X509Certificate2>(StringComparer.OrdinalIgnoreCase);
         
            string folder = Kooboo.Data.AppSettings.RootPath;
            folder = System.IO.Path.Combine(folder, "AppData"); 
            folder = Lib.Helper.IOHelper.CombinePath(folder, "ssl");

            Kooboo.Lib.Helper.IOHelper.EnsureDirectoryExists(folder);

            var allfiles = System.IO.Directory.GetFiles(folder, "*.pfx");

            foreach (var item in allfiles)
            {             
                var info = new System.IO.FileInfo(item);   
                string hostname = info.Name;   

                var allbytes = System.IO.File.ReadAllBytes(item);
                var cert = TryGetCert(allbytes);  
                if (cert !=null)
                {
                    string hostName = cert.GetNameInfo(X509NameType.DnsName, false);

                    result[hostName] = cert; 
                }
            }             
            return result; 

        }

        public static X509Certificate2 TryGetCert(byte[] content)
        {
            List<string> trypass = new List<string>();
            trypass.Add("kooboo");
            trypass.Add("");

            foreach (var item in trypass)
            {
                try
                {
                    var cert = new X509Certificate2(content, item);
                    if (cert !=null)
                    {
                        return cert; 
                    }
                }
                catch (Exception)
                { 
                }
   
            }

            return null; 

        }

    }
}
