using App4.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace App4
{
    public sealed partial class Manuel_Page : Page
    {
        private DispatcherTimer _refreshTimer;
        private DispatcherTimer _proDebounceTimer;
        private DispatcherTimer _jogDebounceTimer;
        private bool _suppressSliderEvents = false;

        public ObservableCollection<SignalItemViewModel> SignalList { get; set; } = new ObservableCollection<SignalItemViewModel>();

        public Manuel_Page()
        {
            this.InitializeComponent();

            _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _refreshTimer.Tick += RefreshTimer_Tick;

            _proDebounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
            _proDebounceTimer.Tick += ProDebounceTimer_Tick;

            _jogDebounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
            _jogDebounceTimer.Tick += JogDebounceTimer_Tick;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            _refreshTimer.Start();
            BuildValueDisplayMap();
            SignalListView.ItemsSource = SignalList;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            _refreshTimer.Stop();
        }

        private KukaRobotInstance GetRobot()
        {
            if (KukaRobotManager.Instance?.Robots != null && KukaRobotManager.Instance.Robots.Count > 0)
                return KukaRobotManager.Instance.Robots[0];
            return null;
        }

        private void RefreshTimer_Tick(object sender, object e)
        {
            BuildValueDisplayMap();

            var robot = GetRobot();
            if (robot != null && robot.IsConnected)
            {
                if (!_proDebounceTimer.IsEnabled && Math.Abs(SliderOverridePro.Value - robot.OverridePro) > 1)
                {
                    _suppressSliderEvents = true;
                    SliderOverridePro.Value = robot.OverridePro;
                    OverrideProValueText.Text = $"{robot.OverridePro}%";
                    _suppressSliderEvents = false;
                }

                if (!_jogDebounceTimer.IsEnabled && Math.Abs(SliderOverrideJog.Value - robot.OverrideJog) > 1)
                {
                    _suppressSliderEvents = true;
                    SliderOverrideJog.Value = robot.OverrideJog;
                    OverrideJogValueText.Text = $"{robot.OverrideJog}%";
                    _suppressSliderEvents = false;
                }
            }

            var gInOut = GlobalData.RobotInputVars;

            string GetVar(string varName) => gInOut.FirstOrDefault(v => v.Name == varName)?.Value ?? "0.0";
            bool IsTrue(string varName) => gInOut.FirstOrDefault(v => v.Name == varName)?.Value?.ToUpper() == "TRUE" || GetVar(varName) == "1";

            TxtBoruX.Text = GetVar("G_GOCATOR_X");
            TxtBoruY.Text = GetVar("G_GOCATOR_Y");
            TxtBoruZ.Text = GetVar("G_GOCATOR_Z");
            TxtBoruA.Text = GetVar("G_GOCATOR_A");
            TxtBoruB.Text = GetVar("G_GOCATOR_B");
            TxtBoruC.Text = GetVar("G_GOCATOR_C");

            StatusBoruTara.Text = "TARA: " + (IsTrue("G_TARA") ? "Açık" : "Kapalı");
            StatusBoruTamam.Text = "TAMAM: " + (IsTrue("G_GOCATOR_TAMAM") ? "Açık" : "Kapalı");
            StatusBoruHazir.Text = "HAZIR: " + (IsTrue("G_OFFSET_HAZIR") ? "Açık" : "Kapalı");

            TxtInficonDeger.Text = GetVar("L_INFICON_DEGER");
            StatusInficonTamam.Text = "TAMAM: " + (IsTrue("G_INFICON_TAMAM") ? "Açık" : "Kapalı");
            StatusInficonOk.Text = "OK (Sonuç): " + (IsTrue("G_INFICON_OK") ? "Başarılı" : "Bekleniyor");

            TxtTablaX.Text = GetVar("T_OFFSET_X");
            TxtTablaY.Text = GetVar("T_OFFSET_Y");
            TxtTablaZ.Text = GetVar("T_OFFSET_Z");
            TxtTablaA.Text = GetVar("T_OFFSET_A");
            TxtTablaB.Text = GetVar("T_OFFSET_B");
            TxtTablaC.Text = GetVar("T_OFFSET_C");

            StatusTablaTara.Text = "TARA: " + (IsTrue("T_TABLA_TARA") ? "Açık" : "Kapalı");
            StatusTablaTamam.Text = "TAMAM: " + (IsTrue("G_TABLA_TAMAM") ? "Açık" : "Kapalı");
        }

        private async void BtnBaslat_Click(object sender, RoutedEventArgs e)
        {
            var robot = GetRobot();
            if (robot != null && robot.IsConnected)
            {
                await WriteGlobalRobotOutVarAsync("G_BASLAT", "TRUE");
                await Task.Delay(500);
                await WriteGlobalRobotOutVarAsync("G_BASLAT", "FALSE");
                try { await robot.StartProgramAsync(); } catch { }
            }
        }

        private async void BtnDur_Click(object sender, RoutedEventArgs e)
        {
            await WriteGlobalRobotOutVarAsync("G_DUR", "TRUE");
            await Task.Delay(500);
            await WriteGlobalRobotOutVarAsync("G_DUR", "FALSE");
            
            var robot = GetRobot();
            if (robot != null && robot.IsConnected)
            {
                try { await robot.StopProgramAsync(); } catch { }
            }
        }

        private async void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            var robot = GetRobot();
            if (robot != null && robot.IsConnected)
            {
                await WriteGlobalRobotOutVarAsync("G_RESET", "TRUE");
                await Task.Delay(500);
                await WriteGlobalRobotOutVarAsync("G_RESET", "FALSE");
                try { await robot.ResetErrorAsync(); } catch { }
            }
        }

        private async void BtnHome_Click(object sender, RoutedEventArgs e)
        {
            var robot = GetRobot();
            if (robot != null && robot.IsConnected)
            {
                try { await robot.GoHomeAsync(); } catch { }
            }
        }

        private async void ComboKlimaTipi_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboKlimaTipi.SelectedIndex >= 0)
            {
                int tipi = ComboKlimaTipi.SelectedIndex + 1;
                await WriteGlobalRobotOutVarAsync("G_KLIMA_TIP", tipi.ToString());
            }
        }

        private void SliderOverridePro_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (_suppressSliderEvents || _proDebounceTimer == null) return;
            int val = (int)e.NewValue;
            OverrideProValueText.Text = $"{val}%";
            _proDebounceTimer.Stop();
            _proDebounceTimer.Start();
        }

        private void SliderOverrideJog_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (_suppressSliderEvents || _jogDebounceTimer == null) return;
            int val = (int)e.NewValue;
            OverrideJogValueText.Text = $"{val}%";
            _jogDebounceTimer.Stop();
            _jogDebounceTimer.Start();
        }

        private async void ProDebounceTimer_Tick(object sender, object e)
        {
            _proDebounceTimer.Stop();
            int val = (int)SliderOverridePro.Value;

            var robots = KukaRobotManager.Instance?.Robots;
            if (robots != null)
            {
                foreach (var r in robots)
                {
                    if (r.IsConnected)
                        try { await r.SetOverrideProAsync(val); } catch { }
                }
            }
        }

        private async void JogDebounceTimer_Tick(object sender, object e)
        {
            _jogDebounceTimer.Stop();
            int val = (int)SliderOverrideJog.Value;

            var robots = KukaRobotManager.Instance?.Robots;
            if (robots != null)
            {
                foreach (var r in robots)
                {
                    if (r.IsConnected)
                        try { await r.SetOverrideJogAsync(val); } catch { }
                }
            }
        }

        private async void BtnOffsetHazir_Click(object sender, RoutedEventArgs e)
        {
            await WriteGlobalRobotOutVarAsync("G_OFFSET_HAZIR", "TRUE");
            await Task.Delay(500);
            await WriteGlobalRobotOutVarAsync("G_OFFSET_HAZIR", "FALSE");
        }

        private async void BtnGocatorTamam_Click(object sender, RoutedEventArgs e)
        {
            await WriteGlobalRobotOutVarAsync("G_GOCATOR_TAMAM", "TRUE");
            await Task.Delay(500);
            await WriteGlobalRobotOutVarAsync("G_GOCATOR_TAMAM", "FALSE");
        }

        private async void BtnInficonTamam_Click(object sender, RoutedEventArgs e)
        {
            await WriteGlobalRobotOutVarAsync("G_INFICON_TAMAM", "TRUE");
            await Task.Delay(500);
            await WriteGlobalRobotOutVarAsync("G_INFICON_TAMAM", "FALSE");
        }

        private async void BtnTablaTamam_Click(object sender, RoutedEventArgs e)
        {
            await WriteGlobalRobotOutVarAsync("G_TABLA_TAMAM", "TRUE");
            await Task.Delay(500);
            await WriteGlobalRobotOutVarAsync("G_TABLA_TAMAM", "FALSE");
        }

        public async Task WriteGlobalRobotOutVarAsync(string outVarName, string valueToWrite)
        {
            var gVar = GlobalData.RobotOutputVars.FirstOrDefault(v => v.Name == outVarName);
            if (gVar != null)
            {
                gVar.Value = valueToWrite;
            }

            var plcVar = PlcService.Instance.OutputVariables.FirstOrDefault(v => v.Name == outVarName);
            if (plcVar != null)
            {
                plcVar.Value = valueToWrite;
                await PlcService.Instance.WriteAsync(plcVar, valueToWrite);
            }

            var robot = GetRobot();
            if (robot != null && robot.IsConnected)
            {
                await KukaService.Instance.WriteVariableAsync(outVarName, valueToWrite);
            }
        }

        private void BuildValueDisplayMap()
        {
            try
            {
                var plcInputVars = PlcService.Instance.InputVariables;
                var plcOutputVars = PlcService.Instance.OutputVariables;

                // 1. Mevcut listeyi koruyup sadece değerleri değiştireceğiz (UI titremesini önlemek için)
                if (SignalList.Count == 0)
                {
                    // İlk yükleme - Listeyi doldur
                    var mergedVars = new List<SignalItemViewModel>();
                    foreach(var v in GlobalData.RobotInputVars)
                    {
                        var pVar = plcInputVars.FirstOrDefault(x => x.Name == v.Name);
                        mergedVars.Add(new SignalItemViewModel
                        {
                            VarName = v.Name,
                            PlcTagName = pVar?.PlcTag ?? "-",
                            PlcType = pVar?.Type ?? "-",
                            ValueStr = v.Value,
                            IsOutput = false
                        });
                    }
                    foreach(var v in GlobalData.RobotOutputVars)
                    {
                        var pVar = plcOutputVars.FirstOrDefault(x => x.Name == v.Name);
                        mergedVars.Add(new SignalItemViewModel
                        {
                            VarName = v.Name,
                            PlcTagName = pVar?.PlcTag ?? "-",
                            PlcType = pVar?.Type ?? "-",
                            ValueStr = v.Value,
                            IsOutput = true
                        });
                    }

                    foreach (var item in mergedVars.OrderBy(v => v.VarName))
                    {
                        SignalList.Add(item);
                    }
                }
                else
                {
                    // Zaten dolu - Sadece güncel değerleri yerleştir
                    foreach(var item in SignalList)
                    {
                        if (item.IsOutput)
                        {
                            var gVar = GlobalData.RobotOutputVars.FirstOrDefault(v => v.Name == item.VarName);
                            if (gVar != null && item.ValueStr != gVar.Value) 
                            {
                                item.ValueStr = gVar.Value;
                                // INotifyPropertyChanged yok, bu yüzden ufak bir trick yapabiliriz veya
                                // viewmodel'e ekleyebiliriz ama şimdilik performansı etkilememesi için
                                // item'in objesini yenilemek en garantilisi. Fakat ObservableCollection
                                // olduğu için objeyi değiştirince yine titrer.
                                // En iyisi SignalItemViewModel'e INotifyPropertyChanged eklemek.
                            }
                        }
                        else
                        {
                            var gVar = GlobalData.RobotInputVars.FirstOrDefault(v => v.Name == item.VarName);
                            if (gVar != null && item.ValueStr != gVar.Value) 
                            {
                                item.ValueStr = gVar.Value;
                            }
                        }
                    }
                }
            }
            catch { }
        }
    }

    public class SignalItemViewModel : System.ComponentModel.INotifyPropertyChanged
    {
        public string VarName { get; set; }
        public string PlcTagName { get; set; }
        public string PlcType { get; set; }
        
        private string _valueStr;
        public string ValueStr 
        { 
            get => _valueStr; 
            set 
            { 
                if (_valueStr != value) 
                { 
                    _valueStr = value; 
                    PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(ValueStr))); 
                } 
            } 
        }
        
        public bool IsOutput { get; set; }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
    }
}
