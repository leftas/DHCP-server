# DHCP-server
DHCP Server written in C# for IPv4 addresses.

For the time being you can only edit DHCP pool addresses and subnet mask. Later on planning on adding DHCP option to the messages.

**IMPORTANT!**:
You can only use your local IP address from network adapter, that is because you cannot bind remote addresses in Windows(in linux you can use IP_TRANSPARENT with some extra work to make able to bind). So you cannot change the IP address.

Screenshots:
![Start Image](https://i.imgur.com/D39n6ON.png)
![Bounded](https://i.imgur.com/eoOeF1b.png)
