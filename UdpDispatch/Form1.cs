using Newtonsoft.Json;
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
        private List<Config> ipList;
        private List<Dispatcher> dispatchers;
        public Form1()
        {
            InitializeComponent();
            ipList = new List<Config>();
            dataQueue = new ConcurrentQueue<byte[]>();
            dispatchers = new List<Dispatcher>();
            StreamReader sr = new StreamReader("config.csv", Encoding.UTF8);
            String line;

            char[] chs = { ',' };
            while ((line = sr.ReadLine()) != null)
            {
                Config config = JsonConvert.DeserializeObject<Config>(line);
                Dispatcher dispatcher = new Dispatcher(config.source_ip, config.source_port, config.frame_length, config.destination);
                dispatchers.Add(dispatcher);
                ipList.Add(config);
                listBoxDestination.Items.Clear();
                listBoxSource.Items.Add(config.source_ip+":"+config.source_port);
                foreach(var item in config.destination)
                {
                    listBoxDestination.Items.Add(item.dest_ip + ":" + item.dest_port);
                }

            }
            sr.Close();

            startToolStripMenuItem.Enabled = true;
            stopToolStripMenuItem.Enabled = false;
        }

        private void startToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (var dispatcher in dispatchers)
            {
                dispatcher.Start();
            }
            startToolStripMenuItem.Enabled = false;
            stopToolStripMenuItem.Enabled = true;
        }

        private void stopToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            foreach (var dispatcher in dispatchers)
            {
                dispatcher.Stop();
            }
            startToolStripMenuItem.Enabled = true;
            stopToolStripMenuItem.Enabled = false;
        }

        private void listBoxSource_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Get the currently selected item in the ListBox.
            string curItem = listBoxSource.SelectedItem.ToString();

            listBoxDestination.Items.Clear();
            foreach(var item in ipList)
            {
                if((item.source_ip+":"+item.source_port) == curItem)
                {
                    foreach(var i in item.destination)
                    {
                        listBoxDestination.Items.Add(i.dest_ip + ":" + i.dest_port);
                    }
                    break;
                }
            }
        }
    }

    public class DestConfig
    {
        public string dest_ip;
        public int dest_port;
        public DestConfig(string _ip, int _port)
        {
            this.dest_ip = _ip;
            this.dest_port = _port;
        }
    }
    public class Config
    {
        public string source_ip;
        public int source_port;
        public int frame_length;
        public List<DestConfig> destination;

        public Config(string _ip, int _port)
        {
            this.source_ip = _ip;
            this.source_port = _port;
            this.destination = new List<DestConfig>();
        }
    }
}
