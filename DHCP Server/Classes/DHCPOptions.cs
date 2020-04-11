/*
Copyright (c) 2010 Jean-Paul Mikkers
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DNS_Server.Classes
{
    public enum DHCPOption
    {
        Error = -1,
        // 3: RFC 1497 Vendor Extensions
        Pad = 0,
        SubnetMask = 1,
        TimeOffset = 2,
        Router = 3,
        TimeServer = 4,
        NameServer = 5,
        DomainNameServer = 6,
        LogServer = 7,
        CookieServer = 8,
        LPRServer = 9,
        ImpressServer = 10,
        ResourceLocationServer = 11,
        HostName = 12,
        BootFileSize = 13,
        MeritDumpFile = 14,
        DomainName = 15,
        SwapServer = 16,
        RootPath = 17,
        ExtensionPath = 18,

        // 4: IP Layer Parameters per Host
        IPForwardingEnable = 19,
        NonLocalSourceRoutingEnable = 20,
        PolicyFilter = 21,
        MaximumDatagramReassembly = 22,
        DefaultIPTTL = 23,
        PathMTUAgingTimeout = 24,
        PathMTUPlateauTable = 25,

        // 5: IP Layer Parameters per Interface
        InterfaceMTU = 26,
        AllSubnetsAreLocal = 27,
        BroadcastAddress = 28,
        PerformMaskDiscovery = 29,
        MaskSupplier = 30,
        PerformRouterDiscovery = 31,
        RouterSolicitationAddress = 32,
        StaticRoute = 33,

        // 6: Link Layer Parameters per Interface
        TrailerEncapsulation = 34,
        ARPCacheTimeout = 35,
        EthernetEncapsulation = 36,

        // 7: TCP Parameters
        TCPDefaultTTL = 37,
        TCPKeepaliveInterval = 38,
        TCPKeepaliveGarbage = 39,

        // 8: Application and Service parameters
        NetworkInformationServiceDomain = 40,
        NetworkInformationServiceServers = 41,
        NetworkTimeProtocolServers = 42,
        VendorSpecificInformation = 43,
        NetBIOSOverTCPIPNameServer = 44,
        NetBIOSOverTCPIPDatagramDistributionServer = 45,
        NetBIOSOverTCPIPNodeType = 46,
        NetBIOSOverTCPIPScope = 47,
        XWindowSystemFontServer = 48,
        XWindowSystemDisplayManager = 49,
        NetworkInformationServicePlusDomain = 64,
        NetworkInformationServicePlusServers = 65,
        MobileIPHomeAgent = 68,
        SimpleMailTransportProtocolServer = 69,
        PostOfficeProtocolServer = 70,
        NetworkNewsTransportProtocolServer = 71,
        DefaultWorldWideWebServer = 72,
        DefaultFingerServer = 73,
        DefaultInternetRelayChat = 74,
        StreetTalkServer = 75,
        StreetTalkDirectoryAssistanceServer = 76,

        // 9: DHCP Extensions
        RequestedIPAddress = 50,
        IPAddressLeaseTime = 51,
        OptionOverload = 52,
        MessageType = 53,
        ServerIdentifier = 54,
        ParameterRequestList = 55,
        Message = 56,
        MaximumDHCPMessageSize = 57,
        RenewalTimeValue = 58,
        RebindingTimeValue = 59,
        VendorClassIdentifier = 60,
        ClientIdentifier = 61,
        TFTPServerName = 66,
        BootFileName = 67,

        FullyQualifiedDomainName = 81,              // RFC4702

        ClientSystemArchitectureType = 93,          // RFC4578
        ClientNetworkInterfaceIdentifier = 94,      // RFC4578
        ClientMachineIdentifier = 97,               // RFC4578

        AutoConfigure = 116,                        // RFC2563
        ClasslessStaticRoutesA = 121,               // RFC3442

        /*
            128   TFPT Server IP address                        // RFC 4578
            129   Call Server IP address                        // RFC 4578
            130   Discrimination string                         // RFC 4578
            131   Remote statistics server IP address           // RFC 4578
            132   802.1P VLAN ID
            133   802.1Q L2 Priority
            134   Diffserv Code Point
            135   HTTP Proxy for phone-specific applications
         */

        ClasslessStaticRoutesB = 249,

        End = 255,
    }

    public enum DHCPMessageType
    {
        DISCOVER = 1,
        OFFER,
        REQUEST,
        DECLINE,
        ACK,
        NAK,
        RELEASE,
        INFORM,
        Undefined
    }

    public interface IDHCPOption
    {
        bool ZeroTerminatedStrings { get; set; }
        DHCPOption OptionType { get; }
        IDHCPOption FromStream(Stream s);
        void ToStream(Stream s);
    }

    public abstract class DHCPOptionBase : IDHCPOption
    {
        protected DHCPOption m_OptionType;

        public DHCPOption OptionType
        {
            get
            {
                return this.m_OptionType;
            }
        }

        public bool ZeroTerminatedStrings { get; set; }

        public abstract IDHCPOption FromStream(Stream s);
        public abstract void ToStream(Stream s);

        protected DHCPOptionBase(DHCPOption optionType)
        {
            this.m_OptionType = optionType;
        }
    }

    public class DHCPOptionParameterRequestList : DHCPOptionBase
    {
        #region IDHCPOption Members

        public List<DHCPOption> RequestList { get; } = new List<DHCPOption>();

        public override IDHCPOption FromStream(Stream s)
        {
            var result = new DHCPOptionParameterRequestList();
            while (true)
            {
                int c = s.ReadByte();
                if (c < 0)
                {
                    break;
                }

                result.RequestList.Add((DHCPOption)c);
            }
            return result;
        }

        public override void ToStream(Stream s)
        {
            foreach (DHCPOption opt in this.RequestList)
            {
                s.WriteByte((byte)opt);
            }
        }

        #endregion

        public DHCPOptionParameterRequestList()
            : base(DHCPOption.ParameterRequestList)
        {
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (DHCPOption opt in this.RequestList)
            {
                sb.Append(opt.ToString());
                sb.Append(",");
            }
            if (this.RequestList.Count > 0)
            {
                sb.Remove(sb.Length - 1, 1);
            }

            return string.Format("Option(name=[{0}],value=[{1}])", this.OptionType, sb.ToString());
        }
    }

    public class DHCPOptionHostName : DHCPOptionBase
    {
        #region IDHCPOption Members

        public string HostName { get; private set; }

        public override IDHCPOption FromStream(Stream s)
        {
            var result = new DHCPOptionHostName
            {
                HostName = s.ReadString()
            };
            return result;
        }

        public override void ToStream(Stream s)
        {
            s.WriteString(this.ZeroTerminatedStrings, this.HostName);
        }

        #endregion

        public DHCPOptionHostName()
            : base(DHCPOption.HostName)
        {
        }

        public DHCPOptionHostName(string hostName)
            : base(DHCPOption.HostName)
        {
            this.HostName = hostName;
        }

        public override string ToString()
        {
            return string.Format("Option(name=[{0}],value=[{1}])", this.OptionType, this.HostName);
        }

    }

    public class DHCPOptionMessage : DHCPOptionBase
    {
        #region IDHCPOption Members

        public string Message { get; private set; }

        public override IDHCPOption FromStream(Stream s)
        {
            var result = new DHCPOptionMessage
            {
                Message = s.ReadString()
            };
            return result;
        }

        public override void ToStream(Stream s)
        {
            s.WriteString(this.ZeroTerminatedStrings, this.Message);
        }

        #endregion

        public DHCPOptionMessage()
            : base(DHCPOption.Message)
        {
        }

        public DHCPOptionMessage(string message)
            : base(DHCPOption.Message)
        {
            this.Message = message;
        }

        public override string ToString()
        {
            return string.Format("Option(name=[{0}],value=[{1}])", this.OptionType, this.Message);
        }
    }

    public class DHCPOptionTFTPServerName : DHCPOptionBase
    {
        #region IDHCPOption Members

        public string Name { get; private set; }

        public override IDHCPOption FromStream(Stream s)
        {
            var result = new DHCPOptionTFTPServerName
            {
                Name = s.ReadString()
            };
            return result;
        }

        public override void ToStream(Stream s)
        {
            s.WriteString(this.ZeroTerminatedStrings, this.Name);
        }

        #endregion

        public DHCPOptionTFTPServerName()
            : base(DHCPOption.TFTPServerName)
        {
        }

        public DHCPOptionTFTPServerName(string name)
            : base(DHCPOption.TFTPServerName)
        {
            this.Name = name;
        }

        public override string ToString()
        {
            return string.Format("Option(name=[{0}],value=[{1}])", this.OptionType, this.Name);
        }
    }

    public class DHCPOptionBootFileName : DHCPOptionBase
    {
        #region IDHCPOption Members

        public string Name { get; private set; }

        public override IDHCPOption FromStream(Stream s)
        {
            var result = new DHCPOptionBootFileName
            {
                Name = s.ReadString()
            };
            return result;
        }

        public override void ToStream(Stream s)
        {
            s.WriteString(this.ZeroTerminatedStrings, this.Name);
        }

        #endregion

        public DHCPOptionBootFileName()
            : base(DHCPOption.BootFileName)
        {
        }

        public DHCPOptionBootFileName(string name)
            : base(DHCPOption.BootFileName)
        {
            this.Name = name;
        }

        public override string ToString()
        {
            return string.Format("Option(name=[{0}],value=[{1}])", this.OptionType, this.Name);
        }
    }

    public class DHCPOptionOptionOverload : DHCPOptionBase
    {
        #region IDHCPOption Members

        public byte Overload { get; private set; }

        public override IDHCPOption FromStream(Stream s)
        {
            var result = new DHCPOptionOptionOverload();
            if (s.Length != 1)
            {
                throw new IOException("Invalid DHCP option length");
            }

            result.Overload = (byte)s.ReadByte();
            return result;
        }

        public override void ToStream(Stream s)
        {
            s.WriteByte(this.Overload);
        }

        #endregion

        public DHCPOptionOptionOverload()
            : base(DHCPOption.OptionOverload)
        {
        }

        public DHCPOptionOptionOverload(byte overload)
            : base(DHCPOption.OptionOverload)
        {
            this.Overload = overload;
        }

        public override string ToString()
        {
            return string.Format("Option(name=[{0}],value=[{1}])", this.OptionType, this.Overload);
        }
    }

    public class DHCPOptionMaximumDHCPMessageSize : DHCPOptionBase
    {
        #region IDHCPOption Members

        public ushort MaxSize { get; private set; }

        public override IDHCPOption FromStream(Stream s)
        {
            var result = new DHCPOptionMaximumDHCPMessageSize();
            if (s.Length != 2)
            {
                throw new IOException("Invalid DHCP option length");
            }

            result.MaxSize = s.ReadUInt16();
            return result;
        }

        public override void ToStream(Stream s)
        {
            s.WriteUInt16(this.MaxSize);
        }

        #endregion

        public DHCPOptionMaximumDHCPMessageSize()
            : base(DHCPOption.MaximumDHCPMessageSize)
        {
        }

        public DHCPOptionMaximumDHCPMessageSize(ushort maxSize)
            : base(DHCPOption.MaximumDHCPMessageSize)
        {
            this.MaxSize = maxSize;
        }

        public override string ToString()
        {
            return string.Format("Option(name=[{0}],value=[{1}])", this.OptionType, this.MaxSize);
        }
    }

    public class DHCPOptionMessageType : DHCPOptionBase
    {
        #region IDHCPOption Members

        public DHCPMessageType MessageType { get; private set; }

        public override IDHCPOption FromStream(Stream s)
        {
            var result = new DHCPOptionMessageType();
            if (s.Length != 1)
            {
                throw new IOException("Invalid DHCP option length");
            }

            result.MessageType = (DHCPMessageType)s.ReadByte();
            return result;
        }

        public override void ToStream(Stream s)
        {
            s.WriteByte((byte)this.MessageType);
        }

        #endregion

        public DHCPOptionMessageType()
            : base(DHCPOption.MessageType)
        {
        }

        public DHCPOptionMessageType(DHCPMessageType messageType)
            : base(DHCPOption.MessageType)
        {
            this.MessageType = messageType;
        }

        public override string ToString()
        {
            return string.Format("Option(name=[{0}],value=[{1}])", this.OptionType, this.MessageType);
        }
    }

    public class DHCPOptionServerIdentifier : DHCPOptionBase
    {
        #region IDHCPOption Members

        public IPAddress IPAddress { get; private set; }

        public override IDHCPOption FromStream(Stream s)
        {
            var result = new DHCPOptionServerIdentifier();
            if (s.Length != 4)
            {
                throw new IOException("Invalid DHCP option length");
            }

            result.IPAddress = s.ReadIPAddress();
            return result;
        }

        public override void ToStream(Stream s)
        {
            s.WriteIPAddress(this.IPAddress);
        }

        #endregion

        public DHCPOptionServerIdentifier()
            : base(DHCPOption.ServerIdentifier)
        {
        }

        public DHCPOptionServerIdentifier(IPAddress ipAddress)
            : base(DHCPOption.ServerIdentifier)
        {
            this.IPAddress = ipAddress;
        }

        public override string ToString()
        {
            return string.Format("Option(name=[{0}],value=[{1}])", this.OptionType, this.IPAddress.ToString());
        }
    }

    public class DHCPOptionRequestedIPAddress : DHCPOptionBase
    {
        #region IDHCPOption Members

        public IPAddress IPAddress { get; private set; }

        public override IDHCPOption FromStream(Stream s)
        {
            var result = new DHCPOptionRequestedIPAddress();
            if (s.Length != 4)
            {
                throw new IOException("Invalid DHCP option length");
            }

            result.IPAddress = s.ReadIPAddress();
            return result;
        }

        public override void ToStream(Stream s)
        {
            s.WriteIPAddress(this.IPAddress);
        }

        #endregion

        public DHCPOptionRequestedIPAddress()
            : base(DHCPOption.RequestedIPAddress)
        {
        }

        public DHCPOptionRequestedIPAddress(IPAddress ipAddress)
            : base(DHCPOption.RequestedIPAddress)
        {
            this.IPAddress = ipAddress;
        }

        public override string ToString()
        {
            return string.Format("Option(name=[{0}],value=[{1}])", this.OptionType, this.IPAddress.ToString());
        }
    }

    public class DHCPOptionSubnetMask : DHCPOptionBase
    {
        #region IDHCPOption Members

        public IPAddress SubnetMask { get; private set; }

        public override IDHCPOption FromStream(Stream s)
        {
            var result = new DHCPOptionSubnetMask();
            if (s.Length != 4)
            {
                throw new IOException("Invalid DHCP option length");
            }

            result.SubnetMask = s.ReadIPAddress();
            return result;
        }

        public override void ToStream(Stream s)
        {
            s.WriteIPAddress(this.SubnetMask);
        }

        #endregion

        public DHCPOptionSubnetMask()
            : base(DHCPOption.SubnetMask)
        {
        }

        public DHCPOptionSubnetMask(IPAddress subnetMask)
            : base(DHCPOption.SubnetMask)
        {
            this.SubnetMask = subnetMask;
        }

        public override string ToString()
        {
            return string.Format("Option(name=[{0}],value=[{1}])", this.OptionType, this.SubnetMask.ToString());
        }
    }

    public class DHCPOptionIPAddressLeaseTime : DHCPOptionBase
    {
        #region IDHCPOption Members

        public TimeSpan LeaseTime
        {
            get;
            private set;
        }

        public override IDHCPOption FromStream(Stream s)
        {
            var result = new DHCPOptionIPAddressLeaseTime();
            if (s.Length != 4)
            {
                throw new IOException("Invalid DHCP option length");
            }

            this.LeaseTime = TimeSpan.FromSeconds(s.ReadUInt32());
            return result;
        }

        public override void ToStream(Stream s)
        {
            s.WriteUInt32((uint)this.LeaseTime.TotalSeconds);
        }

        #endregion

        public DHCPOptionIPAddressLeaseTime()
            : base(DHCPOption.IPAddressLeaseTime)
        {
        }

        public DHCPOptionIPAddressLeaseTime(TimeSpan leaseTime)
            : base(DHCPOption.IPAddressLeaseTime)
        {
            this.LeaseTime = leaseTime;
            if (this.LeaseTime > Utility.InfiniteTimeSpan)
            {
                this.LeaseTime = Utility.InfiniteTimeSpan;
            }
        }

        public override string ToString()
        {
            return string.Format("Option(name=[{0}],value=[{1}])", this.OptionType, this.LeaseTime == Utility.InfiniteTimeSpan ? "Infinite" : this.LeaseTime.ToString());
        }
    }

    public class DHCPOptionRenewalTimeValue : DHCPOptionBase
    {
        #region IDHCPOption Members

        public TimeSpan TimeSpan
        {
            get;
            private set;
        }

        public override IDHCPOption FromStream(Stream s)
        {
            var result = new DHCPOptionRenewalTimeValue();
            if (s.Length != 4)
            {
                throw new IOException("Invalid DHCP option length");
            }

            this.TimeSpan = TimeSpan.FromSeconds(s.ReadUInt32());
            return result;
        }

        public override void ToStream(Stream s)
        {
            s.WriteUInt32((uint)this.TimeSpan.TotalSeconds);
        }

        #endregion

        public DHCPOptionRenewalTimeValue()
            : base(DHCPOption.RenewalTimeValue)
        {
        }

        public DHCPOptionRenewalTimeValue(TimeSpan timeSpan)
            : base(DHCPOption.RenewalTimeValue)
        {
            this.TimeSpan = timeSpan;
        }

        public override string ToString()
        {
            return string.Format("Option(name=[{0}],value=[{1}])", this.OptionType, this.TimeSpan.ToString());
        }
    }

    public class DHCPOptionRebindingTimeValue : DHCPOptionBase
    {
        #region IDHCPOption Members

        public TimeSpan TimeSpan
        {
            get;
            private set;
        }

        public override IDHCPOption FromStream(Stream s)
        {
            var result = new DHCPOptionRebindingTimeValue();
            if (s.Length != 4)
            {
                throw new IOException("Invalid DHCP option length");
            }

            this.TimeSpan = TimeSpan.FromSeconds(s.ReadUInt32());
            return result;
        }

        public override void ToStream(Stream s)
        {
            s.WriteUInt32((uint)this.TimeSpan.TotalSeconds);
        }

        #endregion

        public DHCPOptionRebindingTimeValue()
            : base(DHCPOption.RebindingTimeValue)
        {
        }

        public DHCPOptionRebindingTimeValue(TimeSpan timeSpan)
            : base(DHCPOption.RebindingTimeValue)
        {
            this.TimeSpan = timeSpan;
        }

        public override string ToString()
        {
            return string.Format("Option(name=[{0}],value=[{1}])", this.OptionType, this.TimeSpan.ToString());
        }
    }

    public class DHCPOptionGeneric : DHCPOptionBase
    {
        public byte[] Data { get; set; }

        #region IDHCPOption Members

        public override IDHCPOption FromStream(Stream s)
        {
            var result = new DHCPOptionGeneric(this.m_OptionType)
            {
                Data = new byte[s.Length]
            };
            s.Read(result.Data, 0, result.Data.Length);
            return result;
        }

        public override void ToStream(Stream s)
        {
            s.Write(this.Data, 0, this.Data.Length);
        }

        #endregion

        public DHCPOptionGeneric(DHCPOption option) : base(option)
        {
            this.Data = Array.Empty<byte>();
        }

        public DHCPOptionGeneric(DHCPOption option, byte[] data) : base(option)
        {
            this.Data = data;
        }

        public override string ToString()
        {
            return string.Format("Option(name=[{0}],value=[{1}])", this.m_OptionType, Utility.BytesToHexString(this.Data, " "));
        }
    }

    public class DHCPOptionFullyQualifiedDomainName : DHCPOptionBase
    {
        public byte[] Data { get; set; }

        #region IDHCPOption Members

        public override IDHCPOption FromStream(Stream s)
        {
            var result = new DHCPOptionFullyQualifiedDomainName
            {
                Data = new byte[s.Length]
            };
            s.Read(result.Data, 0, result.Data.Length);
            return result;
        }

        public override void ToStream(Stream s)
        {
            s.Write(this.Data, 0, this.Data.Length);
        }

        #endregion

        public DHCPOptionFullyQualifiedDomainName()
            : base(DHCPOption.FullyQualifiedDomainName)
        {
            this.Data = Array.Empty<byte>();
        }

        public override string ToString()
        {
            return string.Format("Option(name=[{0}],value=[{1}])", this.OptionType, Utility.BytesToHexString(this.Data, " "));
        }
    }

    public class DHCPOptionVendorClassIdentifier : DHCPOptionBase
    {
        public byte[] Data { get; set; }

        #region IDHCPOption Members

        public override IDHCPOption FromStream(Stream s)
        {
            var result = new DHCPOptionVendorClassIdentifier
            {
                Data = new byte[s.Length]
            };
            s.Read(result.Data, 0, result.Data.Length);
            return result;
        }

        public override void ToStream(Stream s)
        {
            s.Write(this.Data, 0, this.Data.Length);
        }

        #endregion

        public DHCPOptionVendorClassIdentifier()
            : base(DHCPOption.VendorClassIdentifier)
        {
            this.Data = Array.Empty<byte>();
        }

        public DHCPOptionVendorClassIdentifier(byte[] data)
            : base(DHCPOption.VendorClassIdentifier)
        {
            this.Data = data;
        }

        public override string ToString()
        {
            return string.Format("Option(name=[{0}],value=[{1}])", this.OptionType, Utility.BytesToHexString(this.Data, " "));
        }
    }

    public class DHCPOptionClientIdentifier : DHCPOptionBase
    {
        public HardwareType HardwareType { get; set; }

        public byte[] Data { get; set; }

        #region IDHCPOption Members

        public override IDHCPOption FromStream(Stream s)
        {
            var result = new DHCPOptionClientIdentifier();
            this.HardwareType = (HardwareType)s.ReadByte();
            result.Data = new byte[s.Length - s.Position];
            s.Read(result.Data, 0, result.Data.Length);
            return result;
        }

        public override void ToStream(Stream s)
        {
            s.WriteByte((byte)this.HardwareType);
            s.Write(this.Data, 0, this.Data.Length);
        }

        #endregion

        public DHCPOptionClientIdentifier()
            : base(DHCPOption.ClientIdentifier)
        {
            this.HardwareType = HardwareType.Unknown;
            this.Data = Array.Empty<byte>();
        }

        public DHCPOptionClientIdentifier(HardwareType hardwareType, byte[] data)
            : base(DHCPOption.ClientIdentifier)
        {
            this.HardwareType = hardwareType;
            this.Data = data;
        }

        public override string ToString()
        {
            return string.Format("Option(name=[{0}],htype=[{1}],value=[{2}])", this.OptionType, this.HardwareType, Utility.BytesToHexString(this.Data, " "));
        }
    }

    public class DHCPOptionVendorSpecificInformation : DHCPOptionBase
    {
        public byte[] Data { get; set; }

        #region IDHCPOption Members

        public override IDHCPOption FromStream(Stream s)
        {
            var result = new DHCPOptionVendorSpecificInformation
            {
                Data = new byte[s.Length]
            };
            s.Read(result.Data, 0, result.Data.Length);
            return result;
        }

        public override void ToStream(Stream s)
        {
            s.Write(this.Data, 0, this.Data.Length);
        }

        #endregion

        public DHCPOptionVendorSpecificInformation()
            : base(DHCPOption.VendorSpecificInformation)
        {
            this.Data = Array.Empty<byte>();
        }

        public DHCPOptionVendorSpecificInformation(byte[] data)
            : base(DHCPOption.VendorSpecificInformation)
        {
            this.Data = data;
        }

        public DHCPOptionVendorSpecificInformation(string data)
            : base(DHCPOption.VendorSpecificInformation)
        {
            var ms = new MemoryStream();
            ms.WriteString(this.ZeroTerminatedStrings, data);
            ms.Flush();
            this.Data = ms.ToArray();
        }

        public override string ToString()
        {
            return string.Format("Option(name=[{0}],value=[{1}])", this.OptionType, Utility.BytesToHexString(this.Data, " "));
        }
    }

    public class DHCPOptionFixedLength : DHCPOptionBase
    {
        #region IDHCPOption Members

        public override IDHCPOption FromStream(Stream s)
        {
            return this;
        }

        public override void ToStream(Stream s)
        {
        }

        #endregion

        public DHCPOptionFixedLength(DHCPOption option) : base(option)
        {
        }

        public override string ToString()
        {
            return string.Format("Option(name=[{0}],value=[])", this.m_OptionType);
        }
    }
}
