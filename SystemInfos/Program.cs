
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;

namespace SystemInfos
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            SystemDetails si = new SystemDetails();
            si.Dump();

            Console.ReadLine();
        }

    }

    public class SystemDetails
    {
        public string UserName { get; private set; } // User name of PC
        public string OS_Version { get; private set; } // OS version of pc
        public string ComputerName { get; private set; }// Machine name
        public string OS_Architecture { get; private set; } // Processor type
        public string CPU_Kerne { get; }
        public string LatestDotNet { get; }
        public string TotalRam { get; }


        public SystemDetails()
        {
            UserName = Environment.UserName;
            OS_Version = GetOSInfo();
            ComputerName = Environment.MachineName;
            OS_Architecture = Environment.Is64BitOperatingSystem ? "64-Bit" : "32-Bit";
            CPU_Kerne = Environment.ProcessorCount.ToString();
            TotalRam = GetRAM();
            LatestDotNet = Get45PlusFromRegistry();
        }

        public string GetOSInfo()
        {
            string caption;
            Version version;

            using (ManagementObjectSearcher mos = new ManagementObjectSearcher("SELECT Caption, Version FROM Win32_OperatingSystem"))
            {
                IEnumerable<ManagementObject> attribs = mos.Get().OfType<ManagementObject>();
                caption = attribs.FirstOrDefault().GetPropertyValue("Caption").ToString() ?? "Unknown";
                version = new Version((attribs.FirstOrDefault().GetPropertyValue("Version") ?? "0.0.0.0").ToString());
            }

            return $"{caption} {version}";
        }


        public string GetRAM()
        {
            //https://docs.microsoft.com/de-de/windows/desktop/CIMWin32Prov/win32-operatingsystem

            string pn = string.Join(",",
                "FreePhysicalMemory",
                "FreeVirtualMemory",
                "TotalVirtualMemorySize",
                "TotalVisibleMemorySize",
                "FreeSpaceInPagingFiles"
                );

            ObjectQuery winQuery = new ObjectQuery($"SELECT {pn} FROM Win32_OperatingSystem");
            List<Tuple<string, dynamic>> data = new List<Tuple<string, dynamic>>();

            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(winQuery))
            {
                foreach (ManagementBaseObject moc in searcher.Get())
                {
                    PropertyDataCollection props = moc.Properties;
                    foreach (PropertyData prop in props)
                    {
                        data.Add(new Tuple<string, dynamic>(prop.Name, prop.Value));
                    }
                }
            }
            StringBuilder sb = new StringBuilder();
            foreach (Tuple<string, dynamic> d in data)
            {
                //Console.WriteLine($"{d.Item1}\t{d.Item2}");
                sb.AppendLine($"\t\t{d.Item1}\t{FormatSizeHumanReadable(d.Item2)}");
            }

            return sb.ToString().TrimStart('\t');
        }

        public string FormatSizeHumanReadable(object kilobytes)
        {
            string[] sizes = { /*"B", */"KB", "MB", "GB", "TB" };

            decimal len = 0m;
            try
            {
                len = (ulong)kilobytes;
            }
            catch (Exception)
            {
                return kilobytes?.ToString() ?? "";
            }
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            // Adjust the format string to your preferences. For example "{0:0.#}{1}" would
            // show a single decimal place, and no space.
            return string.Format("{0:0.##} {1}", len, sizes[order]);
        }

        public void Dump()
        {
            List<string> props = typeof(SystemDetails).GetProperties().Select(p => p.Name + "\t" + p.GetValue(this)).ToList();
            props.ForEach(p => Console.WriteLine(p));
        }

        private static string Get45PlusFromRegistry()
        {
            const string subkey = @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\";

            using (RegistryKey ndpKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(subkey))
            {
                if (ndpKey != null && ndpKey.GetValue("Release") != null)
                {
                    return $"{CheckFor45PlusVersion((int)ndpKey.GetValue("Release"))}";
                }
                else
                {
                    return ".NET Framework Version 4.5 or later is not detected.";
                }
            }

            // Checking the version using >= enables forward compatibility.
            string CheckFor45PlusVersion(int releaseKey)
            {
                if (releaseKey >= 528040)
                {
                    return "4.8 or later";
                }

                if (releaseKey >= 461808)
                {
                    return "4.7.2";
                }

                if (releaseKey >= 461308)
                {
                    return "4.7.1";
                }

                if (releaseKey >= 460798)
                {
                    return "4.7";
                }

                if (releaseKey >= 394802)
                {
                    return "4.6.2";
                }

                if (releaseKey >= 394254)
                {
                    return "4.6.1";
                }

                if (releaseKey >= 393295)
                {
                    return "4.6";
                }

                if (releaseKey >= 379893)
                {
                    return "4.5.2";
                }

                if (releaseKey >= 378675)
                {
                    return "4.5.1";
                }

                if (releaseKey >= 378389)
                {
                    return "4.5";
                }
                // This code should never execute. A non-null release key should mean
                // that 4.5 or later is installed.
                return "No 4.5 or later version detected";
            }
        }
    }
}
