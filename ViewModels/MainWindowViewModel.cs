using Avalonia.Media;
using NModbus;
using NModbus.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Input;
using mbclientava.Models;
using mbclientava.Commands;


namespace mbclientava.ViewModels
{

    public class MainWindowViewModel : INotifyPropertyChanged
    {

        public string AppTitle { get; } = "Energy Meter";

        public readonly ISlaveDataStore _dataStore = new SlaveDataStore();

        //private string slaveID;
        //public string slava
        //{
        //    get => slaveID;
        //    set
        //    {
        //        if (slaveID == value) return;

        //        slaveID = value;
        //        OnChanged(nameof(slava));
        //    }
        //}

        private ObservableCollection<RegisterRow> registers;
        public ObservableCollection<RegisterRow> Registers { get => registers; set { registers = value; OnChanged(nameof(Registers)); } }
        public ObservableCollection<string> Logs { get; } = [];

        private string _status = "Stopped";
        public string Status
        {
            get => _status;
            set { _status = value; OnChanged(nameof(Status)); }
        }

        private string _statusButton = "Listen";
        public string StatusButton
        {
            get => _statusButton;
            set { _statusButton = value; OnChanged(nameof(StatusButton)); }
        }

        private IBrush _statusBrush = Brushes.Gray;
        public IBrush StatusBrush
        {
            get => _statusBrush;
            set { _statusBrush = value; OnChanged(nameof(StatusBrush)); }
        }

        private string _port = "502";
        public string Port
        {
            get => _port;
            set
            {
                if (_port == value) return;
                _port = value;
                OnChanged(nameof(Port));
            }
        }

        public ICommand ToggleCommand { get; }
        public ICommand ClearErrorsCommand { get; }
        public ICommand ClearGridCommand { get; }

        private TcpListener? _listener;
        private readonly ModbusFactory _factory = new();
        private IModbusSlaveNetwork? _network;
        private IModbusSlave? _slave;
        private CancellationTokenSource? _cts;

        public MainWindowViewModel()
        {
            LoadDefaultRegisters();


            ToggleCommand = new RelayCommand(ToggleServer);
            ClearErrorsCommand = new RelayCommand(() => Logs.Clear());
            ClearGridCommand = new RelayCommand(() => Registers?.Clear());
        }

        private void LoadDefaultRegisters()
        {
            var regss = new List<RegisterRow>
            {
                new("Voltage", 1, 2300, false),
                new("Current", 2,105, false),
                new("Frequency", 3, 5000, false),
                new( "Energy",4,1234, false),
                new("Mode", 5, 3333, true)
            };

            Registers = new ObservableCollection<RegisterRow>(regss);
        }

        private bool IsRunning => _listener != null;

        private void ToggleServer()
        {
            if (IsRunning) StopServer(); else StartServer();
        }

        private void StartServer()
        {
            try
            {
                if (Registers.Count == 0)
                {
                    LoadDefaultRegisters();
                    Logs.Add("Default registers loaded");
                }
                int port = int.TryParse(Port, out var p) ? p : throw new Exception("Invalid port");

                _listener = new TcpListener(IPAddress.Any, port);
                _listener.Start();
                Logs.Add($"Started listening on port {port}");


                _slave = _factory.CreateSlave(2, _dataStore);

                foreach (var reg in Registers)
                {
                    reg.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(RegisterRow.Value) && reg.Writable)
                        {
                            try
                            {
                                _slave?.DataStore?.HoldingRegisters?.WritePoints((ushort)reg.Address, new ushort[] { (ushort)reg.Value });
                            }
                            catch (Exception ex)
                            {
                                Logs.Add($"Write error for {reg.Name}: {ex.Message}");
                            }
                        }
                    };
                }


                foreach (var r in Registers)
                {
                    _slave.DataStore.HoldingRegisters.WritePoints((ushort)r.Address, new ushort[] { (ushort)r.Value });
                }

                _network = _factory.CreateSlaveNetwork(_listener);
                _network.AddSlave(_slave);
                _cts = new CancellationTokenSource();
                _ = _network.ListenAsync(_cts.Token);

                Status = "Running";
                StatusButton = "Stop";
                StatusBrush = Brushes.Green;
            }
            catch (Exception ex)
            {
                Logs.Add(ex.Message);
                Status = "Error";
                StatusButton = "Listen";
                StatusBrush = Brushes.Red;
            }
        }

        private void StopServer()
        {
            try
            {
                _cts?.Cancel();
                _listener?.Stop();
                _listener = null;
                _network = null;
                Status = "Stopped";
                StatusButton = "Listen";
                StatusBrush = Brushes.Gray;
            }
            catch (Exception ex)
            {
                Logs.Add(ex.Message);
                Status = "Error";
                StatusBrush = Brushes.Red;
            }
        }

        private void OnChanged(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
        public event PropertyChangedEventHandler? PropertyChanged;
    }
   

}