using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.UI; 
using Microsoft.UI.Xaml.Media;

namespace App4
{
    public sealed partial class Auto_Page
    {
        public static ObservableCollection<RfidDef> GlobalKnownRfids { get; private set; } = new();
        public ObservableCollection<RfidDef> KnownRfids => GlobalKnownRfids;

        public ObservableCollection<PlcVariable> Station1Outputs { get; set; } = new();
        public ObservableCollection<PlcVariable> Station2Outputs { get; set; } = new();
        public ObservableCollection<PlcVariable> Station3Outputs { get; set; } = new();
        public ObservableCollection<PlcVariable> Station4Outputs { get; set; } = new();

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (GlobalKnownRfids.Count == 0)
            {
                GlobalKnownRfids.Add(new RfidDef { Id = "RF123", Description = "Klima A Tipi" });
                GlobalKnownRfids.Add(new RfidDef { Id = "RF456", Description = "Klima B Tipi" });
                GlobalKnownRfids.Add(new RfidDef { Id = "RF789", Description = "Klima C Tipi" });
            }
            
            ReplaceStationsWithExtended();
            InitializeOutputVariables();
        }

        private void InitializeOutputVariables()
        {
            if (Station1Outputs.Count > 0) return;
            AddStationOutputs(Station1Outputs, 1);
            AddStationOutputs(Station2Outputs, 2);
            AddStationOutputs(Station3Outputs, 3);
            AddStationOutputs(Station4Outputs, 4);
        }

        private void AddStationOutputs(ObservableCollection<PlcVariable> outputs, int stationId)
        {
            outputs.Add(CreateVarExt($"ST{stationId}_RFID_MODE", "Mixed", $"RFID Çalýþma Modu", true, $"DB10.DBX{(stationId-1)*20}.0")); 
            outputs.Add(CreateVarExt($"ST{stationId}_RFID_TARGET", "", $"Hedef RFID", true, $"DB10.STR{(stationId-1)*20}.4"));
            outputs.Add(CreateVarExt($"ST{stationId}_ID_MATCHED", "FALSE", $"ID Eþleþti (1=OK)", true, $"DB10.DBX{(stationId-1)*20}.20"));
            outputs.Add(CreateVarExt($"ST{stationId}_PROCESS_RESULT", "0", $"Ýþlem Sonucu", true, $"DB10.DBX{(stationId-1)*20}.22"));
            outputs.Add(CreateVarExt($"ST{stationId}_CONVEYOR_PERM", "FALSE", $"Konveyör Ýzni", true, $"DB10.DBX{(stationId-1)*20}.24"));
        }

        private PlcVariable CreateVarExt(string name, string value, string description, bool isEditable, string tag)
        {
            return new PlcVariable { Name = name, Value = value, Description = description, IsEditable = isEditable, PlcTag = tag };
        }

        private void ReplaceStationsWithExtended()
        {
            for(int i = 0; i < Stations.Count; i++)
            {
                if (Stations[i] is not ExtendedStationViewModel)
                {
                    var item = Stations[i];
                    var ext = new ExtendedStationViewModel();
                    // Copy base props
                    ext.Name = item.Name;
                    ext.Description = item.Description;
                    ext.StatusTag = item.StatusTag;
                    ext.AlarmTag = item.AlarmTag;
                    ext.ModeTag = item.ModeTag;
                    ext.ProducingTag = item.ProducingTag;
                    ext.ProductionCountTag = item.ProductionCountTag;
                    ext.EfficiencyTag = item.EfficiencyTag;
                    ext.CurrentRfidTag = item.CurrentRfidTag;

                    // Fix for missing tag in Station 1 initialization
                    if (string.IsNullOrEmpty(ext.CurrentRfidTag) && i == 0)
                    {
                        ext.CurrentRfidTag = "ST1_RFID_ACT";
                    }

                    ext.AllowedRfid = item.AllowedRfid;
                    ext.CurrentRfid = item.CurrentRfid;
                    ext.Mode = item.Mode;
                    ext.IsProducing = item.IsProducing;
                    ext.HasAlarm = item.HasAlarm;
                    ext.IsRobotPresent = item.IsRobotPresent;
                    ext.ProcessStatus = item.ProcessStatus;
                    ext.ProductionCount = item.ProductionCount;
                    ext.Efficiency = item.Efficiency;

                    // Init extended props
                    ext.RfidOpMode = RfidOperationMode.Mixed;
                    if (!string.IsNullOrEmpty(ext.AllowedRfid))
                    {
                        ext.TargetRfid = ext.AllowedRfid;
                        if(ext.AllowedRfid != "") ext.RfidOpMode = RfidOperationMode.Specific;
                    }
                    
                    // Tags
                    if(!string.IsNullOrEmpty(ext.StatusTag))
                    {
                         ext.PlcTagRfidMode = ext.StatusTag.Replace("_STATUS", "_RFID_MODE");
                         ext.PlcTagTargetRfid = ext.StatusTag.Replace("_STATUS", "_RFID_TARGET");
                    }

                    // Replace in-place to avoid clearing collection
                    Stations[i] = ext;
                }
            }
        }

        private void BtnAddRfid_Click(object sender, RoutedEventArgs e)
        {
            if (TxtNewRfidId != null && !string.IsNullOrWhiteSpace(TxtNewRfidId.Text))
            {
                GlobalKnownRfids.Add(new RfidDef 
                { 
                    Id = TxtNewRfidId.Text, 
                    Description = TxtNewRfidDesc != null ? TxtNewRfidDesc.Text : "" 
                });
                
                TxtNewRfidId.Text = "";
                if(TxtNewRfidDesc != null) TxtNewRfidDesc.Text = "";
            }
        }
    }

    public class ExtendedStationViewModel : StationViewModel
    {
        public string PlcTagRfidMode { get; set; }
        public string PlcTagTargetRfid { get; set; }

        public ObservableCollection<RfidDef> RefRfids => Auto_Page.GlobalKnownRfids;
        
        public System.Collections.Generic.List<RfidOperationMode> RfidOpModes { get; } = new System.Collections.Generic.List<RfidOperationMode>
        {
             RfidOperationMode.Mixed,
             RfidOperationMode.Specific
        };

        private RfidOperationMode _rfidOpMode;
        public RfidOperationMode RfidOpMode
        {
             get => _rfidOpMode;
             set 
             { 
                 _rfidOpMode = value; 
                 OnPropertyChanged(nameof(RfidOpMode)); 
                 OnPropertyChanged(nameof(IsSpecificRfidVisible));
             }
        }

        private string _targetRfid;
        public string TargetRfid
        {
             get => _targetRfid;
             set 
             { 
                 _targetRfid = value; 
                 OnPropertyChanged(nameof(TargetRfid)); 
                 AllowedRfid = value;
             }
        }

        public Visibility IsSpecificRfidVisible => RfidOpMode == RfidOperationMode.Specific ? Visibility.Visible : Visibility.Collapsed;
    }
}
