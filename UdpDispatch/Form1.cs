using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace UdpDispatch
{
    public partial class Form1 : Form
    {
        private ConcurrentQueue<byte[]> dataQueue;
        private BackgroundWorker backgroundWorkerReceiveData;
        private BackgroundWorker backgroundWorkerProcessData;
        private List<Config> ipList;
        private UdpClient udpClient;
        private UdpClient udpSendWave;
        public Form1()
        {
            InitializeComponent();
            ipList = new List<Config>();
            dataQueue = new ConcurrentQueue<byte[]>();
            StreamReader sr = new StreamReader("config.csv", Encoding.UTF8);
            String line;

            char[] chs = { ',' };
            while ((line = sr.ReadLine()) != null)
            {
                string[] items = line.Split(chs);
                Config config = new Config(items[0], int.Parse(items[1]));
                ipList.Add(config);
                listBox1.Items.Add(line);
            }
            sr.Close();

            backgroundWorkerProcessData = new BackgroundWorker();
            backgroundWorkerReceiveData = new BackgroundWorker();
            backgroundWorkerProcessData.WorkerSupportsCancellation = true;
            backgroundWorkerProcessData.DoWork += BackgroundWorkerProcessData_DoWork;
            backgroundWorkerReceiveData.WorkerSupportsCancellation = true;
            backgroundWorkerReceiveData.DoWork += BackgroundWorkerReceiveData_DoWork;
        }

        private void BackgroundWorkerReceiveData_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bgWorker = sender as BackgroundWorker;

            this.udpClient = new UdpClient(29);
            //IPEndPoint object will allow us to read datagrams sent from any source.
            IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
            while (true)
            {
                try
                {
                    // Blocks until a message returns on this socket from a remote host.
                    Byte[] receiveBytes = udpClient.Receive( ref RemoteIpEndPoint);

                    dataQueue.Enqueue(receiveBytes);

                    if (bgWorker.CancellationPending == true)
                    {
                        e.Cancel = true;
                        break;
                    }
                }
                catch (Exception ex)
                {
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
            List <UdpClient> clients= new List<UdpClient>();
            List<IPEndPoint>  remoteIpEndPoints = new List<IPEndPoint>();
            UdpClient client = new UdpClient();
            foreach (var config in ipList)
            {
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(config.ip), config.port);
                remoteIpEndPoints.Add(endPoint);
            }

            while (true)
            {
                try
                {
                    int dataCount = dataQueue.Count;
                    byte[] line = new byte[8];

                    if (dataCount > 0)
                    {
                        bool success = dataQueue.TryDequeue(out line);
                        foreach(var endpoint in remoteIpEndPoints)
                        {
                            client.Send(line, line.Length, endpoint);
                        }
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    
                }



                if (bgWorker.CancellationPending == true)
                {
                    client.Close();
                    e.Cancel = true;
                    break;
                }

                Thread.Sleep(10);
            }
        }

        class Config
        {
            public string ip;
            public int port;

            public Config(string _ip,int _port)
            {
                this.ip = _ip;
                this.port = _port;
            }
        }

        private void buttonStart_Click(object sender, EventArgs e)
        {
            backgroundWorkerProcessData.RunWorkerAsync();
            backgroundWorkerReceiveData.RunWorkerAsync();
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            backgroundWorkerProcessData.CancelAsync();
            backgroundWorkerReceiveData.CancelAsync();
            udpClient.Close();
        }
    }
}
