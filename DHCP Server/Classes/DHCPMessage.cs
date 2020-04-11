using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace DNS_Server.Classes
{
    public enum HardwareType : byte
    {
        Unknown = 0x0,
        Ethernet = 0x01,
        ExperimentalEthernet,
        AmateurRadio,
        ProteonTokenRing,
        Chaos,
        IEEE802Networks,
        ArcNet,
        Hyperchnnel,
        Lanstar
    }

    public enum DhcpOperation : byte
    {
        BootRequest = 0x01,
        BootReply
    }

    public sealed class DHCPMessage
    {
        private static readonly IDHCPOption[] optionsTemplates;

        static DHCPMessage()
        {
            optionsTemplates = new IDHCPOption[256];
            for (int t = 1; t < 255; t++)
            {
                optionsTemplates[t] = new DHCPOptionGeneric((DHCPOption)t);
            }
            optionsTemplates[0] = new DHCPOptionFixedLength(DHCPOption.Pad);
            optionsTemplates[255] = new DHCPOptionFixedLength(DHCPOption.End);
            optionsTemplates[(int)DHCPOption.HostName] = new DHCPOptionHostName();
            optionsTemplates[(int)DHCPOption.IPAddressLeaseTime] = new DHCPOptionIPAddressLeaseTime();
            optionsTemplates[(int)DHCPOption.ServerIdentifier] = new DHCPOptionServerIdentifier();
            optionsTemplates[(int)DHCPOption.RequestedIPAddress] = new DHCPOptionRequestedIPAddress();
            optionsTemplates[(int)DHCPOption.OptionOverload] = new DHCPOptionOptionOverload();
            optionsTemplates[(int)DHCPOption.TFTPServerName] = new DHCPOptionTFTPServerName();
            optionsTemplates[(int)DHCPOption.BootFileName] = new DHCPOptionBootFileName();
            optionsTemplates[(int)DHCPOption.MessageType] = new DHCPOptionMessageType();
            optionsTemplates[(int)DHCPOption.Message] = new DHCPOptionMessage();
            optionsTemplates[(int)DHCPOption.MaximumDHCPMessageSize] = new DHCPOptionMaximumDHCPMessageSize();
            optionsTemplates[(int)DHCPOption.ParameterRequestList] = new DHCPOptionParameterRequestList();
            optionsTemplates[(int)DHCPOption.RenewalTimeValue] = new DHCPOptionRenewalTimeValue();
            optionsTemplates[(int)DHCPOption.RebindingTimeValue] = new DHCPOptionRebindingTimeValue();
            optionsTemplates[(int)DHCPOption.VendorClassIdentifier] = new DHCPOptionVendorClassIdentifier();
            optionsTemplates[(int)DHCPOption.ClientIdentifier] = new DHCPOptionClientIdentifier();
            optionsTemplates[(int)DHCPOption.FullyQualifiedDomainName] = new DHCPOptionFullyQualifiedDomainName();
            optionsTemplates[(int)DHCPOption.SubnetMask] = new DHCPOptionSubnetMask();
        }
        public DhcpOperation Operation { get; set; }
        public HardwareType HardwareType { get; set; } = HardwareType.Ethernet;

        //There is goes ClientHardwareAddress

        public byte Hops { get; set; }
        public uint XID { get; set; }
        public ushort SecondsElapsed { get; set; }
        public ushort Flags { get; set; } // 0x8000 - Broadcast bit

        public bool Broadcast => ((this.Flags >> 15) & 1) != 0;
        public IPAddress ClientIPAddress { get; set; } = IPAddress.Any;
        public IPAddress YourIPAddress { get; set; } = IPAddress.Any;
        public IPAddress NextServerIPAddress { get; set; } = IPAddress.Any;
        public IPAddress RelayAgentIPAddress { get; set; } = IPAddress.Any;

        private byte[] clientHardwareAddress;

        public byte[] GetClientHardwareAddress()
        {
            return this.clientHardwareAddress;
        }

        public void SetClientHardwareAddress(byte[] value)
        {
            this.clientHardwareAddress = value;
        }

        public string ServerName { get; set; } = string.Empty;
        public string BootFileName { get; set; } = string.Empty;

        public List<IDHCPOption> Options { get; set; } = new List<IDHCPOption>();

        public DHCPMessage() { }

        public DHCPMessage(DHCPMessage source)
        {
            this.Operation = DhcpOperation.BootReply;
            this.HardwareType = source.HardwareType;
            this.SetClientHardwareAddress(source.GetClientHardwareAddress());
            this.Hops = 0;
            this.XID = source.XID;
            this.SecondsElapsed = 0;
            this.Flags = source.Flags;
            this.ClientIPAddress = IPAddress.Any;
            this.YourIPAddress = IPAddress.Any;
            this.NextServerIPAddress = IPAddress.Any;
            this.RelayAgentIPAddress = source.RelayAgentIPAddress;
        }

        private DHCPMessage(Stream stream)
        {
            this.Operation = (DhcpOperation)stream.ReadByte();
            this.HardwareType = (HardwareType)stream.ReadByte();
            this.SetClientHardwareAddress(new byte[stream.ReadByte()]);
            this.Hops = (byte)stream.ReadByte();
            this.XID = stream.ReadUInt32();
            this.SecondsElapsed = stream.ReadUInt16();
            this.Flags = stream.ReadUInt16();
            this.ClientIPAddress = stream.ReadIPAddress();
            this.YourIPAddress = stream.ReadIPAddress();
            this.NextServerIPAddress = stream.ReadIPAddress();
            this.RelayAgentIPAddress = stream.ReadIPAddress();

            stream.Read(this.GetClientHardwareAddress(), 0, this.GetClientHardwareAddress().Length);

            for(int i = this.GetClientHardwareAddress().Length; i < 16; i++)
            {
                stream.ReadByte();
            }

            byte[] serverHostNameBuffer = new byte[64];
            stream.Read(serverHostNameBuffer, 0, serverHostNameBuffer.Length);

            byte[] bootFileNameBuffer = new byte[128];
            stream.Read(bootFileNameBuffer, 0, bootFileNameBuffer.Length);

            byte magicByte;
            if ((magicByte = (byte)stream.ReadByte()) != 0x63)
            {
                throw new IOException($"Magic options byte was not correct\n Should been 0x63 was 0x{magicByte:X} \n Stream offset: {stream.Position - 1}");
            }
            if ((magicByte = (byte)stream.ReadByte()) != 0x82)
            {
                throw new IOException($"Magic options byte was not correct\n Should been 0x82 was 0x{magicByte:X} \n Stream offset: {stream.Position - 1}");
            }
            if ((magicByte = (byte)stream.ReadByte()) != 0x53)
            {
                throw new IOException($"Magic options byte was not correct\n Should been 0x53 was 0x{magicByte:X} \n Stream offset: {stream.Position - 1}");
            }
            if ((magicByte = (byte)stream.ReadByte()) != 0x63)
            {
                throw new IOException($"Magic options byte was not correct\n Should been 0x63 was 0x{magicByte:X} \n Stream offset: {stream.Position - 1}");
            }

            byte[] optionsBuffer = new byte[stream.Length - stream.Position];
            stream.Read(optionsBuffer, 0, optionsBuffer.Length);

            switch (this.CheckOverload(new MemoryStream(optionsBuffer)))
            {
                default:
                    this.ServerName = StreamHelper.ReadZString(new MemoryStream(serverHostNameBuffer));
                    this.BootFileName = StreamHelper.ReadZString(new MemoryStream(bootFileNameBuffer));
                    this.Options = this.AssignOptions(new MemoryStream(optionsBuffer));
                    break;

                case 1:
                    this.BootFileName = StreamHelper.ReadZString(new MemoryStream(bootFileNameBuffer));
                    this.Options = this.AssignOptions(new MemoryStream(optionsBuffer), new MemoryStream(serverHostNameBuffer));
                    break;

                case 2:
                    this.ServerName = StreamHelper.ReadZString(new MemoryStream(serverHostNameBuffer));
                    this.Options = this.AssignOptions(new MemoryStream(optionsBuffer), new MemoryStream(bootFileNameBuffer));
                    break;
                case 3:
                    this.Options = this.AssignOptions(new MemoryStream(optionsBuffer), new MemoryStream(serverHostNameBuffer), new MemoryStream(bootFileNameBuffer));
                    break;
            }
        }

        public IDHCPOption GetOption(DHCPOption optionType) => this.Options.Find(v => v.OptionType == optionType);

        public bool IsRequestedParameter(DHCPOption optionType)
        {
            var dhcpOptionParameterRequestList = (DHCPOptionParameterRequestList)this.GetOption(DHCPOption.ParameterRequestList);
            return dhcpOptionParameterRequestList?.RequestList.Contains(optionType) == true;
        }

        private List<IDHCPOption> AssignOptions(Stream options, params Stream[] addtional)
        {
            var optionsList = new List<IDHCPOption>();

            this.AddOptions(optionsList, options);

            foreach(var stream in addtional)
            {
                this.AddOptions(optionsList, stream);
            }

            return optionsList;
        }

        private void AddOptions(List<IDHCPOption> options, Stream stream)
        {
            while(true)
            {
                var option = (DHCPOption)stream.ReadByte();
                if(option == DHCPOption.Pad)
                {
                    continue;
                }
                if(option == DHCPOption.Error || option == DHCPOption.End)
                {
                    break;
                }
                else
                {
                    int len = stream.ReadByte();
                    if(len == -1)
                    {
                        break;
                    }

                    var ms = new MemoryStream(len);
                    stream.WriteToOtherStream(ms, len);
                    ms.Seek(0, SeekOrigin.Begin);
                    options.Add(optionsTemplates[(int)option].FromStream(ms));
                }
            }
        }

        private byte CheckOverload(Stream stream)
        {
            byte result = 0;

            while(true)
            {
                var option = (DHCPOption)stream.ReadByte();

                if(option == DHCPOption.Error || option == DHCPOption.End)
                {
                    break;
                }
                else if(option == DHCPOption.Pad)
                {
                    continue;
                }
                else if(option == DHCPOption.OptionOverload)
                {
                    if (stream.ReadByte() != 1)
                    {
                        throw new IOException("Invalid size of DHCPOption.OptionOverload");
                    }

                    result = (byte)stream.ReadByte();
                }
                else
                {
                    int l = stream.ReadByte();
                    if (l == -1)
                    {
                        break;
                    }
                    stream.Position += l;
                }
            }

            return result;
        }

        public DHCPMessageType MessageType
        {
            get
            {
                var message = (DHCPOptionMessageType)this.GetOption(DHCPOption.MessageType);
                return message?.MessageType ?? DHCPMessageType.Undefined;
            }
            set
            {
                var message = this.MessageType;
                if(message != value)
                {
                    this.Options.Add(new DHCPOptionMessageType(value));
                }
            }
        }

        public static DHCPMessage FromStream(Stream s)
        {
            return new DHCPMessage(s);
        }

        public Stream ToStream(int minPacketSize)
        {
            Stream stream = new MemoryStream(minPacketSize);

            stream.WriteByte((byte)this.Operation);
            stream.WriteByte((byte)this.HardwareType);
            stream.WriteByte((byte)this.GetClientHardwareAddress().Length);
            stream.WriteByte(this.Hops);
            stream.WriteUInt32(this.XID);
            stream.WriteUInt16(this.SecondsElapsed);
            stream.WriteUInt16(this.Broadcast ? (ushort)0x8000 : (ushort)0x0);
            stream.WriteIPAddress(this.ClientIPAddress);
            stream.WriteIPAddress(this.YourIPAddress);
            stream.WriteIPAddress(this.NextServerIPAddress);
            stream.WriteIPAddress(this.RelayAgentIPAddress);
            stream.Write(this.GetClientHardwareAddress(), 0, this.GetClientHardwareAddress().Length);
            for (int t = this.GetClientHardwareAddress().Length; t < 16; t++)
            {
                stream.WriteByte(0);
            }

            stream.WriteZString(this.ServerName, 64);
            stream.WriteZString(this.BootFileName, 128);

            stream.Write(new byte[]{ 0x63, 0x82, 0x53, 0x63}, 0, 4);

            foreach (IDHCPOption option in this.Options)
            {
                var optionStream = new MemoryStream();
                option.ToStream(optionStream);
                optionStream.Seek(0, SeekOrigin.Begin);

                stream.WriteByte((byte)option.OptionType);
                stream.WriteByte((byte)optionStream.Length);
                optionStream.WriteToOtherStream(stream, (int)optionStream.Length);
            }

            stream.WriteByte(255);
            stream.Flush();

            while (stream.Length < minPacketSize)
            {
                stream.WriteByte(0);
            }

            stream.Flush();

            return stream;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendFormat("Operation (op)                    : {0}\r\n", this.Operation)
                .AppendFormat("HardwareType (htype)           : {0}\r\n", this.HardwareType)
                .AppendFormat("Hops                           : {0}\r\n", this.Hops)
                .AppendFormat("XID                            : {0}\r\n", this.XID)
                .AppendFormat("Secs                           : {0}\r\n", this.SecondsElapsed)
                .AppendFormat("Broadcast (flags)              : {0}\r\n", this.Flags)
                .AppendFormat("ClientIPAddress (ciaddr)       : {0}\r\n", this.ClientIPAddress)
                .AppendFormat("YourIPAddress (yiaddr)         : {0}\r\n", this.YourIPAddress)
                .AppendFormat("NextServerIPAddress (siaddr)   : {0}\r\n", this.NextServerIPAddress)
                .AppendFormat("RelayAgentIPAddress (giaddr)   : {0}\r\n", this.RelayAgentIPAddress)
                .AppendFormat("ClientHardwareAddress (chaddr) : {0}\r\n", Utility.BytesToHexString(this.GetClientHardwareAddress(), "-"))
                .AppendFormat("ServerHostName (sname)         : {0}\r\n", this.ServerName)
                .AppendFormat("BootFileName (file)            : {0}\r\n", this.BootFileName);

            foreach (IDHCPOption option in this.Options)
            {
                sb.AppendFormat("Option                         : {0}\r\n", option.ToString());
            }

            return sb.ToString();
        }
    }
}
