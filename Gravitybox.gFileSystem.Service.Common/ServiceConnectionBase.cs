using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Gravitybox.gFileSystem.Service.Common
{
    /// <summary>
    /// This is a client side facade that acts as a native file system interface
    /// </summary>
    public abstract class ServiceConnectionBase
    {
        protected string _server = null;
        protected int _port = 0;

        public delegate void FileUploadEventHandler(object sender, FileProgressEventArgs e);

        public event FileUploadEventHandler FileUpload;
        public event FileUploadEventHandler FileDownload;

        protected virtual void OnFileUpload(FileProgressEventArgs e)
        {
            if (this.FileUpload != null)
                this.FileUpload(this, e);
        }

        protected virtual void OnFileDownload(FileProgressEventArgs e)
        {
            if (this.FileDownload != null)
                this.FileDownload(this, e);
        }

        public ServiceConnectionBase(string server = "localhost", int port = 1900)
        {
            _server = server;
            _port = port;
        }

        /// <summary>
        /// The folder where temp operations are performed
        /// </summary>
        public virtual string WorkingFolder { get; set; } = Path.GetTempPath();

        protected ChannelFactory<ISystemCore> GetFactory(string serverName)
        {
            return GetFactory(serverName, 1900);
        }

        protected ChannelFactory<ISystemCore> GetFactory(string serverName, int port)
        {
            //var myBinding = new CompressedNetTcpBinding() { MaxBufferSize = 10 * 1024 * 1024, MaxReceivedMessageSize = 10 * 1024 * 1024, MaxBufferPoolSize = 10 * 1024 * 1024 };
            var myBinding = new NetTcpBinding() { MaxBufferSize = 10 * 1024 * 1024, MaxReceivedMessageSize = 10 * 1024 * 1024, MaxBufferPoolSize = 10 * 1024 * 1024 };
            myBinding.ReaderQuotas.MaxStringContentLength = 10 * 1024 * 1024;
            myBinding.ReaderQuotas.MaxBytesPerRead = 10 * 1024 * 1024;
            myBinding.ReaderQuotas.MaxArrayLength = 10 * 1024 * 1024;
            myBinding.ReaderQuotas.MaxDepth = 10 * 1024 * 1024;
            myBinding.ReaderQuotas.MaxNameTableCharCount = 10 * 1024 * 1024;
            myBinding.Security.Mode = SecurityMode.None;
            var myEndpoint = new EndpointAddress("net.tcp://" + serverName + ":" + port + "/__gfile");
            return new ChannelFactory<ISystemCore>(myBinding, myEndpoint);
        }
    }
}