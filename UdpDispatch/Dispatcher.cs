using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace UdpDispatch
{
    class Dispatcher
    {
        private string source_ip;
        private int source_port;
        private int frame_length;
        private List<DestConfig> destinations;
        private UdpClient server;
        private UdpClient client;
        private ConcurrentQueue<byte[]> dataQueue;
        private List<IPEndPoint> remoteIpEndPoints;
        private BackgroundWorker backgroundWorkerReceiveData;
        private BackgroundWorker backgroundWorkerProcessData;


        public Dispatcher(string _ip,int port,int flength,List<DestConfig> destinations)
        {
            this.source_ip = _ip;
            this.source_port = port;
            this.destinations = destinations;
            dataQueue = new ConcurrentQueue<byte[]>();
            backgroundWorkerProcessData = new BackgroundWorker();
            backgroundWorkerReceiveData = new BackgroundWorker();
            backgroundWorkerProcessData.WorkerSupportsCancellation = true;
            backgroundWorkerProcessData.DoWork += BackgroundWorkerProcessData_DoWork;
            backgroundWorkerReceiveData.WorkerSupportsCancellation = true;
            backgroundWorkerReceiveData.DoWork += BackgroundWorkerReceiveData_DoWork;

            remoteIpEndPoints = new List<IPEndPoint>();
            foreach (var config in destinations)
            {
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(config.dest_ip), config.dest_port);
                remoteIpEndPoints.Add(endPoint);
            }
        }
        public void Start()
        {
            IPEndPoint iPEndPoint = new IPEndPoint(IPAddress.Parse(source_ip), this.source_port);
            server = new UdpClient(iPEndPoint);
            client = new UdpClient();

            backgroundWorkerProcessData.RunWorkerAsync();
            backgroundWorkerReceiveData.RunWorkerAsync();
        }

        public void Stop()
        {
            backgroundWorkerProcessData.CancelAsync();
            backgroundWorkerReceiveData.CancelAsync();
            server.Close();
            client.Close();
        }

        private void BackgroundWorkerReceiveData_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bgWorker = sender as BackgroundWorker;

            //this.udpClient = new UdpClient(29);
            //IPEndPoint object will allow us to read datagrams sent from any source.
            IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
            while (true)
            {
                try
                {
                    // Blocks until a message returns on this socket from a remote host.
                    Byte[] receiveBytes = server.Receive(ref RemoteIpEndPoint);
                    frame_length = receiveBytes.Length;
                    dataQueue.Enqueue(receiveBytes);

                    if (bgWorker.CancellationPending == true)
                    {
                        e.Cancel = true;
                        break;
                    }
                }
                catch (Exception ex)
                {
                    StreamWriter sw = new StreamWriter(source_ip + ".txt");
                    sw.WriteLine(ex.StackTrace);
                    sw.Close();

                    if (bgWorker.CancellationPending == true)
                    {
                        e.Cancel = true;
                        break;
                    }
                }
                if (bgWorker.CancellationPending == true)
                {
                    e.Cancel = true;
                    break;
                }
            }
        }

        private void BackgroundWorkerProcessData_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bgWorker = sender as BackgroundWorker;
            List<UdpClient> clients = new List<UdpClient>();

            while (true)
            {
                try
                {
                    int dataCount = dataQueue.Count;
                    byte[] line = new byte[frame_length];

                    if (dataCount > 0)
                    {
                        bool success = dataQueue.TryDequeue(out line);
                        foreach (var endpoint in remoteIpEndPoints)
                        {
                            client.Send(line, line.Length, endpoint);
                        }
                    }
                }
                catch (Exception ex)
                {
                    StreamWriter sw = new StreamWriter(source_ip+".txt");
                    sw.WriteLine(ex.StackTrace);
                    sw.Close();
                }
                
                if (bgWorker.CancellationPending == true)
                {
                    server.Close();
                    e.Cancel = true;
                    break;
                }

                Thread.Sleep(10);
            }
        }
    }
}
