using GoPxLSdk;
using GoPxLSdk.GoGdpMsg;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.Storage.Streams;
using static GoPxLSdkSamplesCommon.Utilities;
using System.Collections.ObjectModel;
using Windows.UI;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Dispatching;
using System.IO.Compression; // Zip dosyası yapmak için şart
using Windows.Storage.Pickers; // Kayıt penceresi için
using App4.Utilities; // <-- BU SATIRI MUTLAKA EKLEYİN


namespace App4.PAGES
{
    

    

    public sealed partial class Camera_Page : Page, INotifyPropertyChanged
    {
        public string LivePlcRfid => App4.Utilities.GlobalData.Auto_RfidTag;
        public string LivePlcIndex => App4.Utilities.GlobalData.Auto_IndexTag;
        public string LiveProcessStatus => App4.Utilities.GlobalData.ProcessStatus;
        public bool IsProcessRunning => App4.Utilities.GlobalData.IsProcessRunning;

        // ▼▼▼ OTOMASYON MODU (Otomatik vs Manuel) ▼▼▼
        private bool _isAutomationMode = true; // Varsayılan: Otomatik
        public bool IsAutomationMode
        {
            get => _isAutomationMode;
            set
            {
                if (_isAutomationMode != value)
                {
                    _isAutomationMode = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsManualMode));
                }
            }
        }

        public bool IsManualMode
        {
            get => !_isAutomationMode;
            set => IsAutomationMode = !value;
        }

        // ▼▼▼ MANUEL MOD DEĞERLERİ ▼▼▼
        private string _manualRfidValue = "";
        public string ManualRfidValue
        {
            get => _manualRfidValue;
            set
            {
                if (_manualRfidValue != value)
                {
                    _manualRfidValue = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _manualIndexValue = "";
        public string ManualIndexValue
        {
            get => _manualIndexValue;
            set
            {
                if (_manualIndexValue != value)
                {
                    _manualIndexValue = value;
                    OnPropertyChanged();
                }
            }
        }

        public string SelectedRfidTag
        {
            get => App4.Utilities.GlobalData.Auto_RfidTag;
            set
            {
                if (App4.Utilities.GlobalData.Auto_RfidTag != value)
                {
                    App4.Utilities.GlobalData.Auto_RfidTag = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(RfidTagValue));
                    UpdateRfidWatcher();
                }
            }
        }
        public string SelectedIndexTag
        {
            get => App4.Utilities.GlobalData.Auto_IndexTag;
            set
            {
                if (App4.Utilities.GlobalData.Auto_IndexTag != value)
                {
                    App4.Utilities.GlobalData.Auto_IndexTag = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IndexTagValue));
                    UpdateIndexWatcher();
                }
            }
        }
        public string SelectedTriggerTag
        {
            get => App4.Utilities.GlobalData.Auto_TriggerTag;
            set
            {
                if (App4.Utilities.GlobalData.Auto_TriggerTag != value)
                {
                    App4.Utilities.GlobalData.Auto_TriggerTag = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TriggerTagValue));
                    UpdateTriggerWatcher();
                }
            }
        }

        public string SelectedTriggerTag2
        {
            get => App4.Utilities.GlobalData.Auto_TriggerTag2;
            set
            {
                if (App4.Utilities.GlobalData.Auto_TriggerTag2 != value)
                {
                    App4.Utilities.GlobalData.Auto_TriggerTag2 = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TriggerTag2Value));
                    UpdateTrigger2Watcher();
                }
            }
        }
        public string SelectedMeasurementOutputTag
        {
            get => App4.Utilities.GlobalData.MeasurementOutputTag;
            set
            {
                if (App4.Utilities.GlobalData.MeasurementOutputTag != value)
                {
                    App4.Utilities.GlobalData.MeasurementOutputTag = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(MeasurementOutputValue));
                    OnPropertyChanged(nameof(MeasurementOutputStatusText));
                    OnPropertyChanged(nameof(MeasurementOutputStatusColor));
                    UpdateOutputWatcher();
                }
            }
        }

        // ▼▼▼ TABLA OUTPUT TAG ▼▼▼
        public string SelectedTablaOutputTag
        {
            get => App4.Utilities.GlobalData.TablaOutputTag;
            set
            {
                if (App4.Utilities.GlobalData.TablaOutputTag != value)
                {
                    App4.Utilities.GlobalData.TablaOutputTag = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TablaOutputValue));
                    OnPropertyChanged(nameof(TablaOutputStatusText));
                    OnPropertyChanged(nameof(TablaOutputStatusColor));
                    UpdateTablaOutputWatcher();
                }
            }
        }

        // ▼▼▼ TAG VALUE GÖSTERME (PLC'DEN OKUNAN DEĞERLER) ▼▼▼
        public string RfidTagValue => GetTagValue(App4.Utilities.GlobalData.Auto_RfidTag);
        public string IndexTagValue => GetTagValue(App4.Utilities.GlobalData.Auto_IndexTag);
        public string TriggerTagValue => GetTagValue(App4.Utilities.GlobalData.Auto_TriggerTag);
        public string TriggerTag2Value => GetTagValue(App4.Utilities.GlobalData.Auto_TriggerTag2);

        // Boru Output Value
        public string MeasurementOutputValue => GetTagValue(App4.Utilities.GlobalData.MeasurementOutputTag);

        public string MeasurementOutputStatusText
        {
            get
            {
                var val = MeasurementOutputValue;
                if (val == "1" || val.ToLower() == "true") return "HAZIR (1)";
                return "BEKLİYOR (0)";
            }
        }

        public SolidColorBrush MeasurementOutputStatusColor
        {
            get
            {
                var val = MeasurementOutputValue;
                if (val == "1" || val.ToLower() == "true") return new SolidColorBrush(Microsoft.UI.Colors.LimeGreen);
                return new SolidColorBrush(Microsoft.UI.Colors.Orange);
            }
        }

        // Tabla Output Value
        public string TablaOutputValue => GetTagValue(App4.Utilities.GlobalData.TablaOutputTag);

        public string TablaOutputStatusText
        {
            get
            {
                var val = TablaOutputValue;
                if (val == "1" || val.ToLower() == "true") return "HAZIR (1)";
                return "BEKLİYOR (0)";
            }
        }

        public SolidColorBrush TablaOutputStatusColor
        {
            get
            {
                var val = TablaOutputValue;
                if (val == "1" || val.ToLower() == "true") return new SolidColorBrush(Microsoft.UI.Colors.LimeGreen);
                return new SolidColorBrush(Microsoft.UI.Colors.Orange);
            }
        }

        // --- WATCHER YÖNETİMİ ---
        private PlcVariable _watchedRfidVar;
        private PlcVariable _watchedIndexVar;
        private PlcVariable _watchedTriggerVar;
        private PlcVariable _watchedTriggerVar2;
        private PlcVariable _watchedOutputVar;
        private PlcVariable _watchedTablaOutputVar;

        private void SetupWatchers()
        {
            UpdateRfidWatcher();
            UpdateIndexWatcher();
            UpdateTriggerWatcher();
            UpdateTrigger2Watcher();
            UpdateOutputWatcher();
            UpdateTablaOutputWatcher();
        }

        private void UpdateRfidWatcher()
        {
            if (_watchedRfidVar != null) _watchedRfidVar.PropertyChanged -= OnPlcDisplayValueChanged;
            _watchedRfidVar = FindPlcVariable(SelectedRfidTag);
            if (_watchedRfidVar != null) _watchedRfidVar.PropertyChanged += OnPlcDisplayValueChanged;
            OnPropertyChanged(nameof(RfidTagValue));
        }

        private void UpdateIndexWatcher()
        {
            if (_watchedIndexVar != null) _watchedIndexVar.PropertyChanged -= OnPlcDisplayValueChanged;
            _watchedIndexVar = FindPlcVariable(SelectedIndexTag);
            if (_watchedIndexVar != null) _watchedIndexVar.PropertyChanged += OnPlcDisplayValueChanged;
            OnPropertyChanged(nameof(IndexTagValue));
        }

        private void UpdateTriggerWatcher()
        {
            if (_watchedTriggerVar != null) _watchedTriggerVar.PropertyChanged -= OnPlcDisplayValueChanged;
            _watchedTriggerVar = FindPlcVariable(SelectedTriggerTag);
            if (_watchedTriggerVar != null) _watchedTriggerVar.PropertyChanged += OnPlcDisplayValueChanged;
            OnPropertyChanged(nameof(TriggerTagValue));
        }

        private void UpdateTrigger2Watcher()
        {
            if (_watchedTriggerVar2 != null) _watchedTriggerVar2.PropertyChanged -= OnPlcDisplayValueChanged;
            _watchedTriggerVar2 = FindPlcVariable(SelectedTriggerTag2);
            if (_watchedTriggerVar2 != null) _watchedTriggerVar2.PropertyChanged += OnPlcDisplayValueChanged;
            OnPropertyChanged(nameof(TriggerTag2Value));
        }

        private void UpdateOutputWatcher()
        {
            if (_watchedOutputVar != null) _watchedOutputVar.PropertyChanged -= OnPlcDisplayValueChanged;
            _watchedOutputVar = FindPlcVariable(SelectedMeasurementOutputTag);
            if (_watchedOutputVar != null) _watchedOutputVar.PropertyChanged += OnPlcDisplayValueChanged;
            OnPropertyChanged(nameof(MeasurementOutputValue));
            OnPropertyChanged(nameof(MeasurementOutputStatusText));
            OnPropertyChanged(nameof(MeasurementOutputStatusColor));
        }

        private void UpdateTablaOutputWatcher()
        {
            if (_watchedTablaOutputVar != null) _watchedTablaOutputVar.PropertyChanged -= OnPlcDisplayValueChanged;
            _watchedTablaOutputVar = FindPlcVariable(SelectedTablaOutputTag);
            if (_watchedTablaOutputVar != null) _watchedTablaOutputVar.PropertyChanged += OnPlcDisplayValueChanged;
            OnPropertyChanged(nameof(TablaOutputValue));
            OnPropertyChanged(nameof(TablaOutputStatusText));
            OnPropertyChanged(nameof(TablaOutputStatusColor));
        }

        private PlcVariable FindPlcVariable(string tagName)
        {
            if (string.IsNullOrEmpty(tagName)) return null;
            // Sırayla Genel Input, Genel Output, PlcService ve Robot listelerini tara
            var v = App4.Utilities.GlobalData.GeneralInputVars.FirstOrDefault(x => x.Name == tagName);
            if (v != null) return v;
            v = App4.Utilities.GlobalData.GeneralOutputVars.FirstOrDefault(x => x.Name == tagName);
            if (v != null) return v;

            if (App4.Utilities.PlcService.Instance != null)
            {
                v = App4.Utilities.PlcService.Instance.InputVariables.FirstOrDefault(x => x.Name == tagName);
                if (v != null) return v;
                v = App4.Utilities.PlcService.Instance.OutputVariables.FirstOrDefault(x => x.Name == tagName);
                if (v != null) return v;
            }

            // Robot Instance Variables (canli degerler - oncelikli)
            var robots = App4.Utilities.KukaRobotManager.Instance?.Robots;
            if (robots != null)
            {
                foreach (var robot in robots)
                {
                    v = robot.InputVars.FirstOrDefault(x => x.Name == tagName);
                    if (v != null) return v;
                    v = robot.OutputVars.FirstOrDefault(x => x.Name == tagName);
                    if (v != null) return v;
                }
            }

            // Robot GlobalData sablonu (yedek)
            v = App4.Utilities.GlobalData.RobotInputVars.FirstOrDefault(x => x.Name == tagName);
            if (v != null) return v;
            v = App4.Utilities.GlobalData.RobotOutputVars.FirstOrDefault(x => x.Name == tagName);
            if (v != null) return v;

            return null;
        }

        private void OnPlcDisplayValueChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Value" || e.PropertyName == "CurrentValue")
            {
                this.DispatcherQueue.TryEnqueue(() =>
                {
                    if (sender == _watchedRfidVar) OnPropertyChanged(nameof(RfidTagValue));
                    else if (sender == _watchedIndexVar) OnPropertyChanged(nameof(IndexTagValue));
                    else if (sender == _watchedTriggerVar) OnPropertyChanged(nameof(TriggerTagValue));
                    else if (sender == _watchedTriggerVar2) OnPropertyChanged(nameof(TriggerTag2Value));
                    else if (sender == _watchedOutputVar)
                    {
                        OnPropertyChanged(nameof(MeasurementOutputValue));
                        OnPropertyChanged(nameof(MeasurementOutputStatusText));
                        OnPropertyChanged(nameof(MeasurementOutputStatusColor));
                    }
                    else if (sender == _watchedTablaOutputVar)
                    {
                        OnPropertyChanged(nameof(TablaOutputValue));
                        OnPropertyChanged(nameof(TablaOutputStatusText));
                        OnPropertyChanged(nameof(TablaOutputStatusColor));
                    }
                });
            }
        }

        private string GetTagValue(string tagName)
        {
            if (string.IsNullOrEmpty(tagName)) return "---";
            
            // 1. GeneralInputVars
            var plcVar = App4.Utilities.GlobalData.GeneralInputVars.FirstOrDefault(v => v.Name == tagName);
            if (plcVar != null) return plcVar.Value ?? "---";
            
            // 2. GeneralOutputVars
            var outVar = App4.Utilities.GlobalData.GeneralOutputVars.FirstOrDefault(v => v.Name == tagName);
            if (outVar != null) return outVar.Value ?? "---";

            // 3. PlcService'den de kontrol et
            if (App4.Utilities.PlcService.Instance != null)
            {
                var serviceVar = App4.Utilities.PlcService.Instance.InputVariables.FirstOrDefault(v => v.Name == tagName);
                if (serviceVar != null) return serviceVar.Value ?? "---";

                var serviceOut = App4.Utilities.PlcService.Instance.OutputVariables.FirstOrDefault(v => v.Name == tagName);
                if (serviceOut != null) return serviceOut.Value ?? "---";
            }

            // 4. Robot Instance Variables (canli degerler)
            var robots = App4.Utilities.KukaRobotManager.Instance?.Robots;
            if (robots != null)
            {
                foreach (var robot in robots)
                {
                    var riVar = robot.InputVars.FirstOrDefault(v => v.Name == tagName);
                    if (riVar != null) return riVar.Value ?? "---";
                    var roVar = robot.OutputVars.FirstOrDefault(v => v.Name == tagName);
                    if (roVar != null) return roVar.Value ?? "---";
                }
            }

            // 5. Robot GlobalData sablonu (yedek)
            var robotIn = App4.Utilities.GlobalData.RobotInputVars.FirstOrDefault(v => v.Name == tagName);
            if (robotIn != null) return robotIn.Value ?? "---";

            var robotOut = App4.Utilities.GlobalData.RobotOutputVars.FirstOrDefault(v => v.Name == tagName);
            if (robotOut != null) return robotOut.Value ?? "---";

            return "---";
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        // Input Tagleri için liste (Otomasyon için gerekli)
        public ObservableCollection<string> PlcInputTags { get; set; } = new();
        public ObservableCollection<string> PlcOutputTags { get; set; } = new();
        // Input + Output birleşik liste (RFID TAG seçimi için)
        public ObservableCollection<string> PlcAllTags { get; set; } = new();
        // Global listeye referans (Ok işareti => ile)
        public ObservableCollection<PlcTransferItem> PlcTransferRows => App4.Utilities.GlobalData.PlcTransferRows;

        // ▼▼▼ TABLA KAÇIKLIK PLC AKTARIM REFERANSI ▼▼▼
        public ObservableCollection<PlcTransferItem> TablaTransferRows => App4.Utilities.GlobalData.TablaTransferRows;

        private List<string> _logHistory = new();
        private bool _isWebViewInitialized = false;
      

        // Cached brushes for better performance
        private static readonly SolidColorBrush BrushOrange = new(Microsoft.UI.Colors.Orange);
        private static readonly SolidColorBrush BrushGreen = new(Microsoft.UI.Colors.LimeGreen);
        private static readonly SolidColorBrush BrushRed = new(Microsoft.UI.Colors.Red);
        private static readonly SolidColorBrush BrushIndianRed = new(Microsoft.UI.Colors.IndianRed);

        private Action<string> _automationLogHandler;
        private Action _automationStatusHandler;

        public Camera_Page()
        {
            this.InitializeComponent();
            this.DataContext = this;
            
            // Handlerları oluştur (Bellek sızıntısı ve duplicate log önleme)
            _automationLogHandler = (msg) => this.DispatcherQueue.TryEnqueue(() => AddLog(msg));
            _automationStatusHandler = () =>
            {
                this.DispatcherQueue.TryEnqueue(() =>
                {
                    OnPropertyChanged(nameof(LivePlcRfid));
                    OnPropertyChanged(nameof(LivePlcIndex));
                    OnPropertyChanged(nameof(LiveProcessStatus));
                    OnPropertyChanged(nameof(IsProcessRunning));
                    OnPropertyChanged(nameof(SelectedRfidTag));
                    OnPropertyChanged(nameof(SelectedIndexTag));
                    OnPropertyChanged(nameof(SelectedTriggerTag));
                    OnPropertyChanged(nameof(SelectedTriggerTag2));
                    OnPropertyChanged(nameof(RfidTagValue));
                    OnPropertyChanged(nameof(IndexTagValue));
                    OnPropertyChanged(nameof(TriggerTagValue));
                    OnPropertyChanged(nameof(TriggerTag2Value));
                    OnPropertyChanged(nameof(MeasurementOutputValue));
                    OnPropertyChanged(nameof(MeasurementOutputStatusText));
                    OnPropertyChanged(nameof(MeasurementOutputStatusColor));
                    OnPropertyChanged(nameof(TablaOutputValue));
                    OnPropertyChanged(nameof(TablaOutputStatusText));
                    OnPropertyChanged(nameof(TablaOutputStatusColor));
                });
            };

            this.Loaded += Camera_Page_Loaded;
            this.Unloaded += Camera_Page_Unloaded;
        }

        private void Camera_Page_Unloaded(object sender, RoutedEventArgs e)
        {
            // Olay dinleyicilerini temizle
            if (_automationLogHandler != null)
                App4.Utilities.GlobalData.OnAutomationLog -= _automationLogHandler;
            
            if (_automationStatusHandler != null)
                App4.Utilities.GlobalData.OnAutomationStatusChanged -= _automationStatusHandler;

            // Watcher'ları temizle
            if (_watchedRfidVar != null) _watchedRfidVar.PropertyChanged -= OnPlcDisplayValueChanged;
            if (_watchedIndexVar != null) _watchedIndexVar.PropertyChanged -= OnPlcDisplayValueChanged;
            if (_watchedTriggerVar != null) _watchedTriggerVar.PropertyChanged -= OnPlcDisplayValueChanged;
            if (_watchedTriggerVar2 != null) _watchedTriggerVar2.PropertyChanged -= OnPlcDisplayValueChanged;
            if (_watchedOutputVar != null) _watchedOutputVar.PropertyChanged -= OnPlcDisplayValueChanged;
            if (_watchedTablaOutputVar != null) _watchedTablaOutputVar.PropertyChanged -= OnPlcDisplayValueChanged;
        }

        private async void BtnGetMeasurement_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn != null) btn.IsEnabled = false;

            AddLog("► Ölçüm alma isteği gönderildi...");

            // DÜZELTME: Metot artık Tuple döndürüyor (int status, List data).
            // Deconstruction yaparak sadece status'u alıyoruz veya sonucu kontrol ediyoruz.
            var result = await App4.Utilities.ReceiveMeasurementLogic.ReceiveAndProcessMeasurements(AddLog, this.DispatcherQueue);

            // result.Item1 = Status (int)
            // result.Item2 = Data List (List<GocatorMeasurement>)
            if (result.Item1 == 1) // 1 = Başarılı
            {
                TransferMeasurementsToPlcRows();
            }

            if (btn != null) btn.IsEnabled = true;
        }








        private void LoadPlcTags()
        {
            try
            {
                // 1. GÜNCEL TAG LİSTELERİNİ TOPLA (GEÇİCİ LİSTE)
                var newOutputs = new HashSet<string>();
                var newInputs = new HashSet<string>();

                // A. PLC Servisinden Gelenler
                if (App4.Utilities.PlcService.Instance != null)
                {
                    foreach (var v in App4.Utilities.PlcService.Instance.OutputVariables)
                        if (!string.IsNullOrEmpty(v.Name)) newOutputs.Add(v.Name);

                    foreach (var v in App4.Utilities.PlcService.Instance.InputVariables)
                        if (!string.IsNullOrEmpty(v.Name)) newInputs.Add(v.Name);
                }

                // B. Global Data'dan Gelenler
                foreach (var v in App4.Utilities.GlobalData.GeneralOutputVars)
                    if (!string.IsNullOrEmpty(v.Name)) newOutputs.Add(v.Name);

                foreach (var v in App4.Utilities.GlobalData.GeneralInputVars)
                    if (!string.IsNullOrEmpty(v.Name)) newInputs.Add(v.Name);

                // C. Robot Input/Output Variables (Robot tag'leri direkt secim icin)
                foreach (var v in App4.Utilities.GlobalData.RobotInputVars)
                    if (!string.IsNullOrEmpty(v.Name)) newInputs.Add(v.Name);

                foreach (var v in App4.Utilities.GlobalData.RobotOutputVars)
                    if (!string.IsNullOrEmpty(v.Name)) newOutputs.Add(v.Name);

                // D. Robot Instance Variables (canli robot degiskenleri)
                var robots = App4.Utilities.KukaRobotManager.Instance?.Robots;
                if (robots != null)
                {
                    foreach (var robot in robots)
                    {
                        foreach (var v in robot.InputVars)
                            if (!string.IsNullOrEmpty(v.Name)) newInputs.Add(v.Name);
                        foreach (var v in robot.OutputVars)
                            if (!string.IsNullOrEmpty(v.Name)) newOutputs.Add(v.Name);
                    }
                }

                // ---------------------------------------------------------
                // 2. AKILLI SENKRONİZASYON (Output Tags) - Asla Clear() Yapma!
                // ---------------------------------------------------------

                // Listede olup artık var olmayanları çıkar
                var toRemoveOut = PlcOutputTags.Where(t => !newOutputs.Contains(t)).ToList();
                foreach (var t in toRemoveOut) PlcOutputTags.Remove(t);

                // Yeni gelenleri ekle
                foreach (var t in newOutputs)
                    if (!PlcOutputTags.Contains(t)) PlcOutputTags.Add(t);

                // ---------------------------------------------------------
                // 3. AKILLI SENKRONİZASYON (Input Tags)
                // ---------------------------------------------------------

                var toRemoveIn = PlcInputTags.Where(t => !newInputs.Contains(t)).ToList();
                foreach (var t in toRemoveIn) PlcInputTags.Remove(t);

                foreach (var t in newInputs)
                    if (!PlcInputTags.Contains(t)) PlcInputTags.Add(t);

                // ---------------------------------------------------------
                // 4. AKILLI SENKRONİZASYON (All Tags = Input + Output birleşik)
                // ---------------------------------------------------------
                var allTags = new HashSet<string>(newInputs);
                foreach (var t in newOutputs) allTags.Add(t);

                var toRemoveAll = PlcAllTags.Where(t => !allTags.Contains(t)).ToList();
                foreach (var t in toRemoveAll) PlcAllTags.Remove(t);

                foreach (var t in allTags)
                    if (!PlcAllTags.Contains(t)) PlcAllTags.Add(t);

                AddLog($"✓ PLC Tag listeleri senkronize edildi. (In: {PlcInputTags.Count}, Out: {PlcOutputTags.Count}, All: {PlcAllTags.Count})");
            }
            catch (Exception ex)
            {
                AddLog("PLC Tagleri yüklenemedi: " + ex.Message);
            }
        }

        // ▼▼▼ YENİ METOT: TRANSFER SATIR SEÇİMLERİNİ YENİLE ▼▼▼
        private void RefreshTransferRowBindings()
        {
            try
            {
                // ▼▼▼ KRİTİK: OTOMASYON COMBOBOX'LARINI DOĞRUDAN GÜNCELLE ▼▼▼
                string rfid = App4.Utilities.GlobalData.Auto_RfidTag;
                string index = App4.Utilities.GlobalData.Auto_IndexTag;
                string trigger = App4.Utilities.GlobalData.Auto_TriggerTag;
                string output = App4.Utilities.GlobalData.MeasurementOutputTag;

                // ComboBox'ları doğrudan kod ile set et
                if (!string.IsNullOrEmpty(rfid) && PlcAllTags.Contains(rfid))
                    CmbRfidTag.SelectedItem = rfid;

                if (!string.IsNullOrEmpty(index))
                    CmbIndexTag.Text = index;

                if (!string.IsNullOrEmpty(trigger) && PlcInputTags.Contains(trigger))
                    CmbTriggerTag.SelectedItem = trigger;

                if (!string.IsNullOrEmpty(output) && PlcOutputTags.Contains(output))
                    CmbMeasurementOutputTag.SelectedItem = output;

                // Tabla Output ComboBox güncelle
                string tablaOutput = App4.Utilities.GlobalData.TablaOutputTag;
                if (!string.IsNullOrEmpty(tablaOutput) && PlcOutputTags.Contains(tablaOutput))
                    CmbTablaOutputTag.SelectedItem = tablaOutput;

                // Trigger2 ComboBox güncelle
                string trigger2 = App4.Utilities.GlobalData.Auto_TriggerTag2;
                if (!string.IsNullOrEmpty(trigger2) && PlcInputTags.Contains(trigger2))
                    CmbTriggerTag2.SelectedItem = trigger2;

                // Transfer satır seçimlerini toplu güncelle (null -> değer ile binding tetiklenir)
                foreach (var row in PlcTransferRows)
                {
                    if (!string.IsNullOrEmpty(row.SelectedTag) && PlcOutputTags.Contains(row.SelectedTag))
                    {
                        string saved = row.SelectedTag;
                        row.SelectedTag = null;
                        row.SelectedTag = saved;
                    }
                }

                // Tabla Transfer satır seçimlerini de güncelle
                foreach (var row in TablaTransferRows)
                {
                    if (!string.IsNullOrEmpty(row.SelectedTag) && PlcOutputTags.Contains(row.SelectedTag))
                    {
                        string saved = row.SelectedTag;
                        row.SelectedTag = null;
                        row.SelectedTag = saved;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"RefreshTransferRowBindings Error: {ex.Message}");
            }
        }



        private void TransferMeasurementsToPlcRows()
        {
            try
            {
                var measurements = App4.Utilities.GlobalData.LastMeasurements;

                if (measurements.Count == 0) return;

                // Ölçüm sayısı kadar veya Tablo satır sayısı kadar (hangisi küçükse) döngü kur
                int count = Math.Min(measurements.Count, PlcTransferRows.Count);

                for (int i = 0; i < count; i++)
                {
                    // Soldaki ölçüm değerini al
                    var measuredVal = measurements[i].Value.ToString();

                    // Sağdaki satıra yaz
                    // Value özelliği NotifyPropertyChanged olduğu için ekran otomatik güncellenir
                    PlcTransferRows[i].Value = measuredVal;

                    // Görsel durumu güncelle
                    PlcTransferRows[i].Status = "WAIT";
                    PlcTransferRows[i].StatusColor = BrushOrange;
                }

                AddLog($"► {count} adet veri PLC tablosuna aktarıldı.");
            }
            catch (Exception ex)
            {
                AddLog($"Veri aktarım hatası: {ex.Message}");
            }
        }



        // --- YENİ SATIR EKLEME ---
        private void BtnAddPlcRow_Click(object sender, RoutedEventArgs e)
        {
            // 1. Sıra numarasını ve rengi belirle
            var index = PlcTransferRows.Count + 1;
            var color = (index % 2 == 1)
                ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 15, 15, 15))
                : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 20, 20, 20));

            // 2. Yeni satırı oluştur
            var newItem = new PlcTransferItem
            {
                Index = index,
                SelectedTag = null,
                Value = "0",
                Status = "WAIT",
                StatusColor = BrushOrange,
                BackgroundColor = color
            };

            // ▼▼▼ KRİTİK NOKTA: DİNLEYİCİ EKLEME ▼▼▼
            // Bu yeni satırın "SelectedTag" özelliği değişirse (yani kullanıcı listeden seçim yaparsa)
            // hemen git GlobalData üzerindeki kayıt fonksiyonunu çalıştır diyoruz.
            newItem.PropertyChanged += (s, ev) =>
            {
                if (ev.PropertyName == "SelectedTag")
                {
                    App4.Utilities.GlobalData.SaveTransferRows();
                }
            };
            // ▲▲▲ ▲▲▲

            // 3. Listeye ekle
            PlcTransferRows.Add(newItem);

            // 4. Ekleme işlemi bittiği için son durumu kaydet
            App4.Utilities.GlobalData.SaveTransferRows();
        }

        // --- SİSTEM YEDEKLEME FONKSİYONU ---
        // --- SİSTEM YEDEKLEME FONKSİYONU (DİREKT MASAÜSTÜNE) ---
        private async void BtnBackup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 1. Kaynak ve Hedef Yolları Belirle
                string sourcePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "App4");
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string zipFileName = $"App4_Yedek_{DateTime.Now:yyyyMMdd_HHmm}.zip";
                string destinationZip = Path.Combine(desktopPath, zipFileName);

                // 2. Kaynak Kontrolü
                if (!Directory.Exists(sourcePath))
                {
                    AddLog("⚠ Yedeklenecek veri klasörü bulunamadı (Henüz veri oluşmamış olabilir).");
                    return;
                }

                // Butonu kilitle (Çift tıklamayı önle)
                if (sender is Button btn) btn.IsEnabled = false;
                AddLog("⏳ Masaüstüne yedek alınıyor, lütfen bekleyin...");

                // 3. Arka Planda Sıkıştırma İşlemi
                await Task.Run(() =>
                {
                    try
                    {
                        // Eğer aynı isimde dosya varsa sil
                        if (File.Exists(destinationZip)) File.Delete(destinationZip);

                        // Klasörü direkt ziple
                        System.IO.Compression.ZipFile.CreateFromDirectory(sourcePath, destinationZip);
                    }
                    catch (Exception zipEx)
                    {
                        // Hata olursa (örn: dosya kullanımdayken) fırlat
                        throw new Exception("Sıkıştırma hatası: " + zipEx.Message);
                    }
                });

                AddLog($"✅ YEDEKLEME BAŞARILI!");
                AddLog($"📁 Konum: Masaüstü\\{zipFileName}");

                // Butonu aç
                if (sender is Button btnRestore) btnRestore.IsEnabled = true;
            }
            catch (Exception ex)
            {
                AddLog($"❌ Yedekleme Hatası: {ex.Message}");
                if (sender is Button btn) btn.IsEnabled = true;
            }
        }


        #region ===  GOCATOR YEDEKLEME MANTIĞI ===

        // GOCATOR YEDEKLEME MANTIĞI
        public class GocatorBackupLogic
        {
            private const string SYSTEM_BACKUP_PATH = "/system/commands/archive";
            private static string SENSOR_IP => App4.Utilities.GlobalData.Gocator_IpAddress;
            private static int CONTROL_PORT => App4.Utilities.GlobalData.Gocator_Port;
            private const int RECEIVE_DATA_TIMEOUT_MSEC = 60000;

            public static async Task<string> PerformBackup(Action<string> log)
            {
                return await Task.Run(() =>
                {
                    try
                    {
                        IPAddress ipAddress = IPAddress.Parse(SENSOR_IP);
                        using (GoSystem system = new GoSystem(ipAddress, (ushort)CONTROL_PORT))
                        {
                            log("Sensöre bağlanılıyor (Yedekleme)...");
                            system.Connect();

                            // Yedek içeriğini belirle (Jobs, Workspaces, Global vb.)
                            JObject payload = new JObject
                            {
                                ["contents"] = new JArray {
                            "global",
                            "allWorkspaces",
                            "allJobs",
                            "replay",
                            "liveJob"
                        }
                            };

                            log("Yedek verisi sensörden çekiliyor (Bu işlem sürebilir)...");

                            // API Çağrısı
                            JObject response = system.Client().Call(SYSTEM_BACKUP_PATH, payload).GetResponse(RECEIVE_DATA_TIMEOUT_MSEC).Payload;

                            // Byte verisini al
                            byte[] data = response["data"].ToObject<byte[]>();

                            // Dosyayı Masaüstüne Kaydet
                            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                            string fileName = $"Gocator_Backup_{DateTime.Now:yyyyMMdd_HHmm}.gpbak";
                            string fullPath = Path.Combine(desktopPath, fileName);

                            File.WriteAllBytes(fullPath, data);

                            system.Disconnect();
                            return fullPath; // Başarılı ise dosya yolunu döndür
                        }
                    }
                    catch (Exception ex)
                    {
                        log($"Gocator Yedek Hatası: {ex.Message}");
                        return null;
                    }
                });
            }
        }


        // --- GOCATOR YEDEKLEME BUTONU ---
        private async void BtnBackupGocator_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn != null) btn.IsEnabled = false;

            AddLog("► Gocator yedekleme işlemi başlatıldı...");

            // Mantık sınıfını çağır
            string resultPath = await GocatorBackupLogic.PerformBackup(AddLog);

            if (!string.IsNullOrEmpty(resultPath))
            {
                AddLog("✅ GOCATOR YEDEĞİ ALINDI!");
                AddLog($"📁 Dosya: {Path.GetFileName(resultPath)}");
                AddLog($"📍 Konum: Masaüstü");
            }
            else
            {
                AddLog("❌ Gocator yedeği alınamadı.");
            }

            if (btn != null) btn.IsEnabled = true;
        }

        // BU SINIF Camera_Page.xaml.cs EN ALTINA EKLENECEK
       

        // --- JOB YÖNETİM DEĞİŞKENLERİ ---
        // Sensörden çekilen Job listesini burada tutacağız
        public ObservableCollection<string> AvailableJobs { get; set; } = new();

        // --- SAYFA YÜKLENİRKEN LİSTEYİ GÜNCELLE ---
        // 'Camera_Page_Loaded' fonksiyonunun sonuna şu satırı ekleyin:
        // await RefreshJobList(); 

        // 1. JOB LİSTESİNİ YENİLEME FONKSİYONU
        private async Task RefreshJobList()
        {
            // Listeyi temizle
            AvailableJobs.Clear();
            SensorJobListView.Items.Clear();

            var jobs = await App4.Utilities.GocatorJobLogic.GetJobList(AddLog);
            // Sensörden veriyi çek
           
            foreach (var job in jobs)
            {
                AvailableJobs.Add(job); // ComboBox'lar için veri kaynağı
                SensorJobListView.Items.Add(job); // Sol panel listesi
            }

            // --- YENİ EKLENEN KISIM: KAYDET ---
            if (jobs.Count > 0)
            {
                SaveCachedJobs(); // Listeyi dosyaya yaz
                AddLog($"✓ Job listesi güncellendi ve kaydedildi. ({jobs.Count} dosya)");
            }
            else
            {
                AddLog("⚠ Job listesi boş veya çekilemedi.");
            }
        }

        // 2. COMBOBOX YÜKLENDİĞİNDE İÇİNİ DOLDUR
        // (XAML'da Loaded="JobCombo_Loaded" demiştik)
        private void JobCombo_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox cmb)
            {
                cmb.ItemsSource = AvailableJobs;
            }
        }

        // 3. JOB LİSTESİNİ YENİLEME BUTONU
        private async void BtnRefreshJobs_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button; if (btn != null) btn.IsEnabled = false;
            AddLog("Job listesi sensörden çekiliyor...");
            await RefreshJobList();
            if (btn != null) btn.IsEnabled = true;
        }

        // 4. JOB YÜKLEME (LOAD - AKTİF ETME)
        private async void BtnLoadSelectedJob_Click(object sender, RoutedEventArgs e)
        {
            if (SensorJobListView.SelectedItem is string jobName)
            {
                var btn = sender as Button; if (btn != null) btn.IsEnabled = false;
                AddLog($"Job yükleniyor: {jobName}...");

                bool success = await GocatorJobLogic.LoadJob(jobName, AddLog);

                if (success) AddLog($"✅ {jobName} başarıyla aktif edildi.");
                else AddLog("❌ Job yüklenemedi.");

                if (btn != null) btn.IsEnabled = true;
            }
            else
            {
                AddLog("⚠ Lütfen listeden bir job seçiniz.");
            }
        }

        // 5. JOB İNDİRME (DOWNLOAD - BACKUP)
        private async void BtnDownloadSelectedJob_Click(object sender, RoutedEventArgs e)
        {
            if (SensorJobListView.SelectedItem is string jobName)
            {
                var btn = sender as Button; if (btn != null) btn.IsEnabled = false;

                string path = await GocatorJobLogic.DownloadJob(jobName, AddLog);

                if (path != null) AddLog($"✅ Dosya indirildi: {Path.GetFileName(path)} (Masaüstü)");

                if (btn != null) btn.IsEnabled = true;
            }
            else
            {
                AddLog("⚠ Lütfen indirilecek job'ı seçiniz.");
            }
        }

        // 6. PC'DEN JOB YÜKLEME (UPLOAD)
        private async void BtnUploadNewJob_Click(object sender, RoutedEventArgs e)
        {
            // Dosya Seçici
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.FileTypeFilter.Add(".gpjob");

            // WinUI Handle Ayarı (Yedekleme kısmındaki gibi)
            var window = (Application.Current as App)?.MainWindow;
            if (window != null)
            {
                var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hWnd);
            }

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                AddLog($"Yükleniyor: {file.Name}...");
                bool success = await GocatorJobLogic.UploadJob(file.Path, AddLog);

                if (success)
                {
                    AddLog($"✅ {file.Name} sensöre yüklendi.");
                    await RefreshJobList(); // Listeyi güncelle
                }
            }
        }

        // 7. SEÇİM KAYDETME
        private void JobSelectionChanged_Save(object sender, SelectionChangedEventArgs e)
        {
            // ComboBox değiştiğinde GlobalData'yı kaydet ki program açılıp kapandığında hatırlasın
            App4.Utilities.GlobalData.SaveRfids();
        }

        // --- JOB ÖNBELLEK (CACHE) DOSYA YOLU ---
        private readonly string _jobCacheFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "App4", "Gocator_Jobs_Cache.json");

        // 1. ÖNBELLEĞİ YÜKLE (UYGULAMA AÇILINCA)
        private void LoadCachedJobs()
        {
            try
            {
                if (File.Exists(_jobCacheFilePath))
                {
                    var json = File.ReadAllText(_jobCacheFilePath);
                    var list = JsonConvert.DeserializeObject<List<string>>(json);

                    if (list != null && list.Count > 0)
                    {
                        AvailableJobs.Clear();
                        SensorJobListView.Items.Clear();

                        foreach (var job in list)
                        {
                            AvailableJobs.Add(job);
                            SensorJobListView.Items.Add(job);
                        }
                        AddLog($"✓ Önbellekten {list.Count} adet Job yüklendi.");
                    }
                }
            }
            catch (Exception ex) { AddLog("Job Cache yüklenemedi: " + ex.Message); }
        }

        // 2. ÖNBELLEĞE KAYDET (LİSTE YENİLENİNCE)
        private void SaveCachedJobs()
        {
            try
            {
                // AvailableJobs listesini JSON'a çevirip kaydet
                var json = JsonConvert.SerializeObject(AvailableJobs, Formatting.Indented);
                File.WriteAllText(_jobCacheFilePath, json);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Job Cache Save Error: " + ex.Message); }
        }

        // --- JOB SEQUENCE YÖNETİMİ (YENİ EKLENECEK KODLAR) ---

        // 1. Reçeteye Job Ekleme
        private void BtnAddJobToSequence_Click(object sender, RoutedEventArgs e)
        {
            // Butona tıklandığında hangi RFID kartındayız onu bulalım
            var btn = sender as Button;
            var rfidItem = btn?.DataContext as App4.Utilities.RfidDef;

            if (rfidItem != null)
            {
                // Butonun yanındaki ComboBox'ı bulmak için Grid içinde gezmemiz lazım
                // Veya daha kolayı: DataTemplate içinde olduğumuz için UI tree'den bulmak zor olabilir.
                // Hile: XAML'da ComboBox ismini verdik ama DataTemplate içinde olduğu için doğrudan erişemeyiz.
                // Çözüm: Butonun "Parent"ı olan Grid'in içindeki ComboBox'ı bulmak.

                var parentGrid = VisualTreeHelper.GetParent(btn) as Grid;
                var comboBox = parentGrid?.Children.OfType<ComboBox>().FirstOrDefault();

                if (comboBox != null && comboBox.SelectedItem is string selectedJob)
                {
                    // Listeye ekle
                    rfidItem.JobSequence.Add(selectedJob);

                    // Kaydet
                    App4.Utilities.GlobalData.SaveRfids();

                    // Seçimi sıfırla (isteğe bağlı)
                    comboBox.SelectedIndex = -1;
                }
                else
                {
                    AddLog("⚠ Lütfen önce bir Job seçiniz.");
                }
            }
        }

        // 2. Reçeteden Job Silme
        private void BtnRemoveJobFromSequence_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var jobName = btn?.DataContext as string; // Silinecek Job'ın adı

            if (btn != null && btn.Tag is App4.Utilities.RfidDef parentRfid && jobName != null)
            {
                parentRfid.JobSequence.Remove(jobName);
                App4.Utilities.GlobalData.SaveRfids();
            }
        }

        // 3. Job Sırası Yukarı Taşıma
        private void BtnMoveJobUp_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var jobName = btn?.DataContext as string;
            
            if (btn != null && btn.Tag is App4.Utilities.RfidDef parentRfid && jobName != null)
            {
                int currentIndex = parentRfid.JobSequence.IndexOf(jobName);
                if (currentIndex > 0)
                {
                    // Yukarı taşı
                    parentRfid.JobSequence.RemoveAt(currentIndex);
                    parentRfid.JobSequence.Insert(currentIndex - 1, jobName);
                    App4.Utilities.GlobalData.SaveRfids();
                }
            }
        }

        // 4. Job Sırası Aşağı Taşıma
        private void BtnMoveJobDown_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var jobName = btn?.DataContext as string;
            
            if (btn != null && btn.Tag is App4.Utilities.RfidDef parentRfid && jobName != null)
            {
                int currentIndex = parentRfid.JobSequence.IndexOf(jobName);
                if (currentIndex < parentRfid.JobSequence.Count - 1)
                {
                    // Aşağı taşı
                    parentRfid.JobSequence.RemoveAt(currentIndex);
                    parentRfid.JobSequence.Insert(currentIndex + 1, jobName);
                    App4.Utilities.GlobalData.SaveRfids();
                }
            }
        }


        // --- OTOMASYON AYARLARI ---
        // ▼▼▼ YENİ: SEÇİM DEĞİŞTİĞİNDE KAYDET ▼▼▼
        private void AutomationComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox cmb && cmb.SelectedItem is string selectedTag)
            {
                // Hangi ComboBox'tan geldiğini kontrol et
                if (cmb.Name == "CmbRfidTag")
                {
                    App4.Utilities.GlobalData.Auto_RfidTag = selectedTag;
                    AddLog($"✓ RFID Tag kaydedildi: {selectedTag}");
                }
                else if (cmb.Name == "CmbTriggerTag")
                {
                    App4.Utilities.GlobalData.Auto_TriggerTag = selectedTag;
                    AddLog($"✓ Trigger Tag kaydedildi: {selectedTag}");
                }
                else if (cmb.Name == "CmbMeasurementOutputTag")
                {
                    App4.Utilities.GlobalData.MeasurementOutputTag = selectedTag;
                    AddLog($"✓ Measurement Output Tag kaydedildi: {selectedTag}");
                }

                // Kaydetme işlemi GlobalData setter'da yapılıyor
                // Ek güvenlik için manuel olarak da çağır
                App4.Utilities.GlobalData.SaveAutomationSettings();
            }
        }

        // --- JOB INDEX TAG ARAMA (AutoSuggestBox) ---
        private void CmbIndexTag_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                var query = sender.Text?.Trim().ToUpperInvariant() ?? "";
                if (string.IsNullOrEmpty(query))
                {
                    sender.ItemsSource = PlcAllTags.ToList();
                }
                else
                {
                    sender.ItemsSource = PlcAllTags
                        .Where(t => t.ToUpperInvariant().Contains(query))
                        .ToList();
                }
            }
        }

        private void CmbIndexTag_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            if (args.SelectedItem is string selectedTag)
            {
                SelectedIndexTag = selectedTag;
                App4.Utilities.GlobalData.SaveAutomationSettings();
                AddLog($"✓ Index Tag kaydedildi: {selectedTag}");
            }
        }

        private void CmbIndexTag_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (args.ChosenSuggestion is string selectedTag)
            {
                SelectedIndexTag = selectedTag;
                App4.Utilities.GlobalData.SaveAutomationSettings();
                AddLog($"✓ Index Tag kaydedildi: {selectedTag}");
            }
            else if (!string.IsNullOrEmpty(args.QueryText))
            {
                // Tam eşleşme kontrolü
                var match = PlcAllTags.FirstOrDefault(t => t.Equals(args.QueryText, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    SelectedIndexTag = match;
                    sender.Text = match;
                    App4.Utilities.GlobalData.SaveAutomationSettings();
                    AddLog($"✓ Index Tag kaydedildi: {match}");
                }
            }
        }

        // --- 4. MANUEL BUTON & UI EVENTLERİ ---
        private async void BtnManualTrigger_Click(object sender, RoutedEventArgs e)
        {
            if (IsManualMode)
            {
                var btn = sender as Button;
                if (btn != null) btn.IsEnabled = false;

                // Manuel modda girilen değerler
                string rfidValue = ManualRfidValue.Trim();
                string indexValue = ManualIndexValue.Trim();

                // Kontrol: değerler girilmiş mi?
                if (string.IsNullOrEmpty(rfidValue) || string.IsNullOrEmpty(indexValue))
                {
                    AddLog("⚠ RFID ve Index değerleri zorunludur!");
                    if (btn != null) btn.IsEnabled = true;
                    return;
                }

                AddLog($"✓ Manuel tetikleme: RFID={rfidValue}, Index={indexValue}");

                try
                {
                    // 1. RFID'ye göre kart tanımını bul
                    var rfidDef = App4.Utilities.GlobalData.KnownRfids
                        .FirstOrDefault(r => r.Id == rfidValue);

                    string selectedJob = null;

                    if (rfidDef != null && rfidDef.JobSequence != null && rfidDef.JobSequence.Count > 0)
                    {
                        // Kart tanımlanmış ve job sequence'i var
                        // Index değerini int'e çevir
                        if (int.TryParse(indexValue, out int jobIndex))
                        {
                            if (jobIndex >= 0 && jobIndex < rfidDef.JobSequence.Count)
                            {
                                selectedJob = rfidDef.JobSequence[jobIndex];
                                AddLog($"✓ Kart bulundu: {rfidValue}");
                                AddLog($"✓ JobSequence'den job seçildi (Index {jobIndex}): {selectedJob}");
                            }
                            else
                            {
                                AddLog($"❌ Index {jobIndex} kart tanımında geçersiz (Job sayısı: {rfidDef.JobSequence.Count})");
                                if (btn != null) btn.IsEnabled = true;
                                return;
                            }
                        }
                        else
                        {
                            AddLog($"❌ Index değeri sayı olmalıdır: {indexValue}");
                            if (btn != null) btn.IsEnabled = true;
                            return;
                        }
                    }
                    else
                    {
                        // Kart tanımlanmamış veya job sequence yok
                        // Genel job listesinde ara
                        AddLog($"⚠ Kart tanımında job sequence bulunamadı, genel job listesinde aranıyor...");
                        selectedJob = await FindAndSelectJobByIndex(indexValue);
                    }

                    if (string.IsNullOrEmpty(selectedJob))
                    {
                        AddLog($"❌ Job bulunamadı (RFID: {rfidValue}, Index: {indexValue})");
                        if (btn != null) btn.IsEnabled = true;
                        return;
                    }

                    AddLog($"► Seçili Job: {selectedJob}");
                    AddLog($"► Job yükleniyor...");

                    // 2. Job'u seçili yap
                    bool jobLoadSuccess = await GocatorJobLogic.LoadJob(selectedJob, AddLog);

                    if (!jobLoadSuccess)
                    {
                        AddLog($"❌ Job yüklenemedi: {selectedJob}");
                        if (btn != null) btn.IsEnabled = true;
                        return;
                    }

                    AddLog($"✓ {selectedJob} aktif edildi");

                    // ▼▼▼ ÖLÇÜM SİNYALİNİ SIFIRLA ▼▼▼
                    App4.Utilities.GlobalData.ResetMeasurementSignal();

                    // 3. Seçili job ile ölçüm ver al
                    AddLog($"► Sensörden ölçüm verisi alınıyor...");
                    var result = await App4.Utilities.ReceiveMeasurementLogic.ReceiveAndProcessMeasurements(AddLog, this.DispatcherQueue);

                    if (result.Item1 == 1) // Başarılı
                    {
                        // ▼▼▼ SİNYAL GÖNDER ▼▼▼
                        App4.Utilities.GlobalData.SetMeasurementSignal();

                        // 4. Ölçüm verilerini PLC satırlarına aktar
                        TransferMeasurementsToPlcRows();
                        
                        AddLog($"✅ BAŞARILI!");
                        AddLog($"  RFID: {rfidValue} | Index: {indexValue} | Job: {selectedJob}");
                        AddLog($"  {result.Item2.Count} adet ölçüm verisi aktarıldı");
                    }
                    else
                    {
                        AddLog("❌ Sensör verisi alınamadı");
                        AddLog("⚠ Job içerisinde Output (Çıktı) olmayabilir veya zaman aşımı.");
                    }
                }
                catch (Exception ex)
                {
                    AddLog($"❌ Hata: {ex.Message}");
                }
                finally
                {
                    if (btn != null) btn.IsEnabled = true;
                }
            }
            else
            {
                // Otomatik moddaysa automation sequence çalışır
                AddLog("► Otomatik tetikleme başlatılıyor...");
                _ = App4.Utilities.GlobalData.RunAutomationSequence();
            }
        }

        // Index değerine göre job bulma (Kart tanımında yoksa genel listede ara)
        private async Task<string> FindAndSelectJobByIndex(string indexValue)
        {
            try
            {
                // AvailableJobs listesinde index değerini ara
                var matchedJob = AvailableJobs.FirstOrDefault(j => 
                    j.Contains(indexValue) || 
                    j.EndsWith(indexValue) ||
                    j.Contains($"_{indexValue}") ||
                    j.Contains($"-{indexValue}")
                );

                if (!string.IsNullOrEmpty(matchedJob))
                {
                    AddLog($"✓ Job bulundu: {matchedJob}");
                    return matchedJob;
                }
                else
                {
                    // Job listesini yenile ve tekrar ara
                    AddLog($"⚠ Job tarafından bulunmadı, liste yenileniyor...");
                    await RefreshJobList();
                    
                    matchedJob = AvailableJobs.FirstOrDefault(j => 
                        j.Contains(indexValue) || 
                        j.EndsWith(indexValue) ||
                        j.Contains($"_{indexValue}") ||
                        j.Contains($"-{indexValue}")
                    );

                    if (!string.IsNullOrEmpty(matchedJob))
                    {
                        AddLog($"✓ Job bulundu: {matchedJob}");
                        return matchedJob;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                AddLog($"❌ Job arama hatası: {ex.Message}");
                return null;
            }
        }





        #endregion



        private void BtnDeletePlcRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is PlcTransferItem item)
            {
                // 1. Listeden sil
                PlcTransferRows.Remove(item);

                // 2. Sıra numaralarını ve renkleri düzelt (1, 2, 3... diye sıralansın)
                for (int i = 0; i < PlcTransferRows.Count; i++)
                {
                    PlcTransferRows[i].Index = i + 1;
                    PlcTransferRows[i].BackgroundColor = ((i + 1) % 2 == 1)
                        ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 15, 15, 15))
                        : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 20, 20, 20));
                }

                // 3. ▼▼▼ KRİTİK NOKTA: KAYDET ▼▼▼
                // Değişikliği dosyaya işle (Eskiden burada yerel fonksiyon vardı, şimdi Global'i çağırıyoruz)
                App4.Utilities.GlobalData.SaveTransferRows();
            }
        }

        #region ═══ TABLA KAÇIKLIK ÖLÇÜM & PLC AKTARIM ═══

        // --- TABLA ÖLÇÜMÜ AL (JOB 0) ---
        private async void BtnGetTablaMeasurement_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn != null) btn.IsEnabled = false;

            try
            {
                AddLog("► [TABLA] Job 0 yükleniyor...");

                // 1. Job listesinden ilk job'u (index 0) al
                if (AvailableJobs.Count == 0)
                {
                    AddLog("⚠ [TABLA] Job listesi boş, sensörden çekiliyor...");
                    await RefreshJobList();
                }

                if (AvailableJobs.Count == 0)
                {
                    AddLog("❌ [TABLA] Sensörde hiç job bulunamadı.");
                    return;
                }

                string job0Name = AvailableJobs[0]; // Job 0 = listedeki ilk job
                AddLog($"► [TABLA] Job 0: {job0Name}");

                // 2. Job 0'ı yükle
                bool loadOk = await GocatorJobLogic.LoadJob(job0Name, AddLog);
                if (!loadOk)
                {
                    AddLog($"❌ [TABLA] Job yüklenemedi: {job0Name}");
                    return;
                }

                AddLog($"✓ [TABLA] {job0Name} aktif edildi");

                // 3. Ölçüm al (dispatcher=null → GlobalData.LastMeasurements'a yazmaz)
                AddLog("► [TABLA] Sensörden tabla ölçüm verisi alınıyor...");
                var result = await App4.Utilities.ReceiveMeasurementLogic.ReceiveAndProcessMeasurements(AddLog, null);

                if (result.Item1 == 1 && result.Item2 != null)
                {
                    // 4. Tabla sinyal gönder
                    App4.Utilities.GlobalData.SetTablaMeasurementSignal();

                    // 5. TablaLastMeasurements koleksiyonunu güncelle (UI thread'de)
                    this.DispatcherQueue.TryEnqueue(() =>
                    {
                        App4.Utilities.GlobalData.TablaLastMeasurements.Clear();
                        foreach (var m in result.Item2)
                        {
                            App4.Utilities.GlobalData.TablaLastMeasurements.Add(m);
                        }
                        App4.Utilities.GlobalData.SaveTablaMeasurements();

                        // 6. Tabla PLC satırlarına aktar
                        TransferTablaToPlcRows();
                    });

                    AddLog($"✅ [TABLA] {result.Item2.Count} adet tabla kaçıklık ölçümü alındı.");
                }
                else
                {
                    AddLog("❌ [TABLA] Sensör verisi alınamadı.");
                }
            }
            catch (Exception ex)
            {
                AddLog($"❌ [TABLA] Hata: {ex.Message}");
            }
            finally
            {
                if (btn != null) btn.IsEnabled = true;
            }
        }

        // --- TABLA ÖLÇÜMLERİNİ PLC SATIRLARINA AKTAR ---
        private void TransferTablaToPlcRows()
        {
            try
            {
                var measurements = App4.Utilities.GlobalData.TablaLastMeasurements;
                if (measurements.Count == 0) return;

                int count = Math.Min(measurements.Count, TablaTransferRows.Count);

                for (int i = 0; i < count; i++)
                {
                    TablaTransferRows[i].Value = measurements[i].Value.ToString();
                    TablaTransferRows[i].Status = "WAIT";
                    TablaTransferRows[i].StatusColor = BrushOrange;
                }

                AddLog($"► [TABLA] {count} adet veri tabla PLC tablosuna aktarıldı.");
            }
            catch (Exception ex)
            {
                AddLog($"[TABLA] Veri aktarım hatası: {ex.Message}");
            }
        }

        // --- TABLA PLC YENİ SATIR EKLEME ---
        private void BtnAddTablaPlcRow_Click(object sender, RoutedEventArgs e)
        {
            var index = TablaTransferRows.Count + 1;
            var color = (index % 2 == 1)
                ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 15, 15, 15))
                : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 20, 20, 20));

            var newItem = new PlcTransferItem
            {
                Index = index,
                SelectedTag = null,
                Value = "0",
                Status = "WAIT",
                StatusColor = BrushOrange,
                BackgroundColor = color
            };

            // Tag seçimi değişirse kaydet
            newItem.PropertyChanged += (s, ev) =>
            {
                if (ev.PropertyName == "SelectedTag")
                {
                    App4.Utilities.GlobalData.SaveTablaTransferRows();
                }
            };

            TablaTransferRows.Add(newItem);
            App4.Utilities.GlobalData.SaveTablaTransferRows();
        }

        // --- TABLA PLC SATIR SİLME ---
        private void BtnDeleteTablaPlcRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is PlcTransferItem item)
            {
                TablaTransferRows.Remove(item);

                // Sıra numaralarını ve renkleri düzelt
                for (int i = 0; i < TablaTransferRows.Count; i++)
                {
                    TablaTransferRows[i].Index = i + 1;
                    TablaTransferRows[i].BackgroundColor = ((i + 1) % 2 == 1)
                        ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 15, 15, 15))
                        : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 20, 20, 20));
                }

                App4.Utilities.GlobalData.SaveTablaTransferRows();
            }
        }

        #endregion

        #region ═══ WebView2 Initialization (Modern Approach) ═══

        private async void Camera_Page_Loaded(object sender, RoutedEventArgs e)
        {
            // 1. Tag listelerini doldur
            LoadPlcTags();

            // 2. Job listelerini yükle (cache'den hızlı, arka planda sensörden güncelle)
            LoadCachedJobs();
            _ = RefreshJobList(); // Fire-and-forget, UI'ı bloklama

            // 3. Kayıtlı seçimleri UI'a doğrudan ata (Task.Delay yerine direkt)
            RefreshTransferRowBindings();

            // 4. Watcher'ları kur
            SetupWatchers();

            // 5. Event'leri bağla (Duplicate önlemek için önce çıkar)
            App4.Utilities.GlobalData.OnAutomationLog -= _automationLogHandler;
            App4.Utilities.GlobalData.OnAutomationLog += _automationLogHandler;

            App4.Utilities.GlobalData.OnAutomationStatusChanged -= _automationStatusHandler;
            App4.Utilities.GlobalData.OnAutomationStatusChanged += _automationStatusHandler;

            if (_isWebViewInitialized) return;

            try
            {
                AddLog("► WebView2 Ortamı Hazırlanıyor...");

                // 1. Temp klasöründe UserData oluştur (Önbellek/Cache için)
                string userDataFolder = Path.Combine(Path.GetTempPath(), "App4_WebView2_Cache");

                // Environment oluştur
                var env = await CoreWebView2Environment.CreateWithOptionsAsync(null, userDataFolder, null);

                // 2. CoreWebView2'yi bu environment ile başlat
                await PointCloudWebView.EnsureCoreWebView2Async(env);

                // 3. Assets Klasörünü Belirle ve Eşle
                string assetsPath;
                try
                {
                    // Paketli uygulama (MSIX) için yol
                    assetsPath = Path.Combine(Package.Current.InstalledLocation.Path, "Assets");
                }
                catch
                {
                    // Paketsiz çalışıyorsa (Debug/Release folder)
                    assetsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets");
                }

                if (!Directory.Exists(assetsPath))
                {
                    AddLog($"⚠ Assets klasörü bulunamadı: {assetsPath}");
                }
                else
                {
                    AddLog($"► Mapping Yolu: {assetsPath}");
                }

                // Sanal Host: https://appassets/ -> Yerel Assets klasörü
                PointCloudWebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                    "appassets",
                    assetsPath,
                    CoreWebView2HostResourceAccessKind.Allow);

                // Ayarlar
                PointCloudWebView.CoreWebView2.Settings.IsScriptEnabled = true;
                PointCloudWebView.CoreWebView2.Settings.AreDevToolsEnabled = true;

                // Eventleri bağla
                PointCloudWebView.NavigationCompleted += PointCloudWebView_NavigationCompleted;
                PointCloudWebView.WebMessageReceived += PointCloudWebView_WebMessageReceived;

                // 4. HTML'i Yükle
                AddLog("► HTML Yükleniyor...");
                PointCloudWebView.Source = new Uri("https://appassets/PointCloud3DViewer.html");

                _isWebViewInitialized = true;
            }
            catch (Exception ex)
            {
                AddLog($"✗ WebView2 Başlatma Hatası: {ex.Message}");
                // UI'daki yükleniyor ekranını kaldırıp hata göster
                ShowCloudStatus("HATA: WebView2 Başlatılamadı", Microsoft.UI.Colors.Red, false);
            }
        }

        private void PointCloudWebView_NavigationCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
        {
            if (args.IsSuccess)
            {
                AddLog("✓ HTML Sayfası Başarıyla Yüklendi");
                // Yüklenme dönen simgeyi durdur
                ShowCloudStatus("Hazır", Microsoft.UI.Colors.LimeGreen, false);
            }
            else
            {
                AddLog($"✗ HTML Navigasyon Hatası: {args.WebErrorStatus}");
                ShowCloudStatus("Sayfa Yüklenemedi", Microsoft.UI.Colors.Red, false);
            }
        }

        private void PointCloudWebView_WebMessageReceived(WebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
        {
            try
            {
                string msg = args.TryGetWebMessageAsString();
                if (!string.IsNullOrEmpty(msg))
                {
                    Debug.WriteLine($"[WEBVIEW LOG] {msg}");
                    // HTML'den gelen "Error:" mesajlarını da loglayabiliriz
                    if (msg.StartsWith("Error")) AddLog($"⚠ WebView JS Hatası: {msg}");
                }
            }
            catch { }
        }

        #endregion

        #region ═══ Surface / Point Cloud Process ═══

        private async void SurfaceButton_Click(object sender, RoutedEventArgs e)
        {
            // WebView hazır mı kontrolü
            if (!_isWebViewInitialized || PointCloudWebView.CoreWebView2 == null)
            {
                AddLog("✗ WebView henüz hazır değil, işlem yapılamaz.");
                ShowCloudStatus("WebView Hazır Değil", Microsoft.UI.Colors.Red, false);
                return;
            }

            try
            {
                // UI Durum Güncelleme
                ShowCloudStatus("Veriler Alınıyor...", Microsoft.UI.Colors.Orange, true);
                SurfaceButton.IsEnabled = false;

                AddLog("════════════════════════════════════════════");
                AddLog("► POINT CLOUD İŞLEMİ BAŞLATILIYOR");

                // Asenkron olarak veriyi çek
                var (result, pointCloudJson) = await ReceiveSurfaceSample.ReceiveSurfacePointCloudNet(AddLog);

                if (result != OK_STATUS)
                {
                    ShowCloudStatus("Sensör Hatası", Microsoft.UI.Colors.Red, false);
                    AddLog($"✗ Gocator Hatası (Kod: {result})");
                    return;
                }

                if (string.IsNullOrEmpty(pointCloudJson))
                {
                    ShowCloudStatus("Veri Boş", Microsoft.UI.Colors.Orange, false);
                    AddLog("✗ JSON Verisi oluşturulamadı veya boş.");
                    return;
                }

                AddLog($"✓ {pointCloudJson.Length} byte JSON alındı. WebView'a aktarılıyor...");
                AddLog("💡 Bakış Açısı: Z ekseni → Kırmızı (yakın) ön plana, Mavi (uzak) arka planda");
                ShowCloudStatus("Çizim Yapılıyor...", Microsoft.UI.Colors.LightBlue, true);

                // JSON verisini doğrudan JavaScript'e gönder
                string jsCode = $@"
                    (function() {{
                        try {{
                            if (typeof window.loadPointCloud === 'function') {{
                                const data = {pointCloudJson};
                                console.log('Loading ' + (data.points ? data.points.length : 0) + ' points...');
                                window.loadPointCloud(data);
                            }} else {{
                                console.error('loadPointCloud function not found');
                                window.chrome.webview.postMessage('Error: loadPointCloud fonksiyonu bulunamadı');
                            }}
                        }} catch(err) {{
                            console.error('Error loading point cloud:', err);
                            window.chrome.webview.postMessage('Error: ' + err.message);
                        }}
                    }})();
                ";

                AddLog("► JavaScript execute ediliyor...");
                var jsResult = await PointCloudWebView.ExecuteScriptAsync(jsCode);
                AddLog($"✓ JavaScript sonucu: {jsResult}");

                // İşlem tamamlandı
                ShowCloudStatus("Point Cloud Hazır!", Microsoft.UI.Colors.LimeGreen, false);
                AddLog("✓✓✓ İŞLEM TAMAMLANDI ✓✓✓");
                AddLog("════════════════════════════════════════════");
            }
            catch (Exception ex)
            {
                ShowCloudStatus("Uygulama Hatası", Microsoft.UI.Colors.Red, false);
                AddLog($"✗ Kritik Hata: {ex.Message}");
            }
            finally
            {
                SurfaceButton.IsEnabled = true;
                // Status panelini kapat
              
            }
        }

        // BU SINIFI DOSYANIN EN ALTINA EKLEYİN
       
        

        #endregion

        #region ═══ Photo Capture Process ═══

        private async void RunButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StatusLabel.Text = "Çekim Yapılıyor...";
                StatusLabel.Foreground = BrushOrange;
                PhotoButton.IsEnabled = false;
                LoadingRing.IsActive = true;

                AddLog("► Kamera Çekimi Başlatıldı...");
                int result = await ReceiveImageSample.ReceiveImageNet(AddLog);

                if (result == OK_STATUS)
                {
                    StatusLabel.Text = "BAŞARILI";
                    StatusLabel.Foreground = BrushGreen;
                    PhotoStatus.Text = "OK";
                    PhotoStatus.Foreground = BrushGreen;
                    AddLog("✓ Fotoğraf başarıyla çekildi");
                    await ShowCapturedImage();
                }
                else
                {
                    StatusLabel.Text = $"HATA: {result}";
                    StatusLabel.Foreground = BrushRed;
                    PhotoStatus.Text = "HATA";
                    PhotoStatus.Foreground = BrushIndianRed;
                    AddLog("✗ Çekim başarısız oldu");
                }
            }
            catch (Exception ex)
            {
                AddLog($"✗ Hata: {ex.Message}");
            }
            finally
            {
                LoadingRing.IsActive = false;
                PhotoButton.IsEnabled = true;
            }
        }

        private async Task ShowCapturedImage()
        {
            try
            {
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                DirectoryInfo di = new(desktopPath);

                FileInfo? rawFile = di.GetFiles("Gocator_*.raw")
                    .OrderByDescending(f => f.LastWriteTime)
                    .FirstOrDefault();

                if (rawFile != null)
                {
                    AddLog($"► Dosya bulundu: {rawFile.Name}");
                    await ConvertRawToBmpAndDisplay(rawFile, SensorDisplay);
                }
                else
                {
                    AddLog("⚠ Görüntülenecek RAW dosyası bulunamadı.");
                }
            }
            catch (Exception ex) { AddLog($"✗ Görüntüleme Hatası: {ex.Message}"); }
        }

        private async Task ConvertRawToBmpAndDisplay(FileInfo rawFile, Image targetImage)
        {
            try
            {
                string namePart = rawFile.Name.Replace("Gocator_", "").Replace(".raw", "");
                string[] parts = namePart.Split('_');

                if (parts.Length >= 2 && int.TryParse(parts[0], out int width) && int.TryParse(parts[1], out int height))
                {
                    byte[] rawBytes = await Task.Run(() => File.ReadAllBytes(rawFile.FullName));

                    if (rawBytes.Length != width * height)
                    {
                        AddLog("⚠ Dosya boyutu beklenen çözünürlükle eşleşmiyor.");
                        return;
                    }

                    byte[] bgraBytes = new byte[width * height * 4];
                    for (int i = 0; i < rawBytes.Length; i++)
                    {
                        int pos = i * 4;
                        byte val = rawBytes[i];
                        bgraBytes[pos] = val;
                        bgraBytes[pos + 1] = val;
                        bgraBytes[pos + 2] = val;
                        bgraBytes[pos + 3] = 255; // Alpha
                    }

                    string bmpPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                        $"Gocator_{width}_{height}.bmp");

                    await SaveBitmapFile(bmpPath, bgraBytes, width, height);
                    await DisplayBitmapFile(new FileInfo(bmpPath), targetImage);
                }
            }
            catch (Exception ex) { AddLog($"✗ Dönüştürme Hatası: {ex.Message}"); }
        }

        private async Task SaveBitmapFile(string filePath, byte[] bgraData, int width, int height)
        {
            using (FileStream fs = new(filePath, FileMode.Create, FileAccess.Write))
            {
                byte[] fileHeader = new byte[14];
                fileHeader[0] = 66; // B
                fileHeader[1] = 77; // M

                int fileSize = 54 + (width * height * 3);
                BitConverter.GetBytes(fileSize).CopyTo(fileHeader, 2);
                BitConverter.GetBytes(54).CopyTo(fileHeader, 10);

                byte[] dibHeader = new byte[40];
                BitConverter.GetBytes(40).CopyTo(dibHeader, 0);
                BitConverter.GetBytes(width).CopyTo(dibHeader, 4);
                BitConverter.GetBytes(height).CopyTo(dibHeader, 8);
                dibHeader[12] = 1; // Planes
                dibHeader[14] = 24; // Bits per pixel

                await fs.WriteAsync(fileHeader, 0, fileHeader.Length);
                await fs.WriteAsync(dibHeader, 0, dibHeader.Length);

                byte[] pixelData = new byte[width * height * 3];
                for (int i = 0; i < width * height; i++)
                {
                    int srcPos = i * 4;
                    int dstPos = i * 3;
                    pixelData[dstPos] = bgraData[srcPos];
                    pixelData[dstPos + 1] = bgraData[srcPos + 1];
                    pixelData[dstPos + 2] = bgraData[srcPos + 2];
                }

                await fs.WriteAsync(pixelData, 0, pixelData.Length);
            }
        }

        private async Task DisplayBitmapFile(FileInfo bmpFile, Image targetImage)
        {
            using (IRandomAccessStream stream = await (await StorageFile.GetFileFromPathAsync(bmpFile.FullName))
                .OpenAsync(FileAccessMode.Read))
            {
                var bitmap = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage();
                await bitmap.SetSourceAsync(stream);
                targetImage.Source = bitmap;
                
                // Görüntü yüklendikten sonra "Görüntü bekleniyor" yazısını gizle
                NoDataText.Visibility = Visibility.Collapsed;
            }
        }

        #endregion

        #region ═══ Helper Methods (Logs, Status) ═══

        // Renk parametresi eklendi (string color = null)
        // 1. TEK PARAMETRELİ LOG (Delegeler ve Action<string> için gerekli)
        private void AddLog(string message)
        {
            AddLog(message, null); // Diğer fonksiyonu çağırır
        }

        // 2. İKİ PARAMETRELİ LOG (Renkli uyarılar için gerekli)
        private void AddLog(string message, string color)
        {
            try
            {
                string timestamp = DateTime.Now.ToString("HH:mm:ss");
                string logEntry = $"[{timestamp}] {message}";

                // Log geçmişine ekle
                _logHistory.Add(logEntry);
                if (_logHistory.Count > 200) _logHistory.RemoveAt(0);

                // UI Güncelle (Renk şimdilik metin tabanlı logda göz ardı ediliyor ama kod kırılmıyor)
                this.DispatcherQueue.TryEnqueue(() =>
                {
                    if (LogOutput != null)
                        LogOutput.Text = string.Join("\n", _logHistory);
                });

                Debug.WriteLine(logEntry);
            }
            catch { }
        }

        private void ShowCloudStatus(string message, Windows.UI.Color color, bool isLoading)
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {
                if (Cloud3DStatus != null)
                {
                    Cloud3DStatus.Text = message;
                    Cloud3DStatus.Foreground = new SolidColorBrush(color);
                }

               
            });
        }


        #endregion
    }

   
    #region ═══ GOCATOR CLASSES ═══

    public class ReceiveImageSample
    {
        private const string SCAN_MODE_PATH = "$.parameters.scanModeSettings.scanMode";
        private const int IMAGE_MODE = 0;
        private const string GOCATOR_CONTROL_PATH = "/controls/gocator";
        private const string GOCATOR_OUTPUT_PATH = GOCATOR_CONTROL_PATH + "/outputs";
        private const string GOCATOR_ADD_OUTPUT_PATH = GOCATOR_OUTPUT_PATH + "/commands/add";
        private const string REPLAY_PATH = "/replay/playback";
        private const int RECEIVE_DATA_TIMEOUT_MSEC = 60000;
        private static string SENSOR_IP => App4.Utilities.GlobalData.Gocator_IpAddress;
        private static int CONTROL_PORT => App4.Utilities.GlobalData.Gocator_Port;

        public static async Task<int> ReceiveImageNet(Action<string>? log = null)
        {
            IPAddress ipAddress = IPAddress.Parse(SENSOR_IP);
            return await Task.Run(async () =>
            {
                using (GoSystem system = new GoSystem(ipAddress, (ushort)CONTROL_PORT))
                {
                    try
                    {
                        log?.Invoke("Sensöre bağlanılıyor...");
                        system.Connect();
                        if (VerifyConnection(system) == ERROR_STATUS) return ERROR_STATUS;

                        JObject response = system.Client().Read(REPLAY_PATH).GetResponse().Payload;
                        bool replayDataEnabled = (bool)response.GetValue("enabled")!;

                        if (!replayDataEnabled)
                        {
                            response = system.Client().Read(SCANNER_PATH).GetResponse().Payload;
                            if ((int)response.SelectToken(SCAN_MODE_PATH)! != IMAGE_MODE)
                            {
                                log?.Invoke("Mod değiştiriliyor: IMAGE");
                                JObject payload = new JObject { ["parameters"] = new JObject { ["scanModeSettings"] = new JObject { ["scanMode"] = IMAGE_MODE } } };
                                system.Client().Update(SCANNER_PATH, payload).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
                                await Task.Delay(500);
                            }
                        }

                        system.Client().Update(GOCATOR_CONTROL_PATH, new JObject { ["enabled"] = true }).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
                        await Task.Delay(500);

                        if (system.RunningState() == GoSystem.State.Ready) system.Start();

                        // Output Add Check
                        string dataSourceKey = "Image";
                        string imageDataSourceId = $"scan:{ENGINE_ID}:{SCANNER_ID}:{SCAN_ENGINE_COMPONENT}{dataSourceKey}0";
                        bool outputExists = false;
                        try
                        {
                            var map = (JArray)system.Client().Read(GOCATOR_OUTPUT_PATH).GetResponse().Payload.GetValue("map")!;
                            outputExists = map.Any(m => m.ToString().Contains(dataSourceKey));
                        }
                        catch { }

                        if (!outputExists)
                        {
                            system.Client().Call(GOCATOR_ADD_OUTPUT_PATH, new JObject { ["source"] = imageDataSourceId, ["outputId"] = 0, ["autoShift"] = true }).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
                        }

                        using (GoGdpClient gdpClient = new GoGdpClient())
                        {
                            gdpClient.Connect(system.Address, system.GdpPort());
                            gdpClient.ReceiveDataSync(RECEIVE_DATA_TIMEOUT_MSEC);

                            if (gdpClient.DataSet != null)
                            {
                                for (int i = 0; i < gdpClient.DataSet.Count; i++)
                                {
                                    if (gdpClient.DataSet.GdpMsgAt(i) is GoGdpImage imageMsg)
                                    {
                                        int width = (int)imageMsg.Width;
                                        int height = (int)imageMsg.Height;

                                        byte[] flatData = new byte[width * height];
                                        System.Buffer.BlockCopy(imageMsg.Pixels, 0, flatData, 0, flatData.Length);

                                        string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"Gocator_{width}_{height}.raw");
                                        File.WriteAllBytes(path, flatData);
                                        log?.Invoke($"RAW Kaydedildi: {Path.GetFileName(path)}");
                                    }
                                }
                            }
                            gdpClient.Close();
                        }
                        system.Stop();
                        return OK_STATUS;
                    }
                    catch (Exception ex)
                    {
                        log?.Invoke($"Resim Alma Hatası: {ex.Message}");
                        return ERROR_STATUS;
                    }
                }
            });
        }
    }

    public class ReceiveSurfaceSample
    {
        private const string SCAN_MODE_PATH = "$.parameters.scanModeSettings.scanMode";
        private const int SURFACE_MODE = 3;
        private const string GOCATOR_CONTROL_PATH = "/controls/gocator";
        private const string GOCATOR_OUTPUT_PATH = GOCATOR_CONTROL_PATH + "/outputs";
        private const string GOCATOR_ADD_OUTPUT_PATH = GOCATOR_OUTPUT_PATH + "/commands/add";
        private const string REPLAY_PATH = "/replay/playback";
        private const int RECEIVE_DATA_TIMEOUT_MSEC = 60000;
        private static string SENSOR_IP => App4.Utilities.GlobalData.Gocator_IpAddress;
        private static int CONTROL_PORT => App4.Utilities.GlobalData.Gocator_Port;

        public static async Task<(int status, string pointCloudJson)> ReceiveSurfacePointCloudNet(Action<string>? log = null)
        {
            IPAddress ipAddress = IPAddress.Parse(SENSOR_IP);
            return await Task.Run(async () =>
            {
                using (GoSystem system = new GoSystem(ipAddress, (ushort)CONTROL_PORT))
                {
                    try
                    {
                        log?.Invoke("Sensöre bağlanılıyor...");
                        system.Connect();
                        if (VerifyConnection(system) == ERROR_STATUS)
                            return (ERROR_STATUS, "");

                        JObject response = system.Client().Read(REPLAY_PATH).GetResponse().Payload;
                        bool replayDataEnabled = (bool)response.GetValue("enabled")!;

                        if (!replayDataEnabled)
                        {
                            response = system.Client().Read(SCANNER_PATH).GetResponse().Payload;
                            if ((int)response.SelectToken(SCAN_MODE_PATH)! != SURFACE_MODE)
                            {
                                log?.Invoke("Mod değiştiriliyor: SURFACE");
                                JObject payload = new JObject { ["parameters"] = new JObject { ["scanModeSettings"] = new JObject { ["scanMode"] = SURFACE_MODE } } };
                                system.Client().Update(SCANNER_PATH, payload).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
                                
                            }
                        }

                        system.Client().Update(SCANNER_PATH, new JObject { ["parameters"] = new JObject { ["scanModeSettings"] = new JObject { ["intensityEnabled"] = true } } }).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);

                        system.Client().Update(GOCATOR_CONTROL_PATH, new JObject { ["enabled"] = true }).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
                        

                        if (system.RunningState() == GoSystem.State.Ready)
                        {
                            system.Start();
                           
                        }

                        string dataSourceKey = "UniformSurface";
                        string uniformSurfaceDataSourceId = $"scan:{ENGINE_ID}:{SCANNER_ID}:top{dataSourceKey}0";
                        bool outputExists = false;
                        try
                        {
                            var map = (JArray)system.Client().Read(GOCATOR_OUTPUT_PATH).GetResponse().Payload.GetValue("map")!;
                            outputExists = map.Any(m => m.ToString().Contains(dataSourceKey));
                        }
                        catch { }

                        if (!outputExists)
                        {
                            log?.Invoke("Uniform Output ekleniyor...");
                            system.Client().Call(GOCATOR_ADD_OUTPUT_PATH, new JObject { ["source"] = uniformSurfaceDataSourceId, ["outputId"] = 0, ["autoShift"] = true }).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
                        }

                        string resultJson = "";
                        using (GoGdpClient gdpClient = new GoGdpClient())
                        {
                            gdpClient.Connect(system.Address, system.GdpPort());
                            log?.Invoke("Veri bekleniyor...");
                            gdpClient.ReceiveDataSync(RECEIVE_DATA_TIMEOUT_MSEC);

                            int count = (int)(gdpClient.DataSet?.Count ?? 0);
                            log?.Invoke($"Veri paketi alındı. Mesaj sayısı: {count}");

                            if (count > 0 && gdpClient.DataSet != null)
                            {
                                for (int i = 0; i < count; i++)
                                {
                                    if (gdpClient.DataSet.GdpMsgAt(i) is GoGdpSurfaceUniform uMsg)
                                    {
                                        log?.Invoke("Uniform Surface işleniyor...");
                                        resultJson = ProcessUniformSurface(uMsg);
                                        if (!string.IsNullOrEmpty(resultJson)) break;
                                    }
                                    else if (gdpClient.DataSet.GdpMsgAt(i) is GoGdpSurfacePointCloud pMsg)
                                    {
                                        log?.Invoke("Point Cloud işleniyor...");
                                        resultJson = ProcessSurfacePointCloud(pMsg);
                                        if (!string.IsNullOrEmpty(resultJson)) break;
                                    }
                                }
                            }
                            gdpClient.Close();
                        }

                        system.Stop();
                        return string.IsNullOrEmpty(resultJson) ? (ERROR_STATUS, "") : (OK_STATUS, resultJson);
                    }
                    catch (Exception ex)
                    {
                        log?.Invoke($"Surface Alma Hatası: {ex.Message}");
                        return (ERROR_STATUS, "");
                    }
                }
            });
        }

        private static string ProcessUniformSurface(GoGdpSurfaceUniform msg)
        {
            var data = new PointCloudData
            {
                metadata = new PointCloudMetadata
                {
                    timestamp = DateTime.Now,
                    pointCount = (int)(msg.Width * msg.Length),
                    offsetX = msg.Offset.X,
                    offsetY = msg.Offset.Y,
                    offsetZ = msg.Offset.Z,
                    resolutionX = msg.Resolution.X,
                    resolutionY = msg.Resolution.Y,
                    resolutionZ = msg.Resolution.Z,
                    width = (uint)msg.Width,
                    length = (uint)msg.Length
                }
            };

            // Kapasiteyi önceden ayarla
            data.points = new List<Point3D>((int)(msg.Width * msg.Length));

            for (int r = 0; r < msg.Length; r++)
            {
                for (int c = 0; c < msg.Width; c++)
                {
                    short val = msg.Ranges[r, c];
                    if (val != short.MinValue)
                    {
                        data.points.Add(new Point3D
                        {
                            x = -(msg.Offset.X + msg.Resolution.X * c), // X eksenini ters çevir (aynalama düzeltme)
                            y = msg.Offset.Y + msg.Resolution.Y * r,
                            z = msg.Offset.Z + msg.Resolution.Z * val,
                            intensity = (msg.Intensities != null && r < msg.IntensityLength && c < msg.IntensityWidth) 
                                ? msg.Intensities[r, c] 
                                : (byte)0
                        });
                    }
                }
            }

            return JsonConvert.SerializeObject(data, new JsonSerializerSettings 
            { 
                NullValueHandling = NullValueHandling.Ignore 
            });
        }

        private static string ProcessSurfacePointCloud(GoGdpSurfacePointCloud msg)
        {
            var data = new PointCloudData
            {
                metadata = new PointCloudMetadata
                {
                    timestamp = DateTime.Now,
                    pointCount = (int)(msg.Width * msg.Length),
                    offsetX = msg.Offset.X,
                    offsetY = msg.Offset.Y,
                    offsetZ = msg.Offset.Z,
                    resolutionX = msg.Resolution.X,
                    resolutionY = msg.Resolution.Y,
                    resolutionZ = msg.Resolution.Z,
                    width = (uint)msg.Width,
                    length = (uint)msg.Length
                }
            };

            // Kapasiteyi önceden ayarla
            data.points = new List<Point3D>((int)(msg.Width * msg.Length));

            for (int r = 0; r < msg.Length; r++)
            {
                for (int c = 0; c < msg.Width; c++)
                {
                    var p = msg.Ranges[r, c];
                    if (p.Z != short.MinValue)
                    {
                        data.points.Add(new Point3D
                        {
                            x = -(msg.Offset.X + msg.Resolution.X * p.X), // X eksenini ters çevir (aynalama düzeltme)
                            y = msg.Offset.Y + msg.Resolution.Y * p.Y,
                            z = msg.Offset.Z + msg.Resolution.Z * p.Z,
                            intensity = (msg.Intensities != null && r < msg.IntensityLength && c < msg.IntensityWidth) 
                                ? msg.Intensities[r, c] 
                                : (byte)0
                        });
                    }
                }
            }

            return JsonConvert.SerializeObject(data, new JsonSerializerSettings 
            { 
                NullValueHandling = NullValueHandling.Ignore 
            });
        }

        public class PointCloudData
        {
            [JsonProperty("metadata")]
            public PointCloudMetadata metadata { get; set; } = new();
            
            [JsonProperty("points")]
            public List<Point3D> points { get; set; } = new();
        }

        public class PointCloudMetadata
        {
            [JsonProperty("timestamp")]
            public DateTime timestamp { get; set; }
            
            [JsonProperty("pointCount")]
            public int pointCount { get; set; }
            
            [JsonProperty("offsetX")]
            public double offsetX { get; set; }
            
            [JsonProperty("offsetY")]
            public double offsetY { get; set; }
            
            [JsonProperty("offsetZ")]
            public double offsetZ { get; set; }
            
            [JsonProperty("resolutionX")]
            public double resolutionX { get; set; }
            
            [JsonProperty("resolutionY")]
            public double resolutionY { get; set; }
            
            [JsonProperty("resolutionZ")]
            public double resolutionZ { get; set; }
            
            [JsonProperty("width")]
            public uint width { get; set; }
            
            [JsonProperty("length")]
            public uint length { get; set; }
        }

        public class Point3D
        {
            [JsonProperty("x")]
            public double x { get; set; }
            
            [JsonProperty("y")]
            public double y { get; set; }
            
            [JsonProperty("z")]
            public double z { get; set; }
            
            [JsonProperty("intensity")]
            public byte intensity { get; set; }
        }
    }
   
    // BU SINIF Camera_Page.xaml.cs EN ALTINA Gelecek (Namespace içine)
    

 #endregion


}