using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;




class UdpFileServer
{
    private const int BufferSize = 8192; // Define packet size (8 KB)
    private const int Port = 8080;
    private const int AckTimeout = 3000; // 3 seconds timeout for resending a packet
    private static ConcurrentDictionary<string, ClientSession> clientSessions = new ConcurrentDictionary<string, ClientSession>();

    public static async Task StartServer()
    {
        using UdpClient udpServer = new UdpClient(Port);
        Console.WriteLine("Server is listening...");

        while (true)
        {
            UdpReceiveResult result = await udpServer.ReceiveAsync();
            string clientKey = result.RemoteEndPoint.ToString(); 
            
            if (!clientSessions.ContainsKey(clientKey))//TODO session Id must be used for the key
            {
                byte[] buffer = result.Buffer;
                int filePathLength = buffer.Length -4;
                var filePath = System.Text.Encoding.ASCII.GetString(buffer,0,filePathLength);
                var threadCount = BitConverter.ToInt32(buffer,filePathLength);


                var sessionId = Guid.NewGuid().ToString();
                var fileData = File.ReadAllBytes(filePath);
                int totalPackets = (int)Math.Ceiling((double)fileData.Length / BufferSize);

                ClientSession session = new ClientSession(sessionId,filePath,result.RemoteEndPoint,totalPackets,threadCount,fileData);
                clientSessions[clientKey] = session;


                var sessionIdBytes = System.Text.Encoding.ASCII.GetBytes(sessionId);
                var totalPacketsByte = BitConverter.GetBytes(totalPackets);
                var initialResponse = new byte[sessionIdBytes.Length+4];

                Buffer.BlockCopy(sessionIdBytes, 0, initialResponse, 0, sessionIdBytes.Length);
                Buffer.BlockCopy(totalPacketsByte, 0, initialResponse, sessionIdBytes.Length, totalPacketsByte.Length);

                await udpServer.SendAsync(initialResponse, initialResponse.Length, session.ClientEndpoint);
                Console.WriteLine($"New session created: {sessionId} for client {result.RemoteEndPoint} with {totalPackets} Packets");
            }
            else{
                Task.Run(() => HandleClient(udpServer, result ,clientKey));
            }
        }
    }

    private static async Task HandleClient(UdpClient udpServer, UdpReceiveResult result ,string clientKey)
    {
        //TODO
    }

    // Client session class to store session-related data
    class ClientSession
    {
        public string SessionId { get; private set; }
        public string FilePath { get; private set; }
        public IPEndPoint ClientEndpoint { get; private set; }
        public bool[] AckReceived { get; private set; }
        public int TotalPackets { get; set; }

        public int[] SentWithoutAck { get; set; }

        public byte[] FileData { get; set;}
        public ClientSession(string sessionId, string filePath, IPEndPoint clientEndpoint, int totalPackets, int threadCount, byte[] fileData)
        {
            SessionId = sessionId;
            FilePath = filePath;
            ClientEndpoint = clientEndpoint;
            TotalPackets = 0;
            AckReceived = new bool[totalPackets];
            SentWithoutAck = new int[threadCount];
            FileData = fileData;
        }

    }


    static async Task Main(string[] args)
    {
        await StartServer();
    }
}


        // var session = clientSessions[clientKey];

        // if (File.Exists(session.FilePath))
        // {
        //     byte[] fileData = File.ReadAllBytes(session.FilePath);
        //     int totalPackets = (int)Math.Ceiling((double)fileData.Length / BufferSize);
        //     session.TotalPackets = totalPackets;
        //     byte[] totalPacketsBytes = BitConverter.GetBytes(totalPackets);
        //     await udpServer.SendAsync(totalPacketsBytes, totalPacketsBytes.Length, session.ClientEndpoint);
        //     Console.WriteLine($"Sending file of size {fileData.Length} bytes in {totalPackets} packets to {session.ClientEndpoint}");

        //     while (true)
        //     {
        //         // Wait for a packet request or ACK from the client
        //         UdpReceiveResult requestResult = await udpServer.ReceiveAsync();
        //         string receivedSessionId = System.Text.Encoding.ASCII.GetString(requestResult.Buffer, 0, 36); // First 36 bytes for session ID

        //         if (receivedSessionId == session.SessionId)
        //         {
        //             int sequenceNumber = BitConverter.ToInt32(requestResult.Buffer, 36); // After session ID

        //             if (!session.AckReceived[sequenceNumber])
        //             {
        //                 Console.WriteLine($"Sending packet {sequenceNumber} to {session.ClientEndpoint}");

        //                 // Send the requested packet
        //                 byte[] packet = new byte[BufferSize];
        //                 Array.Copy(fileData, sequenceNumber * BufferSize, packet, 0, Math.Min(BufferSize, fileData.Length - sequenceNumber * BufferSize));

        //                 // Attach sequence number to the packet (first 4 bytes) and session ID
        //                 byte[] packetWithSeq = new byte[BufferSize + 40];
        //                 System.Text.Encoding.ASCII.GetBytes(session.SessionId).CopyTo(packetWithSeq, 0); // First 36 bytes are session ID
        //                 BitConverter.GetBytes(sequenceNumber).CopyTo(packetWithSeq, 36); // Next 4 bytes are sequence number
        //                 Array.Copy(packet, 0, packetWithSeq, 40, packet.Length);

        //                 await udpServer.SendAsync(packetWithSeq, packetWithSeq.Length, session.ClientEndpoint);

        //                 // Start waiting for the ACK
        //                 DateTime packetSendTime = DateTime.Now;

        //                 while (!session.AckReceived[sequenceNumber])
        //                 {
        //                     // Check for ACK timeout
        //                     if ((DateTime.Now - packetSendTime).TotalMilliseconds > AckTimeout)
        //                     {
        //                         Console.WriteLine($"ACK not received for packet {sequenceNumber}, resending...");
        //                         await udpServer.SendAsync(packetWithSeq, packetWithSeq.Length, session.ClientEndpoint);
        //                         packetSendTime = DateTime.Now; // Reset send time
        //                     }

        //                     // Check for ACK from the client
        //                     if (udpServer.Available > 0)
        //                     {
        //                         UdpReceiveResult ackResult = await udpServer.ReceiveAsync();
        //                         string ackSessionId = System.Text.Encoding.ASCII.GetString(ackResult.Buffer, 0, 36);
        //                         if (ackSessionId == session.SessionId)
        //                         {
        //                             int ackNumber = BitConverter.ToInt32(ackResult.Buffer, 36);
        //                             if (ackNumber == sequenceNumber)
        //                             {
        //                                 Console.WriteLine($"ACK received for packet {sequenceNumber}");
        //                                 session.AckReceived[sequenceNumber] = true; // Mark ACK as received
        //                                 break;
        //                             }
        //                         }
        //                     }

        //                     await Task.Delay(100); // Small delay to avoid busy waiting
        //                 }
        //             }
        //         }
        //     }
        // }
        // else
        // {
        //     Console.WriteLine($"File not found: {session.FilePath}");
        // }
