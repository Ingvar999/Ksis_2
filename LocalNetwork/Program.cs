using System;
using System.Threading;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace LocalNetwork
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Название устройства: "+Dns.GetHostName());
            var host = Dns.GetHostEntry("");
            IPAddress localIP = null;
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIP = ip;
                }
            }
            Console.WriteLine("IP устройства: "+localIP.ToString());
            IPAddress mask = GetSubnetMask(localIP);
            Console.WriteLine("Маска подсети: "+mask.ToString());
            GetMacAddress();
            Console.WriteLine("Устройства в сети");
            PingAddresses(localIP, mask);
            Console.ReadKey();
        }

        static void PingAddresses(IPAddress localIP, IPAddress mask)
        {
            UInt32 longip = (UInt32)GetLongIP(localIP);
            UInt32 longmask = (UInt32)GetLongIP(mask);
            UInt32 longSubNetwork = longip & longmask;
            for (longip = longSubNetwork; longip < longSubNetwork + ~longmask -1; ++longip)
            {
                Thread t = new Thread(new ParameterizedThreadStart(Request));
                t.Start(longip);
            }
            Console.WriteLine("\ndone");
        }

        static void Request(object x)
        {
            Ping ping = new Ping();
            IPAddress ip = new IPAddress(GetArray((UInt32)x));
            PingReply pingReply = ping.Send(ip, 100);
            if (pingReply.Status == IPStatus.Success)
            {
                Console.WriteLine("\n" + ip.ToString()+"\n"+ConvertIpToMAC(ip));
                try
                {
                    Console.WriteLine(Dns.GetHostEntry(ip).HostName);
                }
                catch
                {
                   
                }
            }

        }

        public static string ConvertIpToMAC(IPAddress ip)
        {
            byte[] addr = new byte[6];
            int length = addr.Length;
            SendARP(ip.GetHashCode(), 0, addr, ref length);
            return BitConverter.ToString(addr, 0, 6);
        }

        [DllImport("IPHLPAPI.DLL", ExactSpelling = true)]
        public static extern int SendARP(int DestinationIP, int SourceIP, [Out] byte[] pMacAddr, ref int PhyAddrLen);

        static byte[] GetArray(long x)
        {
            byte[] res = new byte[4];
            for (int i = 3; i >= 0; --i)
            {
                res[i] = (byte)(x & 255);
                x >>= 8;
            }
            return res;
        }

        static long GetLongIP(IPAddress ip)
        {
            long longip = 0;
            for (int i = 0; i < 4; ++i)
            {
                longip <<= 8;
                longip += ip.GetAddressBytes()[i];
            }
            return longip;
        }

        public static void GetMacAddress()
        {
            Console.WriteLine("Адреса физических устройств");
            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus == OperationalStatus.Up)
                {
                    Console.WriteLine(nic.GetPhysicalAddress().ToString());
                }
            }
        }

        public static IPAddress GetSubnetMask(IPAddress address)
        {
            foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                foreach (UnicastIPAddressInformation unicastIPAddressInformation in adapter.GetIPProperties().UnicastAddresses)
                {
                    if (unicastIPAddressInformation.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        if (address.Equals(unicastIPAddressInformation.Address))
                        {
                            return unicastIPAddressInformation.IPv4Mask;
                        }
                    }
                }
            }
            throw new ArgumentException($"Can't find subnetmask for IP address '{address}'");
        }
    }
}
