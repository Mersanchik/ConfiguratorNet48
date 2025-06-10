using System;
using System.Collections.Generic;
using System.Linq;
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
using Configurator.Windows;
using System.Text.RegularExpressions;
using System.IO.Ports;
using System.Threading;
using System.IO;
using Microsoft.Win32;
using System.Windows.Threading;

namespace Configurator
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private SerialPort _serialPort;
        string txtPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "COMSettingsParameters.txt");//файл парметров подключения

        private DispatcherTimer _timer;
        private DispatcherTimer _timer2;
        public MainWindow()
        {
            InitializeComponent();
            CheckFileParameters();
            tbMessage.Text = "Плата имеет 2 кнопки:\nПервая с конца открывает терминал, для сенсора.\nВторая послать запрос, для коптера.";
            btSendConfiguration.Content = "Отправить конфигурацию";
            btSendConfiguration.IsEnabled = false;
            btReadConfiguration.Visibility = Visibility.Hidden;
            btSendToConsole.IsEnabled = false;


            string[] lines = File.ReadAllLines(txtPath);//чтение всех строк из файла
            foreach (var items in SerialPort.GetPortNames())
            {
                if (lines.Contains(items))
                {
                    tbNameComPotr.Text = items;
                    break;
                }
                else
                {
                    tbNameComPotr.Text = "COM порт не выбран!";
                }
            }

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1); // Интервал обновления в секундах
            _timer.Tick += Timer_Tick;
            _timer.Start();

        }

        string fileName, filePath;
        private void CreateFileWithDateTime()
        {
            DateTime now = DateTime.Now;
            fileName = $"BboxResult_{now:yyyy-MM-dd_HH-mm-ss}.txt";
            filePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);//файл сохранения результатов bbox 
        }

        private void Timer_Tick(object sender, EventArgs e)//Для обновления данных о выбранном порте.
        {
            string[] lines = File.ReadAllLines(txtPath);
            foreach (var items in SerialPort.GetPortNames())
            {
                if (lines.Contains(items))
                {
                    tbNameComPotr.Text = items;
                    break;
                }
                else
                {
                    tbNameComPotr.Text = "COM порт не выбран!";
                }
            }
        }

        private void CheckFileParameters()//Проверяет наличие файла COMSettingsParameters.txt и BboxResult.txt
        {
            if (!File.Exists(txtPath))
            {
                using (File.Create(txtPath)) { }
            }
        }

        /// <summary>
        /// The method for connecting the COM port
        /// The method sets up the port and connects to it.
        /// </summary>
        private void InitializeSerialPort()//Подключиться
        {
            try
            {
                string[] lines = File.ReadAllLines(txtPath);//чтение всех строк из файла
                string txtCOMPort = Convert.ToString(lines[0]);//COM порт
                

                _serialPort = new SerialPort
                {
                    PortName = txtCOMPort,
                    ReadTimeout = 1000,
                    WriteTimeout = 1000
                };


                string txtSpeed = Convert.ToString(lines[1]);//Скорость
                switch (txtSpeed)
                {
                    case "9600":
                        _serialPort.BaudRate = 9600;
                        break;

                    case "19200":
                        _serialPort.BaudRate = 19200;
                        break;

                    case "38400":
                        _serialPort.BaudRate = 38400;
                        break;

                    case "57600":
                        _serialPort.BaudRate = 57600;
                        break;

                    case "115200":
                        _serialPort.BaudRate = 115200;
                        break;

                    case "921600":
                        _serialPort.BaudRate = 921600;
                        break;
                }

                string txtDataBits = Convert.ToString(lines[2]);//Биты данных
                switch (txtDataBits)
                {
                    case "8":
                        _serialPort.DataBits = 8;
                        break;

                    case "7":
                        _serialPort.DataBits = 7;
                        break;
                }

                string txtStopBits = Convert.ToString(lines[3]);//Стоп биты
                switch (txtStopBits)
                {
                    case "0":
                        _serialPort.StopBits = StopBits.None;
                        break;

                    case "1":
                        _serialPort.StopBits = StopBits.One;
                        break;

                    case "2":
                        _serialPort.StopBits = StopBits.Two;
                        break;
                }

                string txtTheParity = Convert.ToString(lines[4]);//Четность
                switch (txtTheParity)
                {
                    case "(0, Нет)":
                        _serialPort.Parity = Parity.None;
                        break;

                    case "(1, Нечет)":
                        _serialPort.Parity = Parity.Odd;
                        break;

                    case "(2, Чет)":
                        _serialPort.Parity = Parity.Even;
                        break;
                }

                try
                {
                    _serialPort.DataReceived += SerialPort_DataReceived;//Открытие порта
                    try
                    {
                        _serialPort.Open();
                        tbConsole.Text += "\nCOM порт открыт\n";
                        tbMessage.Text = "Для отправки команды, зажимаем первую кнопку с конца, выбираем команду в списке или прописываем команду вручную и нажимаем 'Отправить'!";
                        btSendToConsole.IsEnabled = true;
                        btConnect.IsEnabled = false;
                        btDisConnect.IsEnabled = true;

                    }
                    catch (Exception ex)
                    {
                        tbMessage.Text = "";
                        tbMessage.Text += $"Ошибка при открытии COM порта: {ex.Message}";
                    }
                }
                catch { }
            }
            catch
            {
                MessageBox.Show("Убедитесь что перед 'Подключением' были заполнены 'Параметры подключения'!", "Ошибка!");
            }
        }
        
        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)//Вывод данных COM порта
        {
            try
            {
                string data = _serialPort.ReadExisting();
                Dispatcher.Invoke(new Action(() =>
                {
                    tbConsole.Text += data;

                    if (cbCommandsTerminal.Text != "")
                    {
                        if (CNF.Visibility != Visibility.Hidden | RTC.Visibility != Visibility.Hidden | BBOX.Visibility != Visibility.Hidden | ADC.Visibility != Visibility.Hidden)
                        {
                            Regex regex2 = new Regex(@"BTN:PRESS:1", RegexOptions.Singleline);//если пользователь нажимает кнопку на плате и в консоли выводится сообщение об открытие терминала
                            Match match2 = regex2.Match(data);

                            if (match2.Success)
                            {
                                Dispatcher.Invoke(new Action(() =>
                                {
                                    btSendConfiguration.IsEnabled = true;
                                    btReadConfiguration.IsEnabled = true;

                                }));
                            }

                            Regex regex3 = new Regex(@"\.\.\.TERM_CLOSED\.\.\.", RegexOptions.Singleline);//когда терминал закрывается
                            Match match3 = regex3.Match(data);

                            if (match3.Success)
                            {
                                Dispatcher.Invoke(new Action(() =>
                                {
                                    btSendConfiguration.IsEnabled = false;
                                    btReadConfiguration.IsEnabled = false;

                                }));
                            }


                            Regex regex7 = new Regex(@"ADC\[\d+\] t=(\d+):.*?AKB=(\d+) VE=(\d+) ES=(\d+).*?BH1750=(\d+).*?RF_rssi=(-?\d+)", RegexOptions.Singleline);
                            Match match7 = regex7.Match(data);

                            if (match7.Success)
                            {
                                if (cbCommandsTerminal.Text == "Приведение результатов (adc)")
                                {
                                    tbADC.Text = match7.Groups[1].Value;
                                    tbAKB.Text = match7.Groups[2].Value;
                                    tbVE.Text = match7.Groups[3].Value;
                                    tbES.Text = match7.Groups[4].Value;
                                    tbBH.Text = match7.Groups[5].Value;
                                    tbRFRssi.Text = match7.Groups[6].Value;
                                }
                            }

                            Regex regex6 = new Regex(@"RTC=(\d+):(\d+)", RegexOptions.Singleline);
                            Match match6 = regex6.Match(data);

                            if (match6.Success)
                            {
                                if (cbCommandsTerminal.Text == "Считать время (rtc)")
                                {
                                    tbRtcN.Text = match6.Groups[1].Value;
                                }
                            }


                            Regex startBbox = new Regex(@"\.bbox\s+\d+\s+\d+");
                            Regex endBbox = new Regex(@"FLM_RET=\d+\s+BBOX_STAT\[\d+\]:OK");
                            Match startMatch = startBbox.Match(data);
                            Match endMatch = endBbox.Match(data);

                            if (startMatch.Success)
                            {
                                if (cbCommandsTerminal.Text == "Данные из ЧЯ (bbox N C)")
                                {
                                    string dataToWrite = data.Substring(startMatch.Index);

                                    if (endMatch.Success)
                                    {
                                        dataToWrite = data.Substring(startMatch.Index, endMatch.Index + endMatch.Length - startMatch.Index);
                                    }
                                    else
                                    {
                                        StringBuilder fullData = new StringBuilder(dataToWrite);
                                        while (!endMatch.Success)
                                        {
                                            data = _serialPort.ReadExisting();
                                            fullData.Append(data);
                                            endMatch = endBbox.Match(fullData.ToString());
                                        }
                                        dataToWrite = fullData.ToString();
                                    }
                                    CreateFileWithDateTime();
                                    using (StreamWriter streamWriter = new StreamWriter(filePath, true))
                                    {
                                        streamWriter.WriteLine(dataToWrite);
                                    }
                                }

                            }
                        }
                        else
                        {
                            tbMessage.Text = "Выберите команду!";
                        }

                        Regex regex8 = new Regex(@"ROLE \(1-COPTER; 2-SENSOR\) = (\d+).*?tout\(ms\)= (\d+).*?ack_range=(-?\d+).*?timeout\(sek\)=(\d+).*?freq\(Hz\)=(\d+).*?Hold_rssi=(-?\d+).*?tout\(sec\)=(\d+)", RegexOptions.Singleline);
                        Match match8 = regex8.Match(data);

                        if (match8.Success)
                        {
                            string role = match8.Groups[1].Value;
                            if (role == "1")
                            {
                                Copter.IsChecked = true;
                                Sensor.IsChecked = false;
                            }
                            else
                            {
                                if (role == "2")
                                {
                                    Sensor.IsChecked = true;
                                    Copter.IsChecked = false;
                                }
                            }
                            tbTout.Text = match8.Groups[2].Value;
                            tbAckRange.Text = match8.Groups[3].Value;
                            tbTimeout.Text = match8.Groups[4].Value;
                            tbFreq.Text = match8.Groups[5].Value;
                            tbHoldRssi.Text = match8.Groups[6].Value;
                            tbToutSec.Text = match8.Groups[7].Value;  
                        }

                        Regex regex9 = new Regex(@"FLM_RET=0\s+BBOX_STAT\[(\d+)\]:OK", RegexOptions.Singleline);
                        Match match9 = regex9.Match(data);

                        if (match9.Success)
                        {
                            if (cbCommandsTerminal.Text == "Информация о ЧЯ (bbox)")
                            {
                                tbBboxC.Text = match9.Groups[1].Value;
                            }
                        }

                        Regex regex = new Regex(@"--- WORK --- SENSOR ---", RegexOptions.Singleline);//когда был выбран Датчик
                        Match match = regex.Match(data);

                        if (match.Success)
                        {
                            Dispatcher.Invoke(new Action(() =>
                            {
                                btSendConfiguration.IsEnabled = false;
                                btSendToConsole.IsEnabled = true;
                                CNF.Visibility = Visibility.Hidden;
                            }));
                        }

                        Regex regex1 = new Regex(@"--- WORK --- COPTER ----", RegexOptions.Singleline);//когда был выбран Коптер
                        Match match1 = regex1.Match(data);

                        if (match1.Success)
                        {
                            Dispatcher.Invoke(new Action(() =>
                            {
                                btSendConfiguration.IsEnabled = true;
                                btSendToConsole.IsEnabled = true;
                                CNF.Visibility = Visibility.Hidden;
                            }));
                        }
                    }

                    Regex regex4 = new Regex(@"TERM_EXI_OFF\.", RegexOptions.Singleline);
                    Match match4 = regex4.Match(data);

                    if (match4.Success)
                    {
                        Dispatcher.Invoke(new Action(() =>
                        {
                            btSendConfiguration.IsEnabled = true;
                            btReadConfiguration.IsEnabled = true;

                        }));
                    }

                    Regex regex5 = new Regex(@"\.\.\.TERM_EXIT\.\.\.", RegexOptions.Singleline);
                    Match match5 = regex5.Match(data);

                    if (match5.Success)
                    {
                        Dispatcher.Invoke(new Action(() =>
                        {
                            btSendConfiguration.IsEnabled = false;
                            btReadConfiguration.IsEnabled = false;

                        }));
                    }

                    Regex regex10 = new Regex(@"REQ_DATA:", RegexOptions.Singleline);
                    Match match10 = regex10.Match(data);

                    if (match10.Success)
                    {
                        Dispatcher.Invoke(new Action(() =>
                        {
                            btSendConfiguration.IsEnabled = true;

                        }));
                    }
                }));

                try
                {
                    //Чтение ответа от платы
                    string response = _serialPort.ReadLine();
                }
                catch (TimeoutException ex)
                {
                    tbMessage.Text = "Плата не отвечает в течение 20 секунд.";
                }
            }
            catch { }
        }

        private void tbTout_PreviewTextInput(object sender, TextCompositionEventArgs e)//Запрет на ввод букв
        {
            Regex regex = new Regex("[^0-9]");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void tbHoldRssi_KeyDown(object sender, KeyEventArgs e)//Ввод только чисел и знака минуса
        {
            if (!(e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9 || e.Key >= Key.D0 && e.Key <= Key.D9 ||
                e.Key == Key.OemMinus || e.Key == Key.Back))
            {
                e.Handled = true;
            }
        }

        private void tbHoldRssi_TextChanged(object sender, TextChangedEventArgs e)//Проверка на ввод только числа и одного минуса
        {
            TextBox textBox = (TextBox)sender;
            string text = textBox.Text;

            // Проверяем, что текст содержит не более одного символа минуса и состоит только из цифр и минуса
            if (!Regex.IsMatch(text, @"^-?\d*$") || text.Count(c => c == '-') > 1)
            {
                // Если условие не выполняется, удаляем последний введенный символ
                textBox.Text = text.Remove(text.Length - 1);
                tbMessage.Text = "В строке может содержаться только 1 минус";
            }
        }

        private void tbROLE_KeyDown(object sender, KeyEventArgs e)//Ввод только числа 1 и 2
        {
            if (!(e.Key >= Key.NumPad1 && e.Key <= Key.NumPad2 || e.Key >= Key.D1 && e.Key <= Key.D2 ||
                e.Key == Key.Back))
            {
                e.Handled = true;
            }
        }

        private void btConnect_Click(object sender, RoutedEventArgs e)//Подключиться
        {
            try
            {
                InitializeSerialPort();
                if (_serialPort.IsOpen)//Проверяем что порт подключен 
                {
                    BtConnectionParameters.IsEnabled = false;
                }
                else
                {
                    _serialPort.Close();
                }
            }
            catch { }
        }

        private void btDisConnect_Click(object sender, RoutedEventArgs e)//Отключиться
        {
            try
            {
                if (_serialPort.IsOpen)//Проверяем что порт подключен 
                {
                    _timer2 = new DispatcherTimer();
                    _timer2.Interval = TimeSpan.FromSeconds(5);
                    _timer2.Tick += Timer_Tick2;

                    _serialPort.WriteLine("exit" + "\r");
                    tbMessage.Text = "Отключение COM порта.\nПожалуйста подождите!";
                    _timer2.Start();
                }
            }
            catch { }

        }

        private void Timer_Tick2(object sender, EventArgs e)//Для кнопки "Отключиться"
        {
            _timer2.Stop();
            _serialPort.Close();
            tbConsole.Text += "\nCOM порт закрыт\n";
            tbMessage.Text = "Плата имеет 2 кнопки:\nПервая с конца открывает терминал, для сенсора.\nВторая послать запрос, для коптера.";

            btConnect.IsEnabled = true;
            btDisConnect.IsEnabled = false;
            btSendConfiguration.IsEnabled = false;
            btReadConfiguration.Visibility = Visibility.Hidden;
            btSendToConsole.IsEnabled = false;
            BtConnectionParameters.IsEnabled = true;

            CNF.Visibility = Visibility.Hidden;
            RTC.Visibility = Visibility.Hidden;
            BBOX.Visibility = Visibility.Hidden;
            ADC.Visibility = Visibility.Hidden;

            tbTextForConsole.Text = "";
            cbCommandsTerminal.Text = "";
        }

        private void btSendToConsole_Click(object sender, RoutedEventArgs e)//Отправить текст в консоль
        {
            tbMessage.Text = "";
            try
            {
                if (_serialPort.IsOpen)
                {
                    try
                    {
                        if (cbCommandsTerminal.Text == "Редактировать конфигурацию (cnf 1)")
                        {
                            btSendConfiguration.Content = "Отправить конфигурацию";
                            tbTextForConsole.Text = "cnf 1";
                            CNF.Visibility = Visibility.Visible;
                            RTC.Visibility = Visibility.Hidden;
                            BBOX.Visibility = Visibility.Hidden;
                            ADC.Visibility = Visibility.Hidden;
                            tbTextForConsole.IsReadOnly = true;
                            btReadConfiguration.Visibility = Visibility.Visible;
                            tbMesBbox.Visibility = Visibility.Hidden;
                            tbMessage.Text = "Убедитесь, что вы прожали кнопку на плате после того как заполнили поля (если ранее не была отправлена команда exit 0)!\n\nЗаполните поля слева!\n\nТакже, если уже есть заготовленные переменные нажмите кнопку 'Загрузить конфигурацию'.\nЕсли хотите заготовить шаблон для повторного его использования,\n" +
                                "заполните поля и нажмите кнопку 'Сохранить конфигурацию'.\nДля отправки конфигурации нажмите 'Отправить конфигурацию'";
                        }
                        else
                        {
                            if (cbCommandsTerminal.Text == "Установить время (rtc 1)")
                            {
                                btSendConfiguration.Content = "Отправить время";
                                RTC.Visibility = Visibility.Visible;
                                tbTextTitleRtc.Text = "Установка времени RTC";
                                tbRtcN.IsReadOnly = false;
                                CNF.Visibility = Visibility.Hidden;
                                BBOX.Visibility = Visibility.Hidden;
                                ADC.Visibility = Visibility.Hidden;
                                tbTextForConsole.IsReadOnly = true;
                                tbRtcN.IsReadOnly = false;
                                btReadConfiguration.Visibility = Visibility.Hidden;
                                btSendConfiguration.Visibility = Visibility.Visible;
                                tbMesBbox.Visibility = Visibility.Hidden;
                                tbMessage.Text = "Убедитесь, что вы прожали кнопку на плате после того как заполнили поля (если ранее не была отправлена команда exit 0)!\n\nЗаполните поля слева!\n\nТакже, если уже есть заготовленные переменные нажмите кнопку 'Загрузить конфигурацию'.\nЕсли хотите заготовить шаблон для повторного его использования,\n" +
                                    "заполните поля и нажмите кнопку 'Сохранить конфигурацию'.\nДля отправки конфигурации нажмите 'Отправить конфигурацию'";
                            }
                            else
                            {
                                if (cbCommandsTerminal.Text == "Данные из ЧЯ (bbox N C)")
                                {
                                    btSendConfiguration.Content = "Получить данные";
                                    BBOX.Visibility = Visibility.Visible;
                                    RTC.Visibility = Visibility.Hidden;
                                    CNF.Visibility = Visibility.Hidden;
                                    ADC.Visibility = Visibility.Hidden;
                                    tbTextForConsole.IsReadOnly = true;
                                    btInitializeTheLog.Visibility = Visibility.Visible;
                                    ResettingTheLog.Visibility = Visibility.Visible;
                                    btReadConfiguration.Visibility = Visibility.Hidden; 
                                    spFirstPart.Visibility = Visibility.Visible;
                                    btSendConfiguration.Visibility = Visibility.Visible;
                                    tbMesBbox.Visibility = Visibility.Visible;
                                    tbMessage.Text = "Убедитесь, что вы прожали кнопку на плате после того как заполнили поля (если ранее не была отправлена команда exit 0)!\n\nЗаполните поля слева!\n\nТакже, если уже есть заготовленные переменные нажмите кнопку 'Загрузить конфигурацию'.\nЕсли хотите заготовить шаблон для повторного его использования,\n" +
                                    "заполните поля и нажмите кнопку 'Сохранить конфигурацию'.\nДля отправки конфигурации нажмите 'Отправить конфигурацию'";
                                }
                                else
                                {
                                    if (cbCommandsTerminal.Text == "Закрыть терминал (exit)")
                                    {
                                        tbTextForConsole.Text = "exit";
                                        _serialPort.WriteLine("exit" + "\r");
                                        btSendConfiguration.Content = "Отправить конфигурацию";
                                        BBOX.Visibility = Visibility.Hidden;
                                        RTC.Visibility = Visibility.Hidden;
                                        CNF.Visibility = Visibility.Hidden;
                                        ADC.Visibility = Visibility.Hidden;
                                        tbTextForConsole.IsReadOnly = true;
                                        btReadConfiguration.Visibility = Visibility.Hidden;
                                        tbMesBbox.Visibility = Visibility.Hidden;
                                        tbMessage.Text = "Убедитесь, что вы прожали кнопку на плате и после этого прожали кнопку 'Отправить' (если ранее не была отправлена команда exit 0).";
                                    }
                                    else
                                    {
                                        if (cbCommandsTerminal.Text == "Запрет закрытия терминала (exit 0)")
                                        {
                                            tbTextForConsole.Text = "exit 0";
                                            _serialPort.WriteLine("exit 0" + "\r");
                                            btSendConfiguration.Content = "Отправить конфигурацию";
                                            tbTextForConsole.IsReadOnly = true;
                                            BBOX.Visibility = Visibility.Hidden;
                                            RTC.Visibility = Visibility.Hidden;
                                            CNF.Visibility = Visibility.Hidden;
                                            ADC.Visibility = Visibility.Hidden;
                                            btReadConfiguration.Visibility = Visibility.Hidden;
                                            tbMesBbox.Visibility = Visibility.Hidden;
                                            tbMessage.Text = "Убедитесь, что вы прожали кнопку на плате и после этого прожали кнопку 'Отправить' (если ранее не была отправлена команда exit 0).";
                                        }
                                        else
                                        {
                                            if (cbCommandsTerminal.Text == "Считать конфигурацию (cnf)")
                                            {
                                                ReadTheConfigurationFromTheDevice();
                                                tbTextForConsole.Text = "cnf";
                                                btSendConfiguration.Visibility = Visibility.Visible;
                                                tbMesBbox.Visibility = Visibility.Hidden;
                                            }
                                            else
                                            {
                                                if (cbCommandsTerminal.Text == "Считать время (rtc)")
                                                {
                                                    Thread.Sleep(1500);
                                                    _serialPort.WriteLine("rtc" + "\r");
                                                    Thread.Sleep(1000);
                                                    tbMessage.Text = "Убедитесь, что вы прожали кнопку на плате и после этого прожали кнопку 'Отправить' (если ранее не была отправлена команда exit 0).\n\nЕсли данные не считались с первого раза, повторите попытку заново!";
                                                    tbRtcN.IsReadOnly = true;
                                                    tbTextForConsole.Text = "rtc";
                                                    tbTextTitleRtc.Text = "Считанное время RTC";
                                                    btSendConfiguration.Visibility = Visibility.Hidden;
                                                    tbTextForConsole.IsReadOnly = true;
                                                    BBOX.Visibility = Visibility.Hidden;
                                                    RTC.Visibility = Visibility.Visible;
                                                    CNF.Visibility = Visibility.Hidden;
                                                    ADC.Visibility = Visibility.Hidden;
                                                    btReadConfiguration.Visibility = Visibility.Hidden;
                                                    tbMesBbox.Visibility = Visibility.Hidden;
                                                }
                                                else
                                                {
                                                    if (cbCommandsTerminal.Text == "Приведение результатов (adc)")
                                                    {
                                                        Thread.Sleep(1500);
                                                        _serialPort.WriteLine("adc" + "\r");
                                                        Thread.Sleep(1000);
                                                        tbTextForConsole.Text = "adc";
                                                        btSendConfiguration.Visibility = Visibility.Hidden;
                                                        tbTextForConsole.IsReadOnly = true;
                                                        tbMessage.Text = "Убедитесь, что вы прожали кнопку на плате и после этого прожали кнопку 'Отправить' (если ранее не была отправлена команда exit 0).\n\nЕсли данные не считались с первого раза, повторите попытку заново!";
                                                        ADC.Visibility = Visibility.Visible;
                                                        BBOX.Visibility = Visibility.Hidden;
                                                        RTC.Visibility = Visibility.Hidden;
                                                        CNF.Visibility = Visibility.Hidden;
                                                        btReadConfiguration.Visibility = Visibility.Hidden;
                                                        tbMesBbox.Visibility = Visibility.Hidden;
                                                    }
                                                    else
                                                    {
                                                        if (cbCommandsTerminal.Text == "Информация о ЧЯ (bbox)")
                                                        {
                                                            Thread.Sleep(1500);
                                                            _serialPort.WriteLine("bbox" + "\r");
                                                            Thread.Sleep(1000);
                                                            ADC.Visibility = Visibility.Hidden;
                                                            BBOX.Visibility = Visibility.Visible;
                                                            RTC.Visibility = Visibility.Hidden;
                                                            CNF.Visibility = Visibility.Hidden;
                                                            tbTextForConsole.Text = "bbox";
                                                            tbTextForConsole.IsReadOnly = true;
                                                            btSendConfiguration.Visibility = Visibility.Hidden;
                                                            btInitializeTheLog.Visibility = Visibility.Hidden;
                                                            ResettingTheLog.Visibility = Visibility.Hidden;
                                                            btReadConfiguration.Visibility = Visibility.Hidden;
                                                            spFirstPart.Visibility = Visibility.Hidden;
                                                            tbMessage.Text = "Убедитесь, что вы прожали кнопку на плате и после этого прожали кнопку 'Отправить' (если ранее не была отправлена команда exit 0).\n\nЕсли данные не считались с первого раза, повторите попытку заново!";
                                                            tbMesBbox.Visibility = Visibility.Hidden;
                                                        }
                                                        else
                                                        {
                                                            _serialPort.WriteLine(tbTextForConsole.Text + "\r");
                                                            tbTextForConsole.IsReadOnly = true;
                                                            btReadConfiguration.Visibility = Visibility.Hidden;

                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }

                                }
                            }

                        }
                    }
                    catch (Exception ex)
                    {
                        tbMessage.Text = $"Ошибка при отправке команды: {ex.Message}";
                    }
                }
            }
            catch { }
        }

        private void btClearTheConsole_Click(object sender, RoutedEventArgs e)//Очистить данные в консоле
        {
            tbConsole.Text = "";
            tbMessage.Text = "";
        }

        /// <summary>
        /// Uploading data from a TextBox to a text document ConfigurationParameters.txt
        /// Загрузка данных из TextBox в текстовый документ ConfigurationParameters.txt
        /// </summary>
        private void UploadingDataToTextFile()//Загрузка данные в текстовые файл
        {
            if (cbCommandsTerminal.Text == "Редактировать конфигурацию (cnf 1)")
            {
                if (Sensor.IsChecked == true & tbTout.Text != "" & tbAckRange.Text != "" & tbTimeout.Text != "" & tbFreq.Text != "" & tbHoldRssi.Text != "" & tbToutSec.Text != "")
                {
                    string[] textInCB =
                    {
                        "2",
                        tbTout.Text.ToString(),
                        tbAckRange.Text.ToString(),
                        tbTimeout.Text.ToString(),
                        tbFreq.Text.ToString(),
                        tbHoldRssi.Text.ToString(),
                        tbToutSec.Text.ToString()
                    };

                    try
                    {
                        SaveFileDialog save = new SaveFileDialog();
                        save.DefaultExt = ".txt";
                        save.Filter = "Text Files (.txt)|*.txt";
                        string baseDirectory = System.AppDomain.CurrentDomain.BaseDirectory;
                        save.InitialDirectory = baseDirectory;
                        if (save.ShowDialog() == true)
                        {
                            File.WriteAllLines(save.FileName, textInCB);
                        }
                        MessageBox.Show("Данные сохранены!");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при записи в файл: {ex.Message}");
                    }
                }
                else
                {
                    if (Copter.IsChecked == true & tbTout.Text != "" & tbFreq.Text != "")
                    {
                        string[] textInCB =
                        {
                        "1",
                        tbTout.Text.ToString(),
                        tbAckRange.Text.ToString(),
                        tbTimeout.Text.ToString(),
                        tbFreq.Text.ToString(),
                        tbHoldRssi.Text.ToString(),
                        tbToutSec.Text.ToString()
                    };

                        try
                        {
                            SaveFileDialog save = new SaveFileDialog();
                            save.DefaultExt = ".txt";
                            save.Filter = "Text Files (.txt)|*.txt";
                            string baseDirectory = System.AppDomain.CurrentDomain.BaseDirectory;
                            save.InitialDirectory = baseDirectory;
                            if (save.ShowDialog() == true)
                            {
                                File.WriteAllLines(save.FileName, textInCB);
                            }
                            MessageBox.Show("Данные сохранены!");
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Ошибка при записи в файл: {ex.Message}");
                        }
                    }
                    else
                    {
                        MessageBox.Show("Заполните все поля!");
                    }
                }
            }
            else
            {
                if (cbCommandsTerminal.Text == "Установить время (rtc 1)")
                {
                    if (tbRtcN.Text != "")
                    {
                        string[] textInCB =
                        {
                            tbRtcN.Text.ToString()
                        };

                        try
                        {
                            SaveFileDialog save = new SaveFileDialog();
                            save.DefaultExt = ".txt";
                            save.Filter = "Text Files (.txt)|*.txt";
                            string baseDirectory = System.AppDomain.CurrentDomain.BaseDirectory;
                            save.InitialDirectory = baseDirectory;
                            if (save.ShowDialog() == true)
                            {
                                File.WriteAllLines(save.FileName, textInCB);
                            }
                            MessageBox.Show("Данные сохранены!");
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Ошибка при записи в файл: {ex.Message}");
                        }
                    }
                    else
                    {
                        MessageBox.Show("Заполните все поля!");
                    }
                }
                else
                {
                    if (cbCommandsTerminal.Text == "Данные из ЧЯ (bbox N C)")
                    {
                        if (tbBboxN.Text != "" & tbBboxC.Text != "")
                        {
                            string[] textInCB =
                            {
                                tbRtcN.Text.ToString(),
                                tbBboxC.Text.ToString()
                            };

                            try
                            {
                                SaveFileDialog save = new SaveFileDialog();
                                save.DefaultExt = ".txt";
                                save.Filter = "Text Files (.txt)|*.txt";
                                string baseDirectory = System.AppDomain.CurrentDomain.BaseDirectory;
                                save.InitialDirectory = baseDirectory;
                                if (save.ShowDialog() == true)
                                {
                                    File.WriteAllLines(save.FileName, textInCB);
                                }
                                MessageBox.Show("Данные сохранены!");
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Ошибка при записи в файл: {ex.Message}");
                            }
                        }
                        else
                        {
                            MessageBox.Show("Заполните все поля!");
                        }
                    }
                    else
                    {
                        MessageBox.Show("Для сохранения необоходимо выбрать для какой команды заполняются данные!");
                    }
                }
            }
        }

        private void btSaveConfiguration_Click(object sender, RoutedEventArgs e)//Сохранить конфигурацию
        {
            UploadingDataToTextFile();
        }

        /// <summary>
        /// A method for outputting data from a text document ConfigurationParameters.txt
        /// Метод для вывода данных из текстового документа ConfigurationParameters.txt
        /// </summary>
        private void DataOutputFromATextDocument()//Вывод данных из текстового документа
        {
            try
            {
                OpenFileDialog open = new OpenFileDialog();
                open.Filter = "Text Files (.txt)|*.txt";
                string baseDirectory = System.AppDomain.CurrentDomain.BaseDirectory;
                open.InitialDirectory = baseDirectory;
                if (open.ShowDialog() == true)
                {
                    if (cbCommandsTerminal.Text == "Редактировать конфигурацию (cnf 1)")
                    {
                        string[] lines = File.ReadAllLines(open.FileName);//чтение всех строк из файла
                        string role = Convert.ToString(lines[0]);
                        if (role == "1")
                        {
                            Copter.IsChecked = true;
                            Sensor.IsChecked = false;
                        }
                        else
                        {
                            if (role == "2")
                            {
                                Sensor.IsChecked = true;
                                Copter.IsChecked = false;
                            }
                        }
                        tbTout.Text = Convert.ToString(lines[1]);
                        tbAckRange.Text = Convert.ToString(lines[2]);
                        tbTimeout.Text = Convert.ToString(lines[3]);
                        tbFreq.Text = Convert.ToString(lines[4]);
                        tbHoldRssi.Text = Convert.ToString(lines[5]);
                        tbToutSec.Text = Convert.ToString(lines[6]);
                    }
                    else
                    {
                        if (cbCommandsTerminal.Text == "Установить время (rtc 1)")
                        {
                            string[] lines = File.ReadAllLines(open.FileName);//чтение всех строк из файла
                            tbRtcN.Text = Convert.ToString(lines[0]);
                        }
                        else
                        {
                            if (cbCommandsTerminal.Text == "Данные из ЧЯ (bbox N C)")
                            {
                                string[] lines = File.ReadAllLines(open.FileName);//чтение всех строк из файла
                                tbRtcN.Text = Convert.ToString(lines[0]);
                                tbBboxC.Text = Convert.ToString(lines[1]);
                            }
                            else
                            {
                                MessageBox.Show("Перед открытием файла, необохдимо указать для какой команды присваиваются переменные!");
                            }
                        }

                    }
                }
            }
            catch
            {
                MessageBox.Show("Возможно в файле содержаться неподходящие данные!", "Ошибка");
                tbTout.Text = "";
                tbAckRange.Text = "";
                tbTimeout.Text = "";
                tbFreq.Text = "";
                tbHoldRssi.Text = "";
                tbToutSec.Text = "";
                tbRtcN.Text = "";
                tbRtcN.Text = "";
                tbBboxC.Text = "";
            }
        }

        private void btDownloadConiguration_Click(object sender, RoutedEventArgs e)//Загрузить конфигурацию
        {
            DataOutputFromATextDocument();
        }

        private void btSendConfiguration_Click(object sender, RoutedEventArgs e)//Отправить конфигурацию
        {
            try
            {
                tbMessage.Text = "";
                if (cbCommandsTerminal.Text == "Редактировать конфигурацию (cnf 1)")
                {
                    if (Sensor.IsChecked == true)
                    {
                        if (tbTout.Text != "" & tbAckRange.Text != "" & tbTimeout.Text != "" & tbFreq.Text != "" & tbHoldRssi.Text != "" & tbToutSec.Text != "")
                        {
                            _serialPort.WriteLine("cnf 1" + "\r");
                            List<string> variables = new List<string>();// Список для хранения всех переменных

                            // Добавление переменных в список
                            variables.Add("2");
                            variables.Add(tbTout.Text);
                            variables.Add(tbAckRange.Text);
                            variables.Add(tbTimeout.Text);
                            variables.Add(tbFreq.Text);
                            variables.Add(tbHoldRssi.Text);
                            variables.Add(tbToutSec.Text);

                            // Отправка каждой переменной через COM-порт
                            foreach (var variable in variables)
                            {
                                if (!string.IsNullOrEmpty(variable))
                                {
                                    _serialPort.WriteLine(variable + "\r");
                                    tbTextForConsole.IsReadOnly = false;
                                    btSendToConsole.IsEnabled = false;
                                    btSendConfiguration.IsEnabled = false;
                                    btReadConfiguration.Visibility = Visibility.Visible;
                                    btReadConfiguration.IsEnabled = false;

                                }
                            }
                            Sensor.IsChecked = false;
                            Copter.IsChecked = false;
                            tbTout.Text = "";
                            tbAckRange.Text = "";
                            tbTimeout.Text = "";
                            tbFreq.Text = "";
                            tbHoldRssi.Text = "";
                            tbToutSec.Text = "";
                        }
                        else
                        {
                            MessageBox.Show("Заполните все поля!");
                        }
                    }
                    else
                    {
                        if (Copter.IsChecked == true)
                        {
                            if (tbTout.Text != "" & tbFreq.Text != "")
                            {
                                _serialPort.WriteLine("cnf 1" + "\r");
                                List<string> variables = new List<string>();// Список для хранения всех переменных

                                // Добавление переменных в список
                                variables.Add("1");
                                variables.Add(tbTout.Text);
                                variables.Add("20");
                                variables.Add("15");
                                variables.Add(tbFreq.Text);
                                variables.Add("-110");
                                variables.Add("5");

                                // Отправка каждой переменной через COM-порт
                                foreach (var variable in variables)
                                {
                                    if (!string.IsNullOrEmpty(variable))
                                    {
                                        _serialPort.WriteLine(variable + "\r");
                                        tbTextForConsole.IsReadOnly = false;
                                        btSendToConsole.IsEnabled = false;
                                        btSendConfiguration.IsEnabled = false;
                                        btReadConfiguration.Visibility = Visibility.Visible;
                                        btReadConfiguration.IsEnabled = false;

                                    }
                                }
                                Copter.IsChecked = false;
                                Sensor.IsChecked = false;
                                tbTout.Text = "";
                                tbAckRange.Text = "";
                                tbTimeout.Text = "";
                                tbFreq.Text = "";
                                tbHoldRssi.Text = "";
                                tbToutSec.Text = "";
                            }
                            else
                            {
                                MessageBox.Show("Заполните все поля!");
                            }
                        }
                    }
                }
                else
                {
                    if (cbCommandsTerminal.Text == "Установить время (rtc 1)")
                    {
                        if (tbRtcN.Text != "")
                        {
                            int N = Convert.ToInt32(tbRtcN.Text);
                            _serialPort.WriteLine("rtc " + N + "\r");
                            tbTextForConsole.Text = "rtc " + N;
                            tbTextForConsole.IsReadOnly = false;
                            btReadConfiguration.Visibility = Visibility.Hidden;
                        }
                        else
                        {
                            MessageBox.Show("Заполните все поля!");
                        }
                    }
                    else
                    {
                        if (cbCommandsTerminal.Text == "Данные из ЧЯ (bbox N C)")
                        {
                            if (tbBboxN.Text != "" & tbBboxC.Text != "")
                            {
                                int N = Convert.ToInt32(tbBboxN.Text);
                                int C = Convert.ToInt32(tbBboxC.Text);
                                if (N >= 0 & C > 0)
                                {
                                    Thread.Sleep(1500);
                                    _serialPort.WriteLine("bbox " + N + " " + C + "\r");
                                    Thread.Sleep(1000);
                                    tbTextForConsole.Text = "bbox " + N + " " + C;
                                    tbTextForConsole.IsReadOnly = false;
                                    btReadConfiguration.Visibility = Visibility.Hidden;
                                }
                                else
                                {
                                    MessageBox.Show("Должно подходить под условие: N>=0 и C>0");
                                }

                            }
                            else
                            {
                                MessageBox.Show("Заполните все поля!");
                            }
                        }
                    }

                }
            }
            catch { }
        }

        private void btInitializeTheLog_Click(object sender, RoutedEventArgs e)//Инциализировать журнал для bbox
        {
            _serialPort.WriteLine("bbox -1" + "\r");
            tbMessage.Text = "Убедитесь, что вы прожали кнопку на плате и открылся терминал (если ранее не была отправлена команда exit 0)!\nЕсли ошиблись, прожмите кнопку на плате и снова нажмите 'Инициализировать'";
        }

        private void ResettingTheLog_Click(object sender, RoutedEventArgs e)//Сброс журнала для bbox
        {
            _serialPort.WriteLine("bbox -2" + "\r");
            tbMessage.Text = "Убедитесь, что вы прожали кнопку на плате и открылся терминал (если ранее не была отправлена команда exit 0)!\nЕсли ошиблись, прожмите кнопку на плате и снова нажмите 'Сброс журнала'";
        }

        private void btReadConfiguration_Click(object sender, RoutedEventArgs e)//Считать конфигурацию с устройства
        {
            ReadTheConfigurationFromTheDevice();
            tbTextForConsole.Text = "cnf";
        }

        private void ReadTheConfigurationFromTheDevice()//Считывание конфигурации с устройства
        {
            if (_serialPort.IsOpen)
            {
                try
                {
                    Thread.Sleep(1500);
                    _serialPort.WriteLine("cnf" + "\r");
                    Thread.Sleep(1000);
                    tbMessage.Text = "При отправке команды убедитесь, что вы нажали кнопку на плате (первая кнопка с конца), если ранее не была отправлена команда exit 0.\n\nЕсли данные не считались с первого раза, повторите попытку заново!";
                    ADC.Visibility = Visibility.Hidden;
                    BBOX.Visibility = Visibility.Hidden;
                    RTC.Visibility = Visibility.Hidden;
                    CNF.Visibility = Visibility.Visible;
                }
                catch (Exception ex)
                {
                    tbMessage.Text = $"Ошибка при чтении данных: {ex.Message}";
                }
            }
        }

        private void BtConnectionParameters_Click(object sender, RoutedEventArgs e)//Параметры подключения: Открытие окна Настройка COM 
        {
            CheckFileParameters();
            COMSettingsWindows COMSettings = new COMSettingsWindows();
            COMSettings.ShowDialog();
        }

        private void tbConsole_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Получаем текущую позицию прокрутки
            double currentScrollPosition = tbConsole.VerticalOffset;
            double scrollableHeight = tbConsole.ExtentHeight - tbConsole.ViewportHeight;

            // Проверяем, находится ли позиция прокрутки в самом нижнем положении
            if (currentScrollPosition == scrollableHeight)
            {
                tbConsole.ScrollToEnd();
            }
        }

        private void Copter_Checked(object sender, RoutedEventArgs e)//Если выбран Коптер
        {
            tbAckRange.IsEnabled = false;
            tbAckRange.Text = "-";
            tbTimeout.IsEnabled = false;
            tbTimeout.Text = "-";
            tbHoldRssi.IsEnabled = false;
            tbHoldRssi.Text = "-";
            tbToutSec.IsEnabled = false;
            tbToutSec.Text = "-";
            Sensor.IsChecked = false;
        }

        private void Sensor_Checked(object sender, RoutedEventArgs e)//Если выбран Датчик
        {
            tbAckRange.IsEnabled = true;
            tbTimeout.IsEnabled = true;
            tbHoldRssi.IsEnabled = true;
            tbToutSec.IsEnabled = true;
            tbAckRange.Text = "";
            tbTimeout.Text = "";
            tbHoldRssi.Text = "";
            tbToutSec.Text = "";
            Copter.IsChecked = false;
        }

        private void menuHelp_Click(object sender, RoutedEventArgs e)//кнопка "Помощь" в меню
        {
            HelpWindow helpWindow = new HelpWindow();
            helpWindow.ShowDialog();
        }

        private void menuAboutTheProgram_Click(object sender, RoutedEventArgs e)//кнопка "О программе" в меню
        {
            AboutTheProgram aboutTheProgram = new AboutTheProgram();
            aboutTheProgram.ShowDialog();
        }
    }
}
