
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
	TARGET := ../../lib/linux_x64d/libGoApi.so
	INTERMEDIATES := 
	OBJ_DIR := ../../build/GoApi-gnumk_linux_x64-Debug
	PREBUILD := 
	POSTBUILD := 
	COMPILER_FLAGS := -g -march=x86-64 -fpic -fvisibility=hidden
	C_FLAGS := -std=gnu99 -Wall -Wno-unused-variable -Wno-unused-parameter -Wno-unused-value -Wno-missing-braces
	CXX_FLAGS := -std=c++17 -Wall -Wfloat-conversion -Wno-psabi
	INCLUDE_DIRS := -I. -I../GoApi -I../../Platform/kApi -I../../Platform/kFireSync -I../../Platform/kVision -I../extern/json-develop/include
	DEFINES := -DK_DEBUG -DGOAPI_EXPORT
	LINKER_FLAGS := -shared -Wl,-no-undefined -Wl,-rpath,'$$ORIGIN' -Wl,--hash-style=gnu
	LIB_DIRS := -L../../lib/linux_x64d
	LIBS := -Wl,--start-group -lkApi -Wl,--end-group
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
	OBJECTS := ../../build/GoApi-gnumk_linux_x64-Debug/GoApi.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Debug/GoApiLib.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Debug/Profiling.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Debug/Exception.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Debug/Logging.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Debug/MemoryLeakDetection.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Debug/EventType.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Debug/Path.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Debug/PathExpressions.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Debug/ReqRespInfo.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Debug/ResourceBase.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Debug/ResourceRouter.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Debug/GoGetText.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Debug/Catalog.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Debug/AsyncLoop.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Debug/AsyncNotifier.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Debug/AsyncRunner.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Debug/AsyncTimer.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Debug/AsyncUdpSocket.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Debug/Node.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Debug/Structure.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Debug/ArrayBase.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Debug/TypeInfo.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Debug/NodeType.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Debug/JsonSerializer.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Debug/XmlSerializer.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Debug/GoDataTreeSerializer.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Debug/Url.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Debug/PointerWrapper.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Debug/BeginEndUpdater.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Debug/ScopedUpdater.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Debug/CoordUtils.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Debug/MemUtils.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Debug/GoDataTree.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Debug/GoDataTreeSerializerInputIterator.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Debug/GoDataTreeSerializerOutputWriter.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Debug/GoJsonSerializer.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Debug/GoMsgPackSerializer.cpp.o
	DEP_FILES = ../../build/GoApi-gnumk_linux_x64-Debug/GoApi.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Debug/GoApiLib.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Debug/Profiling.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Debug/Exception.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Debug/Logging.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Debug/MemoryLeakDetection.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Debug/EventType.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Debug/Path.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Debug/PathExpressions.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Debug/ReqRespInfo.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Debug/ResourceBase.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Debug/ResourceRouter.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Debug/GoGetText.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Debug/Catalog.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Debug/AsyncLoop.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Debug/AsyncNotifier.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Debug/AsyncRunner.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Debug/AsyncTimer.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Debug/AsyncUdpSocket.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Debug/Node.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Debug/Structure.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Debug/ArrayBase.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Debug/TypeInfo.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Debug/NodeType.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Debug/JsonSerializer.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Debug/XmlSerializer.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Debug/GoDataTreeSerializer.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Debug/Url.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Debug/PointerWrapper.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Debug/BeginEndUpdater.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Debug/ScopedUpdater.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Debug/CoordUtils.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Debug/MemUtils.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Debug/GoDataTree.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Debug/GoDataTreeSerializerInputIterator.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Debug/GoDataTreeSerializerOutputWriter.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Debug/GoJsonSerializer.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Debug/GoMsgPackSerializer.cpp.d
	TARGET_DEPS = ../../Platform/kApi/../../lib/linux_x64d/libkApi.so

endif

ifeq ($(config),Release)
	optimize := 2
	strip := 1
	wstack := 0
	TARGET := ../../lib/linux_x64/libGoApi.so
	INTERMEDIATES := 
	OBJ_DIR := ../../build/GoApi-gnumk_linux_x64-Release
	PREBUILD := 
	POSTBUILD := 
	COMPILER_FLAGS := -march=x86-64 -fpic -fvisibility=hidden
	C_FLAGS := -std=gnu99 -Wall -Wno-unused-variable -Wno-unused-parameter -Wno-unused-value -Wno-missing-braces
	CXX_FLAGS := -std=c++17 -Wall -Wfloat-conversion -Wno-psabi
	INCLUDE_DIRS := -I. -I../GoApi -I../../Platform/kApi -I../../Platform/kFireSync -I../../Platform/kVision -I../extern/json-develop/include
	DEFINES := -DGOAPI_EXPORT
	LINKER_FLAGS := -shared -Wl,-no-undefined -Wl,-rpath,'$$ORIGIN' -Wl,-O1 -Wl,--hash-style=gnu
	LIB_DIRS := -L../../lib/linux_x64
	LIBS := -Wl,--start-group -lkApi -Wl,--end-group
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
	OBJECTS := ../../build/GoApi-gnumk_linux_x64-Release/GoApi.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Release/GoApiLib.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Release/Profiling.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Release/Exception.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Release/Logging.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Release/MemoryLeakDetection.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Release/EventType.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Release/Path.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Release/PathExpressions.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Release/ReqRespInfo.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Release/ResourceBase.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Release/ResourceRouter.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Release/GoGetText.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Release/Catalog.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Release/AsyncLoop.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Release/AsyncNotifier.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Release/AsyncRunner.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Release/AsyncTimer.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Release/AsyncUdpSocket.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Release/Node.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Release/Structure.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Release/ArrayBase.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Release/TypeInfo.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Release/NodeType.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Release/JsonSerializer.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Release/XmlSerializer.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Release/GoDataTreeSerializer.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Release/Url.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Release/PointerWrapper.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Release/BeginEndUpdater.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Release/ScopedUpdater.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Release/CoordUtils.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Release/MemUtils.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Release/GoDataTree.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Release/GoDataTreeSerializerInputIterator.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Release/GoDataTreeSerializerOutputWriter.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Release/GoJsonSerializer.cpp.o \
	../../build/GoApi-gnumk_linux_x64-Release/GoMsgPackSerializer.cpp.o
	DEP_FILES = ../../build/GoApi-gnumk_linux_x64-Release/GoApi.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Release/GoApiLib.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Release/Profiling.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Release/Exception.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Release/Logging.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Release/MemoryLeakDetection.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Release/EventType.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Release/Path.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Release/PathExpressions.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Release/ReqRespInfo.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Release/ResourceBase.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Release/ResourceRouter.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Release/GoGetText.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Release/Catalog.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Release/AsyncLoop.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Release/AsyncNotifier.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Release/AsyncRunner.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Release/AsyncTimer.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Release/AsyncUdpSocket.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Release/Node.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Release/Structure.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Release/ArrayBase.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Release/TypeInfo.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Release/NodeType.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Release/JsonSerializer.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Release/XmlSerializer.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Release/GoDataTreeSerializer.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Release/Url.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Release/PointerWrapper.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Release/BeginEndUpdater.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Release/ScopedUpdater.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Release/CoordUtils.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Release/MemUtils.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Release/GoDataTree.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Release/GoDataTreeSerializerInputIterator.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Release/GoDataTreeSerializerOutputWriter.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Release/GoJsonSerializer.cpp.d \
	../../build/GoApi-gnumk_linux_x64-Release/GoMsgPackSerializer.cpp.d
	TARGET_DEPS = ../../Platform/kApi/../../lib/linux_x64/libkApi.so

endif

.PHONY: all all-obj all-dep clean

all: $(OBJ_DIR)
	$(PREBUILD)
	$(SILENT) $(MAKE) -f GoApi-Linux_X64.mk all-dep
	$(SILENT) $(MAKE) -f GoApi-Linux_X64.mk all-obj

clean:
	$(SILENT) $(info Cleaning $(OBJ_DIR))
	$(SILENT) $(RM_RF) $(OBJ_DIR)
	$(SILENT) $(info Cleaning $(TARGET) $(INTERMEDIATES))
	$(SILENT) $(RM_F) $(TARGET) $(INTERMEDIATES)

all-obj: $(OBJ_DIR) $(TARGET)
all-dep: $(OBJ_DIR) $(DEP_FILES)

$(OBJ_DIR): GoApi-Linux_X64.mk
	$(info Cleanse $(OBJ_DIR))
	$(SILENT) $(RM_RF) $(OBJ_DIR)
	$(SILENT) $(MKDIR_P) $(OBJ_DIR)

ifeq ($(regenerate),1)
ifeq ($(MAKECMDGOALS),)
ifeq ($(MAKE_RESTARTS),)
GoApi-Linux_X64.mk: GoApi.xml ../GoPxLSdk.xml ../../Platform/scripts/Utils/Generator/GnuMk.py ../../Platform/kApi/kApi.xml 
	$(info RegenX64 GoApi-Linux_X64.mk)
	$(SILENT) $(KGENERATOR) --writers=GnuMk_Linux_X64 --platforms=Linux_X64 --project=GoApi ../GoPxLSdk.xml
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

../../build/GoApi-gnumk_linux_x64-Debug/GoApi.cpp.o ../../build/GoApi-gnumk_linux_x64-Debug/GoApi.cpp.d: GoApi/GoApi.cpp
	$(SILENT) $(info GccX64 GoApi/GoApi.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Debug/GoApi.cpp.o -c GoApi/GoApi.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Debug/GoApiLib.cpp.o ../../build/GoApi-gnumk_linux_x64-Debug/GoApiLib.cpp.d: GoApi/GoApiLib.cpp
	$(SILENT) $(info GccX64 GoApi/GoApiLib.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Debug/GoApiLib.cpp.o -c GoApi/GoApiLib.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Debug/Profiling.cpp.o ../../build/GoApi-gnumk_linux_x64-Debug/Profiling.cpp.d: GoApi/Profiling/Profiling.cpp
	$(SILENT) $(info GccX64 GoApi/Profiling/Profiling.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Debug/Profiling.cpp.o -c GoApi/Profiling/Profiling.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Debug/Exception.cpp.o ../../build/GoApi-gnumk_linux_x64-Debug/Exception.cpp.d: GoApi/Exception.cpp
	$(SILENT) $(info GccX64 GoApi/Exception.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Debug/Exception.cpp.o -c GoApi/Exception.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Debug/Logging.cpp.o ../../build/GoApi-gnumk_linux_x64-Debug/Logging.cpp.d: GoApi/Logging.cpp
	$(SILENT) $(info GccX64 GoApi/Logging.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Debug/Logging.cpp.o -c GoApi/Logging.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Debug/MemoryLeakDetection.cpp.o ../../build/GoApi-gnumk_linux_x64-Debug/MemoryLeakDetection.cpp.d: GoApi/MemoryLeakDetection.cpp
	$(SILENT) $(info GccX64 GoApi/MemoryLeakDetection.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Debug/MemoryLeakDetection.cpp.o -c GoApi/MemoryLeakDetection.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Debug/EventType.cpp.o ../../build/GoApi-gnumk_linux_x64-Debug/EventType.cpp.d: GoApi/Resource/EventType.cpp
	$(SILENT) $(info GccX64 GoApi/Resource/EventType.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Debug/EventType.cpp.o -c GoApi/Resource/EventType.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Debug/Path.cpp.o ../../build/GoApi-gnumk_linux_x64-Debug/Path.cpp.d: GoApi/Resource/Path.cpp
	$(SILENT) $(info GccX64 GoApi/Resource/Path.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Debug/Path.cpp.o -c GoApi/Resource/Path.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Debug/PathExpressions.cpp.o ../../build/GoApi-gnumk_linux_x64-Debug/PathExpressions.cpp.d: GoApi/Resource/PathExpressions.cpp
	$(SILENT) $(info GccX64 GoApi/Resource/PathExpressions.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Debug/PathExpressions.cpp.o -c GoApi/Resource/PathExpressions.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Debug/ReqRespInfo.cpp.o ../../build/GoApi-gnumk_linux_x64-Debug/ReqRespInfo.cpp.d: GoApi/Resource/ReqRespInfo.cpp
	$(SILENT) $(info GccX64 GoApi/Resource/ReqRespInfo.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Debug/ReqRespInfo.cpp.o -c GoApi/Resource/ReqRespInfo.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Debug/ResourceBase.cpp.o ../../build/GoApi-gnumk_linux_x64-Debug/ResourceBase.cpp.d: GoApi/Resource/ResourceBase.cpp
	$(SILENT) $(info GccX64 GoApi/Resource/ResourceBase.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Debug/ResourceBase.cpp.o -c GoApi/Resource/ResourceBase.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Debug/ResourceRouter.cpp.o ../../build/GoApi-gnumk_linux_x64-Debug/ResourceRouter.cpp.d: GoApi/Resource/ResourceRouter.cpp
	$(SILENT) $(info GccX64 GoApi/Resource/ResourceRouter.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Debug/ResourceRouter.cpp.o -c GoApi/Resource/ResourceRouter.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Debug/GoGetText.cpp.o ../../build/GoApi-gnumk_linux_x64-Debug/GoGetText.cpp.d: GoApi/Translations/GoGetText.cpp
	$(SILENT) $(info GccX64 GoApi/Translations/GoGetText.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Debug/GoGetText.cpp.o -c GoApi/Translations/GoGetText.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Debug/Catalog.cpp.o ../../build/GoApi-gnumk_linux_x64-Debug/Catalog.cpp.d: GoApi/Translations/Catalog.cpp
	$(SILENT) $(info GccX64 GoApi/Translations/Catalog.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Debug/Catalog.cpp.o -c GoApi/Translations/Catalog.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Debug/AsyncLoop.cpp.o ../../build/GoApi-gnumk_linux_x64-Debug/AsyncLoop.cpp.d: GoApi/Async/AsyncLoop.cpp
	$(SILENT) $(info GccX64 GoApi/Async/AsyncLoop.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Debug/AsyncLoop.cpp.o -c GoApi/Async/AsyncLoop.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Debug/AsyncNotifier.cpp.o ../../build/GoApi-gnumk_linux_x64-Debug/AsyncNotifier.cpp.d: GoApi/Async/AsyncNotifier.cpp
	$(SILENT) $(info GccX64 GoApi/Async/AsyncNotifier.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Debug/AsyncNotifier.cpp.o -c GoApi/Async/AsyncNotifier.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Debug/AsyncRunner.cpp.o ../../build/GoApi-gnumk_linux_x64-Debug/AsyncRunner.cpp.d: GoApi/Async/AsyncRunner.cpp
	$(SILENT) $(info GccX64 GoApi/Async/AsyncRunner.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Debug/AsyncRunner.cpp.o -c GoApi/Async/AsyncRunner.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Debug/AsyncTimer.cpp.o ../../build/GoApi-gnumk_linux_x64-Debug/AsyncTimer.cpp.d: GoApi/Async/AsyncTimer.cpp
	$(SILENT) $(info GccX64 GoApi/Async/AsyncTimer.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Debug/AsyncTimer.cpp.o -c GoApi/Async/AsyncTimer.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Debug/AsyncUdpSocket.cpp.o ../../build/GoApi-gnumk_linux_x64-Debug/AsyncUdpSocket.cpp.d: GoApi/Async/AsyncUdpSocket.cpp
	$(SILENT) $(info GccX64 GoApi/Async/AsyncUdpSocket.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Debug/AsyncUdpSocket.cpp.o -c GoApi/Async/AsyncUdpSocket.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Debug/Node.cpp.o ../../build/GoApi-gnumk_linux_x64-Debug/Node.cpp.d: GoApi/Properties/Nodes/Node.cpp
	$(SILENT) $(info GccX64 GoApi/Properties/Nodes/Node.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Debug/Node.cpp.o -c GoApi/Properties/Nodes/Node.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Debug/Structure.cpp.o ../../build/GoApi-gnumk_linux_x64-Debug/Structure.cpp.d: GoApi/Properties/Nodes/Structure.cpp
	$(SILENT) $(info GccX64 GoApi/Properties/Nodes/Structure.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Debug/Structure.cpp.o -c GoApi/Properties/Nodes/Structure.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Debug/ArrayBase.cpp.o ../../build/GoApi-gnumk_linux_x64-Debug/ArrayBase.cpp.d: GoApi/Properties/Nodes/ArrayBase.cpp
	$(SILENT) $(info GccX64 GoApi/Properties/Nodes/ArrayBase.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Debug/ArrayBase.cpp.o -c GoApi/Properties/Nodes/ArrayBase.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Debug/TypeInfo.cpp.o ../../build/GoApi-gnumk_linux_x64-Debug/TypeInfo.cpp.d: GoApi/Properties/Nodes/TypeInfo.cpp
	$(SILENT) $(info GccX64 GoApi/Properties/Nodes/TypeInfo.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Debug/TypeInfo.cpp.o -c GoApi/Properties/Nodes/TypeInfo.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Debug/NodeType.cpp.o ../../build/GoApi-gnumk_linux_x64-Debug/NodeType.cpp.d: GoApi/Properties/Nodes/NodeType.cpp
	$(SILENT) $(info GccX64 GoApi/Properties/Nodes/NodeType.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Debug/NodeType.cpp.o -c GoApi/Properties/Nodes/NodeType.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Debug/JsonSerializer.cpp.o ../../build/GoApi-gnumk_linux_x64-Debug/JsonSerializer.cpp.d: GoApi/Properties/Serializers/JsonSerializer.cpp
	$(SILENT) $(info GccX64 GoApi/Properties/Serializers/JsonSerializer.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Debug/JsonSerializer.cpp.o -c GoApi/Properties/Serializers/JsonSerializer.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Debug/XmlSerializer.cpp.o ../../build/GoApi-gnumk_linux_x64-Debug/XmlSerializer.cpp.d: GoApi/Properties/Serializers/XmlSerializer.cpp
	$(SILENT) $(info GccX64 GoApi/Properties/Serializers/XmlSerializer.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Debug/XmlSerializer.cpp.o -c GoApi/Properties/Serializers/XmlSerializer.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Debug/GoDataTreeSerializer.cpp.o ../../build/GoApi-gnumk_linux_x64-Debug/GoDataTreeSerializer.cpp.d: GoApi/Properties/Serializers/GoDataTreeSerializer.cpp
	$(SILENT) $(info GccX64 GoApi/Properties/Serializers/GoDataTreeSerializer.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Debug/GoDataTreeSerializer.cpp.o -c GoApi/Properties/Serializers/GoDataTreeSerializer.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Debug/Url.cpp.o ../../build/GoApi-gnumk_linux_x64-Debug/Url.cpp.d: GoApi/Net/Url.cpp
	$(SILENT) $(info GccX64 GoApi/Net/Url.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Debug/Url.cpp.o -c GoApi/Net/Url.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Debug/PointerWrapper.cpp.o ../../build/GoApi-gnumk_linux_x64-Debug/PointerWrapper.cpp.d: GoApi/PointerWrapper/PointerWrapper.cpp
	$(SILENT) $(info GccX64 GoApi/PointerWrapper/PointerWrapper.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Debug/PointerWrapper.cpp.o -c GoApi/PointerWrapper/PointerWrapper.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Debug/BeginEndUpdater.cpp.o ../../build/GoApi-gnumk_linux_x64-Debug/BeginEndUpdater.cpp.d: GoApi/Updating/BeginEndUpdater.cpp
	$(SILENT) $(info GccX64 GoApi/Updating/BeginEndUpdater.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Debug/BeginEndUpdater.cpp.o -c GoApi/Updating/BeginEndUpdater.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Debug/ScopedUpdater.cpp.o ../../build/GoApi-gnumk_linux_x64-Debug/ScopedUpdater.cpp.d: GoApi/Updating/ScopedUpdater.cpp
	$(SILENT) $(info GccX64 GoApi/Updating/ScopedUpdater.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Debug/ScopedUpdater.cpp.o -c GoApi/Updating/ScopedUpdater.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Debug/CoordUtils.cpp.o ../../build/GoApi-gnumk_linux_x64-Debug/CoordUtils.cpp.d: GoApi/Utils/CoordUtils.cpp
	$(SILENT) $(info GccX64 GoApi/Utils/CoordUtils.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Debug/CoordUtils.cpp.o -c GoApi/Utils/CoordUtils.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Debug/MemUtils.cpp.o ../../build/GoApi-gnumk_linux_x64-Debug/MemUtils.cpp.d: GoApi/Utils/MemUtils.cpp
	$(SILENT) $(info GccX64 GoApi/Utils/MemUtils.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Debug/MemUtils.cpp.o -c GoApi/Utils/MemUtils.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Debug/GoDataTree.cpp.o ../../build/GoApi-gnumk_linux_x64-Debug/GoDataTree.cpp.d: GoApi/GoDataTree/GoDataTree.cpp
	$(SILENT) $(info GccX64 GoApi/GoDataTree/GoDataTree.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Debug/GoDataTree.cpp.o -c GoApi/GoDataTree/GoDataTree.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Debug/GoDataTreeSerializerInputIterator.cpp.o ../../build/GoApi-gnumk_linux_x64-Debug/GoDataTreeSerializerInputIterator.cpp.d: GoApi/GoDataTree/Serializer/GoDataTreeSerializerInputIterator.cpp
	$(SILENT) $(info GccX64 GoApi/GoDataTree/Serializer/GoDataTreeSerializerInputIterator.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Debug/GoDataTreeSerializerInputIterator.cpp.o -c GoApi/GoDataTree/Serializer/GoDataTreeSerializerInputIterator.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Debug/GoDataTreeSerializerOutputWriter.cpp.o ../../build/GoApi-gnumk_linux_x64-Debug/GoDataTreeSerializerOutputWriter.cpp.d: GoApi/GoDataTree/Serializer/GoDataTreeSerializerOutputWriter.cpp
	$(SILENT) $(info GccX64 GoApi/GoDataTree/Serializer/GoDataTreeSerializerOutputWriter.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Debug/GoDataTreeSerializerOutputWriter.cpp.o -c GoApi/GoDataTree/Serializer/GoDataTreeSerializerOutputWriter.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Debug/GoJsonSerializer.cpp.o ../../build/GoApi-gnumk_linux_x64-Debug/GoJsonSerializer.cpp.d: GoApi/GoDataTree/Serializer/GoJsonSerializer.cpp
	$(SILENT) $(info GccX64 GoApi/GoDataTree/Serializer/GoJsonSerializer.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Debug/GoJsonSerializer.cpp.o -c GoApi/GoDataTree/Serializer/GoJsonSerializer.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Debug/GoMsgPackSerializer.cpp.o ../../build/GoApi-gnumk_linux_x64-Debug/GoMsgPackSerializer.cpp.d: GoApi/GoDataTree/Serializer/GoMsgPackSerializer.cpp
	$(SILENT) $(info GccX64 GoApi/GoDataTree/Serializer/GoMsgPackSerializer.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Debug/GoMsgPackSerializer.cpp.o -c GoApi/GoDataTree/Serializer/GoMsgPackSerializer.cpp -MMD -MP

endif

ifeq ($(config),Release)

../../build/GoApi-gnumk_linux_x64-Release/GoApi.cpp.o ../../build/GoApi-gnumk_linux_x64-Release/GoApi.cpp.d: GoApi/GoApi.cpp
	$(SILENT) $(info GccX64 GoApi/GoApi.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Release/GoApi.cpp.o -c GoApi/GoApi.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Release/GoApiLib.cpp.o ../../build/GoApi-gnumk_linux_x64-Release/GoApiLib.cpp.d: GoApi/GoApiLib.cpp
	$(SILENT) $(info GccX64 GoApi/GoApiLib.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Release/GoApiLib.cpp.o -c GoApi/GoApiLib.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Release/Profiling.cpp.o ../../build/GoApi-gnumk_linux_x64-Release/Profiling.cpp.d: GoApi/Profiling/Profiling.cpp
	$(SILENT) $(info GccX64 GoApi/Profiling/Profiling.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Release/Profiling.cpp.o -c GoApi/Profiling/Profiling.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Release/Exception.cpp.o ../../build/GoApi-gnumk_linux_x64-Release/Exception.cpp.d: GoApi/Exception.cpp
	$(SILENT) $(info GccX64 GoApi/Exception.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Release/Exception.cpp.o -c GoApi/Exception.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Release/Logging.cpp.o ../../build/GoApi-gnumk_linux_x64-Release/Logging.cpp.d: GoApi/Logging.cpp
	$(SILENT) $(info GccX64 GoApi/Logging.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Release/Logging.cpp.o -c GoApi/Logging.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Release/MemoryLeakDetection.cpp.o ../../build/GoApi-gnumk_linux_x64-Release/MemoryLeakDetection.cpp.d: GoApi/MemoryLeakDetection.cpp
	$(SILENT) $(info GccX64 GoApi/MemoryLeakDetection.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Release/MemoryLeakDetection.cpp.o -c GoApi/MemoryLeakDetection.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Release/EventType.cpp.o ../../build/GoApi-gnumk_linux_x64-Release/EventType.cpp.d: GoApi/Resource/EventType.cpp
	$(SILENT) $(info GccX64 GoApi/Resource/EventType.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Release/EventType.cpp.o -c GoApi/Resource/EventType.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Release/Path.cpp.o ../../build/GoApi-gnumk_linux_x64-Release/Path.cpp.d: GoApi/Resource/Path.cpp
	$(SILENT) $(info GccX64 GoApi/Resource/Path.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Release/Path.cpp.o -c GoApi/Resource/Path.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Release/PathExpressions.cpp.o ../../build/GoApi-gnumk_linux_x64-Release/PathExpressions.cpp.d: GoApi/Resource/PathExpressions.cpp
	$(SILENT) $(info GccX64 GoApi/Resource/PathExpressions.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Release/PathExpressions.cpp.o -c GoApi/Resource/PathExpressions.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Release/ReqRespInfo.cpp.o ../../build/GoApi-gnumk_linux_x64-Release/ReqRespInfo.cpp.d: GoApi/Resource/ReqRespInfo.cpp
	$(SILENT) $(info GccX64 GoApi/Resource/ReqRespInfo.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Release/ReqRespInfo.cpp.o -c GoApi/Resource/ReqRespInfo.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Release/ResourceBase.cpp.o ../../build/GoApi-gnumk_linux_x64-Release/ResourceBase.cpp.d: GoApi/Resource/ResourceBase.cpp
	$(SILENT) $(info GccX64 GoApi/Resource/ResourceBase.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Release/ResourceBase.cpp.o -c GoApi/Resource/ResourceBase.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Release/ResourceRouter.cpp.o ../../build/GoApi-gnumk_linux_x64-Release/ResourceRouter.cpp.d: GoApi/Resource/ResourceRouter.cpp
	$(SILENT) $(info GccX64 GoApi/Resource/ResourceRouter.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Release/ResourceRouter.cpp.o -c GoApi/Resource/ResourceRouter.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Release/GoGetText.cpp.o ../../build/GoApi-gnumk_linux_x64-Release/GoGetText.cpp.d: GoApi/Translations/GoGetText.cpp
	$(SILENT) $(info GccX64 GoApi/Translations/GoGetText.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Release/GoGetText.cpp.o -c GoApi/Translations/GoGetText.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Release/Catalog.cpp.o ../../build/GoApi-gnumk_linux_x64-Release/Catalog.cpp.d: GoApi/Translations/Catalog.cpp
	$(SILENT) $(info GccX64 GoApi/Translations/Catalog.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Release/Catalog.cpp.o -c GoApi/Translations/Catalog.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Release/AsyncLoop.cpp.o ../../build/GoApi-gnumk_linux_x64-Release/AsyncLoop.cpp.d: GoApi/Async/AsyncLoop.cpp
	$(SILENT) $(info GccX64 GoApi/Async/AsyncLoop.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Release/AsyncLoop.cpp.o -c GoApi/Async/AsyncLoop.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Release/AsyncNotifier.cpp.o ../../build/GoApi-gnumk_linux_x64-Release/AsyncNotifier.cpp.d: GoApi/Async/AsyncNotifier.cpp
	$(SILENT) $(info GccX64 GoApi/Async/AsyncNotifier.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Release/AsyncNotifier.cpp.o -c GoApi/Async/AsyncNotifier.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Release/AsyncRunner.cpp.o ../../build/GoApi-gnumk_linux_x64-Release/AsyncRunner.cpp.d: GoApi/Async/AsyncRunner.cpp
	$(SILENT) $(info GccX64 GoApi/Async/AsyncRunner.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Release/AsyncRunner.cpp.o -c GoApi/Async/AsyncRunner.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Release/AsyncTimer.cpp.o ../../build/GoApi-gnumk_linux_x64-Release/AsyncTimer.cpp.d: GoApi/Async/AsyncTimer.cpp
	$(SILENT) $(info GccX64 GoApi/Async/AsyncTimer.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Release/AsyncTimer.cpp.o -c GoApi/Async/AsyncTimer.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Release/AsyncUdpSocket.cpp.o ../../build/GoApi-gnumk_linux_x64-Release/AsyncUdpSocket.cpp.d: GoApi/Async/AsyncUdpSocket.cpp
	$(SILENT) $(info GccX64 GoApi/Async/AsyncUdpSocket.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Release/AsyncUdpSocket.cpp.o -c GoApi/Async/AsyncUdpSocket.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Release/Node.cpp.o ../../build/GoApi-gnumk_linux_x64-Release/Node.cpp.d: GoApi/Properties/Nodes/Node.cpp
	$(SILENT) $(info GccX64 GoApi/Properties/Nodes/Node.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Release/Node.cpp.o -c GoApi/Properties/Nodes/Node.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Release/Structure.cpp.o ../../build/GoApi-gnumk_linux_x64-Release/Structure.cpp.d: GoApi/Properties/Nodes/Structure.cpp
	$(SILENT) $(info GccX64 GoApi/Properties/Nodes/Structure.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Release/Structure.cpp.o -c GoApi/Properties/Nodes/Structure.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Release/ArrayBase.cpp.o ../../build/GoApi-gnumk_linux_x64-Release/ArrayBase.cpp.d: GoApi/Properties/Nodes/ArrayBase.cpp
	$(SILENT) $(info GccX64 GoApi/Properties/Nodes/ArrayBase.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Release/ArrayBase.cpp.o -c GoApi/Properties/Nodes/ArrayBase.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Release/TypeInfo.cpp.o ../../build/GoApi-gnumk_linux_x64-Release/TypeInfo.cpp.d: GoApi/Properties/Nodes/TypeInfo.cpp
	$(SILENT) $(info GccX64 GoApi/Properties/Nodes/TypeInfo.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Release/TypeInfo.cpp.o -c GoApi/Properties/Nodes/TypeInfo.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Release/NodeType.cpp.o ../../build/GoApi-gnumk_linux_x64-Release/NodeType.cpp.d: GoApi/Properties/Nodes/NodeType.cpp
	$(SILENT) $(info GccX64 GoApi/Properties/Nodes/NodeType.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Release/NodeType.cpp.o -c GoApi/Properties/Nodes/NodeType.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Release/JsonSerializer.cpp.o ../../build/GoApi-gnumk_linux_x64-Release/JsonSerializer.cpp.d: GoApi/Properties/Serializers/JsonSerializer.cpp
	$(SILENT) $(info GccX64 GoApi/Properties/Serializers/JsonSerializer.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Release/JsonSerializer.cpp.o -c GoApi/Properties/Serializers/JsonSerializer.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Release/XmlSerializer.cpp.o ../../build/GoApi-gnumk_linux_x64-Release/XmlSerializer.cpp.d: GoApi/Properties/Serializers/XmlSerializer.cpp
	$(SILENT) $(info GccX64 GoApi/Properties/Serializers/XmlSerializer.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Release/XmlSerializer.cpp.o -c GoApi/Properties/Serializers/XmlSerializer.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Release/GoDataTreeSerializer.cpp.o ../../build/GoApi-gnumk_linux_x64-Release/GoDataTreeSerializer.cpp.d: GoApi/Properties/Serializers/GoDataTreeSerializer.cpp
	$(SILENT) $(info GccX64 GoApi/Properties/Serializers/GoDataTreeSerializer.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Release/GoDataTreeSerializer.cpp.o -c GoApi/Properties/Serializers/GoDataTreeSerializer.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Release/Url.cpp.o ../../build/GoApi-gnumk_linux_x64-Release/Url.cpp.d: GoApi/Net/Url.cpp
	$(SILENT) $(info GccX64 GoApi/Net/Url.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Release/Url.cpp.o -c GoApi/Net/Url.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Release/PointerWrapper.cpp.o ../../build/GoApi-gnumk_linux_x64-Release/PointerWrapper.cpp.d: GoApi/PointerWrapper/PointerWrapper.cpp
	$(SILENT) $(info GccX64 GoApi/PointerWrapper/PointerWrapper.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Release/PointerWrapper.cpp.o -c GoApi/PointerWrapper/PointerWrapper.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Release/BeginEndUpdater.cpp.o ../../build/GoApi-gnumk_linux_x64-Release/BeginEndUpdater.cpp.d: GoApi/Updating/BeginEndUpdater.cpp
	$(SILENT) $(info GccX64 GoApi/Updating/BeginEndUpdater.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Release/BeginEndUpdater.cpp.o -c GoApi/Updating/BeginEndUpdater.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Release/ScopedUpdater.cpp.o ../../build/GoApi-gnumk_linux_x64-Release/ScopedUpdater.cpp.d: GoApi/Updating/ScopedUpdater.cpp
	$(SILENT) $(info GccX64 GoApi/Updating/ScopedUpdater.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Release/ScopedUpdater.cpp.o -c GoApi/Updating/ScopedUpdater.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Release/CoordUtils.cpp.o ../../build/GoApi-gnumk_linux_x64-Release/CoordUtils.cpp.d: GoApi/Utils/CoordUtils.cpp
	$(SILENT) $(info GccX64 GoApi/Utils/CoordUtils.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Release/CoordUtils.cpp.o -c GoApi/Utils/CoordUtils.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Release/MemUtils.cpp.o ../../build/GoApi-gnumk_linux_x64-Release/MemUtils.cpp.d: GoApi/Utils/MemUtils.cpp
	$(SILENT) $(info GccX64 GoApi/Utils/MemUtils.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Release/MemUtils.cpp.o -c GoApi/Utils/MemUtils.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Release/GoDataTree.cpp.o ../../build/GoApi-gnumk_linux_x64-Release/GoDataTree.cpp.d: GoApi/GoDataTree/GoDataTree.cpp
	$(SILENT) $(info GccX64 GoApi/GoDataTree/GoDataTree.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Release/GoDataTree.cpp.o -c GoApi/GoDataTree/GoDataTree.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Release/GoDataTreeSerializerInputIterator.cpp.o ../../build/GoApi-gnumk_linux_x64-Release/GoDataTreeSerializerInputIterator.cpp.d: GoApi/GoDataTree/Serializer/GoDataTreeSerializerInputIterator.cpp
	$(SILENT) $(info GccX64 GoApi/GoDataTree/Serializer/GoDataTreeSerializerInputIterator.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Release/GoDataTreeSerializerInputIterator.cpp.o -c GoApi/GoDataTree/Serializer/GoDataTreeSerializerInputIterator.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Release/GoDataTreeSerializerOutputWriter.cpp.o ../../build/GoApi-gnumk_linux_x64-Release/GoDataTreeSerializerOutputWriter.cpp.d: GoApi/GoDataTree/Serializer/GoDataTreeSerializerOutputWriter.cpp
	$(SILENT) $(info GccX64 GoApi/GoDataTree/Serializer/GoDataTreeSerializerOutputWriter.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Release/GoDataTreeSerializerOutputWriter.cpp.o -c GoApi/GoDataTree/Serializer/GoDataTreeSerializerOutputWriter.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Release/GoJsonSerializer.cpp.o ../../build/GoApi-gnumk_linux_x64-Release/GoJsonSerializer.cpp.d: GoApi/GoDataTree/Serializer/GoJsonSerializer.cpp
	$(SILENT) $(info GccX64 GoApi/GoDataTree/Serializer/GoJsonSerializer.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Release/GoJsonSerializer.cpp.o -c GoApi/GoDataTree/Serializer/GoJsonSerializer.cpp -MMD -MP

../../build/GoApi-gnumk_linux_x64-Release/GoMsgPackSerializer.cpp.o ../../build/GoApi-gnumk_linux_x64-Release/GoMsgPackSerializer.cpp.d: GoApi/GoDataTree/Serializer/GoMsgPackSerializer.cpp
	$(SILENT) $(info GccX64 GoApi/GoDataTree/Serializer/GoMsgPackSerializer.cpp)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../build/GoApi-gnumk_linux_x64-Release/GoMsgPackSerializer.cpp.o -c GoApi/GoDataTree/Serializer/GoMsgPackSerializer.cpp -MMD -MP

endif

ifeq ($(MAKECMDGOALS),all-obj)

ifeq ($(config),Debug)

include ../../build/GoApi-gnumk_linux_x64-Debug/GoApi.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Debug/GoApiLib.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Debug/Profiling.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Debug/Exception.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Debug/Logging.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Debug/MemoryLeakDetection.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Debug/EventType.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Debug/Path.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Debug/PathExpressions.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Debug/ReqRespInfo.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Debug/ResourceBase.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Debug/ResourceRouter.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Debug/GoGetText.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Debug/Catalog.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Debug/AsyncLoop.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Debug/AsyncNotifier.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Debug/AsyncRunner.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Debug/AsyncTimer.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Debug/AsyncUdpSocket.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Debug/Node.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Debug/Structure.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Debug/ArrayBase.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Debug/TypeInfo.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Debug/NodeType.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Debug/JsonSerializer.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Debug/XmlSerializer.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Debug/GoDataTreeSerializer.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Debug/Url.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Debug/PointerWrapper.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Debug/BeginEndUpdater.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Debug/ScopedUpdater.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Debug/CoordUtils.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Debug/MemUtils.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Debug/GoDataTree.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Debug/GoDataTreeSerializerInputIterator.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Debug/GoDataTreeSerializerOutputWriter.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Debug/GoJsonSerializer.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Debug/GoMsgPackSerializer.cpp.d

endif

ifeq ($(config),Release)

include ../../build/GoApi-gnumk_linux_x64-Release/GoApi.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Release/GoApiLib.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Release/Profiling.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Release/Exception.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Release/Logging.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Release/MemoryLeakDetection.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Release/EventType.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Release/Path.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Release/PathExpressions.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Release/ReqRespInfo.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Release/ResourceBase.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Release/ResourceRouter.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Release/GoGetText.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Release/Catalog.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Release/AsyncLoop.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Release/AsyncNotifier.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Release/AsyncRunner.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Release/AsyncTimer.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Release/AsyncUdpSocket.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Release/Node.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Release/Structure.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Release/ArrayBase.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Release/TypeInfo.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Release/NodeType.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Release/JsonSerializer.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Release/XmlSerializer.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Release/GoDataTreeSerializer.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Release/Url.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Release/PointerWrapper.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Release/BeginEndUpdater.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Release/ScopedUpdater.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Release/CoordUtils.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Release/MemUtils.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Release/GoDataTree.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Release/GoDataTreeSerializerInputIterator.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Release/GoDataTreeSerializerOutputWriter.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Release/GoJsonSerializer.cpp.d
include ../../build/GoApi-gnumk_linux_x64-Release/GoMsgPackSerializer.cpp.d

endif

endif

