
ifeq ($(OS)$(os), Windows_NT)
	XCOMPILE := 1
	OS_PREFIX := C:
	OS_SUFFIX := .exe
	PYTHON := python
	MKDIR_P := $(PYTHON) ../../../Platform/scripts/Utils/kUtil.py mkdir_p
	RM_F := $(PYTHON) ../../../Platform/scripts/Utils/kUtil.py rm_f
	RM_RF := $(PYTHON) ../../../Platform/scripts/Utils/kUtil.py rm_rf
	CP := $(PYTHON) ../../../Platform/scripts/Utils/kUtil.py cp
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

KAPPGEN := $(PYTHON) ../../../Platform/scripts/Utils/kAppGen.py

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
	TARGET := ../../../lib/linux_x64d/libgtest.a
	INTERMEDIATES := 
	OBJ_DIR := ../../../build/gtest-gnumk_linux_x64-Debug
	PREBUILD := 
	POSTBUILD := 
	COMPILER_FLAGS := -g -march=x86-64 -fpic -fvisibility=hidden
	C_FLAGS := -std=gnu99 -Wall -Wno-unused-variable -Wno-unused-parameter -Wno-unused-value -Wno-missing-braces
	CXX_FLAGS := -std=c++17 -Wall -Wfloat-conversion -Wno-psabi
	INCLUDE_DIRS := -I../../extern/googletest/package/googletest -I../../extern/googletest/package/googletest/include
	DEFINES := -DGTEST_HAS_PTHREAD=0 -D_HAS_EXCEPTIONS=1
	LINKER_FLAGS :=
	LIB_DIRS := -L../../../lib/linux_x64d
	LIBS :=
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
	OBJECTS := ../../../build/gtest-gnumk_linux_x64-Debug/gtest-all.cc.o
	DEP_FILES = ../../../build/gtest-gnumk_linux_x64-Debug/gtest-all.cc.d
	TARGET_DEPS = 

endif

ifeq ($(config),Release)
	optimize := 2
	strip := 1
	wstack := 0
	TARGET := ../../../lib/linux_x64/libgtest.a
	INTERMEDIATES := 
	OBJ_DIR := ../../../build/gtest-gnumk_linux_x64-Release
	PREBUILD := 
	POSTBUILD := 
	COMPILER_FLAGS := -march=x86-64 -fpic -fvisibility=hidden
	C_FLAGS := -std=gnu99 -Wall -Wno-unused-variable -Wno-unused-parameter -Wno-unused-value -Wno-missing-braces
	CXX_FLAGS := -std=c++17 -Wall -Wfloat-conversion -Wno-psabi
	INCLUDE_DIRS := -I../../extern/googletest/package/googletest -I../../extern/googletest/package/googletest/include
	DEFINES := -DGTEST_HAS_PTHREAD=0 -D_HAS_EXCEPTIONS=1
	LINKER_FLAGS :=
	LIB_DIRS := -L../../../lib/linux_x64
	LIBS :=
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
	OBJECTS := ../../../build/gtest-gnumk_linux_x64-Release/gtest-all.cc.o
	DEP_FILES = ../../../build/gtest-gnumk_linux_x64-Release/gtest-all.cc.d
	TARGET_DEPS = 

endif

.PHONY: all all-obj all-dep clean

all: $(OBJ_DIR)
	$(PREBUILD)
	$(SILENT) $(MAKE) -f gtest-Linux_X64.mk all-dep
	$(SILENT) $(MAKE) -f gtest-Linux_X64.mk all-obj

clean:
	$(SILENT) $(info Cleaning $(OBJ_DIR))
	$(SILENT) $(RM_RF) $(OBJ_DIR)
	$(SILENT) $(info Cleaning $(TARGET) $(INTERMEDIATES))
	$(SILENT) $(RM_F) $(TARGET) $(INTERMEDIATES)

all-obj: $(OBJ_DIR) $(TARGET)
all-dep: $(OBJ_DIR) $(DEP_FILES)

$(OBJ_DIR):
	$(SILENT) $(MKDIR_P) $@

ifeq ($(config),Debug)

$(TARGET): $(OBJECTS) $(TARGET_DEPS)
	$(SILENT) $(info ArX64 $(TARGET))
	$(SILENT) $(RM_F) $(TARGET)
	$(SILENT) $(GNU_ARCHIVER) rcs $(TARGET) $(OBJECTS)

endif

ifeq ($(config),Release)

$(TARGET): $(OBJECTS) $(TARGET_DEPS)
	$(SILENT) $(info ArX64 $(TARGET))
	$(SILENT) $(RM_F) $(TARGET)
	$(SILENT) $(GNU_ARCHIVER) rcs $(TARGET) $(OBJECTS)

endif

ifeq ($(config),Debug)

../../../build/gtest-gnumk_linux_x64-Debug/gtest-all.cc.o ../../../build/gtest-gnumk_linux_x64-Debug/gtest-all.cc.d: ../../extern/googletest/package/googletest/src/gtest-all.cc
	$(SILENT) $(info GccX64 ../../extern/googletest/package/googletest/src/gtest-all.cc)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../../build/gtest-gnumk_linux_x64-Debug/gtest-all.cc.o -c ../../extern/googletest/package/googletest/src/gtest-all.cc -MMD -MP

endif

ifeq ($(config),Release)

../../../build/gtest-gnumk_linux_x64-Release/gtest-all.cc.o ../../../build/gtest-gnumk_linux_x64-Release/gtest-all.cc.d: ../../extern/googletest/package/googletest/src/gtest-all.cc
	$(SILENT) $(info GccX64 ../../extern/googletest/package/googletest/src/gtest-all.cc)
	$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o ../../../build/gtest-gnumk_linux_x64-Release/gtest-all.cc.o -c ../../extern/googletest/package/googletest/src/gtest-all.cc -MMD -MP

endif

ifeq ($(MAKECMDGOALS),all-obj)

ifeq ($(config),Debug)

include ../../../build/gtest-gnumk_linux_x64-Debug/gtest-all.cc.d

endif

ifeq ($(config),Release)

include ../../../build/gtest-gnumk_linux_x64-Release/gtest-all.cc.d

endif

endif

