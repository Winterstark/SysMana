using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Echevil;
using System.IO;
using CoreAudioApi;

namespace SysMana
{
    public class DataSources
    {
        #region Get Total RAM
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private class MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
            public MEMORYSTATUSEX()
            {
                this.dwLength = (uint)Marshal.SizeOf(this);
            }
        }

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);
        
        #endregion

        #region Get Recycle Bin Data
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
        public struct SHQUERYRBINFO
        {
            public Int32 cbSize;
            public UInt64 i64Size;
            public UInt64 i64NumItems;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        public static extern int SHQueryRecycleBin(string pszRootPath, ref SHQUERYRBINFO pSHQueryRBInfo); 
        #endregion


        SHQUERYRBINFO binQuery;
        MMDevice audioDevice;
        NetworkMonitor monitor;
        NetworkAdapter[] adapters;
        PerformanceCounter cpuCounter, ramCounter;
        public int TotalRAM;

        DateTime prevWLANCheck, prevFileCheck, prevBinCheck;
        int prevWLANvalue, prevFileValue, prevBinValue;


        public DataSources(List<Meter> meters)
        {
            monitor = new NetworkMonitor();
            adapters = monitor.Adapters;

            MEMORYSTATUSEX memStatus = new MEMORYSTATUSEX();
            if (GlobalMemoryStatusEx(memStatus))
                TotalRAM = (int)((float)memStatus.ullTotalPhys / 1024 / 1024);

            bool startedMonitoring = false;

            foreach (Meter meter in meters)
                switch (meter.data)
                {
                    case "CPU usage":
                        if (cpuCounter == null)
                        {
                            cpuCounter = new PerformanceCounter();
                            cpuCounter.CategoryName = "Processor";
                            cpuCounter.CounterName = "% Processor Time";
                            cpuCounter.InstanceName = "_Total";
                        }
                        break;
                    case "Available memory":
                    case "Used memory":
                        if (ramCounter == null)
                            ramCounter = new PerformanceCounter("Memory", "Available MBytes");
                        break;
                    case "Recycle bin file count":
                    case "Recycle bin size":
                        binQuery = new SHQUERYRBINFO();
                        break;
                    case "Download speed":
                    case "Upload speed":
                        if (!startedMonitoring)
                        {
                            monitor.StartMonitoring();
                            startedMonitoring = true;
                        }
                        break;
                    case "System volume":
                    case "Audio peak level":
                        if (audioDevice == null)
                        {
                            MMDeviceEnumerator DevEnum = new MMDeviceEnumerator();
                            audioDevice = DevEnum.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia);
                        }
                        break;
                }
        }

        public int GetValue(string source, string subSource)
        {
            int adapterInd;

            switch (source)
            {
                case "CPU usage":
                    return (int)cpuCounter.NextValue();
                case "Available memory":
                    return (int)ramCounter.NextValue();
                case "Used memory":
                    return (int)(TotalRAM - ramCounter.NextValue());
                case "Available disk space":
                    return getFreeDiskSpace(subSource);
                case "Used disk space":
                    int free = getFreeDiskSpace(subSource), total = GetTotalDiskSpace(subSource);

                    if (free != -1 && total != -1)
                        return total - free;
                    else
                        return -1;
                case "Recycle bin file count":
                    binQuery.cbSize = Marshal.SizeOf(typeof(SHQUERYRBINFO));

                    try
                    {
                        if (SHQueryRecycleBin(null, ref binQuery) == 0)
                            return (int)binQuery.i64NumItems;
                        else
                            return -1;
                    }
                    catch
                    {
                        return -1;
                    }
                case "Recycle bin size":
                    binQuery.cbSize = Marshal.SizeOf(typeof(SHQUERYRBINFO));

                    if (DateTime.Now.Subtract(prevBinCheck).TotalSeconds < 10)
                        return prevBinValue;
                    else
                    {
                        prevBinCheck = DateTime.Now;

                        try
                        {
                            if (SHQueryRecycleBin(null, ref binQuery) == 0)
                            {
                                prevBinValue = (int)(Convert.ToDouble(binQuery.i64Size) / Convert.ToDouble(1024) / Convert.ToDouble(1024));
                                return prevBinValue;
                            }
                            else
                            {
                                prevBinValue = -1;
                                return -1;
                            }
                        }
                        catch
                        {
                            prevBinValue = -1;
                            return -1;
                        }
                    }
                case "Battery percent remaining":
                    return (int)(SystemInformation.PowerStatus.BatteryLifePercent * 100);
                case "Battery minutes remaining":
                    return SystemInformation.PowerStatus.BatteryLifeRemaining / 60;
                case "Download speed":
                    adapterInd = getAdapterInd(subSource);

                    if (adapterInd != -1)
                        return (int)adapters[adapterInd].DownloadSpeedKbps;
                    else
                        return -1;
                case "Upload speed":
                    adapterInd = getAdapterInd(subSource);

                    if (adapterInd != -1)
                        return (int)adapters[adapterInd].UploadSpeedKbps;
                    else
                        return -1;
                case "Wireless signal strength":
                    if (DateTime.Now.Subtract(prevWLANCheck).TotalSeconds < 10)
                        return prevWLANvalue;
                    else
                    {
                        Process proc = new Process();
                        proc.StartInfo.CreateNoWindow = true;
                        proc.StartInfo.FileName = "netsh";
                        proc.StartInfo.Arguments = "wlan show interfaces";
                        proc.StartInfo.RedirectStandardOutput = true;
                        proc.StartInfo.UseShellExecute = false;
                        proc.Start();

                        prevWLANvalue = -1;

                        while (!proc.StandardOutput.EndOfStream)
                        {
                            string line = proc.StandardOutput.ReadLine();

                            if (line.Contains("Signal"))
                            {
                                int lb = line.IndexOf(": ") + 2;
                                int ub = line.IndexOf("%");

                                prevWLANvalue = int.Parse(line.Substring(lb, ub - lb));
                                prevWLANCheck = DateTime.Now;
                            }
                        }

                        return prevWLANvalue;
                    }
                case "System volume":
                    if (audioDevice.AudioEndpointVolume.Mute)
                        return 0;
                    else
                        return (int)(audioDevice.AudioEndpointVolume.MasterVolumeLevelScalar * 100);
                case "Audio peak level":
                    switch (subSource)
                    {
                        case "Master":
                            return (int)(audioDevice.AudioMeterInformation.MasterPeakValue * 100);
                        case "Left channel":
                            return (int)(audioDevice.AudioMeterInformation.PeakValues[0] * 100);
                        case "Right channel":
                            return (int)(audioDevice.AudioMeterInformation.PeakValues[1] * 100);
                        default:
                            return -1;
                    }
                case "Text file":
                    if (DateTime.Now.Subtract(prevFileCheck).TotalSeconds > 5 && File.Exists(subSource))
                    {
                        prevFileCheck = DateTime.Now;

                        StreamReader file = new StreamReader(subSource);
                        string contents = file.ReadLine();
                        file.Close();

                        if (!int.TryParse(contents, out prevFileValue))
                            prevFileValue = -1;
                    }

                    return prevFileValue;
                default:
                    return -1;
            }
        }

        public string[] ListNetAdapters()
        {
            string[] names = new string[adapters.Length];

            for (int i = 0; i < adapters.Length; i++)
                names[i] = adapters[i].Name;

            return names;
        }

        int getAdapterInd(string adapter)
        {
            for (int i = 0; i < adapters.Length; i++)
                if (adapters[i].Name == adapter)
                    return i;

            return -1;
        }

        public int GetTotalDiskSpace(string disk)
        {
            try
            {
                if (DriveInfo.GetDrives().Any(d => d.Name == disk))
                    return (int)((float)DriveInfo.GetDrives().First(d => d.Name == disk).TotalSize / 1024 / 1024);
                else
                    return -1;
            }
            catch
            {
                return -1;
            }
        }

        int getFreeDiskSpace(string disk)
        {
            try
            {
                if (DriveInfo.GetDrives().Any(d => d.Name == disk))
                    return (int)((float)DriveInfo.GetDrives().First(d => d.Name == disk).AvailableFreeSpace / 1024 / 1024);
                else
                    return -1;
            }
            catch
            {
                return -1;
            }
        }
    }
}
