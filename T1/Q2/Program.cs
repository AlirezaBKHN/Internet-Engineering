using System;
// using SharpPcap;
// using PacketDotNet;
using System.Net;
using System.Text;
using PcapDotNet;
using PcapDotNet.Core;
using PcapDotNet.Packets;
using PcapDotNet.Packets.Icmp;
using PcapDotNet.Packets.IpV4;


class Q2
{
    static void Main(string[] args)
    {
        IList<LivePacketDevice> allDevices = LivePacketDevice.AllLocalMachine;
        PacketDevice device = allDevices.Where(d => d.Description.Contains("Qualcomm")).First();

        using (PacketCommunicator communicator = device.Open(65536, PacketDeviceOpenAttributes.Promiscuous, 1000))
        {
            Console.WriteLine("Listening on " + device.Description + "...");
            communicator.ReceivePackets(0, PacketHandler);
        }

    }

    private static void PacketHandler(Packet packet)
    {
        var ethernetPacket = packet.Ethernet;
        var ipPacket = ethernetPacket.Ip;

        if (ipPacket is IpV4Datagram ipv4Packet)
        {
            // Display the header information in a structured format
            Console.WriteLine(new string('-', 70));

            // Link Layer Information
            Console.WriteLine("Link Layer Header Information:");
            Console.WriteLine($"Source MAC: {ethernetPacket.Source}");
            Console.WriteLine($"Destination MAC: {ethernetPacket.Destination}");

            // Network Layer Information
            Console.WriteLine(new string('-', 70));
            Console.WriteLine("Network Layer Header Information:");
            Console.WriteLine($"IP Version: {ipv4Packet.Version}");
            Console.WriteLine($"Source IP: {ipv4Packet.Source}");
            Console.WriteLine($"Destination IP: {ipv4Packet.Destination}");

            // Transport Layer Information
            Console.WriteLine(new string('-', 70));
            Console.WriteLine("Transport Layer Header Information:");

            switch (ipv4Packet.Protocol)
            {
                case IpV4Protocol.Tcp:
                    var tcpPacket = ipv4Packet.Tcp;
                    Console.WriteLine("TCP:");
                    Console.WriteLine($"  Source Port: {tcpPacket.SourcePort}");
                    Console.WriteLine($"  Destination Port: {tcpPacket.DestinationPort}");
                    Console.WriteLine($"  Sequence Number: {tcpPacket.SequenceNumber}");
                    Console.WriteLine($"  Acknowledgment Number: {tcpPacket.AcknowledgmentNumber}");
                    Console.WriteLine($"  Flags: {(tcpPacket.IsAcknowledgment ? "ACK " : "")}{(tcpPacket.IsPush ? "PUSH " : "")}{(tcpPacket.IsReset ? "RST " : "")}{(tcpPacket.IsSynchronize ? "SYN " : "")}{(tcpPacket.IsFin ? "FIN  " : "")}");

                    Console.WriteLine($"  Window Size: {tcpPacket.Window}");
                    Console.WriteLine($"  Checksum: {tcpPacket.Checksum}");
                    Console.WriteLine($"  Payload: {tcpPacket.Payload}");
                    break;

                case IpV4Protocol.Udp:
                    var udpPacket = ipv4Packet.Udp;
                    Console.WriteLine("UDP:");
                    Console.WriteLine($"  Source Port: {udpPacket.SourcePort}");
                    Console.WriteLine($"  Destination Port: {udpPacket.DestinationPort}");
                    Console.WriteLine($"  Length: {udpPacket.TotalLength}");
                    Console.WriteLine($"  Checksum: {udpPacket.Checksum}");
                    break;


                default:
                    if (ipPacket.Icmp != null)
                    {
                        var icmpPacket = ipPacket.Icmp;
                        Console.WriteLine($"ICMP: {icmpPacket}");
                        Console.WriteLine($"  Source IP: {ethernetPacket.IpV4.Source}");
                        Console.WriteLine($"  Destination IP: {ethernetPacket.IpV4.Destination}");
                        Console.WriteLine($"  Type: {icmpPacket.MessageType}");
                        Console.WriteLine($"  Code: {icmpPacket.Code}");
                        // Console.WriteLine($"  Identifier: {icmpPacket.Identifier}");
                        // Console.WriteLine($"  Sequence Number: {icmpPacket.SequenceNumber}");
                        Console.WriteLine($"  Checksum: {icmpPacket.Checksum}");
                        Console.WriteLine($"  Checksum: {icmpPacket.Payload}");
                    }
                    return;
            }

            // Final border line
            Console.WriteLine(new string('-', 70));
        }
    }
}

// class PacketSniffer
// {
//     static void Main(string[] args)
//     {
//         // Get the list of available devices
//         var devices = CaptureDeviceList.Instance;

//         if (devices.Count < 1)
//         {
//             Console.WriteLine("No devices were found on this machine.");
//             return;
//         }


//         // Choose a device (you can modify this to let the user select a device)
//         var device = devices.Where(x => x.ToString().Contains("FriendlyName: Wi-Fi")).First();

//         // Open the device for capturing
//         device.Open(DeviceModes.Promiscuous);

//         // Register the packet arrival event handler
//         device.OnPacketArrival += new PacketArrivalEventHandler(Device_OnPacketArrival);

//         // Start capturing packets
//         device.StartCapture();

//         Console.WriteLine("Press any key to stop...");
//         Console.ReadKey();

//         // Stop capturing and close the device
//         device.StopCapture();
//         device.Close();
//     }

//     private static void Device_OnPacketArrival(object sender, PacketCapture e)
//     {
//         // Parse the packet
//         var packet = Packet.ParsePacket(e.GetPacket().LinkLayerType, e.GetPacket().Data);

//         // Handle Ethernet packets
//         var ethernetPacket = (EthernetPacket)packet;

//         // Link Layer (Ethernet)
//         Console.WriteLine($"Source MAC: {ethernetPacket.SourceHardwareAddress}");
//         Console.WriteLine($"Destination MAC: {ethernetPacket.DestinationHardwareAddress}");
//         // System.Console.WriteLine($"PayLoad Type: {ethernetPacket.ParentPacket.GetType()}");

//         // Handle IP packets (Network Layer)
//         if (packet.PayloadPacket is IPPacket ipPacket)
//         {
//             string srcIP = ipPacket.SourceAddress.ToString();
//             string destIP = ipPacket.DestinationAddress.ToString();
//             Console.WriteLine($"IP Version: {ipPacket.Version}");
//             Console.WriteLine($"Source IP: {srcIP}");
//             Console.WriteLine($"Destination IP: {destIP}");

//             // Handle TCP packets (Transport Layer)
//             if (ipPacket.PayloadPacket is TcpPacket tcpPacket)
//             {
//                 Console.WriteLine("TCP Packet:");
//                 Console.WriteLine($"Source Port: {tcpPacket.SourcePort}");
//                 Console.WriteLine($"Destination Port: {tcpPacket.DestinationPort}");
//                 Console.WriteLine($"Sequence Number: {tcpPacket.SequenceNumber}");
//                 Console.WriteLine($"Acknowledgment Number: {tcpPacket.AcknowledgmentNumber}");
//                 Console.WriteLine($"Flags: {tcpPacket.Flags}");
//                 Console.WriteLine($"Window Size: {tcpPacket.WindowSize}");
//                 Console.WriteLine($"Checksum: {tcpPacket.Checksum}");

//                 // Display TCP payload data if available
//                 if (tcpPacket.PayloadData.Length > 0)
//                 {
//                     var tcpPayload = Encoding.ASCII.GetString(tcpPacket.PayloadData);
//                     Console.WriteLine($"TCP Payload: {tcpPayload}");
//                 }
//             }
//             // Handle UDP packets
//             else if (ipPacket.PayloadPacket is UdpPacket udpPacket)
//             {
//                 Console.WriteLine("UDP Packet:");
//                 Console.WriteLine($"Source Port: {udpPacket.SourcePort}");
//                 Console.WriteLine($"Destination Port: {udpPacket.DestinationPort}");
//                 Console.WriteLine($"UDP Length: {udpPacket.Length}");
//                 Console.WriteLine($"Checksum: {udpPacket.Checksum}");
//             }
//             // Handle ICMP packets
//             else if (ipPacket.PayloadPacket is IcmpV4Packet icmpPacket)
//             {
//                 Console.WriteLine("ICMP Packet:");
//                 Console.WriteLine($"Type: {icmpPacket.TypeCode}");
//                 Console.WriteLine($"Code: {icmpPacket.Data}"); // Use `Code` instead of `Id`
//                 Console.WriteLine($"Checksum: {icmpPacket.Checksum}");
//             }
//             else
//             {
//                 Console.WriteLine($"Unknown Packet Type: {ipPacket.PayloadPacket.GetType()}");
//             }
//         }

//         // Optionally, extract data from the packet
//         var payload = Encoding.ASCII.GetString(packet.Bytes);
//         // Console.WriteLine($"Payload: {payload}");
//     }
// }
