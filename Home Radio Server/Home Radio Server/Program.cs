using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using NAudio.Wave;
using NAudio;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using UdpAudioServer;
using NAudio.WindowsMediaFormat;

namespace Home_Radio_Server
{
    class UdpAudioServer
    {
        private int i;
        private IPEndPoint ipe;
        private IPAddress ip;
        private static readonly string ipaddress = "224.0.1.1";
        private static readonly int port = 65535;
        private WaveFileReader wfr;
        private WaveFormat wf;
        private Mp3FileReader mp3FR;
        private WMAFileReader wmafs;
        private ConcurrentQueue<byte[]> cq1 = new ConcurrentQueue<byte[]>();
        private Boolean isActive = false;
        private static Random rnd;
        private static BufferedWaveProvider waveprovider;
        private Thread t0;
        private Thread t2;
        private string[] list = new string[14];

        public UdpAudioServer(int port, string ipaddress)
        {
            this.ip = IPAddress.Parse(ipaddress);
            this.ipe = new IPEndPoint(ip, port);
            this.wf = new WaveFormat(44100, 32, 2);
            rnd = new Random();
            waveprovider = new BufferedWaveProvider(wf);
            t0 = new Thread(new ThreadStart(pathexe));
            t2 = new Thread(new ThreadStart(appTermination));
        }

        public void start()
        {
            Console.WriteLine("server is running");
            Console.WriteLine("Press ESC to stop");

            t0.Start();
            t2.Start();
            send();

        }

        public void sendWav()
        {

            wfr = new WaveFileReader(list[i]);
            WaveStream cs = new WaveFormatConversionStream(wf, wfr);
            while (isActive == true)
            {
                int sampleRate = cs.WaveFormat.SampleRate;
                int bytesRead = 0;
                while (cs.Position < cs.Length)
                {
                    byte[] bytes = new byte[sampleRate];
                    bytesRead = cs.Read(bytes, 0, sampleRate);
                    cq1.Enqueue(bytes);
                    Thread.Sleep(20);
                    isActive = false;
                }
            }
            pathexe();
        }

        public void sendMP3()
        {
            using (mp3FR = new Mp3FileReader(list[i]))
            {
                using (WaveStream pcmstream = WaveFormatConversionStream.CreatePcmStream(mp3FR))
                {
                    while (isActive == true)
                    {
                        var readFullyStream = new ReadFullyStream(pcmstream);
                        int sampleRate = pcmstream.WaveFormat.SampleRate;
                        int bytesRead = 0;
                        while (pcmstream.Position < pcmstream.Length)
                        {
                            byte[] bytes = new byte[sampleRate];
                            bytesRead = pcmstream.Read(bytes, 0, sampleRate);
                            cq1.Enqueue(bytes);
                            Thread.Sleep(20);
                            isActive = false;
                        }
                    }
                    pathexe();
                }
            }
        }

        public void sendWMA()
        {
            using (wmafs = new WMAFileReader(list[i]))
            {
                using (WaveStream pcmstream = WaveFormatConversionStream.CreatePcmStream(wmafs))
                {
                    while (isActive == true)
                    {
                        int sampleRate = pcmstream.WaveFormat.SampleRate;
                        int bytesRead = 0;
                        while (pcmstream.Position < pcmstream.Length)
                        {
                            byte[] bytes = new byte[sampleRate];
                            bytesRead = pcmstream.Read(bytes, 0, sampleRate);
                            cq1.Enqueue(bytes);
                            Thread.Sleep(20);
                            isActive = false;
                        }
                    }
                    pathexe();
                }
            }
        }
        public void send()
        {
            Thread.Sleep(200);
            using (UdpClient uclient = new UdpClient())
            {
                uclient.JoinMulticastGroup(ip);
                while (true)
                {
                    if (cq1.Count() != 0)
                    {
                        byte[] b;
                        cq1.TryDequeue(out b);
                        uclient.Send(b, b.Length, ipe);
                        isActive = false;
                    }
                    else
                    {
                        Thread.Sleep(10);
                    }
                }

            }
        }

        public void pathexe()
        {
            isActive = true;
            i = rnd.Next(0, 12);
            i = 9;
            if (String.Equals(Path.GetExtension(list[i]), ".mp3") || String.Equals(Path.GetExtension(list[i]), ".MP3"))
            {
                sendMP3();
            }
            else if (String.Equals(Path.GetExtension(list[i]), ".wav") || String.Equals(Path.GetExtension(list[i]), ".WAV"))
            {
                sendWav();
            }
            else if (String.Equals(Path.GetExtension(list[i]), ".wma") || String.Equals(Path.GetExtension(list[i]), ".WMA"))
            {
                sendWMA();
            }
        }

        public void appTermination()
        {
            while (true)
            {
                if (Console.ReadKey(true).Key == ConsoleKey.Escape)
                {
                    t0.Abort();
                    Environment.Exit(0);
                }
            }
        }
        static void Main(string[] args)
        {
            UdpAudioServer uas = new UdpAudioServer(port, ipaddress);
            uas.start();
        }

    }
}
