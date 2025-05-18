using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using NModbus;


namespace mbclientava.Views
{
    public partial class MainWindow : Window
    {
        private TcpClient? client;
        private IModbusMaster? master;
        private Thread? listenThread;
        private volatile bool isListening = false;

        private bool _paneVisible = true;
       

        private ObservableCollection<string> chatMessages = new();
        private ObservableCollection<string> errorMessages = new();

        public MainWindow()
        {
            InitializeComponent();
            ChatBox.ItemsSource = chatMessages;
            ErrorBox.ItemsSource = errorMessages;
            AttachHandlers();
           
        }

        private void ToggleMenuButton_Click(object? sender, RoutedEventArgs e)
        {
            _paneVisible = !_paneVisible;          // инвертируем флаг
            LeftPanel.IsVisible = _paneVisible;    // прячем / показываем
            ToggleMenuButton.Content = _paneVisible ? "<" : "☰";
        }

        private void AttachHandlers()
        {
            ToggleMenuButton.Click += ToggleMenuButton_Click;
            ConnectButton.Click += ToggleConnection;
            ClearLogs.Click += (_, _) => errorMessages.Clear();
            ClearChat.Click += (_, _) => chatMessages.Clear();
            SendButton.Click += (_, _) => SendMessage();
            MessageInput.KeyDown += (s, e) => {
                if (e.Key == Avalonia.Input.Key.Enter)
                {
                    e.Handled = true;
                    SendMessage();
                }
            };
            IpInput.KeyDown += (s, e) => {
                if (e.Key == Avalonia.Input.Key.Enter && !isListening)
                {
                    e.Handled = true;
                    ToggleConnection(null, null);
                }
            };
            PortInput.KeyDown += (s, e) => {
                if (e.Key == Avalonia.Input.Key.Enter && !isListening)
                {
                    e.Handled = true;
                    ToggleConnection(null, null);
                }
            };
        }

        private void ToggleConnection(object? sender, RoutedEventArgs e)
        {
            if (client != null && client.Connected)
            {
                isListening = false;
                Thread.Sleep(200);
                client.Close();
                client = null;
                master = null;
                StatusLabel.Text = "Disconnected";
                StatusLabel.Foreground = Avalonia.Media.Brushes.Gray;
                ConnectButton.Content = "Connect";
                return;
            }

            try
            {
                client = new TcpClient(IpInput.Text, int.Parse(PortInput.Text));
                master = new ModbusFactory().CreateMaster(client);
                isListening = true;
                StatusLabel.Text = "Connected";
                StatusLabel.Foreground = Avalonia.Media.Brushes.LimeGreen;
                ConnectButton.Content = "Disconnect";

                listenThread = new Thread(() => ListenLoop(master));
                listenThread.IsBackground = true;
                listenThread.Start();
            }
            catch (Exception ex)
            {
                StatusLabel.Text = "Error";
                StatusLabel.Foreground = Avalonia.Media.Brushes.Red;
                errorMessages.Add(ex.Message);
            }
        }

        private void SendMessage()
        {
            try
            {
                if (master == null) return;
                string msg = MessageInput.Text ?? throw new Exception("Message is null...");
                ushort[] data = Encode(msg);
                master.WriteMultipleRegisters(1, 0, data);
                master.WriteSingleRegister(1, 100, 1);
                chatMessages.Add("[" + DateTime.Now.ToLongTimeString() + " - Client]: " + msg);
                MessageInput.Text = string.Empty;
                Thread.Sleep(100);
                master.WriteSingleRegister(1, 100, 0);
            }
            catch (Exception ex)
            {
                errorMessages.Add(ex.Message);
            }
        }

        private void ListenLoop(IModbusMaster master)
        {
            while (isListening)
            {
                try
                {
                    ushort[] flag = master.ReadHoldingRegisters(1, 300, 1);
                    if (flag[0] == 1)
                    {
                        ushort[] buf = master.ReadHoldingRegisters(1, 200, 50);
                        string msg = Decode(buf);
                        if (!string.IsNullOrWhiteSpace(msg))
                        {
                            Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                chatMessages.Add("[" + DateTime.Now.ToLongTimeString() + " - Server]: " + msg);
                            });
                            master.WriteMultipleRegisters(1, 200, new ushort[50]);
                            master.WriteSingleRegister(1, 300, 0);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        errorMessages.Add("ERR: " + ex.Message);
                    });
                }
                Thread.Sleep(100);
            }
        }

        private static ushort[] Encode(string message)
        {
            var bytes = Encoding.ASCII.GetBytes(message);
            int len = (bytes.Length + 1) / 2;
            var result = new ushort[len];
            for (int i = 0; i < bytes.Length; i++)
            {
                int index = i / 2;
                if (i % 2 == 0)
                    result[index] = bytes[i];
                else
                    result[index] |= (ushort)(bytes[i] << 8);
            }
            return result;
        }

        private static string Decode(ushort[] data)
        {
            byte[] bytes = new byte[data.Length * 2];
            for (int i = 0; i < data.Length; i++)
            {
                bytes[i * 2] = (byte)(data[i] & 0xFF);
                bytes[i * 2 + 1] = (byte)(data[i] >> 8);
            }
            return Encoding.ASCII.GetString(bytes).TrimEnd('\0');
        }


    }
}