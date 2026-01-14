#!/usr/bin/python
# This file is part of the FireSync Project Generator.
# Copyright (C) 2008-2016 by LMI Technologies Inc.  All rights reserved.
# Distributed under the terms of the MIT License.
# Redistributed files must retain the above copyright notice.

import re
import os
import platform
from . import Project
from . import WriterBase
from . import Utils

class Plugin:
    Linux_Arm64 = 0
    Linux_Arm7  = 1
    Linux_X64   = 2
    Linux_X86   = 3

def GetUnameMArch(plugin):
    if plugin == Plugin.Linux_Arm64:        return 'aarch64'
    elif plugin == Plugin.Linux_Arm7:       return 'armv7l'
    elif plugin == Plugin.Linux_X64:        return 'x86_64'
    elif plugin == Plugin.Linux_X86:        return 'i686'
    else:                                   raise Exception('Unknown plugin')

def GetTriplet(plugin):
    if plugin == Plugin.Linux_Arm64:        return 'aarch64-linux-gnu'
    elif plugin == Plugin.Linux_Arm7:       return 'arm-none-linux-gnueabihf'
    elif plugin == Plugin.Linux_X64:        return 'x86_64-linux-gnu'
    elif plugin == Plugin.Linux_X86:        return 'x86_64-linux-gnu'  # -m32
    else:                                   raise Exception('Unknown plugin')

def CudaVersionEnvString(plugin):
    if plugin == Plugin.Linux_Arm64:        return 'K_ARM64_LINUX_CUDA_VERSION'
    elif plugin == Plugin.Linux_Arm7:       return 'K_ARM7_LINUX_CUDA_VERSION'
    elif plugin == Plugin.Linux_X64:        return 'K_X64_LINUX_CUDA_VERSION'
    elif plugin == Plugin.Linux_X86:        return 'K_X86_LINUX_CUDA_VERSION'
    else:                                   raise Exception('Unknown plugin')

def GeneratorEnvString():
    return 'K_REGENERATOR'

def CudaVersion(plugin):
    try:
        return os.environ[CudaVersionEnvString(plugin)]
    except KeyError:
        return '0'

def HasCuda(plugin):
    return CudaVersion(plugin) != '0'

# Some legacy/history. Now by default on, unless explicitly disabled via either variable.
def ReGeneratorEnabled():

    envNew = Utils.EnvInt('K_REGENERATOR')
    envOld = Utils.EnvInt('K_GENERATOR_ENABLED')

    if (envNew == 1 or envOld == 1):
        return True

    if (envNew == 0 or envOld == 0):
        return False

    return True

def GetPlatforms(plugin):
    if plugin == Plugin.Linux_Arm64:    return ['Linux_Arm64']
    elif plugin == Plugin.Linux_Arm7:   return ['Linux_Arm7']
    elif plugin == Plugin.Linux_X64:    return ['Linux_X64']
    elif plugin == Plugin.Linux_X86:    return ['Linux_X86']
    else:                               return []

def GetProjects(plugin):
    return ['C/CPP']

def GetName(plugin):
    if plugin == Plugin.Linux_Arm64:    return 'GnuMk_Linux_Arm64'
    elif plugin == Plugin.Linux_Arm7:   return 'GnuMk_Linux_Arm7'
    elif plugin == Plugin.Linux_X64:    return 'GnuMk_Linux_X64'
    elif plugin == Plugin.Linux_X86:    return 'GnuMk_Linux_X86'
    else:                               return ''

def GetSuffix(plugin):
    if plugin == Plugin.Linux_Arm64:    return '-Linux_Arm64'
    elif plugin == Plugin.Linux_Arm7:   return '-Linux_Arm7'
    elif plugin == Plugin.Linux_X64:    return '-Linux_X64'
    elif plugin == Plugin.Linux_X86:    return '-Linux_X86'
    else:                               return ''

def MsgSuffix(plugin):
    if plugin == Plugin.Linux_Arm64:    return 'Arm64'
    elif plugin == Plugin.Linux_Arm7:   return 'Arm7'
    elif plugin == Plugin.Linux_X64:    return 'X64'
    elif plugin == Plugin.Linux_X86:    return 'X86'
    else:                               return ''

def GnuToolsVersion(plugin):
    if plugin == Plugin.Linux_Arm64:    return '11.4.1'
    elif plugin == Plugin.Linux_Arm7:   return '11.4.1'
    elif plugin == Plugin.Linux_X64:    return '11.4.1'
    elif plugin == Plugin.Linux_X86:    return '11.4.1'
    else:                               return '11.4.1'

def GnuToolsPatch(plugin):
    if plugin == Plugin.Linux_Arm64:    return 'p1'
    elif plugin == Plugin.Linux_Arm7:   return 'p1'
    elif plugin == Plugin.Linux_X64:    return 'p1'
    elif plugin == Plugin.Linux_X86:    return 'p1'
    else:                               return 'p1'

def GetOsPrefix(isWindows):
    if isWindows:
        return 'C:'
    else:
        return ''

def GetOsSuffix(isWindows):
    if isWindows:
        return '.exe'
    else:
        return ''

def GetToolsBinPath(plugin):
    return '%s/tools/%s/%s/bin' % (GetOsPrefix(platform.system() == 'Windows'),
        GnuToolsDirectory(plugin), GetTriplet(plugin))

def GetToolsPrefix(plugin):
    return '%s/%s-' % (GetToolsBinPath(plugin), GetTriplet(plugin))

def LlvmDirectory(plugin):
    return 'LLVM_17.0.6'

def GnuToolsDirectory(plugin):
    if plugin == Plugin.Linux_Arm64:    return 'GccArm64_' + GnuToolsVersion(plugin) + '-' + GnuToolsPatch(plugin)
    elif plugin == Plugin.Linux_Arm7:   return 'GccArm7_' + GnuToolsVersion(plugin) + '-' + GnuToolsPatch(plugin)
    elif plugin == Plugin.Linux_X64:    return 'GccX64_' + GnuToolsVersion(plugin) + '-' + GnuToolsPatch(plugin)
    elif plugin == Plugin.Linux_X86:    return 'GccX64_' + GnuToolsVersion(plugin) + '-' + GnuToolsPatch(plugin)
    else:                               raise Exception('Unknown plugin')

def GmakePath(projectFileName):
    herePath = os.path.dirname(os.path.realpath(__file__))
    gmakePath = os.path.normpath(os.path.join(herePath, '..', '..', '..', 'extern', 'Make', 'gmake.exe'))
    return os.path.relpath(gmakePath, os.path.dirname(projectFileName))

def MakeFileSuffix(platformName):
    if platformName == 'Linux_Arm64':   return GetSuffix(Plugin.Linux_Arm64)
    elif platformName == 'Linux_Arm7':  return GetSuffix(Plugin.Linux_Arm7)
    elif platformName == 'Linux_X64':   return GetSuffix(Plugin.Linux_X64)
    elif platformName == 'Linux_X86':   return GetSuffix(Plugin.Linux_X86)
    else:                               raise Exception('Unknown plugin')

#
# Define how the projects are (re-)built
#
def BuildCmd(project, config):
    return '%s -s -f %s%s.mk config=%s' % \
        (GmakePath(project.FileName),
        project.Name,
        MakeFileSuffix(config.Platform),
        config.Name)

def RebuildCmd(project, config):
    return '%s -s -f %s%s.mk config=%s clean all' % \
        (GmakePath(project.FileName),
        project.Name,
        MakeFileSuffix(config.Platform),
        config.Name)

def CleanCmd(project, config):
    return '%s -s -f %s%s.mk config=%s clean' % \
        (GmakePath(project.FileName),
        project.Name,
        MakeFileSuffix(config.Platform),
        config.Name)

class GnuMkWriter(WriterBase.WriterBase):

    #
    # Toolchain settings
    #
    _cFlags = [
        '-std=gnu99',
        '-Wall',
        '-Wno-unused-variable',
        '-Wno-unused-parameter',
        '-Wno-unused-value',
        '-Wno-missing-braces', # this is for GCC bug 53119 -- should be removed when GCC >= 5.x is abundant
    ]

    _cxx11Flags = [
        '-std=c++11',
        '-Wall',
    ]

    _cxx14Flags = [
        '-std=c++14',
        '-Wall',
    ]

    _cxxFlags = [
        '-std=c++17',
        '-Wall',
        '-Wfloat-conversion',
        '-Wno-psabi',
    ]

    _cudaFlagsCommon = [
        #'--default-stream per-thread',     # FSS-1452: Enable once existing Cuda code has been audited for correct stream use
        '--std=c++14',
    ]

    _cudaFlags9x0 = [
        '-x cu',
        '-gencode arch=compute_50,code=sm_50',
        '-gencode arch=compute_52,code=sm_52',
        '-gencode arch=compute_60,code=sm_60',
        '-gencode arch=compute_61,code=sm_61',
        '-gencode arch=compute_62,code=sm_62',
    ]

    _cudaFlags10x0 = [
        '-x cu',
        '-gencode arch=compute_50,code=sm_50',
        '-gencode arch=compute_52,code=sm_52',
        '-gencode arch=compute_60,code=sm_60',
        '-gencode arch=compute_61,code=sm_61',
        '-gencode arch=compute_62,code=sm_62',
        '-gencode arch=compute_70,code=sm_70',
        '-gencode arch=compute_72,code=sm_72',
    ]

    _cudaFlags11x4 = [
        '-x cu',
        '-gencode arch=compute_52,code=sm_52',
        '-gencode arch=compute_60,code=sm_60',
        '-gencode arch=compute_61,code=sm_61',
        '-gencode arch=compute_62,code=sm_62',
        '-gencode arch=compute_70,code=sm_70',
        '-gencode arch=compute_72,code=sm_72',
        #'-gencode arch=compute_75,code=sm_75',  # Future.
        #'-gencode arch=compute_80,code=sm_80',  # Future.
        #'-gencode arch=compute_86,code=sm_86',  # Future.
        #'-gencode arch=compute_87,code=sm_87',  # Future.
    ]

    _cudaFlags11x8 = [
        '-x cu',
        '-gencode arch=compute_52,code=sm_52',
        '-gencode arch=compute_60,code=sm_60',
        '-gencode arch=compute_61,code=sm_61',
        '-gencode arch=compute_62,code=sm_62',
        '-gencode arch=compute_70,code=sm_70',
        '-gencode arch=compute_72,code=sm_72',
        #'-gencode arch=compute_75,code=sm_75',  # Future.
        #'-gencode arch=compute_80,code=sm_80',  # Future.
        #'-gencode arch=compute_86,code=sm_86',  # Future.
        #'-gencode arch=compute_87,code=sm_87',  # Future.
        ##'-gencode arch=compute_89,code=sm_89',  # Future future.
        ##'-gencode arch=compute_90,code=sm_90',  # Future future.
    ]

    _clangCudaFlags = [
        '--cuda-gpu-arch=sm_50',
        '--cuda-gpu-arch=sm_52',
        '--cuda-gpu-arch=sm_60',
        '--cuda-gpu-arch=sm_61',
        '--cuda-gpu-arch=sm_62',
        '--cuda-gpu-arch=sm_70',    # Clang >= 6.x
        '--cuda-gpu-arch=sm_72',    # Clang >= 7.x
        #'--cuda-gpu-arch=sm_75',    # Clang >= 8.x
        #'--cuda-gpu-arch=sm_80',    # Clang >= 11.x
        #'--cuda-gpu-arch=sm_86',    # Clang >= 13.x
        #'--cuda-gpu-arch=sm_87',    # Clang >= 16.x
        ##'--cuda-gpu-arch=sm_89',    # Clang >= 16.x
        ##'--cuda-gpu-arch=sm_90',    # Clang >= 16.x
    ]

    _compilerFlagTemplates = {
        'Symbols': [
            '-g',
        ],

        'Optimize': [
        #    '-O2',     # handled elsewhere
        ],

        'Optimize3': [
        #    '-O3',     # handled elsewhere
        ],

        'OpenMp': [
            '-fopenmp',
        ],

        'LongCalls': [
            '-mlong-calls',
        ],

        'NoBuiltin': [
            '-fno-builtin',
        ],

        'ForceLto': [
            '-flto',
        ],
    }

    _compilerFlagsArm64 = [
        '-march=armv8-a+crypto',
        '-mcpu=cortex-a57+crypto',
    ]

    _compilerFlagsArm7 = [
        '-march=armv7-a',
        '-mtune=cortex-a9',
        '-mfloat-abi=hard',
        '-mno-unaligned-access',
        '-fno-semantic-interposition',
        #'-mfpu=neon',
    ]

    _platformCxxFlagsArm7 = [
    ]

    _compilerFlagsX64 = [
        '-march=x86-64',
    ]

    _compilerFlagsX86 = [
        '-m32',
        '-march=i686',
    ]

    _compilerFlagsGnu = [
        '-fno-gnu-unique',                  # FPT-1884: This flag calls global dtors when calling dlcose; which is preferred
    ]

    _picCompilerFlags = [
        '-fpic',  # can be disabled with 'NoPic' template hint
    ]

    _visibilityCompilerFlags = [
        '-fvisibility=hidden',  # can be disabled with 'AutoExport' template hint
    ]

    def __init__(self, platforms, projects, plugin):
        WriterBase.WriterBase.__init__(self, GetPlatforms(plugin), platforms, GetProjects(plugin), projects)
        self.name = GetName(plugin)
        self.suffix = GetSuffix(plugin)
        self.plugin = plugin
        self.hasCuda = HasCuda(plugin)
        self.cudaVersion = CudaVersion(plugin)
        self.cudaMajor = int(float(self.cudaVersion))
        self.regeneratorEnabled = ReGeneratorEnabled()

    @property
    def Name(self):
        return self.name

    @property
    def TempName(self):
        return self.name.lower()

    @property
    def SolutionExt(self):
        return '.mk'

    @property
    def ProjectExt(self):
        return '.mk'
        
    @property
    def ProjectSuffix(self):
        return self.Suffix + self.ProjectExt

    @property
    def SolutionSuffix(self):
        return self.Suffix + self.SolutionExt

    @property
    def Suffix(self):
        return self.suffix

    def HasSymbols(self, config):
        return True if 'Symbols' in config.Template.split('|') else False

    def DefaultOptimize(self, config):
        components = config.Template.split('|')

        if 'Optimize' in components:            return '2'
        elif 'Optimize3' in components:         return '3'
        elif 'OptimizeSize' in components:      return 's'
        else:                                   return '0'

    def DefaultStrip(self, config):
        return 0 if (self.HasSymbols(config)) else 1

    def DefaultWStackUsage(self, config):
        if (self.HasSymbols(config)):
            return 0

        if self.plugin == Plugin.Linux_Arm7:    return 32768
        else:                                   return 0

    def CudaDirectory(self):
        return 'cuda-%s' % (self.cudaVersion)

    def CudaIncludeDir(self):
        if self.plugin == Plugin.Linux_Arm64:   return 'targets/aarch64-linux/include'
        elif self.plugin == Plugin.Linux_X64:   return 'targets/x86_64-linux/include'
        else:                                   return ''

    def CudaLibDir(self):
        if self.plugin == Plugin.Linux_Arm64:   return 'targets/aarch64-linux/lib'
        elif self.plugin == Plugin.Linux_X64:   return 'targets/x86_64-linux/lib'
        else:                                   return ''

    def CudaFlags(self):
        if (self.cudaVersion >= '11.8'):        return self._cudaFlags11x8 + self._cudaFlagsCommon
        elif (self.cudaVersion >= '11.4'):      return self._cudaFlags11x4 + self._cudaFlagsCommon
        elif (self.cudaVersion >= '10.0'):      return self._cudaFlags10x0 + self._cudaFlagsCommon
        elif (self.cudaVersion >= '9.0'):       return self._cudaFlags9x0 + self._cudaFlagsCommon
        else:                                   return self._cudaFlags9x0 + self._cudaFlagsCommon

    def ClangCudaFlags(self):
        return self._clangCudaFlags

    def ForceCxx11(self, config):
        return True if 'c++11' in config.Template.split('|') else False

    def ForceCxx14(self, config):
        return True if 'c++14' in config.Template.split('|') else False

    def GetPlatformFlags(self):
        if self.plugin == Plugin.Linux_Arm64:   return self._compilerFlagsArm64
        elif self.plugin == Plugin.Linux_Arm7:  return self._compilerFlagsArm7
        elif self.plugin == Plugin.Linux_X64:   return self._compilerFlagsX64
        elif self.plugin == Plugin.Linux_X86:   return self._compilerFlagsX86
        else:                                   return []

    def GetConfigCxxFlags(self, config):
        if self.ForceCxx11(config):
            return self._cxx11Flags
        elif  self.ForceCxx14(config):
            return self._cxx14Flags
        else:
            return self._cxxFlags

    def GetPlatformCxxFlags(self):
        if self.plugin == Plugin.Linux_Arm7:    return self._platformCxxFlagsArm7
        else:                                   return []

    def HasAsm(self, project, config):
        for src in self.FilterSources(project, config):
            source = src.Name
            extension = os.path.splitext(source)[1]
            if extension == '.s' or extension == '.S' or extension == '.asm':
                return True
        return False

    def HasCudaSource(self, project):

        for src in self.SelectSources(project):
            extension = os.path.splitext(src.Name)[1]
            if extension == '.cu':
                return True

        return False

    def UseOpenMp(self, config):
        return True if 'OpenMp' in config.Template.split('|') else False

    def LinkerFlags(self):
        if self.plugin == Plugin.Linux_X86: return [ '-m32' ]
        else:                               return []

    def CompilerFlagsFromTemplate(self, selectionString):
        compilerFlags = []
        components = selectionString.split('|')

        for component in components:
            for tmp in self._compilerFlagTemplates:
                if tmp == component:
                    for flag in self._compilerFlagTemplates[tmp]:
                        compilerFlags.append(flag)

        for flag in self.GetPlatformFlags():
            compilerFlags.append(flag)

        if self.plugin == Plugin.Linux_Arm7:
            if 'NoNeon' not in components: # TODO: change default to no-NEON (if 'Neon' in components)
                compilerFlags.append('-mfpu=neon')
            else:
                compilerFlags.append('-mfpu=vfpv3')

        if 'NoPic' not in components:
            for flag in self._picCompilerFlags:
                compilerFlags.append(flag)

        if 'AutoExport' not in components:
            for flag in self._visibilityCompilerFlags:
                compilerFlags.append(flag)

        return compilerFlags

    # compiler flags specific to GCC (not clang)
    def CompilerFlagsGnu(self):
        return self._compilerFlagsGnu

    def SelectSourceCompilerFlags(self, config, sourceName):
        templates = config.Template.split('|')

        for sourceTemplate in config.SourceTemplates:
            if sourceTemplate.Name == sourceName:
                if sourceTemplate.Add != 'Exclude':
                    if sourceTemplate.Add != '':
                        for component in sourceTemplate.Add.split('|'):
                            templates.append(component)

                    if sourceTemplate.Remove != '':
                        for component in sourceTemplate.Remove.split('|'):
                            templates.remove(component)

        templateStr = '|'.join(templates)
        sourceCompilerFlags = ' '.join(self.CompilerFlagsFromTemplate(templateStr))
        defaultCompilerFlags = ' '.join(self.CompilerFlagsFromTemplate(config.Template))

        if sourceCompilerFlags != defaultCompilerFlags:
            return sourceCompilerFlags
        return ''

    def SelectFramework(self, project, config):

        for component in config.Template.split('|'):
            subcomponents = component.split('=')
            if len(subcomponents) == 1 and subcomponents[0] == 'FrameworkApp':
                return os.path.join(config.BinDir, 'kFramework')
            elif len(subcomponents) == 1 and subcomponents[0] == 'Framework':
                return os.path.join(config.BinDir, config.Target)
            elif len(subcomponents) == 2 and subcomponents[0] == 'Framework':
                return subcomponents[1]

        return None

    def SelectLinkScript(self, project, config):

        for component in config.Template.split('|'):
            subcomponents = component.split('=')
            if len(subcomponents) == 2 and subcomponents[0] == 'LinkScript':
                return subcomponents[1]

        return None

    def SelectSpecs(self, project, config):

        for component in config.Template.split('|'):
            subcomponents = component.split('=')
            if len(subcomponents) == 2 and subcomponents[0] == 'Specs':
                return subcomponents[1]

        return None

    def FilterSources(self, project, config):
        sources = []
        for src in self.SelectSources(project):

            excluded = False
            for sourceTemplate in config.SourceTemplates:
                if sourceTemplate.Name == src.Name:
                    if sourceTemplate.Add == 'Exclude':
                        excluded = True

            if not excluded:
                sources.append(src)

        return sources

    def WriteProject(self, project):
        return self.WriteProjectEx(project)

    def WriteProjectEx(self, project, forceUpdate=False):

        fileName = '%s%s' % (os.path.splitext(project.FileName)[0], self.ProjectSuffix)

        hasCudaSource = self.HasCudaSource(project)
        includeCudaSources = self.hasCuda and hasCudaSource

        with Utils.Output(fileName, Utils.EnvironmentStyle.Dollar, True, forceUpdate=forceUpdate) as output:

            utilsPath = os.path.normpath(os.path.join(os.path.dirname(os.path.realpath(__file__)), '..'))
            utilsRelPath = os.path.relpath(utilsPath, os.path.dirname(project.FileName))

            configurations = self.SelectConfigurations(project)

            if len(configurations) != 0:

                output.Format('\n')

                if self.regeneratorEnabled:
                    output.Format('export %s := 1\n' % (GeneratorEnvString()))
                    output.Format('export %s := %s\n' % (CudaVersionEnvString(self.plugin), self.cudaVersion))
                    output.Format('\n')

                output.Format('ifeq ($(OS)$(os), Windows_NT)\n')
                output.Format('\tXCOMPILE := 1\n')
                output.Format('\tOS_PREFIX := %s\n' % (GetOsPrefix(True)))
                output.Format('\tOS_SUFFIX := %s\n' % (GetOsSuffix(True)))
                output.Format('\tPYTHON := python\n')
                output.Format('\tMKDIR_P := $(PYTHON) %s mkdir_p\n' % os.path.join(utilsRelPath, 'kUtil.py'))
                output.Format('\tRM_F := $(PYTHON) %s rm_f\n' % os.path.join(utilsRelPath, 'kUtil.py'))
                output.Format('\tRM_RF := $(PYTHON) %s rm_rf\n' % os.path.join(utilsRelPath, 'kUtil.py'))
                output.Format('\tCP := $(PYTHON) %s cp\n' % os.path.join(utilsRelPath, 'kUtil.py'))
                if self.regeneratorEnabled:
                    output.Format('\tKGENERATOR := $(PYTHON) %s\n' % os.path.join(utilsRelPath, 'kGenerator.py'))
                output.Format('else\n')
                output.Format('\tBUILD_MACHINE := $(shell uname -m)\n')
                output.Format('\tifneq ($(BUILD_MACHINE), %s)\n' % GetUnameMArch(self.plugin))
                output.Format('\t\tXCOMPILE := 1\n')
                output.Format('\telse\n')
                output.Format('\t\tXCOMPILE := 0\n')
                output.Format('\tendif\n')
                output.Format('\tPYTHON := python3\n')
                output.Format('\tMKDIR_P := mkdir -p\n')
                output.Format('\tRM_F := rm -f\n')
                output.Format('\tRM_RF := rm -rf\n')
                output.Format('\tCP := cp\n')
                if self.regeneratorEnabled:
                    output.Format('\tKGENERATOR := $(PYTHON) %s\n' % os.path.join(utilsRelPath, 'kGenerator.py'))
                output.Format('endif\n')
                output.Format('\n')

                output.Format('TARGET_TRIPLET := %s\n' % (GetTriplet(self.plugin)))
                output.Format('\n')

                output.Format('ifeq ($(XCOMPILE),1)\n')
                output.Format('\tGCC_PATH := $(OS_PREFIX)/tools/%s/$(TARGET_TRIPLET)\n' % (GnuToolsDirectory(self.plugin)))
                output.Format('\tGCC_SYSROOT := $(GCC_PATH)/$(TARGET_TRIPLET)/libc\n')
                output.Format('\tGCC_PREFIX := $(GCC_PATH)/bin/$(TARGET_TRIPLET)-\n')
                output.Format('endif\n')
                output.Format('\n')

                output.Format('GNU_C_COMPILER := $(GCC_PREFIX)gcc$(GCC_SLOT_SUFFIX)$(OS_SUFFIX)\n')
                output.Format('GNU_CXX_COMPILER := $(GCC_PREFIX)g++$(GCC_SLOT_SUFFIX)$(OS_SUFFIX)\n')
                output.Format('GNU_LINKER := $(GCC_PREFIX)g++$(GCC_SLOT_SUFFIX)$(OS_SUFFIX)\n')
                output.Format('GNU_ARCHIVER := $(GCC_PREFIX)ar$(OS_SUFFIX)\n')
                output.Format('GNU_READELF := $(GCC_PREFIX)readelf$(OS_SUFFIX)\n')
                output.Format('\n')

                if includeCudaSources:
                    output.Format('CUDA_PATH := $(OS_PREFIX)/usr/local/%s\n' % self.CudaDirectory())
                    output.Format('NVCC_PREFIX := $(CUDA_PATH)/bin/\n')
                    output.Format('NVCC_COMPILER := $(NVCC_PREFIX)nvcc$(NVCC_SLOT_SUFFIX)$(OS_SUFFIX)\n')
                    output.Format('\n')

                    output.Format('CLANG_PATH := $(OS_PREFIX)/tools/%s\n' % LlvmDirectory(self.plugin))
                    output.Format('CLANG_PREFIX := $(CLANG_PATH)/bin/\n')
                    output.Format('CLANG_COMPILER := $(CLANG_PREFIX)clang$(CLANG_SLOT_SUFFIX)$(OS_SUFFIX)\n')
                    output.Format('\n')

                output.Format('KAPPGEN := $(PYTHON) %s\n' % os.path.join(utilsRelPath, 'kAppGen.py'))
                output.Format('\n')

                if self.regeneratorEnabled:
                    output.Format('# This can be overridden when debugging the build system itself.\n')
                    output.Format('regenerate := 1\n')
                    output.Format('\n')

                output.Format('ifndef verbose\n')
                output.Format('\tSILENT := @\n')
                output.Format('endif\n\n')

                output.Format('ifndef config\n')
                output.Format('\tconfig := %s\n' % (configurations[0].Name))
                output.Format('endif\n\n')

                output.Format('# We require GCC to be installed according to specific conventions (see manuals).\n')
                output.Format('# Tool prerequisites may change between major releases; check and report.\n')
                output.Format('ifeq ($(shell $(GNU_C_COMPILER) --version),)\n')
                output.Format('.PHONY: gcc_err\n')
                output.Format('gcc_err:\n')
                output.Format('\t$(error Cannot build because of missing prerequisite; please install GCC)\n')
                output.Format('endif\n\n')

                if includeCudaSources:
                    output.Format('# We require LLVM/Clang to be installed according to specific conventions (see manuals).\n')
                    output.Format('# Tool prerequisites may change between major releases; check and report.\n')
                    output.Format('ifeq ($(shell $(CLANG_COMPILER) --version),)\n')
                    output.Format('.PHONY: llvm_err\n')
                    output.Format('llvm_err:\n')
                    output.Format('\t$(error Cannot build because of missing prerequisite; please install LLVM/Clang)\n')
                    output.Format('endif\n\n')

                for config in configurations:

                    target = os.path.join(config.BinDir, config.Target)
                    targetExt = os.path.splitext(target)[1]

                    compilerFlags = self.CompilerFlagsFromTemplate(config.Template)
                    linkerFlags = self.LinkerFlags()
                    configType = config.Type
                    frameworkName = self.SelectFramework(project, config)

                    output.Format('ifeq ($(config),%s)\n' % (config.Name))

                    output.Format('\toptimize := %s\n' % (self.DefaultOptimize(config)))
                    output.Format('\tstrip := %u\n' % (self.DefaultStrip(config)))
                    output.Format('\twstack := %u\n' % (self.DefaultWStackUsage(config)))

                    if configType == 'Executable':
                        if frameworkName is not None:
                            appName = os.path.splitext(os.path.join(config.BinDir, config.Target))[0] + '.kapp'
                            output.Format('\tTARGET := %s\n' % (appName))
                            output.Format('\tINTERMEDIATES := %s/%s\n' % (config.BinDir, config.Target))
                        else:
                            output.Format('\tTARGET := %s/%s\n' % (config.BinDir, config.Target))
                            output.Format('\tINTERMEDIATES := \n')

                    elif configType == 'Shared':
                        if frameworkName is not None:
                            appName = os.path.splitext(os.path.join(config.BinDir, config.Target))[0] + '.kapp'
                            output.Format('\tTARGET := %s\n' % (appName))
                            output.Format('\tINTERMEDIATES := %s/%s\n' % (config.LibDir, config.Target))
                        else:
                            output.Format('\tTARGET := %s/%s\n' % (config.LibDir, config.Target))
                            output.Format('\tINTERMEDIATES := \n')

                    else:
                        output.Format('\tTARGET := %s/%s\n' % (config.LibDir, config.Target))
                        output.Format('\tINTERMEDIATES := \n')

                    output.Format('\tOBJ_DIR := %s/%s-%s-%s\n' % (config.TempDir, project.Name, self.TempName, config.Name))
                    output.Format('\tPREBUILD := %s\n' % (config.Prebuild))
                    output.Format('\tPOSTBUILD := %s\n' % (config.Postbuild))

                    output.Format('\tCOMPILER_FLAGS :=')
                    specsFile = self.SelectSpecs(project, config)
                    if (specsFile is not None):
                        output.Format(' -specs=%s' % (specsFile))
                    for flag in compilerFlags:
                        output.Format(' ')
                        output.Format('%s' % (flag))
                    output.Format('\n')

                    output.Format('\tC_FLAGS :=')
                    for flag in self._cFlags:
                        output.Format(' ')
                        output.Format('%s' % (flag))
                    output.Format('\n')

                    output.Format('\tCXX_FLAGS :=')
                    cxxFlags = self.GetConfigCxxFlags(config)
                    platformCxxFlags = self.GetPlatformCxxFlags()
                    for flag in cxxFlags + platformCxxFlags:
                        output.Format(' ')
                        output.Format('%s' % (flag))
                    output.Format('\n')

                    autoIncludes = []
                    if includeCudaSources:
                        autoIncludes.append('$(CUDA_PATH)/%s' % (self.CudaIncludeDir()))

                    output.Format('\tINCLUDE_DIRS :=')
                    for dir in config.IncludeDirs + autoIncludes:
                        output.Format(' ')
                        output.Format('-I%s' % (dir))
                    output.Format('\n')

                    if self.HasAsm(project, config):
                        output.Format('\tASM_INCLUDE_DIRS :=')
                        for dir in config.IncludeDirs:
                            output.Format(' ')
                            output.Format('-Wa,-I%s' % (dir))
                        output.Format('\n')

                    autoDefines = []
                    if includeCudaSources:
                        autoDefines.append('K_HAVE_CUDA')

                    output.Format('\tDEFINES :=')
                    for define in config.Defines + autoDefines:
                        output.Format(' ')
                        output.Format('-D%s' % (define))
                    output.Format('\n')

                    linkScript = self.SelectLinkScript(project, config)
                    output.Format('\tLINKER_FLAGS :=')
                    if linkScript is None:
                        for flag in linkerFlags:
                            output.Format(' ')
                            output.Format('%s' % (flag))

                        if configType == 'Shared':
                            output.Format(' ')
                            output.Format('-shared')

                        if configType == 'Executable' or configType == 'Shared':
                            # for NodeJs we'll allow undefined references to replicate node-gyp behavior
                            if targetExt != '.node':
                                output.Format(' ')
                                output.Format('-Wl,-no-undefined')

                            # for executables we'll allow undefined references in shared libraries that we link against
                            if configType == 'Executable':
                                output.Format(' ')
                                output.Format('-Wl,--allow-shlib-undefined')

                            # use the $ORIGIN variable known by GNU LD to support portable apps
                            # which can run wherever and locate shared libraries with relative paths.
                            # use -rpath-link, to prevent apps from having to explicitly link to secondary libraries.
                            if not configType == 'Shared':
                                output.Format(' -Wl,-rpath,\'$$ORIGIN/%s\'' % (os.path.relpath(config.LibDir, config.BinDir)))
                                output.Format(' -Wl,-rpath-link,%s' % (config.LibDir))
                            else:
                                output.Format(' -Wl,-rpath,\'$$ORIGIN\'')

                            # use O1 for linking, which slightly reduces the size of the executable
                            if not self.HasSymbols(config):
                                output.Format(' ')
                                output.Format('-Wl,-O1')

                            # use hash-style=gnu, which has been supported in GNU/Linux for a long time, but may not be the default
                            output.Format(' ')
                            output.Format('-Wl,--hash-style=gnu')

                    else:
                        tmpDir = '%s/%s-%s-%s' % (config.TempDir, project.Name, self.TempName, config.Name)
                        mapFile = '%s/%s.map' % (tmpDir, os.path.splitext(os.path.basename(config.Target))[0])
                        specsFile = self.SelectSpecs(project, config)
                        if (specsFile is not None):
                            output.Format(' ')
                            output.Format('-Wl,-T,%s -Wl,--Map=%s' % (linkScript, mapFile))
                            output.Format(' -specs=%s ' % (specsFile))
                        else:
                            output.Format(' ')
                            output.Format('-nostartfiles -nostdlib -Wl,-T,%s -Wl,--Map=%s' % (linkScript, mapFile))

                    output.Format('\n')

                    autoLibDirs = []
                    if includeCudaSources:
                        autoLibDirs.append('$(CUDA_PATH)/%s' % (self.CudaLibDir()))

                    output.Format('\tLIB_DIRS :=')
                    for libDir in config.LibraryDirs + autoLibDirs:
                        output.Format(' ')
                        output.Format('-L%s' % (libDir))
                    output.Format('\n')

                    autoLibs = []
                    if includeCudaSources:
                        autoLibs.append('cudart')
                    if self.UseOpenMp(config):
                        autoLibs.append('gomp')

                    output.Format('\tLIBS :=')
                    if len(config.Libs) != 0:
                        output.Format(' -Wl,--start-group')
                        for lib in config.Libs + autoLibs:
                            output.Format(' -l%s' % (lib))
                        output.Format(' -Wl,--end-group')
                    output.Format('\n')

                    output.Format('\tifneq ($(optimize),0)\n')
                    output.Format('\t\tCOMPILER_FLAGS += -O$(optimize)\n')
                    output.Format('\tendif\n')

                    output.Format('\tifeq ($(strip),1)\n')
                    output.Format('\t\tLINKER_FLAGS += -Wl,--strip-debug\n')
                    output.Format('\tendif\n')
                    output.Format('\tifeq ($(strip),2)\n')
                    output.Format('\t\tLINKER_FLAGS += -Wl,--strip-all\n')
                    output.Format('\tendif\n')

                    if self.plugin == Plugin.Linux_Arm7:
                        output.Format('\tifeq ($(thumb),1)\n')
                        output.Format('\t\tCOMPILER_FLAGS += -mthumb\n')
                        output.Format('\tendif\n')

                    # Instrumentation is not supported on bare metal
                    if self.SelectLinkScript(project, config) is None:
                        output.Format('\tifdef profile\n')
                        output.Format('\t\tCOMPILER_FLAGS += -pg\n')
                        output.Format('\t\tLINKER_FLAGS += -pg\n')
                        output.Format('\tendif\n')

                        output.Format('\tifdef coverage\n')
                        output.Format('\t\tCOMPILER_FLAGS += --coverage -fprofile-arcs -ftest-coverage\n')
                        output.Format('\t\tLINKER_FLAGS += --coverage\n')
                        output.Format('\t\tLIBS += -lgcov\n')
                        output.Format('\tendif\n')

                        output.Format('\tifdef sanitize\n')
                        output.Format('\t\tCOMPILER_FLAGS += -fsanitize=$(sanitize)\n')
                        output.Format('\t\tLINKER_FLAGS += -fsanitize=$(sanitize)\n')
                        output.Format('\tendif\n')

                    # TODO: remove this warning fix for bare metal (from FPT-2412).
                    if self.SelectLinkScript(project, config) is not None:
                        output.Format('\n')
                        output.Format('\t# FPT-2412 warning fix for bare metal.\n')
                        output.Format('\tLINKER_FLAGS += -Wl,--no-warn-rwx-segments\n')
                        output.Format('\tLINKER_FLAGS += -z noexecstack\n')
                        output.Format('\n')

                    output.Format('\tGNU_COMPILER_FLAGS := $(COMPILER_FLAGS)')
                    for flag in self.CompilerFlagsGnu():
                        output.Format(' %s' % (flag))
                    output.Format('\n')
                    output.Format('\tifneq ($(wstack),0)\n')
                    output.Format('\t\tGNU_COMPILER_FLAGS += -Wstack-usage=$(wstack)\n')
                    output.Format('\tendif\n')

                    if includeCudaSources:
                        output.Format('\tCLANG_FLAGS := $(COMPILER_FLAGS) --stdlib=libstdc++\n')

                        output.Format('\tifeq ($(XCOMPILE),1)\n')
                        output.Format('\t\tCLANG_FLAGS += --target=$(TARGET_TRIPLET) --gcc-toolchain=$(GCC_PATH) --sysroot=$(GCC_SYSROOT)\n')
                        output.Format('\tendif\n')

                        output.Format('\tCLANG_CUDA_FLAGS := $(CLANG_FLAGS)')
                        for flag in self.ClangCudaFlags():
                            output.Format(' %s' % (flag))
                        output.Format(' --cuda-path=$(CUDA_PATH)')
                        output.Format('\n')
                        output.Format('\tNVCC_FLAGS :=')
                        if True:
                            output.Format(' -ccbin $(GNU_CXX_COMPILER)')
                        for flag in self.CudaFlags():
                            output.Format(' %s' % (flag))
                        output.Format('\n')

                    output.Format('\tOBJECTS := ')
                    first = True
                    for src in self.FilterSources(project, config):
                        source = src.Name
                        extension = os.path.splitext(source)[1]
                        if (extension == '.c' or extension == '.cpp' or extension == '.cc' or (extension == '.cu' and includeCudaSources) 
                            or extension == '.s' or extension == '.S' or extension == '.asm'):
                                if not first:
                                    output.Format(' \\\n\t', False)
                                first = False
                                output.Format('%s/%s-%s-%s/%s.o' % (config.TempDir, project.Name, self.TempName, config.Name, os.path.basename(src.Name)))
                    output.Format('\n')

                    output.Format('\tDEP_FILES = ')
                    first = True
                    for src in self.FilterSources(project, config):
                        source = src.Name
                        extension = os.path.splitext(source)[1]
                        if extension == '.c' or extension == '.cpp' or extension == '.cc' or (extension == '.cu' and includeCudaSources):
                            if not first:
                                output.Format(' \\\n\t', False)
                            first = False
                            output.Format('%s/%s-%s-%s/%s.d' % (config.TempDir, project.Name, self.TempName, config.Name, os.path.basename(src.Name)))
                    output.Format('\n')

                    #
                    # List output files of dependent projects as target dependencies. 
                    # Targets from dependent projects are listed as dependencies here by matching the configuration name
                    # 
                    output.Format('\tTARGET_DEPS = ')
                    first = True
                    for dep in self.SelectDependencies(project):
                        depConfigs = self.SelectConfigurations(dep)
                        for depConfig in depConfigs:
                            if depConfig.Name == config.Name:

                                projectPath = os.path.relpath(os.path.dirname(dep.FileName), os.path.dirname(project.FileName))
                                target = os.path.join(projectPath, (depConfig.BinDir if depConfig.Type == 'Executable' else depConfig.LibDir), depConfig.Target)

                                if not first:
                                    output.Format(' \\\n\t', False)
                                first = False
                                output.Format('%s' % (target))

                    output.Format('\n\n')
                    output.Format('endif\n\n')

                output.Format('.PHONY: all all-obj all-dep clean\n\n')

                output.Format('all: $(OBJ_DIR)\n')
                output.Format('\t$(PREBUILD)\n')
                output.Format('\t$(SILENT) $(MAKE) -f %s%s all-dep\n' % (project.Name, self.ProjectSuffix))
                output.Format('\t$(SILENT) $(MAKE) -f %s%s all-obj\n' % (project.Name, self.ProjectSuffix))
                output.Format('\n')

                output.Format('clean:\n')
                output.Format('\t$(SILENT) $(info Cleaning $(OBJ_DIR))\n')
                output.Format('\t$(SILENT) $(RM_RF) $(OBJ_DIR)\n')
                output.Format('\t$(SILENT) $(info Cleaning $(TARGET) $(INTERMEDIATES))\n')
                output.Format('\t$(SILENT) $(RM_F) $(TARGET) $(INTERMEDIATES)\n')
                output.Format('\n')

                output.Format('all-obj: $(OBJ_DIR) $(TARGET)\n')
                output.Format('all-dep: $(OBJ_DIR) $(DEP_FILES)\n')
                output.Format('\n')

                if self.regeneratorEnabled:
                    output.Format('$(OBJ_DIR): %s%s\n' % (project.Name, self.ProjectSuffix))
                    output.Format('\t$(info Cleanse $(OBJ_DIR))\n')
                    output.Format('\t$(SILENT) $(RM_RF) $(OBJ_DIR)\n')
                    output.Format('\t$(SILENT) $(MKDIR_P) $(OBJ_DIR)\n')
                    output.Format('\n')
                else:
                    output.Format('$(OBJ_DIR):\n')
                    output.Format('\t$(SILENT) $(MKDIR_P) $@\n')
                    output.Format('\n')

                if self.regeneratorEnabled:
                    solFileNameRel = os.path.relpath(project.Solution.FileName, os.path.dirname(project.FileName))

                    output.Format('ifeq ($(regenerate),1)\n')
                    output.Format('ifeq ($(MAKECMDGOALS),)\n')
                    output.Format('ifeq ($(MAKE_RESTARTS),)\n')

                    output.Format('%s%s: %s.xml %s %s ' % (project.Name, self.ProjectSuffix, project.Name, solFileNameRel, os.path.join(utilsRelPath, 'Generator', 'GnuMk.py')))
                    for dep in self.SelectDependencies(project):
                        projectPath = os.path.relpath(os.path.dirname(dep.FileName), os.path.dirname(project.FileName))
                        output.Format('%s.xml ' % os.path.join(projectPath, dep.Name))
                    output.Format('\n')

                    output.Format('\t$(info Regen%s %s%s)\n' % (MsgSuffix(self.plugin), project.Name, self.ProjectSuffix))
                    output.Format('\t$(SILENT) $(KGENERATOR) --writers=%s --platforms=%s --project=%s %s\n' % (self.Name, configurations[0].Platform, project.Name, solFileNameRel))
                    output.Format('endif\n')
                    output.Format('endif\n')
                    output.Format('endif\n')
                    output.Format('\n')

                for config in configurations:

                    output.Format('ifeq ($(config),%s)\n\n' % (config.Name))

                    configType = config.Type
                    frameworkName = self.SelectFramework(project, config)

                    if configType == 'Executable':
                        if frameworkName is not None:
                            target = os.path.splitext(os.path.join(config.BinDir, config.Target))[0] + '.kapp'
                            interName = os.path.join(config.BinDir, config.Target)

                            output.Format('%s: $(OBJECTS) $(TARGET_DEPS)\n' % (interName))
                            output.Format('\t$(SILENT) $(info Ld%s %s)\n' % (MsgSuffix(self.plugin), interName))
                            output.Format('\t$(SILENT) $(GNU_LINKER) $(OBJECTS) $(LINKER_FLAGS) $(LIBS) $(LIB_DIRS) -o%s\n\n' % (interName))

                            output.Format('$(TARGET): %s\n' % (interName))
                            output.Format('\t$(SILENT) $(info Tar%s $(TARGET))\n' % (MsgSuffix(self.plugin)))
                            output.Format('\t$(SILENT) $(KAPPGEN) $(GNU_READELF) %s %s\n' % (frameworkName, target))

                        else:
                            output.Format('$(TARGET): $(OBJECTS) $(TARGET_DEPS)\n')
                            output.Format('\t$(SILENT) $(info Ld%s $(TARGET))\n' % (MsgSuffix(self.plugin)))
                            output.Format('\t$(SILENT) $(GNU_LINKER) $(OBJECTS) $(LINKER_FLAGS) $(LIBS) $(LIB_DIRS) -o$(TARGET)\n\n')

                    elif configType == 'Shared':
                        if frameworkName is not None:
                            target = os.path.splitext(os.path.join(config.BinDir, config.Target))[0] + '.kapp'
                            interName = os.path.join(config.LibDir, config.Target)

                            output.Format('%s: $(OBJECTS) $(TARGET_DEPS)\n' % (interName))
                            output.Format('\t$(SILENT) $(info Ld%s %s)\n' % (MsgSuffix(self.plugin), interName))
                            output.Format('\t$(SILENT) $(GNU_LINKER) $(OBJECTS) $(LINKER_FLAGS) $(LIBS) $(LIB_DIRS) -o%s\n\n' % (interName))

                            output.Format('$(TARGET): %s\n' % (interName))
                            output.Format('\t$(SILENT) $(info Tar%s $(TARGET))\n' % (MsgSuffix(self.plugin)))
                            output.Format('\t$(SILENT) $(KAPPGEN) $(GNU_READELF) %s --application %s %s\n' % (frameworkName, interName, target))

                        else:
                            output.Format('$(TARGET): $(OBJECTS) $(TARGET_DEPS)\n')
                            output.Format('\t$(SILENT) $(info Ld%s $(TARGET))\n' % (MsgSuffix(self.plugin)))
                            output.Format('\t$(SILENT) $(GNU_LINKER) $(OBJECTS) $(LINKER_FLAGS) $(LIBS) $(LIB_DIRS) -o$(TARGET)\n\n')

                    elif configType == 'Library':
                        output.Format('$(TARGET): $(OBJECTS) $(TARGET_DEPS)\n')
                        output.Format('\t$(SILENT) $(info Ar%s $(TARGET))\n' % (MsgSuffix(self.plugin)))
                        output.Format('\t$(SILENT) $(RM_F) $(TARGET)\n')
                        output.Format('\t$(SILENT) $(GNU_ARCHIVER) rcs $(TARGET) $(OBJECTS)\n\n')

                    else:
                        raise Exception('Unknown configuration type')

                    output.Format('endif\n\n')

                if linkScript is not None:
                    output.Format('$(TARGET): %s\n' % (linkScript))
                    output.Format('\n')

                for config in configurations:

                    output.Format('ifeq ($(config),%s)\n\n' % (config.Name))
                    tmpDir = '%s/%s-%s-%s' % (config.TempDir, project.Name, self.TempName, config.Name)

                    for src in self.SelectSources(project):
                        source = src.Name
                        extension = os.path.splitext(source)[1]

                        if extension == '.c' or extension == '.cpp' or extension == '.cc':
                            ppdSource = '%s/%s.d' % (tmpDir, os.path.basename(src.Name))
                            objectFile = '%s/%s.o' % (tmpDir, os.path.basename(src.Name))

                            output.Format('%s %s: ' % (objectFile, ppdSource))
                            output.Format('%s\n' % (src.Name))

                            compilerFlags = self.SelectSourceCompilerFlags(config, src.Name)

                            if len(compilerFlags) == 0:
                                if extension == '.c':
                                    output.Format('\t$(SILENT) $(info Gcc%s %s)\n' % (MsgSuffix(self.plugin), src.Name))
                                    output.Format('\t$(SILENT) $(GNU_C_COMPILER) $(GNU_COMPILER_FLAGS) $(C_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o %s.o -c %s -MMD -MP\n' % \
                                        (os.path.join(tmpDir, os.path.basename(src.Name)), src.Name))

                                else:
                                    output.Format('\t$(SILENT) $(info Gcc%s %s)\n' % (MsgSuffix(self.plugin), src.Name))
                                    output.Format('\t$(SILENT) $(GNU_CXX_COMPILER) $(GNU_COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o %s.o -c %s -MMD -MP\n' % \
                                        (os.path.join(tmpDir, os.path.basename(src.Name)), src.Name))

                            else:
                                if extension == '.c':
                                    output.Format('\t$(SILENT) $(info Gcc%s %s)\n' % (MsgSuffix(self.plugin), src.Name))
                                    output.Format('\t$(SILENT) $(GNU_C_COMPILER) $(C_FLAGS) %s $(DEFINES) $(INCLUDE_DIRS) -o %s.o -c %s -MMD -MP\n' % \
                                        (compilerFlags, os.path.join(tmpDir, os.path.basename(src.Name)), src.Name))

                                else:
                                    output.Format('\t$(SILENT) $(info Gcc%s %s)\n' % (MsgSuffix(self.plugin), src.Name))
                                    output.Format('\t$(SILENT) $(GNU_CXX_COMPILER) $(CXX_FLAGS) %s $(DEFINES) $(INCLUDE_DIRS) -o %s.o -c %s -MMD -MP\n' % \
                                        (compilerFlags, os.path.join(tmpDir, os.path.basename(src.Name)), src.Name))

                            output.Format('\n')

                        elif extension == '.s' or extension == '.S' or extension == '.asm':
                            objectFile = '%s/%s.o' % (tmpDir, os.path.basename(src.Name))

                            output.Format('%s: ' % (objectFile))
                            output.Format('%s\n' % (src.Name))

                            output.Format('\t$(SILENT) $(info Gcc%s %s)\n' % (MsgSuffix(self.plugin), src.Name))
                            output.Format('\t$(SILENT) $(GNU_C_COMPILER) $(GNU_COMPILER_FLAGS) -x assembler-with-cpp $(DEFINES) $(INCLUDE_DIRS) $(ASM_INCLUDE_DIRS) -o %s.o -c %s\n' % (
                                os.path.join(tmpDir, os.path.basename(src.Name)), src.Name))

                            output.Format('\n')

                        elif extension == '.cu' and includeCudaSources:
                            ppdSource = '%s/%s.d' % (tmpDir, os.path.basename(src.Name))
                            objectFile = '%s/%s.o' % (tmpDir, os.path.basename(src.Name))

                            output.Format('ifeq ($(cuda_use_nvcc),1)\n')
                            output.Format('%s %s: ' % (objectFile, ppdSource))
                            output.Format('%s\n' % (src.Name))
                            if float(self.cudaVersion) < 10.2:
                                # Cuda < 10.2 does not support "--generate-dependencies-with-compile" (-MD/-MMD)
                                output.Format('\t$(SILENT) $(info CudaCc%s %s)\n' % (MsgSuffix(self.plugin), src.Name))
                                output.Format('\t$(SILENT) $(NVCC_COMPILER) $(NVCC_FLAGS) --compiler-options="$(GNU_COMPILER_FLAGS) $(DEFINES) $(INCLUDE_DIRS)" -o %s.d -M %s --output-directory="%s"\n' % \
                                    (os.path.join(tmpDir, os.path.basename(src.Name)), src.Name, tmpDir))
                                output.Format('\t$(SILENT) $(NVCC_COMPILER) $(NVCC_FLAGS) --compiler-options="$(GNU_COMPILER_FLAGS) $(DEFINES) $(INCLUDE_DIRS)" -o %s.o -c %s\n' % \
                                    (os.path.join(tmpDir, os.path.basename(src.Name)), src.Name))
                            else:
                                output.Format('\t$(SILENT) $(info CudaCc%s %s)\n' % (MsgSuffix(self.plugin), src.Name))
                                output.Format('\t$(SILENT) $(NVCC_COMPILER) $(NVCC_FLAGS) --compiler-options="$(GNU_COMPILER_FLAGS) $(DEFINES) $(INCLUDE_DIRS)" -o %s.o -c %s -MMD\n' % \
                                    (os.path.join(tmpDir, os.path.basename(src.Name)), src.Name))
                            output.Format('else\n')
                            output.Format('%s %s: ' % (objectFile, ppdSource))
                            output.Format('%s\n' % (src.Name))
                            output.Format('\t$(SILENT) $(info Clang%s %s)\n' % (MsgSuffix(self.plugin), src.Name))
                            output.Format('\t$(SILENT) $(CLANG_COMPILER) $(CLANG_CUDA_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o %s.o -c %s -MMD -MP\n' % \
                                (os.path.join(tmpDir, os.path.basename(src.Name)), src.Name))
                            output.Format('endif\n')
                            output.Format('\n')

                    output.Format('endif\n\n')

                output.Format('ifeq ($(MAKECMDGOALS),all-obj)\n\n')
                for config in configurations:
                    output.Format('ifeq ($(config),%s)\n\n' % (config.Name))

                    for src in self.FilterSources(project, config):
                        source = src.Name
                        extension = os.path.splitext(source)[1]
                        if extension == '.c' or extension == '.cpp' or extension == '.cc' or (extension == '.cu' and includeCudaSources):
                            ppdSource = '%s/%s-%s-%s/%s.d' % (config.TempDir, project.Name, self.TempName, config.Name, os.path.basename(src.Name))
                            output.Format('include %s\n' % (ppdSource))

                    output.Format('\n')
                    output.Format('endif\n\n')
                output.Format('endif\n\n')

    def WriteSolution(self, solution):

        fileName = '%s%s' % (os.path.join(os.path.dirname(solution.FileName), solution.Name), self.SolutionSuffix)

        with Utils.Output(fileName, Utils.EnvironmentStyle.Dollar, True) as output:

            output.Format('ifndef verbose\n')
            output.Format('\tSILENT = @\n')
            output.Format('endif\n\n')        

            output.Format('.PHONY: all\n')
            output.Format('all: ')

            for project in self.SelectProjects(solution):
                output.Format('%s ' % (project.Name))
            output.Format('\n\n')

            for project in self.SelectProjects(solution):
                output.Format('.PHONY: %s\n' % (project.Name))
                output.Format('%s: ' % (project.Name))

                for dep in self.SelectDependencies(project):
                    output.Format('%s ' % (dep.Name))
                output.Format('\n')

                output.Format('\t$(SILENT) $(MAKE) -C %s -f %s%s\n\n' % (
                    os.path.relpath(os.path.dirname(project.FileName), os.path.dirname(solution.FileName)),
                    project.Name,
                    self.ProjectSuffix))

            output.Format('.PHONY: clean\n')
            output.Format('clean: ')

            for project in self.SelectProjects(solution):
                output.Format('%s-clean ' % (project.Name))
            output.Format('\n\n')

            for project in self.SelectProjects(solution):
                output.Format('.PHONY: %s-clean\n' % (project.Name))
                output.Format('%s-clean:\n' % (project.Name))

                output.Format('\t$(SILENT) $(MAKE) -C %s -f %s%s clean\n\n' % (
                    os.path.relpath(os.path.dirname(project.FileName), os.path.dirname(solution.FileName)),
                    project.Name,
                    self.ProjectSuffix))

            output.Format('\n')
