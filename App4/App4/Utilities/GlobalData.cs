using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App4.Utilities
{
    public static class GlobalData
    {
        // Tüm uygulama boyunca tek ve ortak liste
        private static ObservableCollection<RfidDef> _knownRfids;

        public static ObservableCollection<RfidDef> KnownRfids
        {
            get
            {
                if (_knownRfids == null)
                {
                    _knownRfids = new ObservableCollection<RfidDef>();

                    // Varsayılan verileri buraya ekliyoruz (Sayfa açılmadan da veri olsun diye)
                    _knownRfids.Add(new RfidDef { Id = "RF123", Description = "Klima A Tipi" });
                    _knownRfids.Add(new RfidDef { Id = "RF456", Description = "Klima B Tipi" });
                    _knownRfids.Add(new RfidDef { Id = "RF789", Description = "Klima C Tipi" });
                }
                return _knownRfids;
            }
        }
    }
}
