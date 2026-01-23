using DataView2.Engines;
using DataView2.States;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DataView2.ViewModels
{
    public partial class SegmentationTableViewModel : INotifyPropertyChanged
    {
        private readonly ApplicationEngine _appEngine;
        private readonly ApplicationState _appState;

        public SegmentationTableViewModel(ApplicationEngine appEngine, ApplicationState appState)
        {
            _appEngine = appEngine;
            _appState = appState;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


    }
}
