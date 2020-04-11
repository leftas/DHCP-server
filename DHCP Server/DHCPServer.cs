using DNS_Server.Classes;
using GalaSoft.MvvmLight.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Xml.Serialization;

namespace DNS_Server
{
    public class DHCPServer
    {
        public event EventHandler FatalExit;

        private readonly string pathToInfo;
        public IPEndPoint EndPoint { get; set; } = new IPEndPoint(IPAddress.Any, 67);
        public IPAddress SubnetMask { get; set; } = IPAddress.Any;
        public IPAddress PoolStart { get; set; } = IPAddress.Any;
        public IPAddress PoolEnd { get; set; } = IPAddress.Broadcast;

        public string Hostname { get; }

        public bool Active { get; private set; }

        public TimeSpan OfferExpirationTime { get; set; } = TimeSpan.FromSeconds(30);

        public TimeSpan LeaseTime { get; set; } = TimeSpan.FromDays(1);

        private int minimumPacketSize = 576;

        private readonly Random random = new Random((int)DateTime.Now.ToBinary());


        public ObservableDictionary<DHCPClient, DHCPClient> Clients { get; set; } = new ObservableDictionary<DHCPClient, DHCPClient>();

        private readonly List<IPAddress> unknownUsedAddresses = new List<IPAddress>();

        [XmlArray]
        public List<DHCPClient> ClientsList => this.Clients.Select(client => client.Value.Clone()).ToList();

        private readonly SemaphoreSlim clientLock = new SemaphoreSlim(1, 1);

        public List<OptionItem> Options { get; set; } = new List<OptionItem>();


        public int MinimumPacketSize
        {
            get => this.minimumPacketSize;
            set
            {
                this.minimumPacketSize = Math.Max(value, 312);
            }
        }

        private UdpClient Socket { get; set; }

        private Timer Timer { get; set; }

        public DHCPServer(string hostname = null, string infoPath = null)
        {
            this.Hostname = hostname ?? System.Environment.MachineName;
            this.pathToInfo = infoPath ?? Environment.CurrentDirectory + "\\config.xml";
            this.Clients.CollectionChanged += this.Clients_CollectionChanged;
        }

        private void Clients_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            this.SaveClients();
        }

        private readonly XmlSerializer serializer = new XmlSerializer(typeof(List<DHCPClient>));

        private const int TRIES = 10;

        private async void SaveClients()
        {
            if(this.readingClients || !this.Active)
            {
                return;
            }
            await this.clientLock.WaitAsync();
            for (int i = 0; i < TRIES; i++)
            {
                try
                {
                    using TextWriter infoSave = new StreamWriter(this.pathToInfo);
                    this.serializer.Serialize(infoSave, this.ClientsList);
                    break;
                }
                catch
                {
                }

                if (i < TRIES)
                {
                    await Task.Delay(this.random.Next(500, 1000));
                }
                else
                {
                    this.clientLock.Release();
                    throw new IOException($"Cannot update client information at: {this.pathToInfo}");
                }
            }
            this.clientLock.Release();
        }
        private bool readingClients = false;
        private async void ReadClients()
        {
            this.readingClients = true;
            await this.clientLock.WaitAsync();
            for (int i = 0; i < TRIES; i++)
            {
                try
                {
                    using TextReader infoSave = new StreamReader(this.pathToInfo);
                    var clientList = (List<DHCPClient>)this.serializer.Deserialize(infoSave);
                    clientList.ForEach(x => this.Clients.Add(x, x));
                    break;
                }
                catch
                {
                }

                if (i < TRIES)
                {
                    await Task.Delay(this.random.Next(500, 1000));
                }
                else
                {
                    this.clientLock.Release();
                    this.readingClients = false;
                    throw new IOException($"Cannot read clients information at: {this.pathToInfo}");
                }
            }
            this.clientLock.Release();
            this.readingClients = false;
        }

        private const uint IOC_IN = 0x80000000;
        private const uint IOC_VENDOR = 0x18000000;
        private const uint SIO_UDP_CONNRESET = IOC_IN | IOC_VENDOR | 12;

        public void Start()
        {
            this.Socket = new UdpClient(this.EndPoint)
            {
                EnableBroadcast = true
            };
            this.Socket.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
            this.Socket.Client.SendBufferSize = 65536;
            this.Socket.Client.ReceiveBufferSize = 65536;
            this.Socket.Client.IOControl((IOControlCode)SIO_UDP_CONNRESET, new byte[] { 0, 0, 0, 0 }, null);
            this.Socket.Ttl = 10;
            this.Timer = new Timer(this.OnTimer, null, TimeSpan.FromMilliseconds(0), TimeSpan.FromSeconds(1));
            this.Active = true;
            this.ReadClients();
            this.Clients.Remove(x => !this.IsIPAddressInPoolRange(x.IPAddress));
            Task.Run(this.Receive);
        }

        public void Stop()
        {
            this.Active = false;
            this.Socket.Close();
            this.Socket.Dispose();
            this.Timer.Dispose();
            this.Clients.Clear();
        }

        private async void OnTimer(object state)
        {
            await this.clientLock.WaitAsync();
            var removeList = this.Clients.Where(x => (x.Value.State == DHCPClient.ClientState.Offered && (DateTime.Now - x.Value.TimeStateStarted) > this.OfferExpirationTime)
                                        || (x.Value.State == DHCPClient.ClientState.Expired && DateTime.Now > x.Value.TimeStateEnd)).ToList();

            removeList.ForEach(x => this.Clients.Remove(x.Value));

            foreach (var client in this.Clients)
            {
                if(client.Value.State == DHCPClient.ClientState.Bound && DateTime.Now > client.Value.TimeStateEnd)
                {
                    client.Value.State = DHCPClient.ClientState.Expired;
                    client.Value.TimeStateEnd = DateTime.Now;
                    client.Value.StateDuration = TimeSpan.FromDays(1);
                }
            }
            this.clientLock.Release();
        }

        private bool IsIPInRange(IPAddress ip, IPAddress start, IPAddress end)
        {
            var intIP = Utility.IPAddressToUInt(ip);
            return intIP <= Utility.IPAddressToUInt(end) && intIP >= Utility.IPAddressToUInt(start);
        }

        private bool IsIPAddressInPoolRange(IPAddress ip)
        {
            return this.IsIPInRange(ip, this.PoolStart, this.PoolEnd);
        }

        private bool IsIPAddressInSubnet(IPAddress ip)
        {
            return (Utility.IPAddressToUInt(ip) & Utility.IPAddressToUInt(this.SubnetMask)) != 0;
        }

        private bool IsFreeIPAddress(IPAddress ip, bool useReleased = true, DHCPClient client = null)
        {
            if(!this.IsIPAddressInSubnet(ip))
            {
                return false;
            }

            if(ip.Equals(this.EndPoint.Address))
            {
                return false;
            }

            foreach(var serverClient in this.Clients.Values)
            {
                if(serverClient.IPAddress.Equals(ip))
                {
                    if(serverClient == client && serverClient.State != DHCPClient.ClientState.Offered)
                    {
                        return true;
                    }
                    else if (serverClient.State == DHCPClient.ClientState.Expired && (DateTime.Now - serverClient.TimeStateStarted) > TimeSpan.FromMinutes(1))
                    {
                        return true;
                    }
                    else if(useReleased && serverClient.State == DHCPClient.ClientState.Released)
                    {
                        serverClient.IPAddress = IPAddress.Any;
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            foreach (var usedip in this.unknownUsedAddresses)
            {
                if (usedip.Equals(ip))
                {
                    return false;
                }
            }

            return true;
        }

        public IPAddress AllocateNewIP(DHCPClient client, DHCPMessage message)
        {
            if(this.Clients.ContainsKey(client))
            {
                var serverClientInfo = this.Clients[client];
                if (serverClientInfo.State != DHCPClient.ClientState.Offered &&
                    this.IsFreeIPAddress(serverClientInfo.IPAddress, client: serverClientInfo))
                {
                    return this.Clients[client].IPAddress;
                }
            }

            var requstedIP = (DHCPOptionRequestedIPAddress)message.GetOption(DHCPOption.RequestedIPAddress);

            if (requstedIP != null && this.IsFreeIPAddress(requstedIP.IPAddress, true, client))
            {
                return requstedIP.IPAddress;
            }

            for(uint host = Utility.IPAddressToUInt(this.PoolStart); host <= Utility.IPAddressToUInt(this.PoolEnd); host++)
            {
                IPAddress ip = Utility.UIntToIPAddress(host);
                if(this.IsFreeIPAddress(ip, false))
                {
                    return ip;
                }
            }

            for (uint host = Utility.IPAddressToUInt(this.PoolStart); host <= Utility.IPAddressToUInt(this.PoolEnd); host++)
            {
                IPAddress ip = Utility.UIntToIPAddress(host);
                if (this.IsFreeIPAddress(ip, true))
                {
                    return ip;
                }
            }

            return IPAddress.Any;
        }

        private async Task Receive()
        {
            while (true)
            {
                try
                {
                    var result = await this.Socket.ReceiveAsync();

                    using var stream = new MemoryStream(result.Buffer);
                    var message = DHCPMessage.FromStream(stream);

                    var client = DHCPClient.CreateFromDHCPMessage(message);

                    if (message.Operation == DhcpOperation.BootRequest)
                    {
                        await this.clientLock.WaitAsync();

                        switch (message.MessageType)
                        {
                            case DHCPMessageType.DISCOVER:
                            {
                                var serverClient = this.Clients.ContainsKey(client) ? this.Clients[client] : null;
                                if (serverClient != null)
                                {
                                    if (serverClient.State != DHCPClient.ClientState.Bound &&
                                        serverClient.IPAddress.Equals(IPAddress.Any))
                                    {
                                        serverClient.IPAddress = this.AllocateNewIP(serverClient, message);
                                        if (!serverClient.IPAddress.Equals(IPAddress.Any))
                                        {
                                            break;
                                        }
                                    }
                                }
                                else
                                {
                                    client.IPAddress = this.AllocateNewIP(client, message);
                                    if (client.IPAddress.Equals(IPAddress.Any))
                                    {
                                        break;
                                    }
                                    client.State = DHCPClient.ClientState.Offered;
                                    client.TimeStateStarted = DateTime.Now;
                                    client.StateDuration = this.OfferExpirationTime;
                                    if (!this.Clients.ContainsKey(client))
                                    {
                                        this.Clients.Add(client, client);
                                    }
                                    serverClient = client;
                                }
                                //Offer Client
                                await this.SendOffer(message, serverClient.IPAddress);
                            }
                            break;
                            case DHCPMessageType.REQUEST:
                            {
                                if (!this.Clients.ContainsKey(client))
                                {
                                    //We are not aware of this client?
                                    break;
                                }
                                var serverClient = this.Clients[client];
                                var serverIdentifier = (DHCPOptionServerIdentifier)message.GetOption(DHCPOption.ServerIdentifier);
                                var ipaddress = (DHCPOptionRequestedIPAddress)message.GetOption(DHCPOption.RequestedIPAddress);
                                if (serverIdentifier != null)
                                {
                                    //SELECTING STATE
                                    if (!serverIdentifier.IPAddress.Equals(this.EndPoint.Address))
                                    {
                                        // He is not responding to us.
                                        this.Clients.Remove(client);
                                        break;
                                    }
                                    if (serverClient.State != DHCPClient.ClientState.Offered)
                                    {
                                        // We haven't offered him anything? NAK
                                        await this.SendNAK(message);
                                    }
                                    if (!(ipaddress?.IPAddress.Equals(serverClient.IPAddress) ?? false))
                                    {
                                        //He has not requested any IP address OR He explicitly wants another IP address. NAK HIM!
                                        await this.SendNAK(message);
                                    }
                                    serverClient.State = DHCPClient.ClientState.Bound;
                                    serverClient.TimeStateStarted = DateTime.Now;
                                    serverClient.StateDuration = this.LeaseTime;
                                    //Send ACK.
                                    await this.SendACK(message, serverClient);
                                }
                                else
                                {
                                    // This client just rebooted or wants to extend time with me. Therefore, I shall grant his wishes.
                                    // IF AND ONLY IF HE FOLLOWS MY RULES!
                                    if (ipaddress != null)
                                    {
                                        // INIT-REBOOT STATE
                                        if (serverClient.State == DHCPClient.ClientState.Bound ||
                                            serverClient.State == DHCPClient.ClientState.Expired)
                                        {
                                            if (!serverClient.IPAddress.Equals(ipaddress.IPAddress) ||
                                              !this.IsIPAddressInSubnet(ipaddress.IPAddress))
                                            {
                                                // WHAT DO YOU MEAN - Justin Bieber
                                                // Send NAK
                                                // Check for Relay agent address and set broadcast bit if there is.
                                                await this.SendNAK(message);
                                                this.Clients.Remove(serverClient);
                                            }
                                            serverClient.TimeStateStarted = DateTime.Now;
                                            serverClient.StateDuration = this.LeaseTime;
                                            // SEND ACK here
                                            await this.SendACK(message, serverClient);
                                        }
                                        else
                                        {
                                            // WHAT DO YOU MEAN - Justin Bieber
                                            // Send NAK
                                            // Check for Relay agent address and set broadcast bit if there is.
                                            await this.SendNAK(message);
                                            this.Clients.Remove(serverClient);
                                        }
                                    }
                                    else if (!message.ClientIPAddress.Equals(IPAddress.Any))
                                    {
                                        // Renew, rebinding state.
                                        if (serverClient.State == DHCPClient.ClientState.Bound &&
                                            message.ClientIPAddress.Equals(serverClient.IPAddress))
                                        {
                                            //Renew?
                                            serverClient.TimeStateStarted = DateTime.Now;
                                            serverClient.StateDuration = this.LeaseTime;
                                            //SEND ACK
                                            await this.SendACK(message, serverClient);
                                        }
                                        else if (this.IsFreeIPAddress(serverClient.IPAddress, false, serverClient))
                                        {
                                            //REBINDING
                                            serverClient.State = DHCPClient.ClientState.Bound;
                                            serverClient.TimeStateStarted = DateTime.Now;
                                            serverClient.StateDuration = this.LeaseTime;
                                            await this.SendACK(message, serverClient);
                                        }
                                    }
                                    else
                                    {
                                        // Smart guy, wanted to pass by unnoticed! INIT-REBOOT State without IP ADDRESS?
                                        // HOW DARE YOU?
                                    }

                                }

                            }
                            break;
                            case DHCPMessageType.RELEASE:
                            {
                                if (!this.IsThisServer(message) && !this.Clients.ContainsKey(client))
                                {
                                    break;
                                }
                                var serverClient = this.Clients[client];
                                if (message.ClientIPAddress.Equals(serverClient.IPAddress))
                                {
                                    serverClient.State = DHCPClient.ClientState.Released;
                                    serverClient.TimeStateStarted = DateTime.Now;
                                }
                                else
                                {
                                    serverClient.IPAddress = IPAddress.Any;
                                    serverClient.State = DHCPClient.ClientState.Released;
                                    serverClient.TimeStateStarted = DateTime.Now;
                                }
                            }
                            break;
                            case DHCPMessageType.DECLINE:
                            {
                                if (!this.IsThisServer(message) && !this.Clients.ContainsKey(client))
                                {
                                    break;
                                }
                                this.Clients.Remove(client);
                                this.unknownUsedAddresses.Add(message.ClientIPAddress);
                            }
                            break;
                            case DHCPMessageType.INFORM:
                            {
                                // SEND ACK WITHOUT LEASE TIME AND YOURADDRESS FILLED!
                                await this.SendACK(message, null, true);
                            }
                            break;
                        }
                        this.clientLock.Release();
                        this.SaveClients();
                    }
                }
                catch (System.ObjectDisposedException) // Because socket was closed.
                {
                    return;
                }
                catch (System.Net.Sockets.SocketException e)
                {
                    if (this.clientLock.CurrentCount == 0)
                    {
                        this.clientLock.Release();
                    }
                    this.OnFatalError("Unknown error has happened in Receive", e);
                    break;
                }
            }
            if (this.clientLock.CurrentCount == 0)
            {
                this.clientLock.Release();
            }
        }

        private async Task SendMessage(DHCPMessage message, IPEndPoint receiver)
        {
            var stream = (MemoryStream)message.ToStream(this.minimumPacketSize);
            byte[] array = stream.ToArray();
            await this.Socket.SendAsync(array, (int)stream.Length, receiver);
        }

        private void AppendOptions(DHCPMessage senderMsg, DHCPMessage serverMsg)
        {
            foreach(var option in this.Options)
            {
                if(option.Force || senderMsg.IsRequestedParameter(option.Option.OptionType))
                {
                    if(serverMsg.GetOption(option.Option.OptionType) == null)
                    {
                        serverMsg.Options.Add(option.Option);
                    }
                }
            }
        }

        private async Task SendOffer(DHCPMessage source, IPAddress address)
        {

            var message = new DHCPMessage(source)
            {
                YourIPAddress = address,
                RelayAgentIPAddress = source.RelayAgentIPAddress,
                MessageType = DHCPMessageType.OFFER
            };


            message.Options.Add(new DHCPOptionIPAddressLeaseTime(this.LeaseTime));
            message.Options.Add(new DHCPOptionServerIdentifier(((IPEndPoint)this.Socket.Client.LocalEndPoint).Address));
            if(source.IsRequestedParameter(DHCPOption.SubnetMask))
            {
                message.Options.Add(new DHCPOptionSubnetMask(this.SubnetMask));
            }
            this.AppendOptions(source, message);
            await this.SelectAndSendMessage(message, source);



        }

        private async Task SelectAndSendMessage(DHCPMessage msg, DHCPMessage source)
        {
            if(!source.RelayAgentIPAddress.Equals(IPAddress.Any))
            {
                await this.SendMessage(msg, new IPEndPoint(source.RelayAgentIPAddress, 68));
            }
            else
            {
                if(!source.ClientIPAddress.Equals(IPAddress.Any))
                {
                    await this.SendMessage(msg, new IPEndPoint(source.ClientIPAddress, 68));
                }
                else
                {
                    await this.SendMessage(msg, new IPEndPoint(IPAddress.Broadcast, 68));
                }
                //else
                //{
                //    await this.SendMessage(msg, new IPEndPoint(msg.YourIPAddress, 68)); THIS SHOULD BE BEHAVIOUR BY RFC, We cannot override ARP control of Windows.
                //}
            }
        }

        private async Task SendNAK(DHCPMessage source)
        {
            var message = new DHCPMessage(source);
            message.MessageType = DHCPMessageType.NAK;
            message.Options.Add(new DHCPOptionServerIdentifier(((IPEndPoint)this.Socket.Client.LocalEndPoint).Address));

            if(source.IsRequestedParameter(DHCPOption.SubnetMask))
            {
                message.Options.Add(new DHCPOptionSubnetMask(this.SubnetMask));
            }



            if(!source.RelayAgentIPAddress.Equals(IPAddress.Any))
            {
                await this.SendMessage(message, new IPEndPoint(source.RelayAgentIPAddress, 68));
            }
            else
            {
                await this.SendMessage(message, new IPEndPoint(IPAddress.Broadcast, 68));
            }

        }
        //Field      DHCPOFFER            DHCPACK             DHCPNAK
        //-----      ---------            -------             -------
        //'op'       BOOTREPLY            BOOTREPLY           BOOTREPLY
        //'htype'    (From "Assigned Numbers" RFC)
        //'hlen'     (Hardware address length in octets)
        //'hops'     0                    0                   0
        //'xid'      'xid' from client    'xid' from client   'xid' from client
        //           DHCPDISCOVER         DHCPREQUEST         DHCPREQUEST
        //           message              message             message
        //'secs'     0                    0                   0
        //'ciaddr'   0                    'ciaddr' from       0
        //                                DHCPREQUEST or 0
        //'yiaddr'   IP address offered   IP address          0
        //           to client            assigned to client
        //'siaddr'   IP address of next   IP address of next  0
        //           bootstrap server     bootstrap server
        //'flags'    'flags' from         'flags' from        'flags' from
        //           client DHCPDISCOVER  client DHCPREQUEST  client DHCPREQUEST
        //           message              message             message
        //'giaddr'   'giaddr' from        'giaddr' from       'giaddr' from
        //           client DHCPDISCOVER  client DHCPREQUEST  client DHCPREQUEST
        //           message              message             message
        //'chaddr'   'chaddr' from        'chaddr' from       'chaddr' from
        //           client DHCPDISCOVER  client DHCPREQUEST  client DHCPREQUEST
        //           message              message             message
        //'sname'    Server host name     Server host name    (unused)
        //           or options           or options
        //'file'     Client boot file     Client boot file    (unused)
        //           name or options      name or options
        //'options'  options              options
        //        Option                    DHCPOFFER    DHCPACK            DHCPNAK
        //------                    ---------    -------            -------
        //Requested IP address      MUST NOT             MUST NOT           MUST NOT
        //IP address lease time     MUST                MUST (DHCPREQUEST) MUST NOT
        //                                               MUST NOT (DHCPINFORM)
        //Use 'file'/'sname' fields MAY                 MAY                MUST NOT
        //DHCP message type         DHCPOFFER           DHCPACK            DHCPNAK
        //Parameter request list    MUST NOT             MUST NOT           MUST NOT
        //Message                   SHOULD               SHOULD             SHOULD
        //Client identifier         MUST NOT             MUST NOT           MAY
        //Vendor class identifier   MAY                  MAY                MAY
        //Server identifier         MUST                 MUST               MUST
        //Maximum message size      MUST NOT             MUST NOT           MUST NOT
        //All others                MAY                   MAY                MUST NOT

        private async Task SendACK(DHCPMessage source, DHCPClient client, bool infoack = false)
        {
            var message = new DHCPMessage(source)
            {
                ClientIPAddress = source.ClientIPAddress
            };

            if (!infoack)
            {
                message.YourIPAddress = client?.IPAddress ?? IPAddress.Any;
            }
            message.Options.Add(new DHCPOptionServerIdentifier(((IPEndPoint)this.Socket.Client.LocalEndPoint).Address));

            if (source.IsRequestedParameter(DHCPOption.SubnetMask))
            {
                message.Options.Add(new DHCPOptionSubnetMask(this.SubnetMask));
            }

            message.MessageType = DHCPMessageType.ACK;

            if(infoack)
            {
                await this.SendMessage(message, new IPEndPoint(message.ClientIPAddress, 68));
            }
            else
            {
                message.Options.Add(new DHCPOptionIPAddressLeaseTime(this.LeaseTime));
                await this.SelectAndSendMessage(message, source);
            }
        }

        private bool IsThisServer(DHCPMessage msg)
        {
            var serverOption = (DHCPOptionServerIdentifier)msg.GetOption(DHCPOption.ServerIdentifier);
            return serverOption?.IPAddress.Equals(this.EndPoint.Address) ?? false;
        }

        public void OnFatalError(string message, Exception e)
        {
            this.Stop();
            this.FatalExit.Invoke(this, EventArgs.Empty);

        }
    }
}
