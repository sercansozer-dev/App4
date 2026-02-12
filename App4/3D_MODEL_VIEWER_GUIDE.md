# 3D Model Viewer - Kullaným Kýlavuzu / User Guide

## Turkish / Türkçe

### Otomatik Sayfa - Ýstasyon Modelleri

#### Modeleri Ýstasyonlara Atama
1. **Klima Editörü** ? Sol menüden açýn
2. **RFID-Model Eþleþtirme** tablosýnda:
   - RFID ID seçin (RF123, RF456, vb.)
   - "MODEL DOSYASI" sütununda model dosyasýný seçin (*.glb)
   - Deðiþiklikleri kaydedin

3. **Otomatik Mod** sayfasýna gidin
4. Ýstasyonlardan herhangi birinde:
   - **"Beklenen ID" ? "Seç"** týklayýn
   - Yukarýdaki modeli atadýðýnýz RFID'yi seçin
   - 3D Model beyaz alana yüklenecek

#### Model Görüntüleme & Kontroller
- **Döndürme**: Farenizi 3D model üzerine sürükleyin
- **Yakýnlaþ/Uzaklaþ**: Fare tekerleðini kullanýn
- **Pan (Kaydýrma)**: Orta fare tuþu + sürükle
- **Otomatik Döndürme**: Modeller otomatik olarak döner

#### Sorun Giderme
- **Model görülmüyor**: 
  - Modelin Klima Editöründe düzgün atandýðýný kontrol edin
  - Ýstasyonun RFID'yi seçtiðini kontrol edin
  - F12 ile tarayýcý konsolunu açýp hata mesajlarýný kontrol edin

- **Boþ beyaz alan**:
  - 5 saniye kadar bekleyin (Three.js kütüphaneleri yükleniyor)
  - Sayfayý yenileyin (F5)

- **Model çok küçük/büyük**:
  - Fare tekerleðiyle yakýnlaþ/uzaklaþ yapýn

### Reçete Yönetimi - Model Ön Ýzlemesi

1. **Reçete Yönetimi** sayfasýna gidin
2. Reçete listesinden bir reçete seçin
3. **MODEL DETAYLARI** alanýnda:
   - "MODEL DOSYA YOLU" alaný doluysa model yüklenecek
   - 3D görüntü sað taraftaki alanda gösterilecek

4. **3D Puan Seçme**:
   - 3D modelinde týklayarak kontrol noktalarý ekleyin
   - Noktalar otomatik olarak "HEDEF NOKTALAR" listesine eklenir
   - Her nokta X, Y, Z koordinatlarýný kaydeder

### Model Dosyalarý

- **Desteklenen Format**: .glb (GLTF Binary)
- **Konum**: `App4/Utilities/Models/` klasörü
- **Maksimum Boy**: 100 MB (önerilir: <50 MB)
- **Dosya Adý**: Türkçe karakterler sýnýrlamadýr, çerçeve kullanmaktan kaçýnýn

---

## English

### Automatic Page - Station Models

#### Assigning Models to Stations
1. Open **Climate Editor** ? from left menu
2. In the **RFID-Model Matching** table:
   - Select an RFID ID (RF123, RF456, etc.)
   - In the "MODEL FILE" column, select a model file (*.glb)
   - Save changes

3. Go to **Automatic Mode** page
4. In any station:
   - Click on **"Expected ID" ? "Select"**
   - Choose the RFID you assigned a model to above
   - The 3D Model will load in the white area

#### Model Viewing & Controls
- **Rotate**: Drag your mouse over the 3D model
- **Zoom In/Out**: Use mouse wheel
- **Pan**: Middle mouse button + drag
- **Auto Rotation**: Models rotate automatically

#### Troubleshooting
- **Model not visible**:
  - Check that the model is properly assigned in Climate Editor
  - Verify the station has the RFID selected
  - Open browser console (F12) to check error messages

- **Blank white area**:
  - Wait up to 5 seconds (Three.js libraries loading)
  - Refresh the page (F5)

- **Model too small/large**:
  - Use mouse wheel to zoom in/out

### Recipe Management - Model Preview

1. Go to **Recipe Management** page
2. Select a recipe from the recipe list
3. In the **MODEL DETAILS** section:
   - If "MODEL FILE PATH" has a value, the model will load
   - The 3D preview displays on the right side

4. **Adding Target Points on 3D Model**:
   - Click on the 3D model to add control points
   - Points are automatically added to "TARGET POINTS" list
   - Each point records X, Y, Z coordinates

### Model Files

- **Supported Format**: .glb (GLTF Binary)
- **Location**: `App4/Utilities/Models/` folder
- **Maximum Size**: 100 MB (recommended: <50 MB)
- **Filename**: Supports Turkish characters, avoid special frames

---

## Debug Output Reference

### Ekran Çýktýsý / Console Output

Tarayýcý konsolunda (F12) þunlarý görebilirsiniz:

```
[Station1] THREE.js ready
[Station1] Three.js Scene Initialized
[Station1] Loading: model.glb
[Station1] URL: https://localmodels/model.glb
[Station1] Loading... 25%
[Station1] Loading... 50%
[Station1] Loading... 100%
[Station1] Model loaded successfully
[Station1] Ready for models
```

### Hata Mesajleri / Error Messages

```
? THREE.js libraries failed to load. Check CDN access.
? Model load failed: 404 Not Found
? Model processing error: Cannot add undefined object to scene
```

---

## Ýstatistikler / Statistics

| Özellik / Feature | Açýklama / Description |
|---|---|
| Desteklenen Ýstasyonlar | 3 |
| Max Concurrent Scenes | 4 (3 stations + 1 recipe preview) |
| THREE.js Version | r128 (2023 version) |
| Library Load Timeout | 5 seconds |
| Camera FOV | 45 degrees |
| Auto-Rotate Speed | 3.0 RPM |

---

## Ýletiþim / Support

Sorun yaþarsanýz / If you experience issues:
1. Tarayýcý konsolunu kontrol edin / Check browser console (F12)
2. Sayfayý yenileyin / Refresh the page (F5)
3. Uygulamayý yeniden baþlatýn / Restart the application
4. GitHub issues kýsmýnda rapor edin / Report on GitHub issues

