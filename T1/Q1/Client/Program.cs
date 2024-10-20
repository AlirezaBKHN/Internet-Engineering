using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;


class UdpFileClient
{
    public static async Task StartClient(string serverIp, int serverPort, int threadCount, string saveFilePath, string pathToSave)
    {
        using UdpClient udpClient = new UdpClient();
        IPEndPoint serverEndpoint = new IPEndPoint(IPAddress.Parse(serverIp), serverPort);
        byte[] filePathBytes = System.Text.Encoding.ASCII.GetBytes(saveFilePath);
        byte[] threadCountBytes = BitConverter.GetBytes(threadCount);
        byte[] initialRequest = new byte[filePathBytes.Length + threadCountBytes.Length];

        Buffer.BlockCopy(filePathBytes, 0, initialRequest, 0, filePathBytes.Length);
        Buffer.BlockCopy(threadCountBytes, 0, initialRequest, filePathBytes.Length, threadCountBytes.Length);

        await udpClient.SendAsync(initialRequest, initialRequest.Length, serverEndpoint);
        Console.WriteLine($"Requested file from server {serverIp}:{serverPort}: {saveFilePath} with {threadCount} threads.");


        var initialResult = await udpClient.ReceiveAsync();
        var initialBuffer = initialResult.Buffer;
        string sessionId = System.Text.Encoding.ASCII.GetString(initialBuffer.Take(initialResult.Buffer.Length - 4).ToArray());
        int totalPackets = BitConverter.ToInt32(initialBuffer, initialBuffer.Length - 4);

        Console.WriteLine($"Received session ID: {sessionId}");
        Console.WriteLine($"Total packets to be received: {totalPackets}");


        List<byte[]> fileData = new List<byte[]>(new byte[totalPackets][]); // Allocate array for the entire file
        ConcurrentDictionary<int, bool> receivedPackets = new ConcurrentDictionary<int, bool>();
        int packetsPerThread = totalPackets / threadCount;

        // Multithreading to request file in parallel
        Task[] threads = new Task[threadCount];
        var clients = new UdpClient[threadCount];
        for (int i = 0; i < threadCount; i++)
        {
            clients[i] = new UdpClient();
        }
        for (int t = 0; t < threads.Length; t++)
        {
            int threadId = t;
            threads[t] = Task.Run(async () =>
            {

                int startPacket = threadId * packetsPerThread;
                int endPacket = (threadId == threads.Length - 1) ? totalPackets : startPacket + packetsPerThread;
                byte[] packetRequest = new byte[sessionId.Length + 4 + 4]; // sessionID + seq init + seq end
                System.Text.Encoding.ASCII.GetBytes(sessionId).CopyTo(packetRequest, 0);
                BitConverter.GetBytes(startPacket).CopyTo(packetRequest, sessionId.Length);
                BitConverter.GetBytes(endPacket).CopyTo(packetRequest, sessionId.Length + 4);
                await clients[t].SendAsync(packetRequest);
                System.Console.WriteLine($"thread {t} requests packets number {startPacket} to {endPacket}");
                while (true)
                {
                    var ReceivedPacket = await clients[t].ReceiveAsync();
                    if (System.Text.Encoding.ASCII.GetString(ReceivedPacket.Buffer) == "FIN")
                    {
                        break;
                    }


                }

                //TODO
            });
        }

        // Wait for all threads to complete
        await Task.WhenAll(threads);
        // Reassemble and save the file once all packets are received
        using (FileStream fs = new FileStream(pathToSave, FileMode.Create, FileAccess.Write))
        {
            foreach (var packet in fileData)
            {
                if (packet != null)
                    fs.Write(packet, 0, packet.Length);
            }
        }

        Console.WriteLine("File received and saved.");
    }

    static async Task Main(string[] args)
    {
        if (args.Length < 4)
        {
            Console.WriteLine("Usage: UdpFileClient <serverIp> <serverPort> <threadCount> <saveFilePath> <pathToSave>");
            return;
        }

        string serverIp = args[0];
        int serverPort = int.Parse(args[1]);
        int threadCount = int.Parse(args[2]);
        string saveFilePath = args[3];
        // System.Console.WriteLine("");
        var guid = new Guid();
        string pathToSave = $"../ReceivedFiles/{guid.ToString()}.{saveFilePath.Split('.').Last()}";
        await StartClient(serverIp, serverPort, threadCount, saveFilePath, pathToSave);
    }
}


// for (int sequenceNumber = startPacket; sequenceNumber < endPacket; sequenceNumber++)
// {
//     bool packetReceived = false;

//     while (!packetReceived)
//     {
//         // Request packet by sending the sequence number with session ID
//         byte[] packetRequest = new byte[sessionId.Length + 4 +4 ]; // Session ID + sequence number + thread id
//         System.Text.Encoding.ASCII.GetBytes(sessionId).CopyTo(packetRequest, 0); // Add session ID
//         BitConverter.GetBytes(sequenceNumber).CopyTo(packetRequest, sessionId.Length); // Add sequence number
//         BitConverter.GetBytes(t).CopyTo(packetRequest, sessionId.Length+4); // Add thread id

//         await udpClient.SendAsync(packetRequest, packetRequest.Length, serverEndpoint);
//         Console.WriteLine($"Thread {threadId} requested packet {sequenceNumber}.");

//         // Wait to receive the requested packet
//         UdpReceiveResult result = await udpClient.ReceiveAsync();
//         string receivedSessionId = System.Text.Encoding.ASCII.GetString(result.Buffer, 0, sessionId.Length);
//         int receivedSequenceNumber = BitConverter.ToInt32(result.Buffer, sessionId.Length);

//         // Ensure the packet is for this client and the expected sequence number
//         if (receivedSessionId == sessionId && receivedSequenceNumber == sequenceNumber)
//         {
//             byte[] packetData = new byte[result.Buffer.Length - (sessionId.Length + 4)];
//             Array.Copy(result.Buffer, sessionId.Length + 4, packetData, 0, packetData.Length);
//             fileData[sequenceNumber] = packetData;
//             receivedPackets[sequenceNumber] = true;

//             // Send ACK for the received packet
//             byte[] ack = new byte[sessionId.Length + 4]; // Session ID + sequence number
//             System.Text.Encoding.ASCII.GetBytes(sessionId).CopyTo(ack, 0); // Add session ID
//             BitConverter.GetBytes(sequenceNumber).CopyTo(ack, sessionId.Length); // Add sequence number
//             await udpClient.SendAsync(ack, ack.Length, serverEndpoint);
//             Console.WriteLine($"Thread {threadId} acknowledged packet {sequenceNumber}.");

//             packetReceived = true; // Mark packet as successfully received
//         }
//     }
// }