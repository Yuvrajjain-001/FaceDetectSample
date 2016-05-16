using System;
using System.Diagnostics;
using Microsoft.Win32;


namespace Dpu.Utility
{
    /// <summary>
    /// Summary description for AppRegistry.
    /// </summary>
    public class AppRegistry : IDisposable
    {
        private const string keySoftware = "Software";
        private RegistryKey appRegistryKey;

        #region Methods

        public AppRegistry()
        {
        }

        public void Dispose()
        {
            try 
            {
            }
            finally 
            {
                if (this.appRegistryKey != null)
                {
                    this.appRegistryKey.Close();
                    this.appRegistryKey = null;
                }
            }
        }
        
        /*  Not needed?
        private RegistryKey AppRegistryKey
        {
            get
            {
                return this.appRegistryKey;
            }
        }
        */
        
        // returns key for RegistryKey_CURRENT_USER\Software\CompanyKey\AppName
        // creating it if it doesn't exist
        public bool SetRegistryKey(string companyName, string appName)
        {
            bool isSucceeded = false;
            RegistryKey userKey = Registry.CurrentUser;
            try 
            {
                RegistryKey softwareKey = userKey.OpenSubKey(keySoftware, true);
        
                if (softwareKey == null)
                {
                    softwareKey = userKey.CreateSubKey(keySoftware);
                }
            
                if (softwareKey != null)
                {
                    try 
                    {
                        RegistryKey regKey = softwareKey.OpenSubKey(companyName, true);
                        if (regKey == null)
                        {
                            regKey = softwareKey.CreateSubKey(companyName);
                        }

                        if (regKey != null)
                        {
                            try 
                            {
                                this.appRegistryKey = regKey.OpenSubKey(appName, true);
                                if (this.appRegistryKey == null)
                                {
                                    this.appRegistryKey = regKey.CreateSubKey(appName);
                                    isSucceeded = (this.appRegistryKey != null);
                                }
                                else
                                {
                                    isSucceeded = true;
                                }
                            }
                            finally 
                            {
                                regKey.Close();
                            }
                        }
                    }
                    finally 
                    {
                        softwareKey.Close();
                    }
                }
            }
            finally 
            {
                if (userKey != null)
                    userKey.Close();
            }
            return isSucceeded;
        }

        // Returns key for:
        //      RegistryKey_CURRENT_USER\Software\CompanyKey\AppName\sectionName
        private RegistryKey GetSectionKey(string section, bool write)
        {
            Debug.Assert(this.appRegistryKey != null);

            RegistryKey sectionKey = this.appRegistryKey.OpenSubKey(section, write);
            if (sectionKey == null)
            {
                // Create it if it doesn't exist.
                sectionKey = this.appRegistryKey.CreateSubKey(section);
            }

            // the caller to call RegCloseKey() on the returned RegistryKey
            return sectionKey;
        }

        public int GetProfileInt(string sectionName, string entryName, int defaultValue)
        {
            Debug.Assert(sectionName != null && entryName != null);
            Debug.Assert(this.appRegistryKey != null); // use registry

            RegistryKey sectionKey = null;
            object entryValue = null;

            try 
            {
                sectionKey = GetSectionKey(sectionName, false);
                if( sectionKey == null)
                    return defaultValue;

                entryValue = sectionKey.GetValue(entryName, defaultValue);
            }
            finally 
            {
                if (sectionKey != null)
                    sectionKey.Close();
            }

            return (entryValue != null) ? Convert.ToInt32(entryValue.ToString(), 10) : defaultValue;
        }

        public string GetProfileString(string sectionName, string entryName, string defaultValue)
        {
            Debug.Assert(sectionName != null && entryName != null);
            Debug.Assert(this.appRegistryKey != null); // use registry

            RegistryKey sectionKey = null;
            object entryValue = null;

            try 
            {
                sectionKey = GetSectionKey(sectionName, false);
                if( sectionKey == null)
                    return defaultValue;
            
                entryValue = sectionKey.GetValue(entryName, defaultValue);
            }
            finally 
            {
                if (sectionKey != null)
                    sectionKey.Close();
            }

            return (entryValue != null) ? (entryValue.ToString()) : defaultValue;
        }

        public bool WriteProfileInt(string sectionName, string entryName, int entryValue)
        {
            Debug.Assert(sectionName != null && entryName != null);
            Debug.Assert(this.appRegistryKey != null);
            
            RegistryKey sectionKey = null;
            try 
            {

                sectionKey = GetSectionKey(sectionName, true);
                if (sectionKey == null)
                    return false;

                sectionKey.SetValue(entryName, entryValue);
            }
            finally 
            {
                if (sectionKey != null) 
                    sectionKey.Close();
            }
            return true;
        }

        public bool WriteProfileString(string sectionName, string entryName, string entryValue)
        {
            Debug.Assert(sectionName != null && entryName != null);
            Debug.Assert(this.appRegistryKey != null);
            
            RegistryKey sectionKey = null;

            try 
            {
                sectionKey = GetSectionKey(sectionName, true);
                if (sectionKey == null)
                    return false;

                sectionKey.SetValue(entryName, entryValue);
            }
            finally 
            {
                if (sectionKey != null)
                    sectionKey.Close();
            }
            return true;
        }

        #endregion Methods
    }
}
