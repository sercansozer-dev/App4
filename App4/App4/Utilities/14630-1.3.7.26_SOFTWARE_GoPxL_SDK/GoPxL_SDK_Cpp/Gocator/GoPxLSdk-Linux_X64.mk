ifndef verbose
	SILENT = @
endif

.PHONY: all
all: kApi GoApi GoPxLSdk AlignSensor BackupRestore ConfigureSensor ConfigureTool Discover MultilayerOutputs MultiSensorLayout ReceiveAsync ReceiveMetrics ReceiveMeasurement ReceiveProfile ReceiveString ReceiveSurface ReceiveImage Receive2dImage ReplayData SaveJob 

.PHONY: kApi
kApi: 
	$(SILENT) $(MAKE) -C ../Platform/kApi -f kApi-Linux_X64.mk

.PHONY: GoApi
GoApi: kApi 
	$(SILENT) $(MAKE) -C GoApi -f GoApi-Linux_X64.mk

.PHONY: GoPxLSdk
GoPxLSdk: GoApi 
	$(SILENT) $(MAKE) -C GoPxLSdk -f GoPxLSdk-Linux_X64.mk

.PHONY: AlignSensor
AlignSensor: GoPxLSdk 
	$(SILENT) $(MAKE) -C GoPxLSdk/samples/AlignSensor -f AlignSensor-Linux_X64.mk

.PHONY: BackupRestore
BackupRestore: GoPxLSdk 
	$(SILENT) $(MAKE) -C GoPxLSdk/samples/BackupRestore -f BackupRestore-Linux_X64.mk

.PHONY: ConfigureSensor
ConfigureSensor: GoPxLSdk 
	$(SILENT) $(MAKE) -C GoPxLSdk/samples/ConfigureSensor -f ConfigureSensor-Linux_X64.mk

.PHONY: ConfigureTool
ConfigureTool: GoPxLSdk 
	$(SILENT) $(MAKE) -C GoPxLSdk/samples/ConfigureTool -f ConfigureTool-Linux_X64.mk

.PHONY: Discover
Discover: GoPxLSdk 
	$(SILENT) $(MAKE) -C GoPxLSdk/samples/Discover -f Discover-Linux_X64.mk

.PHONY: MultilayerOutputs
MultilayerOutputs: GoPxLSdk 
	$(SILENT) $(MAKE) -C GoPxLSdk/samples/MultilayerOutputs -f MultilayerOutputs-Linux_X64.mk

.PHONY: MultiSensorLayout
MultiSensorLayout: GoPxLSdk 
	$(SILENT) $(MAKE) -C GoPxLSdk/samples/MultiSensorLayout -f MultiSensorLayout-Linux_X64.mk

.PHONY: ReceiveAsync
ReceiveAsync: GoPxLSdk 
	$(SILENT) $(MAKE) -C GoPxLSdk/samples/ReceiveAsync -f ReceiveAsync-Linux_X64.mk

.PHONY: ReceiveMetrics
ReceiveMetrics: GoPxLSdk 
	$(SILENT) $(MAKE) -C GoPxLSdk/samples/ReceiveMetrics -f ReceiveMetrics-Linux_X64.mk

.PHONY: ReceiveMeasurement
ReceiveMeasurement: GoPxLSdk 
	$(SILENT) $(MAKE) -C GoPxLSdk/samples/ReceiveMeasurement -f ReceiveMeasurement-Linux_X64.mk

.PHONY: ReceiveProfile
ReceiveProfile: GoPxLSdk 
	$(SILENT) $(MAKE) -C GoPxLSdk/samples/ReceiveProfile -f ReceiveProfile-Linux_X64.mk

.PHONY: ReceiveString
ReceiveString: GoPxLSdk 
	$(SILENT) $(MAKE) -C GoPxLSdk/samples/ReceiveString -f ReceiveString-Linux_X64.mk

.PHONY: ReceiveSurface
ReceiveSurface: GoPxLSdk 
	$(SILENT) $(MAKE) -C GoPxLSdk/samples/ReceiveSurface -f ReceiveSurface-Linux_X64.mk

.PHONY: ReceiveImage
ReceiveImage: GoPxLSdk 
	$(SILENT) $(MAKE) -C GoPxLSdk/samples/ReceiveImage -f ReceiveImage-Linux_X64.mk

.PHONY: Receive2dImage
Receive2dImage: GoPxLSdk 
	$(SILENT) $(MAKE) -C GoPxLSdk/samples/Receive2dImage -f Receive2dImage-Linux_X64.mk

.PHONY: ReplayData
ReplayData: GoPxLSdk 
	$(SILENT) $(MAKE) -C GoPxLSdk/samples/ReplayData -f ReplayData-Linux_X64.mk

.PHONY: SaveJob
SaveJob: GoPxLSdk 
	$(SILENT) $(MAKE) -C GoPxLSdk/samples/SaveJob -f SaveJob-Linux_X64.mk

.PHONY: clean
clean: kApi-clean GoApi-clean GoPxLSdk-clean AlignSensor-clean BackupRestore-clean ConfigureSensor-clean ConfigureTool-clean Discover-clean MultilayerOutputs-clean MultiSensorLayout-clean ReceiveAsync-clean ReceiveMetrics-clean ReceiveMeasurement-clean ReceiveProfile-clean ReceiveString-clean ReceiveSurface-clean ReceiveImage-clean Receive2dImage-clean ReplayData-clean SaveJob-clean 

.PHONY: kApi-clean
kApi-clean:
	$(SILENT) $(MAKE) -C ../Platform/kApi -f kApi-Linux_X64.mk clean

.PHONY: GoApi-clean
GoApi-clean:
	$(SILENT) $(MAKE) -C GoApi -f GoApi-Linux_X64.mk clean

.PHONY: GoPxLSdk-clean
GoPxLSdk-clean:
	$(SILENT) $(MAKE) -C GoPxLSdk -f GoPxLSdk-Linux_X64.mk clean

.PHONY: AlignSensor-clean
AlignSensor-clean:
	$(SILENT) $(MAKE) -C GoPxLSdk/samples/AlignSensor -f AlignSensor-Linux_X64.mk clean

.PHONY: BackupRestore-clean
BackupRestore-clean:
	$(SILENT) $(MAKE) -C GoPxLSdk/samples/BackupRestore -f BackupRestore-Linux_X64.mk clean

.PHONY: ConfigureSensor-clean
ConfigureSensor-clean:
	$(SILENT) $(MAKE) -C GoPxLSdk/samples/ConfigureSensor -f ConfigureSensor-Linux_X64.mk clean

.PHONY: ConfigureTool-clean
ConfigureTool-clean:
	$(SILENT) $(MAKE) -C GoPxLSdk/samples/ConfigureTool -f ConfigureTool-Linux_X64.mk clean

.PHONY: Discover-clean
Discover-clean:
	$(SILENT) $(MAKE) -C GoPxLSdk/samples/Discover -f Discover-Linux_X64.mk clean

.PHONY: MultilayerOutputs-clean
MultilayerOutputs-clean:
	$(SILENT) $(MAKE) -C GoPxLSdk/samples/MultilayerOutputs -f MultilayerOutputs-Linux_X64.mk clean

.PHONY: MultiSensorLayout-clean
MultiSensorLayout-clean:
	$(SILENT) $(MAKE) -C GoPxLSdk/samples/MultiSensorLayout -f MultiSensorLayout-Linux_X64.mk clean

.PHONY: ReceiveAsync-clean
ReceiveAsync-clean:
	$(SILENT) $(MAKE) -C GoPxLSdk/samples/ReceiveAsync -f ReceiveAsync-Linux_X64.mk clean

.PHONY: ReceiveMetrics-clean
ReceiveMetrics-clean:
	$(SILENT) $(MAKE) -C GoPxLSdk/samples/ReceiveMetrics -f ReceiveMetrics-Linux_X64.mk clean

.PHONY: ReceiveMeasurement-clean
ReceiveMeasurement-clean:
	$(SILENT) $(MAKE) -C GoPxLSdk/samples/ReceiveMeasurement -f ReceiveMeasurement-Linux_X64.mk clean

.PHONY: ReceiveProfile-clean
ReceiveProfile-clean:
	$(SILENT) $(MAKE) -C GoPxLSdk/samples/ReceiveProfile -f ReceiveProfile-Linux_X64.mk clean

.PHONY: ReceiveString-clean
ReceiveString-clean:
	$(SILENT) $(MAKE) -C GoPxLSdk/samples/ReceiveString -f ReceiveString-Linux_X64.mk clean

.PHONY: ReceiveSurface-clean
ReceiveSurface-clean:
	$(SILENT) $(MAKE) -C GoPxLSdk/samples/ReceiveSurface -f ReceiveSurface-Linux_X64.mk clean

.PHONY: ReceiveImage-clean
ReceiveImage-clean:
	$(SILENT) $(MAKE) -C GoPxLSdk/samples/ReceiveImage -f ReceiveImage-Linux_X64.mk clean

.PHONY: Receive2dImage-clean
Receive2dImage-clean:
	$(SILENT) $(MAKE) -C GoPxLSdk/samples/Receive2dImage -f Receive2dImage-Linux_X64.mk clean

.PHONY: ReplayData-clean
ReplayData-clean:
	$(SILENT) $(MAKE) -C GoPxLSdk/samples/ReplayData -f ReplayData-Linux_X64.mk clean

.PHONY: SaveJob-clean
SaveJob-clean:
	$(SILENT) $(MAKE) -C GoPxLSdk/samples/SaveJob -f SaveJob-Linux_X64.mk clean


