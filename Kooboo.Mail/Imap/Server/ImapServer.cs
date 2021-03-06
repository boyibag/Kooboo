//Copyright (c) 2018 Yardi Technology Limited. Http://www.kooboo.com 
//All rights reserved.
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

using LumiSoft.Net;
using Newtonsoft.Json;

namespace Kooboo.Mail.Imap
{
    public class ImapServer : Kooboo.Tasks.IWorkerStarter
    {
        private CancellationTokenSource _cancellationTokenSource;
        private TcpListener _listener;

        public ImapServer(int port)
        {
            Port = port;
        }

        public ImapServer(int port, SslMode mode, X509Certificate certificate)
            : this(port)
        {
            SslMode = mode;
            Certificate = certificate;
        }

        public string Name
        {
            get
            {
                return "Imap";
            }
        }

        public int Port { get; set; }

        public string HostName
        {
            get
            {
                return EmailEnvironment.FQDN;
            }
        }

        [JsonIgnore]
        public X509Certificate Certificate { get; private set; }

        public SslMode SslMode { get; private set; }

        public async void Start()
        {
            if (Lib.Helper.NetworkHelper.IsPortInUse(Port))
                return;

            _listener = new TcpListener(new IPEndPoint(IPAddress.Any, Port));
            _listener.Start();

            _cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _cancellationTokenSource.Token;
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var tcpClient = await _listener.AcceptTcpClientAsync();

                    var session = new ImapSession(this, tcpClient);
                    session.Start();
                }
                catch
                {
                }
            }
        }

        public void Stop()
        {
            if (_listener == null)
                return;

            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
            }

            if (_listener != null)
            {
                _listener.Stop();
            }
        }
    }
}
