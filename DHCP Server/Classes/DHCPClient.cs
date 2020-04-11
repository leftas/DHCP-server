using DNS_Server.Models;
using System;
using System.Linq;
using System.Net;
using System.Xml.Serialization;

namespace DNS_Server.Classes
{
    [Serializable]
    public class DHCPClient : BaseViewModel, IEquatable<DHCPClient>
    {
        private TimeSpan leaseDuration;

        public enum ClientState
        {
            Released,
            Offered,
            Bound,
            Expired
        }

        public byte[] Identifer
        {
            get => this.identifier;
            set => this.SetIdentifier(value);
        }

        private byte[] identifier;

        public byte[] GetIdentifier()
        {
            return this.identifier ?? Array.Empty<byte>();
        }

        public void SetIdentifier(byte[] value)
        {
            this.identifier = value;
            this.OnPropertyChanged(nameof(this.Identifer));
        }

        public byte[] HardwareAddress
        {
            get => this.hardwareAddress;
            set => this.SetHardwareAddress(value);
        }

        private byte[] hardwareAddress;
        private DateTime timeStateStarted;
        private ClientState state;
        private IPAddress iPAddress = IPAddress.Any;

        public byte[] GetHardwareAddress()
        {
            return this.hardwareAddress ?? Array.Empty<byte>();
        }

        public void SetHardwareAddress(byte[] value)
        {
            this.hardwareAddress = value;
            this.OnPropertyChanged(nameof(this.HardwareAddress));
            this.OnPropertyChanged(nameof(this.Client));


        }

#pragma warning disable CA2235 // Mark all non-serializable fields
        public string Hostname { get; set; } = string.Empty;
#pragma warning restore CA2235 // Mark all non-serializable fields

        public ClientState State
        {
            get => this.state;
            set
            {
                this.state = value;
                this.OnPropertyChanged(nameof(this.State));
            }
        }

#pragma warning disable CA2235 // Mark all non-serializable fields
        public DateTime TimeStateStarted
        {
            get { return this.timeStateStarted; }
            set
            {
                this.timeStateStarted = value;
                this.OnPropertyChanged(nameof(this.TimeStateStarted));
            }
        }
#pragma warning restore CA2235 // Mark all non-serializable fields

        public TimeSpan StateDuration
        {
            get => this.leaseDuration;
            set
            {
                this.leaseDuration = Utility.SanitizeTimeSpan(value);
                this.OnPropertyChanged(nameof(this.TimeStateEnd));
                this.OnPropertyChanged(nameof(this.StateDuration));
            }
        }

        public DateTime TimeStateEnd
        {
            get
            {
                return Utility.IsInfiniteTimeSpan(this.StateDuration) ? DateTime.MaxValue : (this.TimeStateStarted + this.StateDuration);
            }
            set
            {
                if (value >= DateTime.MaxValue)
                {
                    this.StateDuration = Utility.InfiniteTimeSpan;
                }
                else
                {
                    this.StateDuration = value - this.TimeStateStarted;
                }
            }
        }

        [XmlIgnore]
        public IPAddress IPAddress
        {
            get { return this.iPAddress; }

            set { this.iPAddress = value; this.OnPropertyChanged(nameof(this.IPAddress)); }
        }
#pragma warning restore CA2235 // Mark all non-serializable fields
        [XmlElement(elementName: "IPAddress")]
        public string IPAddressString
        {
            get => this.IPAddress.ToString();
            set => this.IPAddress = IPAddress.Parse(value);
        }

        public string Client => $"{Utility.BytesToHexString(this.GetHardwareAddress(), "-")} ({this.Hostname})";

        public DHCPClient Clone()
        {
            var client = new DHCPClient
            {
                Hostname = this.Hostname,
                State = this.State,
                IPAddress = this.IPAddress,
                TimeStateStarted = this.TimeStateStarted,
                StateDuration = this.StateDuration
            };
            client.SetIdentifier(this.GetIdentifier());
            client.SetHardwareAddress(this.GetHardwareAddress());
            return client;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                byte[] id = this.Identifer;
                int hashCode = 0;
                foreach (var idByte in id)
                {
                    hashCode += (idByte * 31) ^ idByte;
                }
                return hashCode;
            }
        }

        public bool Equals(DHCPClient other)
        {
            return other?.GetIdentifier().SequenceEqual(this.GetIdentifier()) ?? false;
        }

        internal static DHCPClient CreateFromDHCPMessage(DHCPMessage message)
        {
            var client = new DHCPClient();

            client.SetHardwareAddress(message.GetClientHardwareAddress());

            var name = (DHCPOptionHostName)message.GetOption(DHCPOption.HostName);

            client.Hostname = name?.HostName;

            var identifer = (DHCPOptionClientIdentifier)message.GetOption(DHCPOption.ClientIdentifier);

            client.SetIdentifier(identifer?.Data ?? message.GetClientHardwareAddress());

            return client;
        }
    }
}
