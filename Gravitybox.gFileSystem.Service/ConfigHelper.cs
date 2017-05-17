using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gravitybox.gFileSystem.Service
{
    public static class ConfigHelper
    {
        private static DateTime _lastUpdate = DateTime.MinValue;
        private static Dictionary<string, string> _settings = new Dictionary<string, string>();
        private static readonly object _syncObject = new object();
        public static string _connectionString = string.Empty;

        public static string ConnectionString
        {
            get { return _connectionString; }
            set
            {
                _connectionString = value;
                Refresh();
            }
        }

        #region Constructors

        static ConfigHelper()
        {
            DefaultWorkingFolder = Path.Combine(System.IO.Path.GetTempPath(), "xfs");
        }

        private static void Refresh()
        {
            //Do not rethrow error. If error just skip this and do NOT reset the settings object to null
            try
            {
                lock (_syncObject)
                {
                    var v = GetSettings(ConnectionString);
                    if (v != null)
                        _settings = v;
                }
            }
            catch (Exception ex)
            {
            }
        }

        #endregion

        #region Sql

        private static Dictionary<string, string> GetSettings(string connectionString)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "select * from [ConfigSetting]";
                        command.CommandType = CommandType.Text;
                        var adapter = new SqlDataAdapter(command);
                        var ds = new DataSet();
                        adapter.Fill(ds, "Q");

                        var retval = new Dictionary<string, string>();
                        foreach (DataRow r in ds.Tables[0].Rows)
                        {
                            retval.Add(r["Name"].ToString().ToLower(), (string)r["Value"]);
                        }
                        return retval;
                    }
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        #endregion

        #region Setting Methods

        internal static Dictionary<string, string> AllSettings
        {
            get
            {
                if (DateTime.Now.Subtract(_lastUpdate).TotalSeconds > 60)
                {
                    _lastUpdate = DateTime.Now;
                    Refresh();
                }
                return _settings;
            }
        }

        private static string GetValue(string name)
        {
            return GetValue(name, string.Empty);
        }

        private static string GetValue(string name, string defaultValue)
        {
            if (AllSettings.ContainsKey(name.ToLower()))
                return AllSettings[name.ToLower()];
            return defaultValue;
        }

        private static int GetValue(string name, int defaultValue)
        {
            int retVal;
            if (int.TryParse(GetValue(name, string.Empty), out retVal))
                return retVal;
            return defaultValue;
        }

        private static bool GetValue(string name, bool defaultValue)
        {
            bool retVal;
            if (bool.TryParse(GetValue(name, string.Empty), out retVal))
                return retVal;
            return defaultValue;
        }

        private static DateTime GetValue(string name, DateTime defaultValue)
        {
            DateTime retVal;
            if (DateTime.TryParse(GetValue(name, string.Empty), out retVal))
                return retVal;
            return defaultValue;
        }

        #endregion

        #region Properties

        private static string DefaultWorkingFolder { get; set; }

        private static string MasterKeyFile
        {
            get { return GetValue("MasterKeyFile", string.Empty); }
        }

        public static byte[] MasterKey
        {
            get
            {
                var fileName = MasterKeyFile;
                if (string.IsNullOrEmpty(fileName))
                    fileName = "master.key";

                if (!File.Exists(fileName))
                    throw new Exception();

                var lines = File.ReadAllLines(fileName);
                var full = string.Join(string.Empty, lines.Where(x => !x.StartsWith("-"))).Replace(" ", string.Empty).Replace("-", string.Empty);
                var key = Utilities.ConvertHexStringToByteArray(full);
                if (key != null && key.Length != 32)
                    key = null;
                return key;
            }
        }

        public static string WorkFolder
        {
            get { return GetValue("WorkFolder", DefaultWorkingFolder); }
        }

        public static string StorageFolder
        {
            get { return GetValue("StorageFolder", string.Empty); }
        }

        public static int LockTimeout
        {
            get { return GetValue("LockTimeout", 60); }
        }

        public static int Port => GetValue("Port", 1900);

        #endregion

    }
}