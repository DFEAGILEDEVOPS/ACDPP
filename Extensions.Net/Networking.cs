using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Extensions
{
    public static class Networking
    {
        private static List<string> _LocalIPAddresses;

        public static List<string> LocalIPAddresses
        {
            get
            {
                if (_LocalIPAddresses == null)
                {
                    _LocalIPAddresses = new List<string>();

                    var localIPs = Dns.GetHostAddresses(Dns.GetHostName());
                    foreach (var address in localIPs)
                    {
                        if (address.AddressFamily != AddressFamily.InterNetwork) continue;
                        _LocalIPAddresses.Add(address.ToString());
                    }
                    _LocalIPAddresses.Sort();
                }
                return _LocalIPAddresses;
            }
        }

        public static IEnumerable<DriveInfo> GetAllDrives()
        {
            return DriveInfo.GetDrives();
        }

        public static IEnumerable<DriveInfo> GetLocalDrives()
        {
            return DriveInfo.GetDrives().Where(d => d.DriveType != DriveType.Network).ToList();
        }

        public static IEnumerable<DriveInfo> GetNetworkDrives()
        {
            return DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Network).ToList();
        }

        public static bool IsIPv4Address(this string ipAddress)
        {
            if (ipAddress == null) return false;
            ipAddress = ipAddress.Trim();
            return ipAddress.Match(@"^\d{1,3}(\.\d{1,3}){3}$");
        }

        public static bool IsIPv6Address(this string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(ipAddress)) return false;
            IPAddress testAddress = null;
            if (!IPAddress.TryParse(ipAddress, out testAddress) || testAddress == null) return false;
            return testAddress.AddressFamily == AddressFamily.InterNetworkV6;
        }

        public static bool IsIPAddress(this string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(ipAddress)) return false;
            IPAddress testAddress = null;
            if (!IPAddress.TryParse(ipAddress, out testAddress) || testAddress == null) return false;
            return testAddress.AddressFamily == AddressFamily.InterNetwork || testAddress.AddressFamily == AddressFamily.InterNetworkV6;
        }

        public static IPEndPoint EnpointParse(string ipAddressAndPort)
        {
            var i = ipAddressAndPort.IndexOf(":");
            if (i < 0) throw new ArgumentException("You must supply a port number");

            var port = 0;
            if (!int.TryParse(ipAddressAndPort.Substring(i + 1), out port)) throw new ArgumentException("You must supply a valid port number");
            var Host = ipAddressAndPort.Substring(0, i);
            IPAddress ipAddress = null;
            if (!IPAddress.TryParse(ipAddressAndPort.Substring(0, i), out ipAddress)) throw new ArgumentException("You must supply a valid IP address");

            return new IPEndPoint(ipAddress, port);
        }

        public static IPAddress PingAny(string hostNameOrAddress, int port = 0, int timeOut = 30000)
        {
            if (port == 0)
            {
                port = hostNameOrAddress.AfterFirst(":", includeWhenNoSeperator: false).ToInt32();
                hostNameOrAddress = hostNameOrAddress.BeforeFirst(":");
            }
            if (port == 0) throw new ArgumentException("You must provide a port number", "port");

            var ipAddresses = new List<IPAddress>();
            IPAddress ipAddress;
            if (IPAddress.TryParse(hostNameOrAddress, out ipAddress))
            {
                ipAddresses.Add(ipAddress);
            }
            else
            {
                ipAddresses.AddRange(GetIPv4Addresses(hostNameOrAddress));
            }

            if (ipAddresses.Count == 1) return Ping(new IPEndPoint(ipAddresses[0], port), timeOut) ? ipAddresses[0] : null;

            return PingAny(ipAddresses, port, timeOut);
        }

        public static IPAddress PingAll(string[] hostNamesOrAddresses, int port = 0, int timeOut = 30000)
        {
            return PingAll(new List<string>(hostNamesOrAddresses), port, timeOut);
        }

        public static IPAddress PingAll(List<string> hostNamesOrAddresses, int port = 0, int timeOut = 30000)
        {
            var endPoints = new List<IPEndPoint>();
            foreach (var hostNameOrAddress in hostNamesOrAddresses)
            {
                var localPort = port;
                var localAddress = hostNameOrAddress;
                if (localPort == 0)
                {
                    localPort = hostNameOrAddress.AfterFirst(":", includeWhenNoSeperator: false).ToInt32();
                    localAddress = hostNameOrAddress.BeforeFirst(":");
                }
                if (localPort == 0) throw new ArgumentException("You must provide a port number for '" + localAddress + "'", "port");

                IPAddress ipAddress;
                if (IPAddress.TryParse(localAddress, out ipAddress))
                {
                    endPoints.Add(new IPEndPoint(ipAddress, localPort));
                }
                else
                    foreach (var ipAddress2 in GetIPv4Addresses(localAddress))
                    {
                        endPoints.Add(new IPEndPoint(ipAddress2, localPort));
                    }
            }

            if (endPoints.Count == 1) return Ping(endPoints[0], timeOut) ? endPoints[0].Address : null;

            var result = PingAll(endPoints, timeOut);
            return result == null ? null : result.Address;
        }

        public static IPAddress PingAny(string[] hostNamesOrAddresses, int port = 0, int timeOut = 30000)
        {
            return PingAny(new List<string>(hostNamesOrAddresses), port, timeOut);
        }

        public static IPAddress PingAny(List<string> hostNamesOrAddresses, int port = 0, int timeOut = 30000)
        {
            var endPoints = new List<IPEndPoint>();
            foreach (var hostNameOrAddress in hostNamesOrAddresses)
            {
                var localPort = port;
                var localAddress = hostNameOrAddress;
                if (localPort == 0)
                {
                    localPort = hostNameOrAddress.AfterFirst(":", includeWhenNoSeperator: false).ToInt32();
                    localAddress = hostNameOrAddress.BeforeFirst(":");
                }
                if (localPort == 0) throw new ArgumentException("You must provide a port number for '" + localAddress + "'", "port");

                IPAddress ipAddress;
                if (IPAddress.TryParse(localAddress, out ipAddress))
                {
                    endPoints.Add(new IPEndPoint(ipAddress, localPort));
                }
                else
                    foreach (var ipAddress2 in GetIPv4Addresses(localAddress))
                    {
                        endPoints.Add(new IPEndPoint(ipAddress2, localPort));
                    }
            }

            if (endPoints.Count == 1) return Ping(endPoints[0], timeOut) ? endPoints[0].Address : null;

            var result = PingAny(endPoints, timeOut);
            return result == null ? null : result.Address;
        }

        public static IPAddress PingAll(string hostNameOrAddress, int port = 0, int timeOut = 30000)
        {
            if (port == 0)
            {
                port = hostNameOrAddress.AfterFirst(":", includeWhenNoSeperator: false).ToInt32();
                hostNameOrAddress = hostNameOrAddress.BeforeFirst(":");
            }
            if (port == 0) throw new ArgumentException("You must provide a port number", "port");

            var ipAddresses = new List<IPAddress>();
            IPAddress ipAddress;
            if (IPAddress.TryParse(hostNameOrAddress, out ipAddress))
            {
                ipAddresses.Add(ipAddress);
            }
            else
            {
                ipAddresses.AddRange(GetIPv4Addresses(hostNameOrAddress));
            }

            if (ipAddresses.Count == 1) return Ping(new IPEndPoint(ipAddresses[0], port), timeOut) ? ipAddresses[0] : null;
            return PingAll(ipAddresses, port, timeOut);
        }

        public static IPAddress PingAll(List<IPAddress> ipAddresses, int port, int timeOut = 30000)
        {
            var endPoints = new List<IPEndPoint>();
            foreach (var ipAddress in ipAddresses)
                endPoints.Add(new IPEndPoint(ipAddress, port));

            var result = PingAll(endPoints, timeOut);
            return result == null ? null : result.Address;
        }

        public static IPAddress PingAny(List<IPAddress> ipAddresses, int port, int timeOut = 30000)
        {
            var endPoints = new List<IPEndPoint>();
            foreach (var ipAddress in ipAddresses)
                endPoints.Add(new IPEndPoint(ipAddress, port));

            var result = PingAny(endPoints, timeOut);
            return result == null ? null : result.Address;
        }

        public static IPEndPoint PingAll(IEnumerable<IPEndPoint> endPoints, int timeOut = 30000)
        {
            var cancellationSource = new CancellationTokenSource();

            var tasks = new List<Task<IPEndPoint>>();
            foreach (var ipEndPoint in endPoints)
            {
                tasks.Add(new Task<IPEndPoint>(() => Ping(ipEndPoint, timeOut) ? null : ipEndPoint, cancellationSource.Token));
            }

            var task = tasks.WhenAny(cancellationSource, t => t != null);
            return task.Result;
        }

        public static IPEndPoint PingAny(IEnumerable<IPEndPoint> endPoints, int timeOut = 30000)
        {
            var cancellationSource = new CancellationTokenSource();

            var tasks = new List<Task<IPEndPoint>>();
            foreach (var ipEndPoint in endPoints)
            {
                tasks.Add(new Task<IPEndPoint>(() => Ping(ipEndPoint, timeOut) ? ipEndPoint : null, cancellationSource.Token));
            }

            var task = tasks.WhenAny(cancellationSource, t => t != null);
            return task.Result;
        }

        [DebuggerStepThrough]
        public static bool Ping(IPEndPoint endpoint, int timeOut = 30000)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var result = socket.BeginConnect(endpoint, null, null);

            try
            {
                var success = result.AsyncWaitHandle.WaitOne(timeOut, true);
                if (!success) return false;
                socket.EndConnect(result);
                if (socket.Connected) return true;
            }
            catch (SocketException sex)
            {
                switch (sex.ErrorCode)
                {
                    case 10061:
                    case 10060:
                        return false;
                }
                throw sex;
            }
            finally
            {
                socket.Close();
            }
            return false;
        }

        public static string HttpPingAny(IEnumerable<string> urls, int timeOut = 30000)
        {
            var addresses = new ConcurrentSet<string>(urls);
            if (addresses.Count < 1) return null;

            var cancellationSource = new CancellationTokenSource();

            var tasks = new List<Task<string>>();
            foreach (var address in addresses)
            {
                tasks.Add(new Task<string>(() => HttpPing(new Uri(address), timeOut) ? address : null, cancellationSource.Token));
            }

            var task = tasks.WhenAny(cancellationSource, t => t != null);
            return task.Result;
        }

        public static string HttpPingAll(IEnumerable<string> urls, int timeOut = 30000)
        {
            var addresses = new ConcurrentSet<string>(urls);

            var cancellationSource = new CancellationTokenSource();

            var tasks = new List<Task<string>>();
            foreach (var address in addresses)
            {
                tasks.Add(new Task<string>(() => HttpPing(new Uri(address), timeOut) ? null : address, cancellationSource.Token));
            }

            var task = tasks.WhenAny(cancellationSource, t => t != null);
            return task.Result;
        }

        public static bool HttpPing(string url, int timeOut = 30000, bool allowAutoRedirect = false)
        {
            return HttpPing(new Uri(url), timeOut, allowAutoRedirect);
        }

        [DebuggerStepThrough]
        public static bool HttpPing(Uri uri, int timeOut = 30000, bool allowAutoRedirect = false)
        {
            //This prevents errors due to bad certificates
            ServicePointManager.ServerCertificateValidationCallback += delegate { return true; };
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls;

            var request = (HttpWebRequest) WebRequest.Create(uri);
            request.Timeout = timeOut;
            request.AllowAutoRedirect = allowAutoRedirect; // find out if this site is up and don't follow a redirector
            request.Method = "GET";
            try
            {
                var response = (HttpWebResponse) request.GetResponse();

                // do something with response.Headers to find out information about the request
                return response.StatusCode.IsAny(HttpStatusCode.OK);
            }
            catch (WebException)
            {
                //set flag if there was a timeout or some other issues
            }
            return false;
        }

        //[System.Diagnostics.DebuggerStepThrough]
        public static string ResolveHost(string hostOrIpAddress)
        {
            try
            {
                return Dns.GetHostEntry(hostOrIpAddress).HostName;
            }
            catch
            {
            }
            return null;
        }

        private static IPAddress GetNetworkAddress(this IPAddress address, IPAddress subnetMask)
        {
            var ipAdressBytes = address.GetAddressBytes();
            var subnetMaskBytes = subnetMask.GetAddressBytes();

            if (ipAdressBytes.Length != subnetMaskBytes.Length)
                throw new ArgumentException("Lengths of IP address and subnet mask do not match.");

            var broadcastAddress = new byte[ipAdressBytes.Length];
            for (var i = 0; i < broadcastAddress.Length; i++)
            {
                broadcastAddress[i] = (byte) (ipAdressBytes[i] & subnetMaskBytes[i]);
            }
            return new IPAddress(broadcastAddress);
        }

        private static bool IsInSameSubnet(this IPAddress address2, IPAddress address, IPAddress subnetMask)
        {
            var network1 = address.GetNetworkAddress(subnetMask);
            var network2 = address2.GetNetworkAddress(subnetMask);

            return network1.Equals(network2);
        }

        [DebuggerStepThrough]
        public static bool IsOnLocalSubnet(this string hostName)
        {
            if (string.IsNullOrWhiteSpace(hostName)) throw new Exception("Missing hostname");
            try
            {
                var IPs = Dns.GetHostAddresses(hostName);
                if (IPs == null || IPs.Length < 1) throw new Exception("Could not resolve host name '" + hostName + "'");
                foreach (var address in IPs)
                    if (address.IsOnLocalSubnet()) return true;
            }
            catch
            {
            }

            return false;
        }

        public static bool IsTrustedAddress(this string hostName, string[] trustedIPdomains)
        {
            if (string.IsNullOrWhiteSpace(hostName)) throw new ArgumentNullException(nameof(hostName));
            if (trustedIPdomains == null || trustedIPdomains.Length == 0) throw new ArgumentNullException(nameof(trustedIPdomains));
            if (trustedIPdomains.ContainsI(hostName)) return true;
            try
            {
                var IPs = Dns.GetHostAddresses(hostName);
                if (IPs == null || IPs.Length < 1) throw new Exception("Could not resolve host name '" + hostName + "'");
                if (IPs.Any(address => trustedIPdomains.ContainsI(address.ToString()) || address.IsOnLocalSubnet()))
                    return true;
            }
            catch
            {
            }
            return hostName.IsOnLocalSubnet();
        }

        [DebuggerStepThrough]
        public static bool IPBelongsToHost(this string hostName, string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(hostName)) throw new Exception("Missing hostname");
            if (!ipAddress.IsIPv4Address()) throw new Exception("Invalid IP Address");
            try
            {
                var IPs = Dns.GetHostAddresses(hostName);
                if (IPs == null || IPs.Length < 1) throw new Exception("Could not resolve host name '" + hostName + "'");
                foreach (var address in IPs)
                    if (address.ToString().Equals(ipAddress)) return true;
            }
            catch
            {
            }

            return false;
        }

        [DebuggerStepThrough]
        public static bool IsLocalHost(this string hostName)
        {
            if (string.IsNullOrWhiteSpace(hostName)) throw new Exception("Missing hostname");

            try
            {
                var IPs = Dns.GetHostAddresses(hostName);

                if (IPs == null || IPs.Length < 1) throw new Exception("Could not resolve host name '" + hostName + "'");
                foreach (var address in IPs)
                {
                    if (address.AddressFamily != AddressFamily.InterNetwork) continue;
                    if (address.ToString().EqualsI("127.0.0.1")) return true;
                    if (LocalIPAddresses.ContainsI(address.ToString())) return true;
                }
            }
            catch
            {
            }
            return false;
        }

        public static bool IsLocalAddress(this IPAddress clientIP)
        {
            if (clientIP.ToString() == "127.0.0.1") return true;

            foreach (var networkAdapter in NetworkInterface.GetAllNetworkInterfaces())
                if (networkAdapter.OperationalStatus == OperationalStatus.Up)
                {
                    var interfaceProperties = networkAdapter.GetIPProperties();
                    var IPsettings = interfaceProperties.UnicastAddresses;
                    foreach (var IPsetting in IPsettings)
                    {
                        if (clientIP.Equals(IPsetting.Address)) return true;
                    }
                }

            return false;
        }

        public static bool IsOnLocalSubnet(this IPAddress clientIP)
        {
            if (clientIP.ToString().EqualsI("::1", "127.0.0.1")) return true;

            foreach (var networkAdapter in NetworkInterface.GetAllNetworkInterfaces())
                if (networkAdapter.OperationalStatus == OperationalStatus.Up)
                {
                    var interfaceProperties = networkAdapter.GetIPProperties();
                    var IPsettings = interfaceProperties.UnicastAddresses;
                    foreach (var IPsetting in IPsettings)
                    {
                        if (clientIP.Equals(IPsetting.Address)) return true;
                        if (IPsetting.IPv4Mask == null || IPsetting.IPv4Mask.ToString() == "0.0.0.0") continue;
                        if (clientIP.IsInSameSubnet(IPsetting.Address, IPsetting.IPv4Mask)) return true;
                    }
                }

            return false;
        }

        public static List<string> GetIPAddresses(string hostName)
        {
            var IPv4Addresses = GetIPv4Addresses(hostName);
            var _IPAddresses = new List<string>();
            foreach (var ipAddress in IPv4Addresses)
                _IPAddresses.Add(ipAddress.ToString());
            return _IPAddresses;
        }

        [DebuggerStepThrough]
        public static List<IPAddress> GetIPv4Addresses(string hostName)
        {
            var _IPAddresses = new List<IPAddress>();

            if (string.IsNullOrWhiteSpace(hostName)) return _IPAddresses;

            hostName = hostName.Trim();

            if (IsIPv4Address(hostName))
                _IPAddresses.Add(IPAddress.Parse(hostName));
            else
                try
                {
                    var IPs = Dns.GetHostAddresses(hostName);
                    foreach (var address in IPs)
                    {
                        if (address.AddressFamily != AddressFamily.InterNetwork) continue;
                        _IPAddresses.Add(address);
                    }
                }
                catch (SocketException sex)
                {
                    switch (sex.ErrorCode)
                    {
                        case 11001: //No such host is known
                        case 11002: //Nonauthoritative host not found - try again later
                        case 11004: //Requested name is valid but no records found
                            break;
                        default:
                            throw sex;
                    }
                }
            return _IPAddresses;
        }


        public static Dictionary<string, List<string>> CheckDSNBL(string dnsBL, string hostNameOrAddress)
        {
            var IPv4rAddresses = ReverseIPv4Addresses(hostNameOrAddress);
            var results = new ConcurrentDictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            Parallel.For(0, IPv4rAddresses.Count, i =>
            {
                var records = GetIPAddresses(IPv4rAddresses[i] + "." + dnsBL);
                if (records.Count > 0) results[IPv4rAddresses[i]] = records;
            });

            return results.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        public static string ReverseIPv4Address(IPAddress ipAddress)
        {
            var parts = ipAddress.ToString().Split('.');
            return string.Format("{3}.{2}.{1}.{0}", parts[0], parts[1], parts[2], parts[3]);
        }

        public static string ReverseIPv4Address(string ipAddress)
        {
            if (!ipAddress.IsIPv4Address()) throw new ArgumentException("Invalid IP Address");
            var parts = ipAddress.Split('.');
            return string.Format("{3}.{2}.{1}.{0}", parts[0], parts[1], parts[2], parts[3]);
        }

        public static List<string> ReverseIPv4Addresses(string hostNameOrAddress)
        {
            var _IPAddresses = GetIPAddresses(hostNameOrAddress);

            Parallel.For(0, _IPAddresses.Count, i => { _IPAddresses[i] = ReverseIPv4Address(_IPAddresses[i]); });

            return _IPAddresses;
        }

        public static int SendLine(this NetworkStream networkStream, string line)
        {
            var count = Send(networkStream, line + Environment.NewLine) - Environment.NewLine.Length;
            if (count < 0) return 0;
            return count;
        }

        public static int SendLine(this SslStream sslStream, string line)
        {
            var count = Send(sslStream, line + Environment.NewLine) - Environment.NewLine.Length;
            if (count < 0) return 0;
            return count;
        }

        //[DebuggerStepThrough]
        public static int Send(this NetworkStream networkStream, string text, Encoding encoding = null)
        {
            if (encoding == null) encoding = Encoding.UTF8;
            var bytes = encoding.GetBytes(text);
            networkStream.Write(bytes, 0, bytes.Length);
            return text.Length;
        }

        [DebuggerStepThrough]
        public static int Send(this NetworkStream networkStream, byte[] bytes)
        {
            networkStream.Write(bytes, 0, bytes.Length);
            return bytes.Length;
        }

        //[DebuggerStepThrough]
        public static int Send(this NetworkStream networkStream, byte[] bytes, int offset, int count)
        {
            networkStream.Write(bytes, offset, count);
            return Math.Min(bytes.Length - offset, count);
        }

        [DebuggerStepThrough]
        public static int Send(this SslStream sslStream, string text, Encoding encoding = null)
        {
            if (encoding == null) encoding = Encoding.UTF8;
            sslStream.Write(encoding.GetBytes(text));
            return text.Length;
        }

        [DebuggerStepThrough]
        public static int Send(this SslStream sslStream, byte[] bytes)
        {
            sslStream.Write(bytes);
            return bytes.Length;
        }

        [DebuggerStepThrough]
        public static int Send(this SslStream sslStream, byte[] bytes, int offset, int count)
        {
            sslStream.Write(bytes, offset, count);
            return Math.Min(bytes.Length - offset, count);
        }

        public static int SendLine(this Socket socket, string line)
        {
            var count = Send(socket, line + Environment.NewLine) - Environment.NewLine.Length;
            if (count < 0) return 0;
            return count;
        }

        public static int Send(this Socket socket, string text, Encoding encoding = null)
        {
            if (encoding == null) encoding = Encoding.UTF8;
            return socket.Send(encoding.GetBytes(text));
        }

        [DebuggerStepThrough]
        public static string GetHostByAddress(IPAddress ipAddress)
        {
            try
            {
                var hostEntry = Dns.GetHostEntry(ipAddress);
                return hostEntry.HostName;
            }
            catch (SocketException sex)
            {
                switch (sex.ErrorCode)
                {
                    case 11001: //No such host is known
                    case 11002: //Nonauthoritative host not found - try again later
                    case 11004: //Requested name is valid but no records found
                        break;
                    default:
                        throw sex;
                }
            }
            return null;
        }


        public static string GetIPAddress(this EndPoint endPoint)
        {
            if (endPoint != null && endPoint is IPEndPoint)
                return ((IPEndPoint) endPoint).Address.ToString();
            return null;
        }

        public static IPAddress GetDefaultGateway()
        {
            var card = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault();
            if (card == null) return null;
            var address = card.GetIPProperties().GatewayAddresses.FirstOrDefault();
            return address.Address;
        }

        public static void AddLocalHost(string hostName, string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(hostName)) throw new Exception("You must specify a hostname");
            if (!hostName.IsHostName()) throw new Exception("Invalid hostname '" + hostName + "'");
            if (string.IsNullOrWhiteSpace(ipAddress)) throw new Exception("You must specify a IP Address");
            if (!ipAddress.IsIPAddress()) throw new Exception("Invalid IP Address '" + ipAddress + "'");

            if (LocalHostExists(hostName, ipAddress)) return;
            var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "drivers/etc/hosts");
            File.AppendAllText(filePath, Environment.NewLine + ipAddress + "\t" + hostName + Environment.NewLine);
        }

        public static bool LocalHostExists(string hostName, string ipAddress)
        {
            var lines = File.ReadAllLines(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "drivers/etc/hosts"));
            if (lines == null || lines.Length < 1) return false;
            for (var i = 0; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                lines[i] = lines[i].Trim();
                if (lines[i].StartsWith(ipAddress) && lines[i].EndsWithI(hostName)) return true;
            }
            return false;
        }

        public static HashSet<string> FindIPProxiesText(this string text)
        {
            const string ProxyPattern = @"\d{1,3}(\.\d{1,3}){3}:\d{1,5}";

            return text.FindAll(ProxyPattern);
        }


        public static string FindIP(this string text)
        {
            const string ipPattern = @"\d{1,3}(\.\d{1,3}){3}";
            return text.FindFirst(ipPattern);
        }

        public static HashSet<string> FindIPs(this string text)
        {
            const string ipPattern = @"\d{1,3}(\.\d{1,3}){3}";
            return text.FindAll(ipPattern);
        }
    }
}