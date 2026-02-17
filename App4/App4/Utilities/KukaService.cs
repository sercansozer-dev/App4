using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;

namespace App4.Utilities
{
    public class KukaService
    {
        private static KukaService _instance;
        public static KukaService Instance => _instance ??= new KukaService();

        private TcpClient _client;
        private NetworkStream _stream;
        private bool _isRunning = false;
        
        // Settings are now Dynamic from GlobalData
        public string IpAddress => GlobalData.Robot_IpAddress;
        public int Port => GlobalData.Robot_Port;
        
        private ushort _msgId = 0;
        private System.Threading.SemaphoreSlim _lock = new System.Threading.SemaphoreSlim(1, 1);

        public bool IsConnected => _client != null && _client.Connected;

        public event Action<string> OnLog;
        public DispatcherQueue UiDispatcher { get; set; }

        private KukaService() 
        { 
            // Private constructor for Singleton
        }

        public void Start()
        {
            if (_isRunning) return;
            _isRunning = true;
            Task.Run(CommunicationLoop);
            OnLog?.Invoke("KUKA Servisi Başlatıldı.");
        }

        public void Stop()
        {
            _isRunning = false;
            Disconnect();
        }

        private void Disconnect()
        {
            try
            {
                _stream?.Close();
                _client?.Close();
                _client = null;
            }
            catch { }
        }

        private async Task CommunicationLoop()
        {
            while (_isRunning)
            {
                try
                {
                    if (!IsConnected)
                    {
                        await ConnectAsync();
                    }

                    if (IsConnected)
                    {
                        // GlobalData'daki Robot Değişkenlerini Oku (Sadece INPUT olanlar ve PlcTag dolu olanlar)
                        var variablesToRead = GlobalData.RobotInputVars
                            .Where(v => !string.IsNullOrEmpty(v.PlcTag) && v.PlcTag.Trim().Length > 0)
                            .ToList();
                        
                        foreach (var variable in variablesToRead)
                        {
                            if (!IsConnected || !_isRunning) break;

                            try
                            {
                                string val = await ReadVariableAsync(variable.PlcTag);
                                if (!string.IsNullOrEmpty(val))
                                {
                                    // UI Thread Update (Sadece değer değiştiyse)
                                    if (variable.Value != val)
                                    {
                                        DispatchToUi(() => variable.Value = val);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                OnLog?.Invoke($"⚠️ {variable.PlcTag} okunamadı: {ex.Message}");
                            }
                            
                            await Task.Delay(50); // Değişkenler arası bekleme
                        }
                    }
                }
                catch (Exception ex)
                {
                    OnLog?.Invoke($"Hata: {ex.Message}");
                    Disconnect();
                    await Task.Delay(5000); // 5 saniye bekle tekrar dene
                }

                await Task.Delay(500); // Döngü hızı - 500ms (daha yavaş)
            }
        }

        private async Task ConnectAsync()
        {
            try
            {
                string ip = IpAddress; 
                int p = Port;
                OnLog?.Invoke($"Bağlanıyor... {ip}:{p}");
                _client = new TcpClient();
                await _client.ConnectAsync(ip, p);
                _stream = _client.GetStream();
                OnLog?.Invoke("✅ KUKA Bağlantısı Başarılı!");
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"❌ Bağlantı Hatası: {ex.Message}");
                Disconnect();
                await Task.Delay(2000);
            }
        }


        public async Task<string> ReadVariableAsync(string varName)
        {
            return await SendRequestAsync(0, varName);
        }

        public async Task<bool> WriteVariableAsync(string varName, string value)
        {
            var result = await SendRequestAsync(1, varName, value);
            return result != null;
        }

        private async Task<string> SendRequestAsync(byte type, string varName, string writeValue = "")
        {
            await _lock.WaitAsync();
            try
            {
                if (_stream == null || !_stream.CanWrite) return null;

                try
                {
                    _msgId++;
                    string cleanVarName = varName;
                    
                    // Convert boolean string values to 1/0 (KukaProxy expects 1 for TRUE, 0 for FALSE)
                    string cleanValue = writeValue;
                    if (writeValue.Equals("TRUE", StringComparison.OrdinalIgnoreCase))
                        cleanValue = "1";
                    else if (writeValue.Equals("FALSE", StringComparison.OrdinalIgnoreCase))
                        cleanValue = "0";
                    
                    byte[] nameBytes = Encoding.ASCII.GetBytes(cleanVarName);
                    byte[] valueBytes = Encoding.ASCII.GetBytes(cleanValue);
                    ushort nameLen = (ushort)nameBytes.Length;
                    ushort valueLen = (ushort)valueBytes.Length;
                    
                    // KUKAVARPROXY Protocol:
                    // READ:  ID(2) + ContentLen(2) + Mode(1) + VarNameLen(2) + VarName(N)
                    // WRITE: ID(2) + ContentLen(2) + Mode(1) + VarNameLen(2) + VarName(N) + ValueLen(2) + Value(M)
                    
                    ushort contentLen;
                    if (type == 0) // Read
                    {
                        // Mode(1) + VarNameLen(2) + VarName(N)
                        contentLen = (ushort)(1 + 2 + nameLen);
                    }
                    else // Write
                    {
                        // Mode(1) + VarNameLen(2) + VarName(N) + ValueLen(2) + Value(M)
                        contentLen = (ushort)(1 + 2 + nameLen + 2 + valueLen);
                    }

                    List<byte> packet = new List<byte>();
                    
                    // ID (Big Endian)
                    packet.Add((byte)((_msgId >> 8) & 0xFF));
                    packet.Add((byte)(_msgId & 0xFF));

                    // Content Length (Big Endian)
                    packet.Add((byte)((contentLen >> 8) & 0xFF));
                    packet.Add((byte)(contentLen & 0xFF));

                    // Mode (0=Read, 1=Write)
                    packet.Add(type);

                    // Variable Name Length (Big Endian)
                    packet.Add((byte)((nameLen >> 8) & 0xFF));
                    packet.Add((byte)(nameLen & 0xFF));
                    
                    // Variable Name (ASCII)
                    packet.AddRange(nameBytes);

                    // Value (Only for write)
                    if (type == 1)
                    {
                        // Value Length (Big Endian)
                        packet.Add((byte)((valueLen >> 8) & 0xFF));
                        packet.Add((byte)(valueLen & 0xFF));
                        
                        // Value (ASCII)
                        packet.AddRange(valueBytes);
                    }

                    byte[] data = packet.ToArray();
                    
                    // Sadece yazma işlemlerini logla (okuma çok sık olduğu için loglamıyoruz)
                    if (type == 1)
                    {
                        OnLog?.Invoke($"📤 Yazma Gönderiliyor: {varName} = {cleanValue}");
                    }
                    
                    await _stream.WriteAsync(data, 0, data.Length);

                    // Response Reading
                    // Response Format: ID(2) + ContentLen(2) + Mode(1) + ValueLen(2) + Value(N) + Tail(3)
                    byte[] header = new byte[4]; // ID(2) + ContentLen(2)
                    int read = await ReadExactAsync(header, 4);
                    if (read != 4) throw new Exception($"Header incomplete: {read}/4 byte alındı");

                    ushort respId = (ushort)((header[0] << 8) | header[1]);
                    ushort respContentLen = (ushort)((header[2] << 8) | header[3]);

                    if (respContentLen > 0)
                    {
                        byte[] content = new byte[respContentLen];
                        read = await ReadExactAsync(content, respContentLen);
                        
                        // Debug log for troubleshooting
                        if (type == 0) // Sadece okuma için debug
                        {
                            OnLog?.Invoke($"🔍 [{varName}] ContentLen={respContentLen}, Raw={BitConverter.ToString(content).Replace("-", " ")}");
                        }
                        
                        if (read == respContentLen && respContentLen >= 6)
                        {
                            // Parse response: Mode(1) + ValueLen(2) + Value(N) + Tail(3)
                            byte respMode = content[0];
                            ushort respValueLen = (ushort)((content[1] << 8) | content[2]);
                            
                            // Check if we have enough bytes for value + tail
                            if (respContentLen >= 3 + respValueLen + 3)
                            {
                                string value = "";
                                if (respValueLen > 0)
                                {
                                    value = Encoding.ASCII.GetString(content, 3, respValueLen);
                                }
                                
                                // Tail bytes (last 3 bytes): 
                                // 0x00 0x01 0x01 = Success
                                // 0x00 0x01 0x00 = Variable not found
                                // 0x00 0x00 0x00 = Error
                                int tailStart = 3 + respValueLen;
                                byte t1 = content[tailStart];
                                byte t2 = content[tailStart + 1];
                                byte t3 = content[tailStart + 2];
                                
                                bool isSuccess = (t1 == 0x00 && t2 == 0x01 && t3 == 0x01);
                                
                                if (isSuccess)
                                {
                                    if (type == 0)
                                    {
                                        // Sadece başarılı okumayı logla (çok sık log olmaması için)
                                        return value;
                                    }
                                    else
                                    {
                                        OnLog?.Invoke($"✅ Yazma Başarılı: {varName} = {cleanValue}");
                                        return "OK";
                                    }
                                }
                                else
                                {
                                    // Hata detayı
                                    string errorMsg = (t2 == 0x01 && t3 == 0x00) 
                                        ? $"Değişken bulunamadı: {varName}" 
                                        : $"Robot Hatası (Tail={t1:X2}-{t2:X2}-{t3:X2})";
                                    
                                    // Sadece yazma hatalarını logla (okuma hataları çok sık olabilir)
                                    if (type == 1)
                                    {
                                        OnLog?.Invoke($"❌ {errorMsg}");
                                    }
                                    return null;
                                }
                            }
                        }
                    }
                    
                    return null; // Error
                }
                catch (Exception ex)
                {
                    OnLog?.Invoke($"❌ Protokol Hatası: {ex.Message}");
                    Disconnect();
                    return null;
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        private async Task<int> ReadExactAsync(byte[] buffer, int length)
        {
            int offset = 0;
            int remaining = length;
            var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(5)); // 5 saniye timeout
            
            try
            {
                while (remaining > 0)
                {
                    int read = await _stream.ReadAsync(buffer, offset, remaining, cts.Token);
                    if (read == 0) return offset;
                    offset += read;
                    remaining -= read;
                }
                return length;
            }
            catch (OperationCanceledException)
            {
                OnLog?.Invoke($"⏱️ Timeout: {offset}/{length} byte alındı");
                throw new Exception($"Timeout: {offset}/{length} byte alındı");
            }
        }

        private void DispatchToUi(Action action)
        {
            try
            {
                if (UiDispatcher != null) UiDispatcher.TryEnqueue(() => action());
                else
                {
                    OnLog?.Invoke("UI Dispatcher not set!");
                }
            }
            catch { }
        }
    }
}
