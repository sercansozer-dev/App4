# 3D Model Viewer - Debug & Testing Guide

## ? What Was Fixed

1. **Added THREE.js Library Loading Verification**
   - Waits up to 10 seconds for THREE.js, OrbitControls, and GLTFLoader to load
   - Shows clear error if libraries fail to load

2. **Improved HTML Error Handling**
   - On-screen debug messages (top-left corner)
   - Loading percentage display
   - Better error messages

3. **Preserved Model Library Functionality**
   - `LibraryModelList_SelectionChanged()` - Works when clicking model in library list
   - `Update3DPreview()` - Loads and displays models
   - `BtnBrowseFolder_Click()` - Allows uploading new models

4. **Better Path Resolution**
   - Searches Models folder for files
   - Supports nested folders
   - Works with file:// protocol

## ?? How to Debug

### Step 1: Open DevTools
- Press **F12** while in the app
- Go to **Console** tab

### Step 2: Test Recipe Page
1. Go to "**Reçete Yönetimi**" (Recipe Management)
2. In "**MODEL KÜTÜPHANESI**" (Model Library) list, click any model file
3. Check **Console** for messages like:
   ```
   [RecipeViewer] THREE.js loaded
   [RecipeViewer] Scene initialized
   [RecipeViewer] Loading: file:///C:/Users/.../model.glb
   [RecipeViewer] Loading... 50%
   [RecipeViewer] Model loaded
   ```

### Step 3: Test Automatic Mode  
1. Go to "**Otomatik Mod**" (Automatic Mode)
2. In any station, select an RFID with assigned model in "Beklenen ID"
3. Check **Console** for similar messages:
   ```
   [Station] THREE.js ready
   [Station] Scene initialized
   [Station] Loading: file:///C:/...
   [Station] Model loaded
   ```

### Step 4: Check Debug Display
- Look at **top-left corner** of each viewer
- Should show status like "Ready", "Loading...", or error message

## ?? Common Issues & Solutions

### Issue: "THREE.js kütüphaneleri yüklenemedi" (Libraries didn't load)

**Cause**: CDN is unreachable or takes too long

**Solutions**:
1. Check internet connection
2. Wait 10 seconds
3. Refresh page (F5)
4. Check firewall settings
5. Try another network

### Issue: No Model Appears (but no error)

**Cause**: Model file path is wrong

**Debug Steps**:
1. Check Console for file path
2. Copy the `file:///` URL from console
3. Paste in address bar to verify file exists
4. Check that model file is in `Utilities/Models/` folder

### Issue: Console Shows Error in loadModel()

**Common Errors:**
- `undefined is not a function` - THREE.js not loaded yet
- `404 Not Found` - Model file doesn't exist
- `CORS error` - Permissions issue (shouldn't happen with file://)

### Issue: Model Loads but Doesn't Display

**Cause**: Model centering or camera distance wrong

**Solution**: Use mouse wheel to zoom in/out

## ? Testing Checklist

### Recipe Page
- [ ] Add model file using "Browse Folder" button
- [ ] Click model in library list
- [ ] Model appears in preview
- [ ] Can rotate model with mouse
- [ ] "Loading..." indicator appears briefly
- [ ] No errors in console

### Automatic Mode - Station Setup
- [ ] Go to Climate Editor
- [ ] Assign model to RFID RF123 (example)
- [ ] Save settings
- [ ] Go to Automatic Mode
- [ ] Select RF123 in station's "Expected ID"
- [ ] Model appears in white area
- [ ] Can rotate model
- [ ] No errors in console

### Error Handling
- [ ] Select RFID with no model assigned - viewer stays blank (OK)
- [ ] Delete model file - shows error or blank (OK)
- [ ] Disconnect internet - shows CDN error after 10 sec (expected)
- [ ] Refresh page (F5) - HTML reloads and works again

## ?? File Paths to Check

Make sure these exist:

```
C:\Users\Simbiosis\source\repos\App4\App4\App4\
??? Utilities\
?   ??? Models\              ? Model files go here
?       ??? model.glb
?       ??? cad04778-stylish-bml-h-outdoor-top.glb
?       ??? ... other models
??? PAGES\
    ??? Auto_Page.xaml.cs
    ??? Recipes_Page.xaml.cs
```

## ?? How Files Are Loaded Now

### Recipe Page:
```
User clicks model in list
     ?
LibraryModelList_SelectionChanged()
     ?
Gets file path from library
     ?
Calls Update3DPreview(path)
     ?
Converts to file:/// URL
     ?
Executes window.loadModel(fileUri)
     ?
HTML's loadModel() function called
     ?
THREE.js GLTFLoader loads file
     ?
Model renders
```

### Station Viewer:
```
User selects RFID
     ?
UpdateStationModel() called
     ?
Gets model filename from RFID
     ?
Converts to file:/// URL
     ?
Executes window.loadModel(fileUri)
     ?
Same as above...
```

## ?? Advanced Debugging

### Enable More Logging

In Browser Console, paste:

```javascript
// Show all THREE.js initialization
console.log = function(msg) {
    if (msg.includes('Station') || msg.includes('Recipe')) {
        document.body.innerHTML += '<p style="color: cyan; font-size: 10px; margin: 0;">' + msg + '</p>';
    }
};
```

### Test Model Loading Directly

In Browser Console:

```javascript
// Test if libraries loaded
console.log('THREE:', typeof THREE);
console.log('GLTFLoader:', typeof THREE.GLTFLoader);
console.log('OrbitControls:', typeof THREE.OrbitControls);

// Manual test
if (window.loadModel) {
    window.loadModel('file:///C:/Users/Simbiosis/source/repos/App4/App4/App4/Utilities/Models/your-model.glb');
}
```

## ?? Troubleshooting Flowchart

```
Model not loading?
?
?? Check Console (F12)
?  ?? No messages? ? Wait 10 seconds, check network
?  ?? "THREE.js ready"? ? Good, check next
?  ?? Red errors? ? Read error message
?
?? Check Debug Display (top-left)
?  ?? "Ready"? ? loadModel function is ready
?  ?? "Loading..."? ? File is loading, wait
?  ?? Error message? ? File not found or format wrong
?
?? Check File Path
?  ?? Does file exist? ? Open File Explorer
?  ?? Is it .glb format? ? Check extension
?  ?? Is path correct? ? Copy from console
?
?? Still not working?
   ?? Refresh page (F5)
   ?? Restart application
   ?? Check model file integrity
```

## ? Expected Behavior

### First Load (5-10 seconds)
1. Page appears with white area
2. Console shows "THREE.js loaded"
3. Console shows "Scene initialized"
4. On-screen shows "Ready"

### When Selecting Model (1-5 seconds)
1. On-screen shows "Loading..."
2. Console shows percentage: "Loading... 25%, 50%, 100%"
3. Model appears and rotates
4. On-screen shows "Ready"

### If Error Occurs
1. On-screen shows red error box with message
2. Console shows error details
3. White area stays visible (clean state)

---

**Version**: 3.0 (Fixed with Debug Support)
**Last Updated**: 2024
**Status**: ? Ready for Testing

If you still have issues, the Console (F12) will show exactly what went wrong!

