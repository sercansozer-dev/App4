
export K_REGENERATOR := 1
export K_X64_LINUX_CUDA_VERSION := 0

ifeq ($(OS)$(os), Windows_NT)
	XCOMPILE := 1
	OS_PREFIX := C:
	OS_SUFFIX := .exe
	PYTHON := python
	MKDIR_P := $(PYTHON) ../../Platform/scripts/Utils/kUtil.py mkdir_p
	RM_F := $(PYTHON) ../../Platform/scripts/Utils/kUtil.py rm_f
	RM_RF := $(PYTHON) ../../Platform/scripts/Utils/kUtil.py rm_rf
	CP := $(PYTHON) ../../Platform/scripts/Utils/kUtil.py cp
	KGENERATOR := $(PYTHON) ../../Platform/scripts/Utils/kGenerator.py
else
	BUILD_MACHINE := $(shell uname -m)
	ifneq ($(BUILD_MACHINE), x86_64)
		XCOMPILE := 1
	else
		XCOMPILE := 0
	endif
	PYTHON := python3
	MKDIR_P := mkdir -p
	RM_F := rm -f
	RM_RF := rm -rf
	CP := cp
	KGENERATOR := $(PYTHON) ../../Platform/scripts/Utils/kGenerator.py
endif

TARGET_TRIPLET := x86_64-linux-gnu

ifeq ($(XCOMPILE),1)
	GCC_PATH := $(OS_PREFIX)/tools/GccX64_11.4.1-p1/$(TARGET_TRIPLET)
	GCC_SYSROOT := $(GCC_PATH)/$(TARGET_TRIPLET)/libc
	GCC_PREFIX := $(GCC_PATH)/bin/$(TARGET_TRIPLET)-
endif

GNU_C_COMPILER := $(GCC_PREFIX)gcc$(GCC_SLOT_SUFFIX)$(OS_SUFFIX)
GNU_CXX_COMPILER := $(GCC_PREFIX)g++$(GCC_SLOT_SUFFIX)$(OS_SUFFIX)
GNU_LINKER := $(GCC_PREFIX)g++$(GCC_SLOT_SUFFIX)$(OS_SUFFIX)
GNU_ARCHIVER := $(GCC_PREFIX)ar$(OS_SUFFIX)
GNU_READELF := $(GCC_PREFIX)readelf$(OS_SUFFIX)

KAPPGEN := $(PYTHON) ../../Platform/scripts/Utils/kAppGen.py

# This can be overridden when debugging the build system itself.
regenerate := 1

ifndef verbose
	SILENT := @
endif

ifndef config
	config := Debug
endif

# We require GCC to be installed according to specific conventions (see manuals).
# Tool prerequisites may change between major releases; check and report.
ifeq ($(shell $(GNU_C_COMPILER) --version),)
.PHONY: gcc_err
gcc_err:
	$(error Cannot build because of missing prerequisite; please install GCC)
endif

ifeq ($(config),Debug)
	optimize := 0
	strip := 0
	wstack := 0
	TARGET := ../../lib/linux_x64d/libGoPxLSdk.so
	INTERMEDIATES := 
	OBJ_DIR := ../../build/GoPxLSdk-gnumk_linux_x64-Debug
	PREBUILD := 
	POSTBUILD := 
	COMPILER_FLAGS := -g -march=x86-64 -fpic -fvisibility=hidden
	C_FLAGS := -std=gnu99 -Wall -Wno-unused-variable -Wno-unused-parameter -Wno-unused-value -Wno-missing-braces
	CXX_FLAGS := -std=c++17 -Wall -Wfloat-conversion -Wno-psabi
	INCLUDE_DIRS := -I. -I../../Platform/kApi -I../GoApi -I../extern/json-develop/include
	DEFINES := -DK_DEBUG -DK_HOST -DK_PLUGIN -DGOPXLSDK_EMIT
	LINKER_FLAGS := -shared -Wl,-no-undefined -Wl,-rpath,'$$ORIGIN' -Wl,--hash-style=gnu
	LIB_DIRS := -L../../lib/linux_x64d
	LIBS := -Wl,--start-group -lpthread -lkApi -lGoApi -Wl,--end-group
	ifneq ($(optimize),0)
		COMPILER_FLAGS += -O$(optimize)
	endif
	ifeq ($(strip),1)
		LINKER_FLAGS += -Wl,--strip-debug
	endif
	ifeq ($(strip),2)
		LINKER_FLAGS += -Wl,--strip-all
	endif
	ifdef profile
		COMPILER_FLAGS += -pg
		LINKER_FLAGS += -pg
	endif
	ifdef coverage
		COMPILER_FLAGS += --coverage -fprofile-arcs -ftest-coverage
		LINKER_FLAGS += --coverage
		LIBS += -lgcov
	endif
	ifdef sanitize
		COMPILER_FLAGS += -fsanitize=$(sanitize)
		LINKER_FLAGS += -fsanitize=$(sanitize)
	endif
	GNU_COMPILER_FLAGS := $(COMPILER_FLAGS) -fno-gnu-unique
	ifneq ($(wstack),0)
		GNU_COMPILER_FLAGS += -Wstack-usage=$(wstack)
	endif
	OBJECTS := ../../build/GoPxLSdk-gnumk_linux_x64-Debug/Version.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/Def.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoDataSet.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpClient.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoDiscoveryClient.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoInstance.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoRequest.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoResponse.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoRestClient.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoSystem.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoTransaction.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoChannelError.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoRequestError.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoJsonError.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpBoundingBox.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpFeatureCircle.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpFeatureLine.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpFeaturePlane.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpFeaturePoint.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpImage.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpMeasurement.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpMesh.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpMsg.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpNull.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpPixelFormat.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpProfileBase.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpProfilePointCloud.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpProfileUniform.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpRendering.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpSignal.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpSpots.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpStamp.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpString.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpSurfaceBase.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpSurfacePointCloud.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpSurfaceUniform.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpTransform.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoCtrlChannelV1.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoNotificationType.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoRequestMethod.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoResponseType.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoUri.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoJson.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoJsonPointer.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoJsonIterator.cpp.o
	DEP_FILES = ../../build/GoPxLSdk-gnumk_linux_x64-Debug/Version.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/Def.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoDataSet.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpClient.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoDiscoveryClient.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoInstance.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoRequest.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoResponse.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoRestClient.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoSystem.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoTransaction.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoChannelError.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoRequestError.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoJsonError.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpBoundingBox.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpFeatureCircle.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpFeatureLine.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpFeaturePlane.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpFeaturePoint.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpImage.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpMeasurement.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpMesh.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpMsg.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpNull.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpPixelFormat.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpProfileBase.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpProfilePointCloud.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpProfileUniform.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpRendering.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpSignal.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpSpots.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpStamp.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpString.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpSurfaceBase.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpSurfacePointCloud.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpSurfaceUniform.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpTransform.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoCtrlChannelV1.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoNotificationType.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoRequestMethod.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoResponseType.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoUri.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoJson.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoJsonPointer.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoJsonIterator.cpp.d
	TARGET_DEPS = ../GoApi/../../lib/linux_x64d/libGoApi.so

endif

ifeq ($(config),Release)
	optimize := 2
	strip := 1
	wstack := 0
	TARGET := ../../lib/linux_x64/libGoPxLSdk.so
	INTERMEDIATES := 
	OBJ_DIR := ../../build/GoPxLSdk-gnumk_linux_x64-Release
	PREBUILD := 
	POSTBUILD := 
	COMPILER_FLAGS := -march=x86-64 -fpic -fvisibility=hidden
	C_FLAGS := -std=gnu99 -Wall -Wno-unused-variable -Wno-unused-parameter -Wno-unused-value -Wno-missing-braces
	CXX_FLAGS := -std=c++17 -Wall -Wfloat-conversion -Wno-psabi
	INCLUDE_DIRS := -I. -I../../Platform/kApi -I../GoApi -I../extern/json-develop/include
	DEFINES := -DK_HOST -DK_PLUGIN -DGOPXLSDK_EMIT
	LINKER_FLAGS := -shared -Wl,-no-undefined -Wl,-rpath,'$$ORIGIN' -Wl,-O1 -Wl,--hash-style=gnu
	LIB_DIRS := -L../../lib/linux_x64
	LIBS := -Wl,--start-group -lpthread -lkApi -lGoApi -Wl,--end-group
	ifneq ($(optimize),0)
		COMPILER_FLAGS += -O$(optimize)
	endif
	ifeq ($(strip),1)
		LINKER_FLAGS += -Wl,--strip-debug
	endif
	ifeq ($(strip),2)
		LINKER_FLAGS += -Wl,--strip-all
	endif
	ifdef profile
		COMPILER_FLAGS += -pg
		LINKER_FLAGS += -pg
	endif
	ifdef coverage
		COMPILER_FLAGS += --coverage -fprofile-arcs -ftest-coverage
		LINKER_FLAGS += --coverage
		LIBS += -lgcov
	endif
	ifdef sanitize
		COMPILER_FLAGS += -fsanitize=$(sanitize)
		LINKER_FLAGS += -fsanitize=$(sanitize)
	endif
	GNU_COMPILER_FLAGS := $(COMPILER_FLAGS) -fno-gnu-unique
	ifneq ($(wstack),0)
		GNU_COMPILER_FLAGS += -Wstack-usage=$(wstack)
	endif
	OBJECTS := ../../build/GoPxLSdk-gnumk_linux_x64-Release/Version.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/Def.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoDataSet.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpClient.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoDiscoveryClient.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoInstance.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoRequest.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoResponse.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoRestClient.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoSystem.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoTransaction.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoChannelError.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoRequestError.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoJsonError.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpBoundingBox.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpFeatureCircle.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpFeatureLine.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpFeaturePlane.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpFeaturePoint.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpImage.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpMeasurement.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpMesh.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpMsg.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpNull.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpPixelFormat.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpProfileBase.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpProfilePointCloud.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpProfileUniform.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpRendering.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpSignal.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpSpots.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpStamp.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpString.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpSurfaceBase.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpSurfacePointCloud.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpSurfaceUniform.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpTransform.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoCtrlChannelV1.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoNotificationType.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoRequestMethod.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoResponseType.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoUri.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoJson.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoJsonPointer.cpp.o \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoJsonIterator.cpp.o
	DEP_FILES = ../../build/GoPxLSdk-gnumk_linux_x64-Release/Version.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/Def.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoDataSet.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpClient.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoDiscoveryClient.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoInstance.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoRequest.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoResponse.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoRestClient.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoSystem.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoTransaction.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoChannelError.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoRequestError.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoJsonError.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpBoundingBox.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpFeatureCircle.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpFeatureLine.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpFeaturePlane.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpFeaturePoint.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpImage.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpMeasurement.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpMesh.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpMsg.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpNull.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpPixelFormat.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpProfileBase.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpProfilePointCloud.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpProfileUniform.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpRendering.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpSignal.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpSpots.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpStamp.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpString.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpSurfaceBase.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpSurfacePointCloud.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpSurfaceUniform.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpTransform.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoCtrlChannelV1.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoNotificationType.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoRequestMethod.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoResponseType.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoUri.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoJson.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoJsonPointer.cpp.d \
	../../build/GoPxLSdk-gnumk_linux_x64-Release/GoJsonIterator.cpp.d
	TARGET_DEPS = ../GoApi/../../lib/linux_x64/libGoApi.so

endif

.PHONY: all all-obj all-dep clean

all: $(OBJ_DIR)
	$(PREBUILD)
	$(SILENT) $(MAKE) -f GoPxLSdk-Linux_X64.mk all-dep
	$(SILENT) $(MAKE) -f GoPxLSdk-Linux_X64.mk all-obj

clean:
	$(SILENT) $(info Cleaning $(OBJ_DIR))
	$(SILENT) $(RM_RF) $(OBJ_DIR)
	$(SILENT) $(info Cleaning $(TARGET) $(INTERMEDIATES))
	$(SILENT) $(RM_F) $(TARGET) $(INTERMEDIATES)

all-obj: $(OBJ_DIR) $(TARGET)
all-dep: $(OBJ_DIR) $(DEP_FILES)

$(OBJ_DIR): GoPxLSdk-Linux_X64.mk
	$(info Cleanse $(OBJ_DIR))
	$(SILENT) $(RM_RF) $(OBJ_DIR)
	$(SILENT) $(MKDIR_P) $(OBJ_DIR)

ifeq ($(regenerate),1)
ifeq ($(MAKECMDGOALS),)
ifeq ($(MAKE_RESTARTS),)
GoPxLSdk-Linux_X64.mk: GoPxLSdk.xml ../GoPxLSdk.xml ../../Platform/scripts/Utils/Generator/GnuMk.py ../GoApi/GoApi.xml 
	$(info RegenX64 GoPxLSdk-Linux_X64.mk)
	$(SILENT) $(KGENERATOR) --writers=GnuMk_Linux_X64 --platforms=Linux_X64 --project=GoPxLSdk ../GoPxLSdk.xml
endif
endif
endif

ifeq ($(config),Debug)

$(TARGET): $(OBJECTS) $(TARGET_DEPS)
	$(SILENT) $(info LdX64 $(TARGET))
	$(SILENT) $(GNU_LINKER) $(OBJECTS) $(LINKER_FLAGS) $(LIBS) $(LIB_DIRS) -o$(TARGET)

endif

ifeq ($(config),Release)

$(TARGET): $(OBJECTS) $(TARGET_DEPS)
	$(SILENT) $(info LdX64 $(TARGET))
	$(SILENT) $(GNU_LINKER) $(OBJECTS) $(LINKER_FLAGS) $(LIBS) $(LIB_DIRS) -o$(TARGET)

endif

ifeq ($(config),Debug)

../../build/GoPxLSdk-gnumk_linux_x64-Debug/Version.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/Version.cpp.d: GoPxLSdk/Version.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/Version.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/Version.cpp.o -c GoPxLSdk/Version.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Debug/Def.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/Def.cpp.d: GoPxLSdk/Def.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/Def.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/Def.cpp.o -c GoPxLSdk/Def.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoDataSet.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoDataSet.cpp.d: GoPxLSdk/GoDataSet.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoDataSet.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoDataSet.cpp.o -c GoPxLSdk/GoDataSet.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpClient.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpClient.cpp.d: GoPxLSdk/GoGdpClient.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoGdpClient.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpClient.cpp.o -c GoPxLSdk/GoGdpClient.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoDiscoveryClient.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoDiscoveryClient.cpp.d: GoPxLSdk/GoDiscoveryClient.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoDiscoveryClient.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoDiscoveryClient.cpp.o -c GoPxLSdk/GoDiscoveryClient.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoInstance.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoInstance.cpp.d: GoPxLSdk/GoInstance.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoInstance.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoInstance.cpp.o -c GoPxLSdk/GoInstance.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoRequest.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoRequest.cpp.d: GoPxLSdk/GoRequest.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoRequest.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoRequest.cpp.o -c GoPxLSdk/GoRequest.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoResponse.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoResponse.cpp.d: GoPxLSdk/GoResponse.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoResponse.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoResponse.cpp.o -c GoPxLSdk/GoResponse.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoRestClient.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoRestClient.cpp.d: GoPxLSdk/GoRestClient.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoRestClient.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoRestClient.cpp.o -c GoPxLSdk/GoRestClient.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoSystem.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoSystem.cpp.d: GoPxLSdk/GoSystem.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoSystem.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoSystem.cpp.o -c GoPxLSdk/GoSystem.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoTransaction.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoTransaction.cpp.d: GoPxLSdk/GoTransaction.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoTransaction.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoTransaction.cpp.o -c GoPxLSdk/GoTransaction.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoChannelError.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoChannelError.cpp.d: GoPxLSdk/GoChannelError.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoChannelError.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoChannelError.cpp.o -c GoPxLSdk/GoChannelError.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoRequestError.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoRequestError.cpp.d: GoPxLSdk/GoRequestError.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoRequestError.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoRequestError.cpp.o -c GoPxLSdk/GoRequestError.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoJsonError.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoJsonError.cpp.d: GoPxLSdk/GoJsonError.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoJsonError.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoJsonError.cpp.o -c GoPxLSdk/GoJsonError.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpBoundingBox.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpBoundingBox.cpp.d: GoPxLSdk/GoGdpMsg/GoGdpBoundingBox.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoGdpMsg/GoGdpBoundingBox.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpBoundingBox.cpp.o -c GoPxLSdk/GoGdpMsg/GoGdpBoundingBox.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpFeatureCircle.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpFeatureCircle.cpp.d: GoPxLSdk/GoGdpMsg/GoGdpFeatureCircle.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoGdpMsg/GoGdpFeatureCircle.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpFeatureCircle.cpp.o -c GoPxLSdk/GoGdpMsg/GoGdpFeatureCircle.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpFeatureLine.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpFeatureLine.cpp.d: GoPxLSdk/GoGdpMsg/GoGdpFeatureLine.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoGdpMsg/GoGdpFeatureLine.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpFeatureLine.cpp.o -c GoPxLSdk/GoGdpMsg/GoGdpFeatureLine.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpFeaturePlane.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpFeaturePlane.cpp.d: GoPxLSdk/GoGdpMsg/GoGdpFeaturePlane.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoGdpMsg/GoGdpFeaturePlane.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpFeaturePlane.cpp.o -c GoPxLSdk/GoGdpMsg/GoGdpFeaturePlane.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpFeaturePoint.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpFeaturePoint.cpp.d: GoPxLSdk/GoGdpMsg/GoGdpFeaturePoint.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoGdpMsg/GoGdpFeaturePoint.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpFeaturePoint.cpp.o -c GoPxLSdk/GoGdpMsg/GoGdpFeaturePoint.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpImage.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpImage.cpp.d: GoPxLSdk/GoGdpMsg/GoGdpImage.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoGdpMsg/GoGdpImage.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpImage.cpp.o -c GoPxLSdk/GoGdpMsg/GoGdpImage.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpMeasurement.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpMeasurement.cpp.d: GoPxLSdk/GoGdpMsg/GoGdpMeasurement.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoGdpMsg/GoGdpMeasurement.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpMeasurement.cpp.o -c GoPxLSdk/GoGdpMsg/GoGdpMeasurement.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpMesh.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpMesh.cpp.d: GoPxLSdk/GoGdpMsg/GoGdpMesh.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoGdpMsg/GoGdpMesh.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpMesh.cpp.o -c GoPxLSdk/GoGdpMsg/GoGdpMesh.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpMsg.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpMsg.cpp.d: GoPxLSdk/GoGdpMsg/GoGdpMsg.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoGdpMsg/GoGdpMsg.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpMsg.cpp.o -c GoPxLSdk/GoGdpMsg/GoGdpMsg.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpNull.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpNull.cpp.d: GoPxLSdk/GoGdpMsg/GoGdpNull.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoGdpMsg/GoGdpNull.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpNull.cpp.o -c GoPxLSdk/GoGdpMsg/GoGdpNull.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpPixelFormat.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpPixelFormat.cpp.d: GoPxLSdk/GoGdpMsg/GoGdpPixelFormat.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoGdpMsg/GoGdpPixelFormat.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpPixelFormat.cpp.o -c GoPxLSdk/GoGdpMsg/GoGdpPixelFormat.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpProfileBase.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpProfileBase.cpp.d: GoPxLSdk/GoGdpMsg/GoGdpProfileBase.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoGdpMsg/GoGdpProfileBase.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpProfileBase.cpp.o -c GoPxLSdk/GoGdpMsg/GoGdpProfileBase.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpProfilePointCloud.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpProfilePointCloud.cpp.d: GoPxLSdk/GoGdpMsg/GoGdpProfilePointCloud.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoGdpMsg/GoGdpProfilePointCloud.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpProfilePointCloud.cpp.o -c GoPxLSdk/GoGdpMsg/GoGdpProfilePointCloud.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpProfileUniform.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpProfileUniform.cpp.d: GoPxLSdk/GoGdpMsg/GoGdpProfileUniform.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoGdpMsg/GoGdpProfileUniform.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpProfileUniform.cpp.o -c GoPxLSdk/GoGdpMsg/GoGdpProfileUniform.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpRendering.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpRendering.cpp.d: GoPxLSdk/GoGdpMsg/GoGdpRendering.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoGdpMsg/GoGdpRendering.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpRendering.cpp.o -c GoPxLSdk/GoGdpMsg/GoGdpRendering.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpSignal.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpSignal.cpp.d: GoPxLSdk/GoGdpMsg/GoGdpSignal.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoGdpMsg/GoGdpSignal.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpSignal.cpp.o -c GoPxLSdk/GoGdpMsg/GoGdpSignal.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpSpots.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpSpots.cpp.d: GoPxLSdk/GoGdpMsg/GoGdpSpots.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoGdpMsg/GoGdpSpots.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpSpots.cpp.o -c GoPxLSdk/GoGdpMsg/GoGdpSpots.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpStamp.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpStamp.cpp.d: GoPxLSdk/GoGdpMsg/GoGdpStamp.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoGdpMsg/GoGdpStamp.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpStamp.cpp.o -c GoPxLSdk/GoGdpMsg/GoGdpStamp.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpString.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpString.cpp.d: GoPxLSdk/GoGdpMsg/GoGdpString.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoGdpMsg/GoGdpString.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpString.cpp.o -c GoPxLSdk/GoGdpMsg/GoGdpString.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpSurfaceBase.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpSurfaceBase.cpp.d: GoPxLSdk/GoGdpMsg/GoGdpSurfaceBase.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoGdpMsg/GoGdpSurfaceBase.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpSurfaceBase.cpp.o -c GoPxLSdk/GoGdpMsg/GoGdpSurfaceBase.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpSurfacePointCloud.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpSurfacePointCloud.cpp.d: GoPxLSdk/GoGdpMsg/GoGdpSurfacePointCloud.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoGdpMsg/GoGdpSurfacePointCloud.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpSurfacePointCloud.cpp.o -c GoPxLSdk/GoGdpMsg/GoGdpSurfacePointCloud.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpSurfaceUniform.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpSurfaceUniform.cpp.d: GoPxLSdk/GoGdpMsg/GoGdpSurfaceUniform.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoGdpMsg/GoGdpSurfaceUniform.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpSurfaceUniform.cpp.o -c GoPxLSdk/GoGdpMsg/GoGdpSurfaceUniform.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpTransform.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpTransform.cpp.d: GoPxLSdk/GoGdpMsg/GoGdpTransform.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoGdpMsg/GoGdpTransform.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpTransform.cpp.o -c GoPxLSdk/GoGdpMsg/GoGdpTransform.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoCtrlChannelV1.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoCtrlChannelV1.cpp.d: GoPxLSdk/Internal/V1/GoCtrlChannelV1.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/Internal/V1/GoCtrlChannelV1.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoCtrlChannelV1.cpp.o -c GoPxLSdk/Internal/V1/GoCtrlChannelV1.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoNotificationType.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoNotificationType.cpp.d: GoPxLSdk/GoNotificationType.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoNotificationType.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoNotificationType.cpp.o -c GoPxLSdk/GoNotificationType.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoRequestMethod.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoRequestMethod.cpp.d: GoPxLSdk/GoRequestMethod.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoRequestMethod.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoRequestMethod.cpp.o -c GoPxLSdk/GoRequestMethod.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoResponseType.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoResponseType.cpp.d: GoPxLSdk/GoResponseType.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoResponseType.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoResponseType.cpp.o -c GoPxLSdk/GoResponseType.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoUri.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoUri.cpp.d: GoPxLSdk/GoUri.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoUri.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoUri.cpp.o -c GoPxLSdk/GoUri.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoJson.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoJson.cpp.d: GoPxLSdk/GoJson.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoJson.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoJson.cpp.o -c GoPxLSdk/GoJson.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoJsonPointer.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoJsonPointer.cpp.d: GoPxLSdk/GoJsonPointer.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoJsonPointer.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoJsonPointer.cpp.o -c GoPxLSdk/GoJsonPointer.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoJsonIterator.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoJsonIterator.cpp.d: GoPxLSdk/GoJsonIterator.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoJsonIterator.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoJsonIterator.cpp.o -c GoPxLSdk/GoJsonIterator.cpp -MMD -MP

endif

ifeq ($(config),Release)

../../build/GoPxLSdk-gnumk_linux_x64-Release/Version.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Release/Version.cpp.d: GoPxLSdk/Version.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/Version.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Release/Version.cpp.o -c GoPxLSdk/Version.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Release/Def.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Release/Def.cpp.d: GoPxLSdk/Def.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/Def.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Release/Def.cpp.o -c GoPxLSdk/Def.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Release/GoDataSet.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoDataSet.cpp.d: GoPxLSdk/GoDataSet.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoDataSet.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoDataSet.cpp.o -c GoPxLSdk/GoDataSet.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpClient.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpClient.cpp.d: GoPxLSdk/GoGdpClient.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoGdpClient.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpClient.cpp.o -c GoPxLSdk/GoGdpClient.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Release/GoDiscoveryClient.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoDiscoveryClient.cpp.d: GoPxLSdk/GoDiscoveryClient.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoDiscoveryClient.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoDiscoveryClient.cpp.o -c GoPxLSdk/GoDiscoveryClient.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Release/GoInstance.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoInstance.cpp.d: GoPxLSdk/GoInstance.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoInstance.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoInstance.cpp.o -c GoPxLSdk/GoInstance.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Release/GoRequest.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoRequest.cpp.d: GoPxLSdk/GoRequest.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoRequest.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoRequest.cpp.o -c GoPxLSdk/GoRequest.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Release/GoResponse.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoResponse.cpp.d: GoPxLSdk/GoResponse.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoResponse.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoResponse.cpp.o -c GoPxLSdk/GoResponse.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Release/GoRestClient.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoRestClient.cpp.d: GoPxLSdk/GoRestClient.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoRestClient.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoRestClient.cpp.o -c GoPxLSdk/GoRestClient.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Release/GoSystem.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoSystem.cpp.d: GoPxLSdk/GoSystem.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoSystem.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoSystem.cpp.o -c GoPxLSdk/GoSystem.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Release/GoTransaction.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoTransaction.cpp.d: GoPxLSdk/GoTransaction.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoTransaction.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoTransaction.cpp.o -c GoPxLSdk/GoTransaction.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Release/GoChannelError.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoChannelError.cpp.d: GoPxLSdk/GoChannelError.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoChannelError.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoChannelError.cpp.o -c GoPxLSdk/GoChannelError.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Release/GoRequestError.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoRequestError.cpp.d: GoPxLSdk/GoRequestError.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoRequestError.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoRequestError.cpp.o -c GoPxLSdk/GoRequestError.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Release/GoJsonError.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoJsonError.cpp.d: GoPxLSdk/GoJsonError.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoJsonError.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoJsonError.cpp.o -c GoPxLSdk/GoJsonError.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpBoundingBox.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpBoundingBox.cpp.d: GoPxLSdk/GoGdpMsg/GoGdpBoundingBox.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoGdpMsg/GoGdpBoundingBox.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpBoundingBox.cpp.o -c GoPxLSdk/GoGdpMsg/GoGdpBoundingBox.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpFeatureCircle.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpFeatureCircle.cpp.d: GoPxLSdk/GoGdpMsg/GoGdpFeatureCircle.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoGdpMsg/GoGdpFeatureCircle.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpFeatureCircle.cpp.o -c GoPxLSdk/GoGdpMsg/GoGdpFeatureCircle.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpFeatureLine.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpFeatureLine.cpp.d: GoPxLSdk/GoGdpMsg/GoGdpFeatureLine.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoGdpMsg/GoGdpFeatureLine.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpFeatureLine.cpp.o -c GoPxLSdk/GoGdpMsg/GoGdpFeatureLine.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpFeaturePlane.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpFeaturePlane.cpp.d: GoPxLSdk/GoGdpMsg/GoGdpFeaturePlane.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoGdpMsg/GoGdpFeaturePlane.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpFeaturePlane.cpp.o -c GoPxLSdk/GoGdpMsg/GoGdpFeaturePlane.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpFeaturePoint.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpFeaturePoint.cpp.d: GoPxLSdk/GoGdpMsg/GoGdpFeaturePoint.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoGdpMsg/GoGdpFeaturePoint.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpFeaturePoint.cpp.o -c GoPxLSdk/GoGdpMsg/GoGdpFeaturePoint.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpImage.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpImage.cpp.d: GoPxLSdk/GoGdpMsg/GoGdpImage.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoGdpMsg/GoGdpImage.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpImage.cpp.o -c GoPxLSdk/GoGdpMsg/GoGdpImage.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpMeasurement.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpMeasurement.cpp.d: GoPxLSdk/GoGdpMsg/GoGdpMeasurement.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoGdpMsg/GoGdpMeasurement.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpMeasurement.cpp.o -c GoPxLSdk/GoGdpMsg/GoGdpMeasurement.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpMesh.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpMesh.cpp.d: GoPxLSdk/GoGdpMsg/GoGdpMesh.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoGdpMsg/GoGdpMesh.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpMesh.cpp.o -c GoPxLSdk/GoGdpMsg/GoGdpMesh.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpMsg.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpMsg.cpp.d: GoPxLSdk/GoGdpMsg/GoGdpMsg.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoGdpMsg/GoGdpMsg.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpMsg.cpp.o -c GoPxLSdk/GoGdpMsg/GoGdpMsg.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpNull.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpNull.cpp.d: GoPxLSdk/GoGdpMsg/GoGdpNull.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoGdpMsg/GoGdpNull.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpNull.cpp.o -c GoPxLSdk/GoGdpMsg/GoGdpNull.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpPixelFormat.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpPixelFormat.cpp.d: GoPxLSdk/GoGdpMsg/GoGdpPixelFormat.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoGdpMsg/GoGdpPixelFormat.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpPixelFormat.cpp.o -c GoPxLSdk/GoGdpMsg/GoGdpPixelFormat.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpProfileBase.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpProfileBase.cpp.d: GoPxLSdk/GoGdpMsg/GoGdpProfileBase.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoGdpMsg/GoGdpProfileBase.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpProfileBase.cpp.o -c GoPxLSdk/GoGdpMsg/GoGdpProfileBase.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpProfilePointCloud.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpProfilePointCloud.cpp.d: GoPxLSdk/GoGdpMsg/GoGdpProfilePointCloud.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoGdpMsg/GoGdpProfilePointCloud.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpProfilePointCloud.cpp.o -c GoPxLSdk/GoGdpMsg/GoGdpProfilePointCloud.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpProfileUniform.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpProfileUniform.cpp.d: GoPxLSdk/GoGdpMsg/GoGdpProfileUniform.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoGdpMsg/GoGdpProfileUniform.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpProfileUniform.cpp.o -c GoPxLSdk/GoGdpMsg/GoGdpProfileUniform.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpRendering.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpRendering.cpp.d: GoPxLSdk/GoGdpMsg/GoGdpRendering.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoGdpMsg/GoGdpRendering.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpRendering.cpp.o -c GoPxLSdk/GoGdpMsg/GoGdpRendering.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpSignal.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpSignal.cpp.d: GoPxLSdk/GoGdpMsg/GoGdpSignal.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoGdpMsg/GoGdpSignal.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpSignal.cpp.o -c GoPxLSdk/GoGdpMsg/GoGdpSignal.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpSpots.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpSpots.cpp.d: GoPxLSdk/GoGdpMsg/GoGdpSpots.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoGdpMsg/GoGdpSpots.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpSpots.cpp.o -c GoPxLSdk/GoGdpMsg/GoGdpSpots.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpStamp.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpStamp.cpp.d: GoPxLSdk/GoGdpMsg/GoGdpStamp.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoGdpMsg/GoGdpStamp.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpStamp.cpp.o -c GoPxLSdk/GoGdpMsg/GoGdpStamp.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpString.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpString.cpp.d: GoPxLSdk/GoGdpMsg/GoGdpString.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoGdpMsg/GoGdpString.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpString.cpp.o -c GoPxLSdk/GoGdpMsg/GoGdpString.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpSurfaceBase.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpSurfaceBase.cpp.d: GoPxLSdk/GoGdpMsg/GoGdpSurfaceBase.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoGdpMsg/GoGdpSurfaceBase.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpSurfaceBase.cpp.o -c GoPxLSdk/GoGdpMsg/GoGdpSurfaceBase.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpSurfacePointCloud.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpSurfacePointCloud.cpp.d: GoPxLSdk/GoGdpMsg/GoGdpSurfacePointCloud.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoGdpMsg/GoGdpSurfacePointCloud.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpSurfacePointCloud.cpp.o -c GoPxLSdk/GoGdpMsg/GoGdpSurfacePointCloud.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpSurfaceUniform.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpSurfaceUniform.cpp.d: GoPxLSdk/GoGdpMsg/GoGdpSurfaceUniform.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoGdpMsg/GoGdpSurfaceUniform.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpSurfaceUniform.cpp.o -c GoPxLSdk/GoGdpMsg/GoGdpSurfaceUniform.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpTransform.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpTransform.cpp.d: GoPxLSdk/GoGdpMsg/GoGdpTransform.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoGdpMsg/GoGdpTransform.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpTransform.cpp.o -c GoPxLSdk/GoGdpMsg/GoGdpTransform.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Release/GoCtrlChannelV1.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoCtrlChannelV1.cpp.d: GoPxLSdk/Internal/V1/GoCtrlChannelV1.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/Internal/V1/GoCtrlChannelV1.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoCtrlChannelV1.cpp.o -c GoPxLSdk/Internal/V1/GoCtrlChannelV1.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Release/GoNotificationType.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoNotificationType.cpp.d: GoPxLSdk/GoNotificationType.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoNotificationType.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoNotificationType.cpp.o -c GoPxLSdk/GoNotificationType.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Release/GoRequestMethod.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoRequestMethod.cpp.d: GoPxLSdk/GoRequestMethod.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoRequestMethod.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoRequestMethod.cpp.o -c GoPxLSdk/GoRequestMethod.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Release/GoResponseType.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoResponseType.cpp.d: GoPxLSdk/GoResponseType.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoResponseType.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoResponseType.cpp.o -c GoPxLSdk/GoResponseType.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Release/GoUri.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoUri.cpp.d: GoPxLSdk/GoUri.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoUri.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoUri.cpp.o -c GoPxLSdk/GoUri.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Release/GoJson.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoJson.cpp.d: GoPxLSdk/GoJson.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoJson.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoJson.cpp.o -c GoPxLSdk/GoJson.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Release/GoJsonPointer.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoJsonPointer.cpp.d: GoPxLSdk/GoJsonPointer.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoJsonPointer.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoJsonPointer.cpp.o -c GoPxLSdk/GoJsonPointer.cpp -MMD -MP

../../build/GoPxLSdk-gnumk_linux_x64-Release/GoJsonIterator.cpp.o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoJsonIterator.cpp.d: GoPxLSdk/GoJsonIterator.cpp
	$(SILENT) $(info GccX64 GoPxLSdk/GoJsonIterator.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoJsonIterator.cpp.o -c GoPxLSdk/GoJsonIterator.cpp -MMD -MP

endif

ifeq ($(MAKECMDGOALS),all-obj)

ifeq ($(config),Debug)

include ../../build/GoPxLSdk-gnumk_linux_x64-Debug/Version.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Debug/Def.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoDataSet.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpClient.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoDiscoveryClient.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoInstance.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoRequest.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoResponse.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoRestClient.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoSystem.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoTransaction.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoChannelError.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoRequestError.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoJsonError.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpBoundingBox.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpFeatureCircle.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpFeatureLine.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpFeaturePlane.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpFeaturePoint.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpImage.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpMeasurement.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpMesh.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpMsg.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpNull.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpPixelFormat.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpProfileBase.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpProfilePointCloud.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpProfileUniform.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpRendering.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpSignal.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpSpots.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpStamp.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpString.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpSurfaceBase.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpSurfacePointCloud.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpSurfaceUniform.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoGdpTransform.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoCtrlChannelV1.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoNotificationType.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoRequestMethod.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoResponseType.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoUri.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoJson.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoJsonPointer.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Debug/GoJsonIterator.cpp.d

endif

ifeq ($(config),Release)

include ../../build/GoPxLSdk-gnumk_linux_x64-Release/Version.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Release/Def.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoDataSet.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpClient.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoDiscoveryClient.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoInstance.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoRequest.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoResponse.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoRestClient.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoSystem.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoTransaction.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoChannelError.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoRequestError.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoJsonError.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpBoundingBox.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpFeatureCircle.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpFeatureLine.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpFeaturePlane.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpFeaturePoint.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpImage.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpMeasurement.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpMesh.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpMsg.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpNull.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpPixelFormat.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpProfileBase.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpProfilePointCloud.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpProfileUniform.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpRendering.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpSignal.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpSpots.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpStamp.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpString.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpSurfaceBase.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpSurfacePointCloud.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpSurfaceUniform.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoGdpTransform.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoCtrlChannelV1.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoNotificationType.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoRequestMethod.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoResponseType.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoUri.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoJson.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoJsonPointer.cpp.d
include ../../build/GoPxLSdk-gnumk_linux_x64-Release/GoJsonIterator.cpp.d

endif

endif

