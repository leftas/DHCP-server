using DNS_Server.Classes;
using DNS_Server.ValidationRules;
using GalaSoft.MvvmLight.CommandWpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace DNS_Server.Models
{
    public class MainWindowModel : BaseViewModel
    {
        private string serverIP;
        private string dHCPPoolStart;
        private string dHCPPoolEnd;
        private string netmask;
        public string ServerIP
        {
            get => this.serverIP;
            set
            {
                this.serverIP = value;
                this.StartServer.RaiseCanExecuteChanged();
                this.StopServer.RaiseCanExecuteChanged();
                this.OnPropertyChanged(nameof(this.ServerIP));
                this.OnPropertyChanged(nameof(this.CanServerBeRun));
            }
        }
        public string DHCPPoolStart
        {
            get => this.dHCPPoolStart;
            set
            {
                this.dHCPPoolStart = value;
                this.StartServer.RaiseCanExecuteChanged();
                this.StopServer.RaiseCanExecuteChanged();
                this.OnPropertyChanged(nameof(this.DHCPPoolStart));
                this.OnPropertyChanged(nameof(this.CanServerBeRun));
            }
        }
        public string DHCPPoolEnd
        {
            get => this.dHCPPoolEnd;
            set
            {
                this.dHCPPoolEnd = value;
                this.StartServer.RaiseCanExecuteChanged();
                this.StopServer.RaiseCanExecuteChanged();
                this.OnPropertyChanged(nameof(this.DHCPPoolEnd));
                this.OnPropertyChanged(nameof(this.CanServerBeRun));
            }
        }
        public string Netmask
        {
            get => this.netmask;
            set
            {
                this.netmask = value;
                this.StartServer.RaiseCanExecuteChanged();
                this.StopServer.RaiseCanExecuteChanged();
                this.OnPropertyChanged(nameof(this.Netmask));
                this.OnPropertyChanged(nameof(this.CanServerBeRun));
            }
        }

        public static IPAddress GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip;
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }

        public MainWindowModel()
        {
            IPAddress localIPAddress = GetLocalIPAddress();
            this.ServerIP = localIPAddress.ToString();
            this.Netmask = Utility.GetSubnetMask(localIPAddress).ToString();
            var ipNumber = Utility.IPAddressToUInt(localIPAddress);
            this.DHCPPoolStart = Utility.UIntToIPAddress(ipNumber & 0b11111111111111111111111100000000).ToString();
            this.DHCPPoolEnd = Utility.UIntToIPAddress(ipNumber | 0b00000000000000000000000011111111).ToString();
        }
        public DHCPServer Server { get; private set; }

        private bool CanServerBeRun {
            get
            {
                if (!(!this.IsActive &&
                       //this.validateIP.Validate(this.ServerIP, CultureInfo.CurrentUICulture).IsValid &&
                       this.validateIP.Validate(this.DHCPPoolStart, CultureInfo.CurrentUICulture).IsValid &&
                       this.validateIP.Validate(this.DHCPPoolEnd, CultureInfo.CurrentUICulture).IsValid &&
                       this.validateIP.Validate(this.Netmask, CultureInfo.CurrentUICulture).IsValid))
                {
                    return false;
                }

                uint startInt = Utility.IPAddressToUInt(IPAddress.Parse(this.DHCPPoolStart));
                uint endInt = Utility.IPAddressToUInt(IPAddress.Parse(this.DHCPPoolEnd));
                uint netMask = Utility.IPAddressToUInt(IPAddress.Parse(this.Netmask));
                return startInt < endInt &&
                       (netMask & startInt) != 0 &&
                       (netMask & endInt) != 0;
            }
        }

        public RelayCommand StartServer => new RelayCommand(this.StartDHCPServer, () => this.CanServerBeRun);
        public RelayCommand StopServer => new RelayCommand(this.StopDHCPServer, () => this.Server?.Active == true);

        public ObservableDictionary<DHCPClient, DHCPClient> ClientsInfo => this.Server?.Clients;

        public IPv4ValidationRule validateIP = new IPv4ValidationRule();

        public bool IsActive => this.Server?.Active ?? false;

        public void StartDHCPServer()
        {
            if(this.Server == null)
            {
                this.Server = new DHCPServer();
                this.OnPropertyChanged(nameof(this.ClientsInfo));
            }
            this.Server.EndPoint = new IPEndPoint(IPAddress.Parse(this.ServerIP), 67);
            this.Server.PoolStart = IPAddress.Parse(this.DHCPPoolStart);
            this.Server.PoolEnd = IPAddress.Parse(this.DHCPPoolEnd);
            this.Server.SubnetMask = IPAddress.Parse(this.Netmask);
            this.Server.Start();
            this.StopServer.RaiseCanExecuteChanged();
            this.StartServer.RaiseCanExecuteChanged();
            this.Server.FatalExit += this.Server_FatalExit;
        }

        private void Server_FatalExit(object sender, EventArgs e)
        {
            this.StopServer.RaiseCanExecuteChanged();
            this.StartServer.RaiseCanExecuteChanged();
        }

        public void StopDHCPServer()
        {
            this.Server.Stop();
            this.StartServer.RaiseCanExecuteChanged();
        }
    }
}
