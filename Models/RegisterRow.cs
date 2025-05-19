using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mbclientava.Models
{
    public class RegisterRow : INotifyPropertyChanged
    {



        public event PropertyChangedEventHandler? PropertyChanged;

        public RegisterRow(string name, ushort address, int value, bool writable)
        {
            Name = name;
            Address = address;
            Value = value;
            Writable = writable;


        }

        private int _address;
        public int Address
        {
            get => _address;
            set { _address = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Address))); }
        }

        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set { _name = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name))); }
        }

        private int _value;
        public int Value
        {
            get => _value;
            set
            {
                if (_value == value) return;
                _value = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));


            }
        }

        private bool _writable;
        public bool Writable
        {
            get => _writable;
            set { _writable = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Writable))); }
        }

    }


}
