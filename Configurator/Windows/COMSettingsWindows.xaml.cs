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
using System.Windows.Shapes;
using System.IO;
using System.IO.Ports;
using System.Threading;

namespace Configurator.Windows
{
    /// <summary>
    /// Логика взаимодействия для COMSettingsWindows.xaml
    /// </summary>
    public partial class COMSettingsWindows : Window
    {
        string filePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "COMSettingsParameters.txt");//файл для сохранения
        public COMSettingsWindows()
        {
            InitializeComponent();
            cbCOMPort.ItemsSource = SerialPort.GetPortNames();
            DataOutputFromATextDocument();
        }

        /// <summary>
        /// A method for outputting data from a text document COMSettingsParameters.txt
        /// Метод для вывода данных из текстового документа COMSettingsParameters.txt
        /// </summary>
        private void DataOutputFromATextDocument()//Вывод данных из текстового документа
        {
            string[] lines = File.ReadAllLines(filePath);//чтение всех строк из файла
            foreach (var items in SerialPort.GetPortNames())
            {
                //var cmbItem = items as ComboBoxItem;
                if (lines.Contains(items))
                {
                    cbCOMPort.SelectedItem = items;
                    break;
                }
            }

            foreach (var items in cbSpeed.Items)
            {
                var cmbItem = items as ComboBoxItem;
                if (cmbItem != null)
                {
                    string txt = cmbItem.Content.ToString();
                    if (lines.Contains(txt))
                    {
                        cbSpeed.SelectedItem = cmbItem;
                    }
                }
            }


            foreach (var items in cbDataBits.Items)
            {
                var cmbItem = items as ComboBoxItem;
                if (cmbItem != null)
                {
                    string txt = cmbItem.Content.ToString();
                    if (lines.Contains(txt))
                    {
                        cbDataBits.SelectedItem = cmbItem;
                    }
                }
            }


            foreach (var items in cbStopBits.Items)
            {
                var cmbItem = items as ComboBoxItem;
                if (cmbItem != null)
                {
                    string txt = cmbItem.Content.ToString();
                    if (lines.Contains(txt))
                    {
                        cbStopBits.SelectedItem = cmbItem;
                    }
                }
            }


            foreach (var items in cbTheParity.Items)
            {
                var cmbItem = items as ComboBoxItem;
                if (cmbItem != null)
                {
                    string txt = cmbItem.Content.ToString();
                    if (lines.Contains(txt))
                    {
                        cbTheParity.SelectedItem = cmbItem;
                    }
                }
            }
        }

        /// <summary>
        /// Uploading data from a ComboBox to a text document COMSettingsParameters.txt
        /// Загрузка данных из ComboBox в текстовый документ COMSettingsParameters.txt
        /// </summary>
        private void UploadingDataToTextFile()//Загрузка данные в текстовые файл
        {
            if (cbCOMPort.SelectedItem != null & cbSpeed.SelectedItem != null & cbDataBits.SelectedItem != null & cbStopBits.SelectedItem != null & cbTheParity.SelectedItem != null)
            {
                string[] textInCB =
                {
                cbCOMPort.Text.ToString(),
                cbSpeed.Text.ToString(),
                cbDataBits.Text.ToString(),
                cbStopBits.Text.ToString(),
                cbTheParity.Text.ToString()
            };

                if (!File.Exists(filePath))
                {
                    using (File.Create(filePath)) { }
                }
                try
                {
                    File.WriteAllLines(filePath, textInCB);
                    MessageBox.Show("Данные сохранены!");
                    COMSettingsWindows COMSettings = new COMSettingsWindows();
                    COMSettings.Close();
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

        private void btSave_Click(object sender, RoutedEventArgs e)//Кнопка сохранить
        {
            UploadingDataToTextFile();
        }

        private void btUpdatingPorts_Click(object sender, RoutedEventArgs e)//Обновление списка портов
        {
            cbCOMPort.ItemsSource = SerialPort.GetPortNames();
        }
    }
}
