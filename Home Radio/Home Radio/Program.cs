using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using NAudio;
using NAudio.Wave;
using System.Collections.Concurrent;

namespace Home_Radio
{
    class AudioClient
    {
        private UdpClient uclient;
        private IPAddress ip;
        private static readonly string mcastaddress = "224.0.1.1";
        private IPEndPoint ipe;
        private static readonly int port = 65535;
        private ConcurrentQueue<byte[]> cq2 = new ConcurrentQueue<byte[]>();
        private WaveFormat wf;
        private static BufferedWaveProvider waveprovider;
        private WaveOut waveOut = new WaveOut();
        private Thread t0;
        private Thread t1;
        private Thread t2;

        public AudioClient(string mcastaddress)
        {
            this.uclient = new UdpClient();
            this.ipe = new IPEndPoint(IPAddress.Any, port);
            uclient.Client.Bind(ipe);
            this.ip = IPAddress.Parse(mcastaddress);
            uclient.JoinMulticastGroup(ip);
            this.wf = new WaveFormat(44100, 16, 2);
            waveprovider = new BufferedWaveProvider(wf);
            waveOut.Init(waveprovider);
            waveOut.Play();
        }
        public void start()
        {
            Console.WriteLine("client is listening");
            t0 = new Thread(new ThreadStart(downloadThread));
            t1 = new Thread(new ThreadStart(streamingThread));
            t2 = new Thread(new ThreadStart(appTermination));
            t0.Start();
            t1.Start();
            t2.Start();
        }
        public void downloadThread()
        {
            byte[] buffer;
            while (true)
            {
                buffer = uclient.Receive(ref ipe);
                cq2.Enqueue(buffer);
            }

        }
        public void streamingThread()
        {
            waveprovider.DiscardOnBufferOverflow = true;
            byte[] buffer;
            waveprovider.BufferDuration = new TimeSpan(1, 0, 0);
            for (; ; )
            {
                if (cq2.TryDequeue(out buffer))
                {
                    waveprovider.AddSamples(buffer, 0, buffer.Length);
                }
                else
                {
                    Thread.Sleep(100);
                }
            }
        }
        //vsHost32.exe crashes when this is used not sure why
        public void appTermination()
        {
            while (true)
            {
                if (Console.ReadKey(true).Key == ConsoleKey.Escape)
                {
                    t1.Abort();
                    t0.Abort();
                    uclient.Close();
                    Environment.Exit(0);
                }
            }
        }

        static void Main(string[] args)
        {
            AudioClient ac = new AudioClient(mcastaddress);
            ac.start();

        }
    }
}
