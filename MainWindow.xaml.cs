using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace greenhouse
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            InitializeSystem();
        }
        private ushort co2Level;
        private ushort startCo2Level;
        private byte cloudiness;

        private DispatcherTimer simulationTimer;
        private DateTime simulationTime;
        private Random random;
        private uint minutesCounter;

        private bool isUvSystemActive;
        private bool isIrSystemActive;
        private bool isCo2SystemActive;
        private DateTime lastIrActivation;

        string logFile;
        private void InitializeSystem()
        {
            random = new Random();
            simulationTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 5, 30, 0);

            co2Level = Convert.ToUInt16(random.Next(300, 801)); // 300-800 ppm
            cloudiness = Convert.ToByte(random.Next(35, 66)); // 35-65%

            isUvSystemActive = false;
            isIrSystemActive = false;
            isCo2SystemActive = false;

            lastIrActivation = simulationTime - TimeSpan.FromHours(1);

            File.Create(System.IO.Path.Combine($"log {DateTime.Now.ToString().Replace(':', '-')}.txt")).Close();
            logFile = $"log {DateTime.Now.ToString().Replace(':', '-')}.txt";
            UpdateDisplay();
            LogEvent($"Система инициализирована. CO₂: {co2Level} ppm, Облачность: {cloudiness}%");
        }

        private void UpdateDisplay()
        {
            tBSimulationTime.Text = "Симуляционное время: " + simulationTime.ToString("HH:mm");

            tBUVState.Text = isUvSystemActive ? "Состояние: Активна" : "Состояние: Выключена";
            tBIKState.Text = isIrSystemActive ? "Состояние: Активна" : "Состояние: Выключена";
            tBCO2State.Text = isCo2SystemActive ? "Состояние: Активна" : "Состояние: Выключена";

            pGClouds.Value = cloudiness;
            pGClouds.ToolTip = cloudiness + "%";
            pGCO2Value.Value = co2Level;
            pGCO2Value.ToolTip = co2Level + " ppm";
        }

        private void LogEvent(string message)
        {
            string logEntry = $"{simulationTime:dd.MM.yyyy HH:mm:ss} - {message}";
            tBLogs.AppendText(logEntry + "\n");
            tBLogs.ScrollToEnd();
            WriteToLogFile(logEntry);
        }
        private void WriteToLogFile(string logEntry)
        {
            try
            {
                StreamWriter logStream = new(logFile, true);
                logStream.WriteLine(logEntry);
                logStream.Close();
            }
            catch (Exception)
            {

            }
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            if (simulationTimer == null)
            {
                simulationTimer = new DispatcherTimer();
                simulationTimer.Interval = TimeSpan.FromSeconds(1);
                simulationTimer.Tick += SimulationTimer_Tick;
                simulationTimer.Start();
                LogEvent("Симуляция запущена");
                btnStart.IsEnabled = false;
                btnStop.IsEnabled = true;
                //btnCO2.IsEnabled = true;
                //btnIK.IsEnabled = true;
                //btnUV.IsEnabled = true;
            }
        }
        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            if (simulationTimer != null)
            {
                simulationTimer.Stop();
                simulationTimer = null;

                LogEvent("Симуляция остановлена");
                btnStart.IsEnabled = true;
                btnStop.IsEnabled = false;
                //btnCO2.IsEnabled = false;
                //btnIK.IsEnabled = false;
                //btnUV.IsEnabled = false;
            }
        }
        private void SimulationTimer_Tick(object sender, EventArgs e)
        {
            simulationTime = simulationTime.AddMinutes(1);
            minutesCounter++;
            if (minutesCounter % 20 == 0)
            {
                if (!isUvSystemActive)
                {
                    double change = (random.NextDouble() * 100) - 50;
                    co2Level = Convert.ToUInt16(Math.Max(300, Math.Min(1300, co2Level + change)));
                }

                double cloudChange = (random.NextDouble() * 26) - 13;
                cloudiness = Convert.ToByte(Math.Max(0, Math.Min(100, cloudiness + cloudChange)));
            }

            if (simulationTime.Hour == 6 && !isUvSystemActive)
            {
                isUvSystemActive = true;
                isCo2SystemActive = true;
                startCo2Level = co2Level;
                LogEvent($"Включена система УФ облучения.");
                LogEvent($"Включена система поднятия CO₂.");
            }
            else if (simulationTime.Hour == 7)
            {
                if (isUvSystemActive)
                {
                    isUvSystemActive = false;
                    pGUV.Value = 0;
                    LogEvent($"Отключена система УФ облучения.");
                }
            }
            if (isUvSystemActive)
            {
                pGUV.Value += 1;
            }
            if (isCo2SystemActive)
            {
                 pGCO2.Value = ((Convert.ToDouble(co2Level) - startCo2Level) / (1200D - startCo2Level)) * 100;
                co2Level += 10;
                if (co2Level > 1200)
                {
                    co2Level = 1200;
                    isCo2SystemActive = false;
                    pGCO2.Value = 0;
                    LogEvent($"Отключена система поднятия CO₂.");
                }
            }

            bool isDayTime = simulationTime.Hour >= 6 && simulationTime.Hour < 20;
            bool isCloudy = cloudiness > 50;

            bool shouldActivateIr = isDayTime && isCloudy && simulationTime - lastIrActivation > TimeSpan.FromHours(1);

            if (shouldActivateIr && !isIrSystemActive)
            {
                isIrSystemActive = true;
                lastIrActivation = simulationTime;
                lastIrActivation += TimeSpan.FromHours(1);
                LogEvent($"Включен ИК обогрев.");
            }

            if (isIrSystemActive && simulationTime.AddHours(1) >= lastIrActivation.AddHours(1))
            {
                isIrSystemActive = false;

                double hourlyConsumption = Math.Round(5 + random.NextDouble() * 10, 2);

                LogEvent($"Выключен ИК обогрев. Потреблено: {hourlyConsumption} кВт·ч");
                pGIK.Value = 0;
            }
            
            if (isIrSystemActive)
            {
                pGIK.Value += 1;
            }

            UpdateDisplay();
        }

        //private void btnCO2_Click(object sender, RoutedEventArgs e)
        //{
        //    if(!isCo2SystemActive)
        //    {
        //        isCo2SystemActive = true;
        //        LogEvent($"{simulationTime} - Система поднятия CO₂ включена вручную.");
        //    }
        //    else
        //    {
        //        isCo2SystemActive = false;
        //        pGCO2.Value = 0;
        //        LogEvent($"{simulationTime} - Система поднятия CO₂ выключена вручную.");
        //    }
        //}

        //private void btnUV_Click(object sender, RoutedEventArgs e)
        //{
        //    if (!isUvSystemActive)
        //    {
        //        isUvSystemActive = true;
        //        LogEvent($"{simulationTime} - Система УФ облучения включена вручную.");
        //    }
        //    else
        //    {
        //        isUvSystemActive = false;
        //        pGUV.Value = 0;
        //        LogEvent($"{simulationTime} - Система УФ облучения выключена вручную.");
        //    }
        //}

        //private void btnIK_Click(object sender, RoutedEventArgs e)
        //{
        //    if (!isIrSystemActive)
        //    {
        //        isIrSystemActive = true;
        //        LogEvent($"{simulationTime} - Система ИК обогрева включена вручную.");
        //    }
        //    else
        //    {
        //        isIrSystemActive = false;
        //        pGUV.Value = 0;
        //        double hourlyConsumption = Math.Round(3 + random.NextDouble() * 7, 2);
        //        LogEvent($"{simulationTime} - Система ИК обогрева выключена вручную. Потреблено: {hourlyConsumption} кВт·ч");
        //    }
        //}
    }
}