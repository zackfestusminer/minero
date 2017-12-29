using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Configuration;
using System.Management;

namespace StartStop
{



    public partial class Form1 : Form
    {
        bool run = true;
        int errCount = 0;
        string _processName = string.Empty;
        int _runFor = 20;
        int _pauseFor = 1;

        //  0 = no just 1
        //  1 = alternate 1 and 2
        //  2 = 2 always
        bool _twoNonVegaThreads = false;

        

        public Form1()
        {
            InitializeComponent();
        }


        private string AppSetting(string setting)
        {

            return ConfigurationManager.AppSettings[setting].ToString();
        }


        private void InitVariables()
        {
            _runFor = Convert.ToInt32(ConfigurationManager.AppSettings["runFor"]) * 1000;
            _pauseFor = Convert.ToInt32(ConfigurationManager.AppSettings["pauseFor"]) * 1000;
            _processName = ConfigurationManager.AppSettings["processName"];

            int TwoThreadsForNonVegas = Convert.ToInt32(ConfigurationManager.AppSettings["TwoThreadsForNonVegas"]);
            switch (TwoThreadsForNonVegas)
            {
                //  0 = no just 1
                //  1 = alternate 1 and 2
                //  2 = 2 always

                case 0:
                    _twoNonVegaThreads = false;
                    break;
                case 1:
                    if (_twoNonVegaThreads)
                        _twoNonVegaThreads = false;
                    else
                        _twoNonVegaThreads = true;
                    break;
                case 2:
                    _twoNonVegaThreads = true;
                    break;
            }
               

            if (Convert.ToInt32(ConfigurationManager.AppSettings["TwoThreadsForNonVegas"]) == 0)
                _twoNonVegaThreads = false;


        }

        private void ShutDownWindows()
        {
            var psi = new ProcessStartInfo("shutdown", "/r /f /t 0");

            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            Process.Start(psi);
        }
        private void Log(string message, bool error) {
            


            System.IO.StreamWriter sw = new System.IO.StreamWriter("Log.txt",true);
            sw.WriteLine(DateTime.Now.ToString() + " " + message);          
            sw.Flush();
            sw.Close();
            sw.Dispose();
            if (error)
            {
                errCount++;
                lblErrorCount.Text = errCount.ToString();
                timer1.Interval = _pauseFor;
            }
         
        }


        /*
                private void RunMiner()
                {

                    //   InitVariables();


                       var proc = new Process();
                       proc.StartInfo.FileName = _processName  ;
                       proc.Start();
                       proc.Close();


                       StartProcess(_processName);
                       timer1.Interval = _runFor ;
                       run = true;
                       Log("Miner ran",false);

                   }
                    */
        private void KillProcess(string processNmae)
        {

            Process[] processes = Process.GetProcessesByName(processNmae);
            foreach (var process in processes)
            {
                process.Kill();
            }
        }


        private void StartProcess(string processName)
        {
            var proc = new Process();
            proc.StartInfo.FileName = processName;
           //proc.StartInfo.Verb = "runas";
            proc.Start();
            proc.Close();
        }
        private void PauseMiner()
        {

            try
            {
               // InitVariables();
                KillProcess(_processName);
                timer1.Interval = _pauseFor;
                run = false;
                Log("Miner Paused", false);

               
            }

            catch (Exception ex)
            {
                Log(ex.ToString(), true);
                ShutDownWindows();
            }
        }


   


        private void StartMiner()
        {

            try
            {
                InitVariables();
                ConfigFile_Builder();
                StartProcess(_processName);
                timer1.Interval = _runFor;
                run = true;
                Log("Miner ran", false);
                timer1.Enabled = true;


            }
            catch (Exception ex)
            {
                Log(ex.ToString(), true);
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            StartMiner();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            PauseMiner();
        }


        private void BuildResetGPUBatch()
        {
            try
            {
                System.IO.StreamWriter sw = new System.IO.StreamWriter("ResetGPUs.bat", false);
                ManagementObjectCollection objVidControls = VideoControllers();
                int countVidControls = objVidControls.Count;
                sw.Write("OverdriveNTool.exe");
                System.Text.StringBuilder sbr = new StringBuilder(250);
                System.Text.StringBuilder sbp= new StringBuilder(250);
                string vegaAlias = AppSetting("VegaDeviceAlias");
                string RXAlias = AppSetting("RXDeviceAlias");


                for (int i = 0; i < countVidControls; i++)
                {
                    sbr.Append(" -r" + i.ToString());
                    sbp.Append(" -p" + i.ToString());                    

                    if (i == 0)
                        sbp.Append ("Vega56");
                    else
                    {
                        sbp.Append("RX480580");
                    }
                }
                sw.WriteLine(sbr.ToString() + " " + sbp.ToString());
                sw.WriteLine("");
                sw.WriteLine("devcon find *> list.txt");
                
                sw.WriteLine("devcon disable *" + vegaAlias);
                sw.WriteLine("devcon enable *" + vegaAlias);

                if (Convert.ToBoolean(AppSetting("DisEnRXDevices")))
                {
                    sw.WriteLine("devcon disable *" + RXAlias);
                    sw.WriteLine("devcon enable *" + RXAlias);
                }
                sw.Flush();
                sw.Close();
                sw.Dispose();
            }
            catch (Exception ex)
            {
                Log(ex.ToString(), true);
            }

        }

        private void ResetGPUs()
        {
            BuildResetGPUBatch();
            StartProcess("ResetGPUsElev");
            int PauseBetweenGPUResetandMining = Convert.ToInt32(AppSetting("PauseBetweenGPUResetandMining")) * 1000;
            System.Threading.Thread.Sleep(PauseBetweenGPUResetandMining);
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                if (Process.GetProcessesByName("StartStop").Length > 1)
                {
                    Log("Attempting to run this app twice. Exiting this instance now.", true);
                    Application.Exit();
                }
                else
                {

                    ResetGPUs();
                    StartMiner();
                }
            }
            catch (Exception ex)
            {
                Log(ex.ToString(), true);
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            try
            {
                if (run)
                {
                    PauseMiner() ;
                }
                else
                {
                    StartMiner();
                }
            }
            catch (Exception ex)
            {
                Log(ex.ToString(),true);
                ShutDownWindows();

            }
        }

        private void txtRunFor_TextChanged(object sender, EventArgs e)
        {

        }

        private void txtPauseFor_TextChanged(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private ManagementObjectCollection VideoControllers()
        {
            return new ManagementObjectSearcher("select * from Win32_VideoController").Get();

        }

        private void ConfigFile_Builder()
        {


            // bool TwoThreadsForNonVegas = Convert.ToBoolean( AppSetting("TwoThreadsForNonVegas"));
            ManagementObjectCollection objVidControls = VideoControllers(); // new ManagementObjectSearcher("select * from Win32_VideoController");
            int gpu_thread_num = 0;
            int index = 0;
            System.Text.StringBuilder sb = new StringBuilder();
            int numGpus = 0;

            int numGpusIndexed = objVidControls.Count - 1;
            numGpus++;

            sb.AppendLine("\"gpu_threads_conf\" : [");
           

            foreach (ManagementObject obj in objVidControls)
            {

                string thread_conf = string.Empty;
                if (obj["Name"].ToString().ToLower().Contains("vega"))
                {
                    sb.AppendLine(AppSetting("ThreadConfig1Vega56").Replace("ReplaceThisIndex", numGpusIndexed.ToString()));
                    sb.AppendLine(AppSetting("ThreadConfig2Vega56").Replace("ReplaceThisIndex", numGpusIndexed.ToString()));                 
                    gpu_thread_num = gpu_thread_num + 2;
                }
                else
                {                
                    sb.AppendLine(AppSetting("ThreadConfig1NonVega").Replace("ReplaceThisIndex", index.ToString()));
                    gpu_thread_num++;
                    if (_twoNonVegaThreads) {                     
                        sb.AppendLine(AppSetting("ThreadConfig2NonVega").Replace("ReplaceThisIndex", index.ToString()));
                        gpu_thread_num++;
                    }
                    index++;
                   
                }               

                    

            }



            sb.AppendLine("],");


            System.IO.StreamWriter sw = new System.IO.StreamWriter("config.txt", false);
            sw.WriteLine("\"gpu_thread_num\" : " + gpu_thread_num.ToString () + ",");
            sw.WriteLine(sb.ToString());

            System.IO.StreamReader sr = new System.IO.StreamReader("config_Footer.txt");
            sw.WriteLine(sr.ReadToEnd());

            sr.Close();
            sr.Dispose();

            sw.Flush();
            sw.Close();
            sw.Dispose();

            /*
            foreach (ManagementObject obj in objvide.Get())
            {
                MessageBox.Show(obj["Name"].ToString());

                Console.Write("Name  -  " + obj["Name"] + "</br>");
                Console.Write("DeviceID  -  " + obj["DeviceID"] + "</br>");
                Console.Write("AdapterRAM  -  " + obj["AdapterRAM"] + "</br>");
                Console.Write("AdapterDACType  -  " + obj["AdapterDACType"] + "</br>");
                Console.Write("Monochrome  -  " + obj["Monochrome"] + "</br>");
                Console.Write("InstalledDisplayDrivers  -  " + obj["InstalledDisplayDrivers"] + "</br>");
                Console.Write("DriverVersion  -  " + obj["DriverVersion"] + "</br>");
                Console.Write("VideoProcessor  -  " + obj["VideoProcessor"] + "</br>");
                Console.Write("VideoArchitecture  -  " + obj["VideoArchitecture"] + "</br>");
                Console.Write("VideoMemoryType  -  " + obj["VideoMemoryType"] + "</br>");

            }
            */
        }

        private void button4_Click(object sender, EventArgs e)
        {
            StartProcess(@"list");
        }
    }


}
