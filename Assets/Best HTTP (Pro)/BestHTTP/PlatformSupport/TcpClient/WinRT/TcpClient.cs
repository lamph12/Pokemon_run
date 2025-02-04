﻿#if (NETFX_CORE || BUILD_FOR_WP8) && !UNITY_EDITOR && !ENABLE_IL2CPP
using System;
using Windows.Networking;
using Windows.Networking.Sockets;

namespace BestHTTP.PlatformSupport.TcpClient.WinRT
{
    public sealed class TcpClient : IDisposable
    {
#region Public Properties

        public bool Connected { get; private set; }
        public TimeSpan ConnectTimeout { get; set; }

        public bool UseHTTPSProtocol { get; set; }

#endregion

#region Private Properties

        internal StreamSocket Socket { get; set; }
        private System.IO.Stream Stream { get; set; }

#endregion

        public TcpClient()
        {
            ConnectTimeout = TimeSpan.FromSeconds(2);
        }

        public void Connect(string hostName, int port)
        {
            //How to secure socket connections with TLS/SSL:
            //http://msdn.microsoft.com/en-us/library/windows/apps/jj150597.aspx

            //Networking in Windows 8 Apps - Using StreamSocket for TCP Communication
            //http://blogs.msdn.com/b/metulev/archive/2012/10/22/networking-in-windows-8-apps-using-streamsocket-for-tcp-communication.aspx

            Socket = new StreamSocket();
            Socket.Control.KeepAlive = true;

            var host = new HostName(hostName);

            SocketProtectionLevel spl = SocketProtectionLevel.PlainSocket;
            if (UseHTTPSProtocol)
                spl = SocketProtectionLevel.
#if UNITY_WSA_8_0 || BUILD_FOR_WP8
                        Ssl;
#else
                        Tls12;
#endif

            // https://msdn.microsoft.com/en-us/library/windows/apps/xaml/jj710176.aspx#content
            try
            {
                var result = Socket.ConnectAsync(host, UseHTTPSProtocol ? "https" : port.ToString(), spl);

                if (ConnectTimeout > TimeSpan.Zero)
                    Connected = result.AsTask().Wait(ConnectTimeout);
                else
                    Connected = result.AsTask().Wait(TimeSpan.FromMilliseconds(-1));
            }
            catch(AggregateException ex)
            {
                if (ex.InnerException != null)
                    //throw ex.InnerException;
                {
                    if ( ex.Message.Contains("No such host is known") || ex.Message.Contains("unreachable host") )
                        throw new Exception("Socket Exception");
                    else
                        throw ex.InnerException;
                }
                else
                    throw ex;
            }

            if (!Connected)
                throw new TimeoutException("Connection timed out!");
        }

        public bool IsConnected()
        {
            return true;
        }

        public System.IO.Stream GetStream()
        {
            if (Stream == null)
                Stream = new DataReaderWriterStream(this);
            return Stream;
        }

        public void Close()
        {
            Dispose();
        }

#region IDisposeble

        private bool disposed = false;

        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (Stream != null)
                        Stream.Dispose();
                    Stream = null;
                    Connected = false;
                }
                disposed = true;
            }
        }

        ~TcpClient()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

#endregion
    }
}

#endif