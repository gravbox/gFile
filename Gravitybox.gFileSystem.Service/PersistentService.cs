using Gravitybox.gFileSystem.Install;
using Gravitybox.gFileSystem.Manager;
using Gravitybox.gFileSystem.Service.Common;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.ServiceProcess;
using System.Text;

namespace Gravitybox.gFileSystem.Service
{
    public partial class PersistentService : ServiceBase
    {
        #region Class Members

        private static ISystemCore _core = null;

        #endregion

        public void Start()
        {
            try
            {
                ConfigHelper.ConnectionString = ConfigurationManager.ConnectionStrings["gFileSystemEntities"].ConnectionString;

                //Test if database connection works
                var a = ConfigHelper.StorageFolder;
            }
            catch(Exception  ex)
            {
                Logger.LogError(ex);
                throw;
            }

            //Master key must be 16 or 32 bit
            if (ConfigHelper.MasterKey == null)
                throw new Exception("Invalid Master Key");
            if (ConfigHelper.MasterKey.Length != 16 && ConfigHelper.MasterKey.Length != 32)
                throw new Exception("Invalid Master Key");

            try
            {
                if (string.IsNullOrEmpty(ConfigHelper.StorageFolder) || !Directory.Exists(ConfigHelper.StorageFolder))
                    throw new Exception("Invalid StorageFolder");
                if (string.IsNullOrEmpty(ConfigHelper.WorkFolder) || !Directory.Exists(ConfigHelper.WorkFolder))
                    throw new Exception("Invalid WorkFolder");

                //Test if have permission to add/delete folders in storage area
                var testFolder = Path.Combine(ConfigHelper.StorageFolder, Guid.NewGuid().ToString());
                Directory.CreateDirectory(testFolder);
                Directory.Delete(testFolder);

                //Test if have permission to add/delete folders in working area
                testFolder = Path.Combine(ConfigHelper.WorkFolder, Guid.NewGuid().ToString());
                Directory.CreateDirectory(testFolder);
                Directory.Delete(testFolder);

                Logger.LogInfo("Storage permission granted");

                //Do this to avoid an infinite hang if the firewall has blocked the port
                //You cannot shut down the service if blocked because it never finishes startup
                var t = new System.Threading.Thread(StartupEndpoint);
                t.Start();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                throw;
            }
        }

        private static void StartupEndpoint()
        {
            try
            {
                Logger.LogInfo("Attempting to execute database upgrade.");
                var connectionStringSettings = ConfigurationManager.ConnectionStrings["gFileSystemEntities"];
                var connectionStringBuilder = new SqlConnectionStringBuilder(connectionStringSettings.ConnectionString)
                {
                    InitialCatalog = "Master"
                };

                var installer = new DatabaseInstaller();
                if (installer.NeedsUpdate(connectionStringSettings.ConnectionString))
                {
                    var setup = new InstallSetup
                    {
                        AcceptVersionWarningsChangedScripts = true,
                        AcceptVersionWarningsNewScripts = true,
                        ConnectionString = connectionStringSettings.ConnectionString,
                        InstallStatus = InstallStatusConstants.Upgrade,
                        MasterConnectionString = connectionStringBuilder.ToString()
                    };

                    installer.Install(setup);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to execute database upgrade.");
                throw new Exception("Failed to execute database upgrade.");
            }

            Logger.LogInfo("Services Started Begin");
            try
            {
                #region Primary Endpoint

                var service = new SystemCore();
                var primaryAddress = new Uri("net.tcp://localhost:" + ConfigHelper.Port + "/__gfile");
                var primaryHost = new ServiceHost(service, primaryAddress);

                //Initialize the service
                //var netTcpBinding = new CompressedNetTcpBinding();
                var netTcpBinding = new NetTcpBinding { MaxBufferSize = 10 * 1024 * 1024, MaxReceivedMessageSize = 10 * 1024 * 1024, MaxBufferPoolSize = 10 * 1024 * 1024 };
                netTcpBinding.ReaderQuotas.MaxStringContentLength = 10 * 1024 * 1024;
                netTcpBinding.ReaderQuotas.MaxBytesPerRead = 10 * 1024 * 1024;
                netTcpBinding.ReaderQuotas.MaxArrayLength = 10 * 1024 * 1024;
                netTcpBinding.ReaderQuotas.MaxDepth = 10 * 1024 * 1024;
                netTcpBinding.ReaderQuotas.MaxNameTableCharCount = 10 * 1024 * 1024;
                netTcpBinding.Security.Mode = SecurityMode.None;
                primaryHost.AddServiceEndpoint(typeof(ISystemCore), netTcpBinding, string.Empty);
                primaryHost.Open();

                //Create Core Listener
                var primaryEndpoint = new EndpointAddress(primaryHost.BaseAddresses.First().AbsoluteUri);
                var primaryClient = new ChannelFactory<ISystemCore>(netTcpBinding, primaryEndpoint);
                _core = primaryClient.CreateChannel();

                #endregion

                Logger.LogInfo("Service Running on Port " + ConfigHelper.Port);
                Logger.LogInfo("Services Started End");

            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                throw;
            }
        }

    }

}