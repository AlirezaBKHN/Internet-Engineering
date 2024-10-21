using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;


class UdpFileClient
{
    public static async Task StartClient(string serverIp, int serverPort, int threadCount, string saveFilePath, string pathToSave)
    {
        using UdpClient udpClient = new UdpClient();
        IPEndPoint serverEndpoint = new IPEndPoint(IPAddress.Parse(serverIp), serverPort);
        byte[] requestType = System.Text.Encoding.ASCII.GetBytes("INT");
        byte[] filePathBytes = System.Text.Encoding.ASCII.GetBytes(saveFilePath);
        byte[] threadCountBytes = BitConverter.GetBytes(threadCount);
        byte[] initialRequest = new byte[requestType.Length + filePathBytes.Length + threadCountBytes.Length];

        Buffer.BlockCopy(requestType, 0, initialRequest, 0, requestType.Length);
        Buffer.BlockCopy(filePathBytes, 0, initialRequest, 3, filePathBytes.Length);
        Buffer.BlockCopy(threadCountBytes, 0, initialRequest, filePathBytes.Length + 3, threadCountBytes.Length);

        await udpClient.SendAsync(initialRequest, initialRequest.Length, serverEndpoint);
        Console.WriteLine($"Requested file from server {serverIp}:{serverPort}: {saveFilePath} with {threadCount} threads.");


        var initialResult = await udpClient.ReceiveAsync();
        var initialBuffer = initialResult.Buffer;
        string sessionId = System.Text.Encoding.ASCII.GetString(initialBuffer.Take(initialResult.Buffer.Length - 4).ToArray());
        int totalPackets = BitConverter.ToInt32(initialBuffer, initialBuffer.Length - 4);

        Console.WriteLine($"Received session ID: {sessionId}");
        Console.WriteLine($"Total packets to be received: {totalPackets}");


        List<byte[]> fileData = new List<byte[]>(new byte[totalPackets][]); // Allocate array for the entire file
        ConcurrentDictionary<int, bool> packets = new ConcurrentDictionary<int, bool>();
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
                byte[] packetRequest = new byte[3 + sessionId.Length + 4 + 4]; // sessionID + seq init + seq end
                System.Text.Encoding.ASCII.GetBytes("SEQ").CopyTo(packetRequest, 0);
                System.Text.Encoding.ASCII.GetBytes(sessionId).CopyTo(packetRequest, 3);
                BitConverter.GetBytes(startPacket).CopyTo(packetRequest, sessionId.Length + 3);
                BitConverter.GetBytes(endPacket).CopyTo(packetRequest, sessionId.Length + 4 + 3);
                await clients[threadId].SendAsync(packetRequest, packetRequest.Length, serverEndpoint);
                System.Console.WriteLine($"thread {threadId} requests packets number {startPacket} to {endPacket}");
                while (true)
                {
                    var packet = await clients[threadId].ReceiveAsync();
                    if (System.Text.Encoding.ASCII.GetString(packet.Buffer) == "FIN")
                    {
                        break;
                    }

                    var fileChunk = new byte[packet.Buffer.Length - 4];
                    var seqNum = BitConverter.ToInt32(packet.Buffer);
                    Array.Copy(packet.Buffer,4,fileChunk,0,fileChunk.Length) ;
                    if (seqNum >= startPacket && seqNum < endPacket)
                    {
                        // System.Console.WriteLine($"thread {threadId} received {seqNum}");
                        // System.Console.WriteLine(String.Join(",",fileChunk));
                        fileData[seqNum] = fileChunk;
                        packets[seqNum] = true;
                    }

                }

                for (int i = startPacket; i < endPacket; i++)
                {

                    if (!packets[i])
                    {
                        var flag = false;
                        var retryRequestPacket = new byte[3 + sessionId.Length + 4];
                        System.Text.Encoding.ASCII.GetBytes("RET").CopyTo(packetRequest, 0);
                        System.Text.Encoding.ASCII.GetBytes(sessionId).CopyTo(packetRequest, 3);
                        BitConverter.GetBytes(i).CopyTo(retryRequestPacket, sessionId.Length + 3);
                        while (!flag)
                        {
                            await clients[threadId].SendAsync(retryRequestPacket, retryRequestPacket.Length, serverEndpoint);
                            var retryResult = await clients[threadId].ReceiveAsync();
                            var fileChunk = new byte[retryResult.Buffer.Length - 4];
                            var seqNum = BitConverter.ToInt32(retryResult.Buffer);
                            if (seqNum >= startPacket && seqNum < endPacket)
                            {
                                fileData[seqNum] = fileChunk;
                                packets[seqNum] = true;
                                flag = true;
                            }
                        }
                    }
                }




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

        Console.WriteLine($"File received and saved in {pathToSave}.");
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
        var guid = Guid.NewGuid();
        string pathToSave = $"../RecFiles/{guid.ToString()}.{saveFilePath.Split('.').Last()}";
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
//             packets[sequenceNumber] = true;

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