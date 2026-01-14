#!/usr/bin/python
# This file is part of the FireSync Project Generator.
# Copyright (C) 2008-2016 by LMI Technologies Inc.  All rights reserved.
# Distributed under the terms of the MIT License.
# Redistributed files must retain the above copyright notice.

import re
import os
import uuid
import sys
from . import Utils
from . import Solution
from . import Project
from . import WriterBase
from . import Xml

class MsvcVersion:
    Msvc2013        = 0
    Msvc2015        = 1
    Msvc2017        = 2
    Msvc2019        = 3
    Msvc2022        = 4

class CxxLanguage:
    Cxx1998         = 0
    Cxx2011         = 1
    Cxx2014         = 2
    Cxx2017         = 3
    Cxx2020         = 4

class SourceType:
    Xaml            = 0
    XamlDef         = 1
    Source          = 2
    Code            = 3
    Form            = 4
    Designres       = 5
    Design          = 6
    AppIcon         = 7
    Resource        = 8
    Settings        = 9
    Header          = 10
    Manifest        = 11
    Misc            = 12
    CudaSource      = 13
    Self            = 14

def GetPlatforms(version):
    return [
        'Win32',
        'Win64',
        'Linux_Arm64',
        'Linux_Arm7',
        'Linux_X64',
        #'Linux_X86',  # Disabled: not a prominent platform feature
        'Any',
        'Doc',
        'Sensor',
    ]

def CudaVersionThrow():
    return os.environ['K_MSVC_CUDA_VERSION']
    
def CudaVersion():
    try:
        return CudaVersionThrow()
    except KeyError:
        return '9.0'

def HasCuda():
    try:
        if CudaVersionThrow() != '0':
            return True
    except KeyError:
        pass

    return False

# Some legacy/history. Now by default off, unless explicitly enabled via either variable (needs to be >= 2).
# Know what you ask for: this feature may not be experienced as pleasent on Visual Studio.
def ReGeneratorEnabled():

    envNew = Utils.EnvInt('K_REGENERATOR')
    envOld = Utils.EnvInt('K_GENERATOR_ENABLED')

    if (envNew >= 2 or envOld >= 2):
        return True

    return False

def GetProjects(version):
    supportedProjectTypes = [ 
        'C/CPP',
        'CPP/CLI',
    ]

    if (version >= MsvcVersion.Msvc2017):
        supportedProjectTypes.append('C#')

    return supportedProjectTypes

def GetBuildModule(platformName):
    module = None

    if platformName == 'Linux_Arm64' or platformName == 'Linux_Arm7' or platformName == 'Linux_X64' or platformName == 'Linux_X86':
        from . import GnuMk as module

    return module

def BuildCmd(project, config):
    module = GetBuildModule(config.Platform)

    if module is not None:
        return module.BuildCmd(project, config)
    else:
        return ''

def RebuildCmd(project, config):
    module = GetBuildModule(config.Platform)

    if module is not None:
        return module.RebuildCmd(project, config)
    else:
        return ''

def CleanCmd(project, config):
    module = GetBuildModule(config.Platform)

    if module is not None:
        return module.CleanCmd(project, config)
    else:
        return ''

class MsvcWriter(WriterBase.WriterBase):

    def __init__(self, userPlatforms, userProjects, version, name, suffix):
        WriterBase.WriterBase.__init__(self, GetPlatforms(version), userPlatforms, GetProjects(version), userProjects)
        self.version = version
        self.name = name
        self.suffix = suffix
        self.cudaVersion = CudaVersion()
        self.cudaMajor = int(float(self.cudaVersion))
        self.hasCuda = HasCuda()
        self.regeneratorEnabled = ReGeneratorEnabled()

    @property
    def Name(self):
        return self.name

    @property
    def Suffix(self):
        return self.suffix

    def ProjectName(self, project):
        return project.Name + self.suffix

    def SolutionName(self, solution):
        return solution.Name + self.suffix

    def IsDpiUnaware(self, config):
        return True if 'DpiUnaware' in config.Template.split('|') else False

    def LinkKey(self, config):
        for component in config.Template.split('|'):
            subcomponents = component.split('=')
            if len(subcomponents) == 2 and subcomponents[0] == 'LinkKey':
                return subcomponents[1]

        return None

    def ProjectExt(self, project):
        if project.Type == 'C#':
            return '.csproj'
        else:
            return '.vcxproj'

    def SourceTypeFromTypeName(self, typeName):
        if typeName == 'source':
            return SourceType.Source
        elif typeName == 'resource':
            return SourceType.Resource
        elif typeName == 'header':
            return SourceType.Header
        elif typeName == 'manifest':
            return SourceType.Manifest
        elif typeName == 'self':
            return SourceType.Self
        else:
            return 'misc'

    def DetermineSourceType(self, source):

        if source.Type != '':
            return self.SourceTypeFromTypeName(source.Type)
        else:
            # auto-detection
            fileName = source.Name
            extension = os.path.splitext(fileName)[1]

            if extension == '.h':
                return SourceType.Header
            elif extension == '.rc':
                return SourceType.Resource
            elif extension == '.manifest':
                return SourceType.Manifest
            elif extension == '.c' or extension == '.cc' or extension == '.cpp' or extension == '.cxx' or extension == '.cs':
                return SourceType.Source
            elif extension == '.cu':
                return SourceType.CudaSource
            else:
                return SourceType.Misc

    def SolutionExt(self):
        return '.sln'

    def ProjectUuid(self, name):
        # + '.project' avoids potential name clash with groups
        return str(uuid.uuid5(uuid.NAMESPACE_DNS, name + '.project')).upper()

    def GroupUuid(self, name):
        # + '.group' avoids potential name clash with projects
        return str(uuid.uuid5(uuid.NAMESPACE_DNS, name + '.group')).upper()

    def FilterId(self, filterName):
        return str(uuid.uuid5(uuid.NAMESPACE_DNS, filterName))

    def ProjectId(self, project):
        # the project ID is generated from the project name and must be unique
        return self.ProjectUuid(project.Name)

    def GroupId(self, group):
        # the group ID is generated from the group name and must be unique
        return self.GroupUuid(group.Name)

    def TempDir(self):
        if self.version == MsvcVersion.Msvc2013:    return '$(ProjectName)-$(Configuration)-$(PlatformName)'
        elif self.version == MsvcVersion.Msvc2015:  return '$(ProjectName)-$(Configuration)-$(PlatformName)'
        elif self.version == MsvcVersion.Msvc2017:  return '$(ProjectName)-$(Configuration)-$(PlatformName)'
        elif self.version == MsvcVersion.Msvc2019:  return '$(ProjectName)-$(Configuration)-$(PlatformName)'
        elif self.version == MsvcVersion.Msvc2022:  return '$(ProjectName)-$(Configuration)-$(PlatformName)'
        else:                                       return ''

    def IntDir(self, config):

        # in VS2017 the build logs are intermittently placed relative to the solution
        # this must be a bug, so we prepend $(ProjectDir) for now
        if self.version >= MsvcVersion.Msvc2017:
            return '$(ProjectDir)' + config.TempDir + '\\' + self.TempDir() + '\\'
        else:
            return config.TempDir + '\\' + self.TempDir() + '\\'

    def SolutionFormatVersion(self):
        if self.version == MsvcVersion.Msvc2013:    return '12.00'
        elif self.version == MsvcVersion.Msvc2015:  return '12.00'
        elif self.version == MsvcVersion.Msvc2017:  return '12.00'
        elif self.version == MsvcVersion.Msvc2019:  return '12.00'
        elif self.version == MsvcVersion.Msvc2022:  return '12.00'
        else:                                       return ''

    def SolutionStudioVersion(self):
        if self.version == MsvcVersion.Msvc2013:    return '2013'
        elif self.version == MsvcVersion.Msvc2015:  return '14'
        elif self.version == MsvcVersion.Msvc2017:  return '15'
        elif self.version == MsvcVersion.Msvc2019:  return '16'
        elif self.version == MsvcVersion.Msvc2022:  return '17'
        else:                                       return ''

    def ProjectVersion(self):
        if self.version == MsvcVersion.Msvc2013:    return '12.0'
        elif self.version == MsvcVersion.Msvc2015:  return '14.0'
        elif self.version == MsvcVersion.Msvc2017:  return '15.0'
        elif self.version == MsvcVersion.Msvc2019:  return '16.0'
        elif self.version == MsvcVersion.Msvc2022:  return None
        else:                                       return None

    def TargetPlatformVersion(self):
        # multiple versions are supported, each VS release -- allow user to override.
        try:
            return os.environ['K_MSVC_SDK_VERSION']
        except KeyError:
            pass

        if self.version == MsvcVersion.Msvc2017:    return '10.0.15063.0'
        elif self.version == MsvcVersion.Msvc2019:  return '10.0'
        elif self.version == MsvcVersion.Msvc2022:  return '10.0'
        else:                                       return None

    def FrameworkVersion(self):
        # multiple versions are supported, each VS release -- allow user to override.
        try:
            return os.environ['K_MSVC_NET_VERSION']
        except KeyError:
            pass

        # return desired framework version, (without 'v' prefix used in VS>=2010).
        if self.version == MsvcVersion.Msvc2013:    return '4.5'
        elif self.version == MsvcVersion.Msvc2015:  return '4.6'
        elif self.version == MsvcVersion.Msvc2017:  return '4.6.1'
        elif self.version == MsvcVersion.Msvc2019:  return '4.6.1'
        elif self.version == MsvcVersion.Msvc2022:  return '4.7.2'
        else:                                       return ''

    def ConvertType(self, config):
        if (config.Platform == 'Linux_Arm64' or
            config.Platform == 'Linux_Arm7' or
            config.Platform == 'Linux_X64' or
            config.Platform == 'Linux_X86' or
            config.Platform == 'Doc' or
            config.Platform == 'Sensor'): 
                                                    return 'Runner'
        else:                                       return config.Type

    def UseX86(self):
        if self.version >= MsvcVersion.Msvc2015:
            try:
                if os.environ['K_MSVC_USE_X86'] == '1':
                    return True
            except KeyError:
                pass

        return False

    def ConvertPlatform(self, platformName, usePseudo = True, isProject = True, isCSharp = False):

        if isProject:
            useX86 = isCSharp
        else:
            useX86 = self.UseX86()

        # Visual Studio sorts the entries in the "Solution Platforms" listbox based on alphabetical order.
        # The only exception to this is "Win32" or "x86", which will always come first in this listbox.
        # We prefix all pseudo projects with an "x" so that "x64" is the first platform if "Win32"
        # or "x86" is not available in the solution.

        if platformName == 'Any':           return 'AnyCPU' if isProject else 'Any CPU'
        elif platformName == 'Win32':       return 'x86' if useX86 else 'Win32'
        elif platformName == 'Win64':       return 'x64'
        elif platformName == 'Linux_Arm64': return 'xLinux_Arm64' if usePseudo else 'x64'
        elif platformName == 'Linux_Arm7':  return 'xLinux_Arm7' if usePseudo else 'x64'
        elif platformName == 'Linux_X64':   return 'xLinux_X64' if usePseudo else 'x64'
        elif platformName == 'Linux_X86':   return 'xLinux_X86' if usePseudo else 'x64'
        elif platformName == 'Doc':         return 'xDoc' if usePseudo else 'x64'
        elif platformName == 'Sensor':      return 'xSensor' if usePseudo else 'x64'
        else:                               raise Exception('Illegal Platform Name')

    def PlatformName(self, config, usePseudo = True, isProject = True, isCSharp = False):
        return self.ConvertPlatform(config.Platform, usePseudo, isProject, isCSharp)

    def ConfigName(self, config, usePseudo = True):
        if config.Platform == 'Any':            return config.Name
        elif config.Platform == 'Win32':        return config.Name
        elif config.Platform == 'Win64':        return config.Name
        elif config.Platform == 'Linux_Arm64':  return config.Name if usePseudo else config.Name + config.Platform
        elif config.Platform == 'Linux_Arm7':   return config.Name if usePseudo else config.Name + config.Platform
        elif config.Platform == 'Linux_X64':    return config.Name if usePseudo else config.Name + config.Platform
        elif config.Platform == 'Linux_X86':    return config.Name if usePseudo else config.Name + config.Platform
        elif config.Platform == 'Doc':          return config.Name if usePseudo else config.Name + config.Platform
        elif config.Platform == 'Sensor':       return config.Name if usePseudo else config.Name + config.Platform
        else:                                   raise Exception('Illegal Platform Name')

    def BuildName(self, config, pseudoName = True, isProject = True, isCSharp = False):
        return '%s|%s' % (self.ConfigName(config, pseudoName), self.PlatformName(config, pseudoName, isProject, isCSharp))

    def GetPlatformToolVersion(self):

        # Allow using a version of Visual Studio build tools, different than the default version.
        # Know what you ask for: use of this feature is highly discouraged.

        def GetToolVersion():
            try:
                return os.environ['K_MSVC_TOOL_VERSION']
            except KeyError:
                return '0'

        toolVersion = GetToolVersion()
        if toolVersion != '0':
            return toolVersion

        # return desired tool version.
        if self.version == MsvcVersion.Msvc2013:    return '120'
        elif self.version == MsvcVersion.Msvc2015:  return '140'
        elif self.version == MsvcVersion.Msvc2017:  return '141'
        elif self.version == MsvcVersion.Msvc2019:  return '142'
        elif self.version == MsvcVersion.Msvc2022:  return '143'
        else:                                       raise Exception('Unsupported Visual Studio version')

    def GetPlatformToolset(self):
        return 'v' + self.GetPlatformToolVersion()

    def GetDefaultWarningLevel(self):
        if self.version == MsvcVersion.Msvc2013:    return 'Level3'
        elif self.version == MsvcVersion.Msvc2015:  return 'Level3'
        elif self.version == MsvcVersion.Msvc2017:  return 'Level3'
        elif self.version == MsvcVersion.Msvc2019:  return 'Level3'
        elif self.version == MsvcVersion.Msvc2022:  return 'Level4'
        else:                                       raise Exception('Unsupported Visual Studio version')

    def GetCxxLanguage(self, cxxVersion):
        if cxxVersion == CxxLanguage.Cxx2014:       return 'stdcpp14'
        elif cxxVersion == CxxLanguage.Cxx2017:     return 'stdcpp17'
        else:                                       return None

    def GetDefaultCxxLanguage(self):
        if self.version == MsvcVersion.Msvc2017:    return CxxLanguage.Cxx2017
        elif self.version == MsvcVersion.Msvc2019:  return CxxLanguage.Cxx2017
        elif self.version == MsvcVersion.Msvc2022:  return CxxLanguage.Cxx2017
        else:                                       return None

    def ForceCxx17(self, selectionString):
        return True if 'c++17' in selectionString.split('|') else False

    def GetDesiredCxxLanguage(self, selectionString):        
        if self.ForceCxx17(selectionString):
            return CxxLanguage.Cxx2017
        else:
            return self.GetDefaultCxxLanguage()

    def GetDesiredCxxLanguageEx(self, selectionString):        
        return self.GetCxxLanguage(self.GetDesiredCxxLanguage(selectionString))

    def GetDefaultWarningSuppressions(self, selectionString):
        if self.version == MsvcVersion.Msvc2013:    return '4100;4127;4706'
        elif self.version == MsvcVersion.Msvc2015:  return '4100;4127;4706;4456;4457'
        elif self.version == MsvcVersion.Msvc2017:  return '4100;4127;4706;4456;4457;4458;4459'
        elif self.version == MsvcVersion.Msvc2019:  return '4100;4127;4706;4456;4457;4458;4459'
        elif self.version == MsvcVersion.Msvc2022:  return '4100;4127;4706;4456;4457;4458;4459'
        else:                                       raise Exception('Unsupported Visual Studio version')

    def GetTemplates(self, selectionString):
        return {
            'Symbols': [
                [
                    [ 'PlatformToolset', self.GetPlatformToolset() ],
                ],
                [
                    [ 'Optimization', 'Disabled' ],
                    [ 'RuntimeLibrary', 'MultiThreadedDebugDLL' ],
                    [ 'WarningLevel', self.GetDefaultWarningLevel() ],
                    [ 'DebugInformationFormat', 'ProgramDatabase' ],
                    [ 'DisableSpecificWarnings',  self.GetDefaultWarningSuppressions(selectionString) ],
                ],
                [
                    [ 'GenerateDebugInformation', 'true' ],
                ],
            ],

            'Optimize': [
                [
                    [ 'PlatformToolset', self.GetPlatformToolset() ],
                ],
                [
                    [ 'Optimization', 'MaxSpeed' ],
                    [ 'IntrinsicFunctions', 'true' ],
                    [ 'RuntimeLibrary', 'MultiThreadedDLL' ],
                    [ 'FunctionLevelLinking', 'true' ],
                    [ 'WarningLevel', self.GetDefaultWarningLevel() ],
                    [ 'DebugInformationFormat', 'ProgramDatabase' ],
                    [ 'DisableSpecificWarnings', self.GetDefaultWarningSuppressions(selectionString) ],
                ],
                [
                    [ 'GenerateDebugInformation', 'true' ],
                    [ 'OptimizeReferences', 'true' ],
                    [ 'EnableCOMDATFolding', 'true' ],
                ],
            ],

            'MiniRebuild': [
                [], [ [ 'MinimalRebuild', 'true' ] ], [],
            ],

            'MultiProcessorCompile': [
                [], [ [ 'MultiProcessorCompilation', 'true' ] ], [],
            ],

            'OpenMp': [
                [], [ [ 'OpenMPSupport', 'true' ] ], [],
            ],

            'FastRuntimeChecks': [
                [], [ [ 'BasicRuntimeChecks', 'EnableFastChecks' ] ], [],
            ],

            'Win32': [
                [], [], [ [ 'TargetMachine', 'MachineX86' ] ],
            ],

            'Win64': [
                [], [], [ [ 'TargetMachine', 'MachineX64' ] ],
            ],

            'Unicode': [
                [ [ 'CharacterSet', 'Unicode' ] ], [], [],
            ],

            'MultiByte': [
                [ [ 'CharacterSet', 'MultiByte' ] ], [], [],
            ],

            'Clr': [
                [ [ 'CLRSupport', 'true' ] ], [ [ 'GenerateXMLDocumentationFiles', 'true' ] ], [],
            ],

            'ClrOldSyntax': [
                [ [ 'CLRSupport', 'OldSyntax' ] ], [], [],
            ],

            'UsePch': [
                [], [ [ 'PrecompiledHeader', 'Use' ] ], [],
            ],

            'CreatePch': [
                [], [ [ 'PrecompiledHeader', 'Create' ] ], [],
            ],

            'OmitPch': [
                [], [ [ 'PrecompiledHeader', 'NotUsing' ] ], [],
            ],
            
            'PchFile': [
                [], [ [ 'PrecompiledHeaderFile', 'stdafx.h' ] ], [],
            ],

            'Console': [
                [], [], [ [ 'SubSystem', 'Console' ] ],
            ],

            'CompileAsCpp': [
                [], [ [ 'CompileAs', 'CompileAsCpp' ] ], [],
            ],

            'CppExceptSEH':[
                [], [ [ 'ExceptionHandling', 'Async' ] ], [],
            ],

            'EnhancedInstrSetSSE2':[
                [], [ [ 'EnableEnhancedInstructionSet', 'StreamingSimdExtensions2' ] ], [],
            ],

            'ParallelCodeGeneration':[
                [], [ [ 'EnableParallelCodeGeneration', 'true' ] ], [],
            ],

            'FpModelPrecise': [
                [], [ [ 'floatingPointModel', 'precise' ] ], [],
            ],

            'FpExcept': [
                [], [ [ 'FloatingPointExceptions', 'true' ] ], [],
            ],

            'MfcAppDll': [
                [ [ 'UseOfMfc', 'Dynamic' ] ], [], [ [ 'SubSystem', 'Windows' ] ],
            ],

            'Exclude': [
                [], [ [ 'ExcludedFromBuild', 'true' ] ], [],
            ],

            'NoNxCompat': [
                [], [], [ [ 'DataExecutionPrevention', 'false' ] ],
            ],

            'NoSafeSeh': [
                [], [], [ [ 'ImageHasSafeExceptionHandlers', 'false' ] ],
            ],

            'requireAdmin': [
                [], [], [ [ 'UACExecutionLevel', 'RequireAdministrator' ] ],
            ],
        }

    def GetCudaFlags(self):
        return 'compute_50,sm_50;compute_52,sm_52;compute_60,sm_60;compute_61,sm_61;compute_62,sm_62'

    def GetCudaAdditionalCompilerOptions(self):
        options = []

        options.append('/wd"4100"')
        options.append('/Fd"$(IntDir)vc%s.pdb"' % (self.GetPlatformToolVersion()))

        if self.cudaMajor >= 11:
            options.append('/std:c++17')

        return ' '.join(options)

    def GetCudaAdditionalOptions(self):
        options = []

        # FSS-1452: Enable the change below once existing Cuda code has been audited for correct stream use
        #options.append('--default-stream per-thread')

        if self.cudaMajor >= 11:
            options.append('-std=c++17')

        options.append('%(AdditionalOptions)')

        return ' '.join(options)

    def GetAdditionalCompilerOptions(self):
        options = []

        options.append('%(AdditionalOptions)')

        # error C1128: number of sections exceeded object file format limit : compile with /bigobj
        options.append('/bigobj')

        # supported by Visual Studio 2017 version 15.7
        if self.version >= MsvcVersion.Msvc2017:
            options.append('/Zc:__cplusplus')

        return ' '.join(options)

    def GetAdditionalLinkerOptions(self, config):

        options = []

        if 'LinkerSuppress4099' in config.Template.split('|'):
            options.append('/ignore:4099')

        if (len(options) == 0):
            return None

        options.insert(0, '%(AdditionalOptions)')
        return ' '.join(options)

    def SelectFlags(self, selectionString):
        globalFlags = []
        compilerFlags = []
        linkerFlags = []
        templates = self.GetTemplates(selectionString)

        # treat 'Optimize3' the same way as 'Optimize'
        components = [('Optimize' if component == 'Optimize3' else component) for component in selectionString.split('|')]

        for component in components:
            items = component.split('=')

            if len(items) == 2:
                for tmp in templates:
                    if tmp == items[0]:
                        for flg in templates[tmp][0]:
                            globalFlags.append([flg[0], items[1]])
                        for flg in templates[tmp][1]:
                            compilerFlags.append([flg[0], items[1]])
                        for flg in templates[tmp][2]:
                            linkerFlags.append([flg[0], items[1]])
            elif len(items) == 1:
                for tmp in templates:
                    if tmp == component:
                        for flg in templates[tmp][0]:
                            globalFlags.append(flg)
                        for flg in templates[tmp][1]:
                            compilerFlags.append(flg)
                        for flg in templates[tmp][2]:
                            linkerFlags.append(flg)
            else:
                raise Exception('Illegal template hint')

        return globalFlags, compilerFlags, linkerFlags

    def SelectProjectReferences(self, project):
        references = []

        for reference in project.References:
            if reference.Type == 'Project':
                for dependency in self.SelectDependencies(project):
                    if dependency.Name == reference.Value:
                        references.append(dependency)

        return references

    def IsProjectReferenceAvailable(self, project, referenceName):

        for dependency in self.SelectDependencies(project):
            if dependency.Name == referenceName:
                return True

        return False

    def HasCudaSource(self, project):

        for src in self.SelectSources(project):
            if self.DetermineSourceType(src) == SourceType.CudaSource:
                return True

        return False

    def HasResource(self, project):

        for src in self.SelectSources(project):
            if self.DetermineSourceType(src) == SourceType.Resource:
                return True

        return False

    def HasSelf(self, project):

        for src in self.SelectSources(project):
            if self.DetermineSourceType(src) == SourceType.Self:
                return True

        return False
        
    def HasMisc(self, project):

        for src in self.SelectSources(project):
            if self.DetermineSourceType(src) == SourceType.Misc:
                return True

        return False

    def ManifestFile(self, project):

        for src in self.SelectSources(project):                
            if self.DetermineSourceType(src) == SourceType.Manifest:
                return src.Name

        return ''

    def UtilsRelPath(self, project):
        utilsPath = os.path.normpath(os.path.join(os.path.dirname(os.path.realpath(__file__)), '..'))
        return os.path.relpath(utilsPath, os.path.dirname(project.FileName))        

    def GeneratorRelPath(self, project):
        return os.path.join(self.UtilsRelPath(project), 'kGenerator.py')

    def SolutionRelPath(self, project):
        return os.path.relpath(project.Solution.FileName, os.path.dirname(project.FileName))

    def BuildCmd(self, project, config):
        if config.BuildCmd is not None:
            return config.BuildCmd
        else:
            return BuildCmd(project, config)

    def RebuildCmd(self, project, config):
        if config.RebuildCmd is not None:
            return config.RebuildCmd
        else:
            return RebuildCmd(project, config)

    def CleanCmd(self, project, config):
        if config.CleanCmd is not None:
            return config.CleanCmd
        else:
            return CleanCmd(project, config)

    def WriteFilterItem200x(self, project, parent, filesItem):
        for childGroup in parent.Groups:
            filterItem = filesItem.AddElem('Filter')
            filterItem.AddAttr('Name', childGroup.Name)
            self.WriteFiltersItem200x(project, childGroup, filterItem)

    def WriteFiltersItem200x(self, project, sourceGroups, filterItem):
        self.WriteFileItem200x(project, sourceGroups, filterItem)
        self.WriteFilterItem200x(project, sourceGroups, filterItem)

    def WriteFileItem200x(self, project, parent, filesItem):
        for src in parent.Sources:
            fileItem = filesItem.AddElem('File')
            fileItem.AddElem()
            fileItem.AddAttr('RelativePath', src.Name)

            # process custom build rules (SourceTemplates), if exist
            for config in self.SelectConfigurations(project):
                for sourceTemplate in config.SourceTemplates:
                    if sourceTemplate.Name == src.Name:
                        globalFlags, compilerFlags, linkerFlags = self.SelectFlags(sourceTemplate.Add)
                        fileConfigurationItem = fileItem.AddElem('FileConfiguration')
                        fileConfigurationItem.AddAttr('Name', self.BuildName(config, False))

                        for flag in globalFlags:
                            fileConfigurationItem.AddAttr(flag[0], flag[1])

                        toolItem = fileConfigurationItem.AddElem('Tool')
                        toolItem.AddAttr('Name', 'VCCLCompilerTool')
                        for flag in compilerFlags:
                            toolItem.AddAttr(flag[0], flag[1])

    def WriteFilterItem201xHelper(self, project, parent, filterItem, filter = ''):
        for childGroup in parent.Groups:
            prefix = '' if len(filter) == 0 else filter + '\\'
            name = prefix + childGroup.Name
            newItem = filterItem.AddElem('Filter')
            newItem.AddAttr('Include', name)
            newItem.AddElem('UniqueIdentifier', '{' + self.FilterId(name) + '}')
            self.WriteFilterItem201xHelper(project, childGroup, filterItem, name)

    def WriteFilterItem201x(self, project, sourceGroups, filtersItem):
        self.WriteFilterItem201xHelper(project, sourceGroups, filtersItem)

    def WriteFilesItem201xHelper(self, project, type, parent, filterItem, filter = ''):
        for src in parent.Sources:
            if self.DetermineSourceType(src) == type:
                if type == SourceType.Misc:
                    srcItem = filterItem.AddElem('None')
                    srcItem.AddAttr('Include', src.Name)
                    srcItem.AddElem('Filter', filter)
                elif type == SourceType.Manifest:
                    srcItem = filterItem.AddElem('Manifest')
                    srcItem.AddAttr('Include', src.Name)
                    srcItem.AddElem('Filter', filter)
                elif type == SourceType.Resource:
                    srcItem = filterItem.AddElem('ResourceCompile')
                    srcItem.AddAttr('Include', src.Name)
                    srcItem.AddElem('Filter', filter)
                elif type == SourceType.Header:
                    srcItem = filterItem.AddElem('ClInclude')
                    srcItem.AddAttr('Include', src.Name)
                    srcItem.AddElem('Filter', filter)
                elif type == SourceType.Source:
                    srcItem = filterItem.AddElem('ClCompile')
                    srcItem.AddAttr('Include', src.Name)
                    srcItem.AddElem('Filter', filter)
                elif type == SourceType.Self:
                    if self.regeneratorEnabled:
                        srcItem = filterItem.AddElem('CustomBuild')
                        srcItem.AddAttr('Include', src.Name)
                        srcItem.AddElem('Filter', filter)
                    else:
                        srcItem = filterItem.AddElem('None')
                        srcItem.AddAttr('Include', src.Name)
                        srcItem.AddElem('Filter', filter)
                elif type == SourceType.CudaSource:
                    if self.hasCuda:
                        srcItem = filterItem.AddElem('CudaCompile')
                    else:
                        srcItem = filterItem.AddElem('None')
                    srcItem.AddAttr('Include', src.Name)
                    srcItem.AddElem('Filter', filter)

        for childGroup in parent.Groups:
            prefix = '' if len(filter) == 0 else filter + '\\'
            self.WriteFilesItem201xHelper(project, type, childGroup, filterItem, prefix + childGroup.Name)

    def WriteFilesItem201x(self, project, type, sourceGroups, parent):
        filtersItem = parent.AddElem('ItemGroup')
        self.WriteFilesItem201xHelper(project, type, sourceGroups, filtersItem)

    def WriteFiltersItem201x(self, project, sourceGroups, parent):

        filtersItem = parent.AddElem('ItemGroup')
        self.WriteFilterItem201x(project, sourceGroups, filtersItem)

        if self.HasCudaSource(project):
            self.WriteFilesItem201x(project, SourceType.CudaSource, sourceGroups, parent)

        self.WriteFilesItem201x(project, SourceType.Misc, sourceGroups, parent)
        self.WriteFilesItem201x(project, SourceType.Manifest, sourceGroups, parent)
        self.WriteFilesItem201x(project, SourceType.Resource, sourceGroups, parent)
        self.WriteFilesItem201x(project, SourceType.Header, sourceGroups, parent)
        self.WriteFilesItem201x(project, SourceType.Source, sourceGroups, parent)
        self.WriteFilesItem201x(project, SourceType.Self, sourceGroups, parent)

    def WriteProject(self, project):
        return self.WriteProjectEx(project)

    def WriteProjectEx(self, project, forceUpdate=False):

        if project.Type == 'C#':
            # not implemented -- just print the UUID
            consoleOut = Utils.ConsoleOut()
            oldColor = consoleOut.Color
            consoleOut.SetColor(Utils.ConsoleOut.Colors.Yellow)
            consoleOut.Print('UUID: {' + self.ProjectId(project) + '}')
            consoleOut.SetColor(oldColor)

        else:
            return self.WriteCcppProject201x(project, forceUpdate)

    def WriteCcppProject201xImpl(self, project, fileName, forceUpdate):

        root = Xml.Element('Project', sepIsForward=False)

        hasCudaSource = self.HasCudaSource(project)
        includeCudaSources = self.hasCuda and hasCudaSource
        hasResource = self.HasResource(project)
        hasSelf = self.HasSelf(project)
        hasMisc = self.HasMisc(project)
        manifestFile = self.ManifestFile(project)

        root.AddAttr('DefaultTargets', 'Build')
        toolsVersion = self.ProjectVersion()
        if (toolsVersion is not None):
            root.AddAttr('ToolsVersion', toolsVersion)
        root.AddAttr('xmlns', 'http://schemas.microsoft.com/developer/msbuild/2003', False)

        configurationsItem = root.AddElem('ItemGroup')
        configurationsItem.AddAttr('Label', 'ProjectConfigurations')

        for config in self.SelectConfigurations(project):

            configurationItem = configurationsItem.AddElem('ProjectConfiguration')
            configurationItem.AddAttr('Include', self.BuildName(config, False))

            configconfigItem = configurationItem.AddElem('Configuration', self.ConfigName(config, False))
            configconfigItem = configurationItem.AddElem('Platform', self.PlatformName(config, False))

        propertyGroupItem = root.AddElem('PropertyGroup')
        propertyGroupItem.AddAttr('Label', 'Globals')
        propertyGroupItem.AddElem('ProjectName', self.ProjectName(project))
        propertyGroupItem.AddElem('ProjectGuid', '{%s}' % self.ProjectId(project))
        propertyGroupItem.AddElem('RootNamespace', project.Name)
        propertyGroupItem.AddElem('TargetFrameworkVersion', 'v' + self.FrameworkVersion())

        targetPlatformVersion = self.TargetPlatformVersion()
        if targetPlatformVersion is not None:
            propertyGroupItem.AddElem('WindowsTargetPlatformVersion', targetPlatformVersion)

        propertyGroupItem.AddElem('Keyword', 'Win32Proj')

        importItem = root.AddElem('Import')
        importItem.AddAttr('Project', '$(VCTargetsPath)\\Microsoft.Cpp.Default.props')

        for config in self.SelectConfigurations(project):
            globalFlags, compilerFlags, linkerFlags = self.SelectFlags(config.Template)

            propertyGroupItem = root.AddElem('PropertyGroup')
            propertyGroupItem.AddAttr('Condition', '\'$(Configuration)|$(Platform)\'==\'%s\'' % (self.BuildName(config, False)))
            propertyGroupItem.AddAttr('Label', 'Configuration')

            if self.ConvertType(config) == 'Executable':
                propertyGroupItem.AddElem('ConfigurationType', 'Application')
            elif self.ConvertType(config) == 'Shared':
                propertyGroupItem.AddElem('ConfigurationType', 'DynamicLibrary')
            elif self.ConvertType(config) == 'Library':
                propertyGroupItem.AddElem('ConfigurationType', 'StaticLibrary')
            else:
                propertyGroupItem.AddElem('ConfigurationType', 'Makefile')

            for flag in globalFlags:
                propertyGroupItem.AddElem(flag[0], flag[1])

        importItem = root.AddElem('Import')
        importItem.AddAttr('Project', '$(VCTargetsPath)\\Microsoft.Cpp.props')

        importGroupItem = root.AddElem('ImportGroup')
        importGroupItem.AddAttr('Label', 'ExtensionSettings')

        if includeCudaSources:
            importItem = importGroupItem.AddElem('Import')
            importItem.AddAttr('Project', '$(VCTargetsPath)\\BuildCustomizations\\CUDA %s.props' % self.cudaVersion)

        else:
            importGroupItem.AddElem() # force special formatting

        for config in self.SelectConfigurations(project):
            importGroupItem = root.AddElem('ImportGroup')
            importGroupItem.AddAttr('Condition', '\'$(Configuration)|$(Platform)\'==\'%s\'' % (self.BuildName(config, False)))
            importGroupItem.AddAttr('Label', 'PropertySheets')

            importItem = importGroupItem.AddElem('Import')
            importItem.AddAttr('Project', '$(UserRootDir)\\Microsoft.Cpp.$(Platform).user.props')
            importItem.AddAttr('Condition', 'exists(\'$(UserRootDir)\\Microsoft.Cpp.$(Platform).user.props\')')
            importItem.AddAttr('Label', 'LocalAppDataPlatform')

        propertyGroupItem = root.AddElem('PropertyGroup')
        propertyGroupItem.AddAttr('Label', 'UserMacros')

        propertyGroupItem = root.AddElem('PropertyGroup')
        propertyGroupItem.AddElem('_ProjectFileVersion', '10.0.40219.1')

        for config in self.SelectConfigurations(project):
            if self.ConvertType(config) == 'Library':
                outDir = config.LibDir
            else:
                outDir = config.BinDir

            outDirItem = propertyGroupItem.AddElem('OutDir', '%s\\' % (outDir))
            outDirItem.AddAttr('Condition', '\'$(Configuration)|$(Platform)\'==\'%s\'' % (self.BuildName(config, False)))

            intDirItem = propertyGroupItem.AddElem('IntDir', self.IntDir(config))
            intDirItem.AddAttr('Condition', '\'$(Configuration)|$(Platform)\'==\'%s\'' % (self.BuildName(config, False)))

            if self.ConvertType(config) != 'Library':
                linkIncrItem = propertyGroupItem.AddElem('LinkIncremental', 'false')  # XXX disable incremental linking
                linkIncrItem.AddAttr('Condition', '\'$(Configuration)|$(Platform)\'==\'%s\'' % (self.BuildName(config, False)))

            if self.ConvertType(config) == 'Runner':
                buildCmdItem = propertyGroupItem.AddElem('NMakeBuildCommandLine', self.BuildCmd(project, config))
                buildCmdItem.AddAttr('Condition', '\'$(Configuration)|$(Platform)\'==\'%s\'' % (self.BuildName(config, False)))

                rebuildCmdItem = propertyGroupItem.AddElem('NMakeReBuildCommandLine', self.RebuildCmd(project, config))
                rebuildCmdItem.AddAttr('Condition', '\'$(Configuration)|$(Platform)\'==\'%s\'' % (self.BuildName(config, False)))

                cleanCmdItem = propertyGroupItem.AddElem('NMakeCleanCommandLine', self.CleanCmd(project, config))
                cleanCmdItem.AddAttr('Condition', '\'$(Configuration)|$(Platform)\'==\'%s\'' % (self.BuildName(config, False)))

                outputItem = propertyGroupItem.AddElem('NMakeOutput', '')
                outputItem.AddAttr('Condition', '\'$(Configuration)|$(Platform)\'==\'%s\'' % (self.BuildName(config, False)))

                first = True
                attrStr = ''
                for define in config.Defines:
                    if not first: attrStr += ';'
                    attrStr += define
                    first = False
                attrStr += ';$(NMakePreprocessorDefinitions)'
                preprocessorDefinitionsItem = propertyGroupItem.AddElem('NMakePreprocessorDefinitions', attrStr)
                preprocessorDefinitionsItem.AddAttr('Condition', '\'$(Configuration)|$(Platform)\'==\'%s\'' % (self.BuildName(config, False)))

                first = True
                attrStr = ''
                for dir in config.IncludeDirs:
                    if not first: attrStr += ';'
                    attrStr += dir
                    first = False
                attrStr += ';$(NMakeIncludeSearchPath)'
                includeSearchPathItem = propertyGroupItem.AddElem('NMakeIncludeSearchPath', attrStr)
                includeSearchPathItem.AddAttr('Condition', '\'$(Configuration)|$(Platform)\'==\'%s\'' % (self.BuildName(config, False)))

            else:
                targetNameItem = propertyGroupItem.AddElem('TargetName', '%s' % (os.path.splitext(config.Target)[0]))
                targetNameItem.AddAttr('Condition', '\'$(Configuration)|$(Platform)\'==\'%s\'' % (self.BuildName(config, False)))

                targetExt = os.path.splitext(config.Target)[1]

                if (self.ConvertType(config) == 'Executable' and targetExt != '.exe') or \
                    (self.ConvertType(config) == 'Shared' and targetExt != '.dll') or \
                    (self.ConvertType(config) == 'Library' and targetExt != '.lib'):
                        targetExtItem = propertyGroupItem.AddElem('TargetExt', '%s' % (os.path.splitext(config.Target)[1]))
                        targetExtItem.AddAttr('Condition', '\'$(Configuration)|$(Platform)\'==\'%s\'' % (self.BuildName(config, False)))

        for config in self.SelectConfigurations(project):

            globalFlags, compilerFlags, linkerFlags = self.SelectFlags(config.Template)

            if self.ConvertType(config) != 'Runner':
                itemDefGroupItem = root.AddElem('ItemDefinitionGroup')
                itemDefGroupItem.AddAttr('Condition', '\'$(Configuration)|$(Platform)\'==\'%s\'' % (self.BuildName(config, False)))

                if (config.Prebuild is not None and len(config.Prebuild) > 0):
                    preBuildEventItem = itemDefGroupItem.AddElem('PreBuildEvent')
                    preBuildEventItem.AddElem('Command', config.Prebuild)

                if (config.Postbuild is not None and len(config.Postbuild) > 0):
                    postBuildEventItem = itemDefGroupItem.AddElem('PostBuildEvent')
                    postBuildEventItem.AddElem('Command', config.Postbuild)

                clCompileItem = itemDefGroupItem.AddElem('ClCompile')

                first = True
                itemStr = ''
                for dir in config.IncludeDirs:
                    if not first: itemStr += ';'
                    itemStr += dir
                    first = False

                if not first: itemStr += ';'
                itemStr += '$(VCInstallDir)UnitTest\\include\\;%(AdditionalIncludeDirectories)'
                clCompileItem.AddElem('AdditionalIncludeDirectories', itemStr)

                autoDefines = []
                if includeCudaSources:
                    autoDefines.append('K_HAVE_CUDA')

                first = True
                itemStr = ''
                for define in config.Defines + autoDefines:
                    if not first: itemStr += ';'
                    itemStr += define
                    first = False

                if not first: itemStr += ';'
                itemStr += '%(PreprocessorDefinitions)'
                clCompileItem.AddElem('PreprocessorDefinitions', itemStr)

                for flag in compilerFlags:
                    clCompileItem.AddElem(flag[0], flag[1])

                # required for linking against static library when intermediate directory is absent (prevents linker warnings)
                if self.ConvertType(config) == 'Library':
                    clCompileItem.AddElem('ProgramDataBaseFileName', '%s\\%s.pdb' % (
                        config.LibDir,
                        os.path.splitext(config.Target)[0]))

                languageStandard = self.GetDesiredCxxLanguageEx(config.Template)
                if languageStandard is not None:
                    clCompileItem.AddElem('LanguageStandard', languageStandard)

                additionalOptions = self.GetAdditionalCompilerOptions()
                if additionalOptions is not None:
                    clCompileItem.AddElem('AdditionalOptions', additionalOptions, format=False)

                if self.ConvertType(config) == 'Executable' or self.ConvertType(config) == 'Shared':
                    linkItem = itemDefGroupItem.AddElem('Link')

                    autoLibs = []
                    if includeCudaSources:
                        autoLibs.append('cudart.lib')

                    first = True
                    itemStr = ''
                    for lib in config.Libs + autoLibs:
                        if not first: itemStr += ';'
                        itemStr += lib
                        first = False

                    if not first: itemStr += ';'
                    itemStr += '%(AdditionalDependencies)'
                    linkItem.AddElem('AdditionalDependencies', itemStr)

                    linkItem.AddElem('OutputFile', '%s\\%s' % (
                        config.BinDir,
                        config.Target))

                    first = True
                    itemStr = ''
                    for dir in config.LibraryDirs:
                        if not first: itemStr += ';'
                        itemStr += dir
                        first = False
                        
                    if not first: itemStr += ';'
                    itemStr += '$(VCInstallDir)UnitTest\\lib;%(AdditionalLibraryDirectories)'
                    linkItem.AddElem('AdditionalLibraryDirectories', itemStr)

                    if self.ConvertType(config) == 'Shared':
                        linkItem.AddElem('ImportLibrary', '%s\\%s.lib' % (
                            config.LibDir,
                            os.path.splitext(config.Target)[0]))

                    subSystemIsSet = False
                    for flag in linkerFlags:
                        linkItem.AddElem(flag[0], flag[1])
                        if flag[0] == 'SubSystem':
                            subSystemIsSet = True

                    # in >=2013 subsystem should be defined when using the XP tool set; don't want to change the XML schema again
                    if self.version >= MsvcVersion.Msvc2013 and not subSystemIsSet:
                        linkItem.AddElem('SubSystem', 'Windows')

                    isDpiUnaware = self.IsDpiUnaware(config)
                    if len(manifestFile) or isDpiUnaware:
                        manifestItem = itemDefGroupItem.AddElem('Manifest')
                        
                        if len(manifestFile):
                            manifestItem.AddElem('AdditionalManifestFiles', '%s;%%(AdditionalManifestFiles)' % (manifestFile))

                        if isDpiUnaware:
                            manifestItem.AddElem('EnableDpiAwareness', 'false')

                    if len(config.DelayLoadLibs) != 0:
                        first = True
                        itemStr = ''
                        for lib in config.DelayLoadLibs:
                            if not first: itemStr += ';'
                            itemStr += os.path.splitext(lib)[0] + '.dll'
                            first = False

                        if not first: itemStr += ';'
                        itemStr += '%(DelayLoadDLLs)'
                        linkItem.AddElem('DelayLoadDLLs', itemStr)

                    linkKey = self.LinkKey(config)
                    if linkKey is not None:
                        propertyGroupItem = root.AddElem('PropertyGroup')
                        propertyGroupItem.AddAttr('Condition', '\'$(Configuration)|$(Platform)\'==\'%s\'' % (self.BuildName(config, False)))
                        propertyGroupItem.AddElem('LinkKeyFile', linkKey)

                    additionalOptions = self.GetAdditionalLinkerOptions(config)
                    if additionalOptions is not None:
                        linkItem.AddElem('AdditionalOptions', additionalOptions, format=False)

                if self.ConvertType(config) == 'Library':
                    libItem = itemDefGroupItem.AddElem('Lib')
                    libItem.AddElem('OutputFile', '%s\\%s' % (
                        config.LibDir,
                        config.Target))

                if includeCudaSources:
                    cudaCompileItem = itemDefGroupItem.AddElem('CudaCompile')
                    targetMachinePlatform = '64' if config.Platform == 'Win64' else '32'
                    cudaCompileItem.AddElem('TargetMachinePlatform', targetMachinePlatform)
                    cudaCompileItem.AddElem('CodeGeneration', self.GetCudaFlags())
                    cudaCompileItem.AddElem('AdditionalCompilerOptions', self.GetCudaAdditionalCompilerOptions(), format=False)
                    cudaCompileItem.AddElem('CudaRuntime', 'Shared')
                    cudaCompileItem.AddElem('AdditionalOptions', self.GetCudaAdditionalOptions(), format=False)

        references = project.References
        if len(references):
            referencesItem = root.AddElem('ItemGroup')

            for reference in references:
                if reference.Type == 'Assembly' or not self.IsProjectReferenceAvailable(project, reference.Value):
                    referenceItem = referencesItem.AddElem('Reference')
                    referenceItem.AddAttr('Include', reference.Value)

                    # Note: this assumes the output path for the referenced project is identical to the current project
                    if reference.Type == 'Project':
                        referenceItem.AddElem('HintPath', '$(TargetDir)' + '\\' + reference.Value + '.dll')

                    referenceItem.AddElem('Private', 'false')
                    referenceItem.AddElem('ReferenceOutputAssembly', 'true')
                    referenceItem.AddElem('CopyLocalSatelliteAssemblies', 'false')

        clCompileGroupItem = root.AddElem('ItemGroup')
        for src in self.SelectSources(project):
            if self.DetermineSourceType(src) == SourceType.Source:

                clCompileItem = clCompileGroupItem.AddElem('ClCompile')
                clCompileItem.AddAttr('Include', src.Name)
                
                # process custom build rules (SourceTemplates), if exist
                for config in self.SelectConfigurations(project):
                    for sourceTemplate in config.SourceTemplates:
                        if sourceTemplate.Name == src.Name:
                            globalFlags, compilerFlags, linkerFlags = self.SelectFlags(sourceTemplate.Add)

                            for flag in compilerFlags:
                                precompiledHeaderItem = clCompileItem.AddElem(flag[0], flag[1])
                                precompiledHeaderItem.AddAttr('Condition', '\'$(Configuration)|$(Platform)\'==\'%s\'' % (self.BuildName(config, False)))

        clIncludeGroupItem = root.AddElem('ItemGroup')
        for src in self.SelectSources(project):
            if self.DetermineSourceType(src) == SourceType.Header:
                clIncludeItem = clIncludeGroupItem.AddElem('ClInclude')
                clIncludeItem.AddAttr('Include', src.Name)

        if hasMisc:
            miscGroupItem = root.AddElem('ItemGroup')
            for src in self.SelectSources(project):
                if self.DetermineSourceType(src) == SourceType.Misc:
                    miscItem = miscGroupItem.AddElem('None')
                    miscItem.AddAttr('Include', src.Name)

        if hasResource:
            resouceGroupItem = root.AddElem('ItemGroup')
            for src in self.SelectSources(project):
                if self.DetermineSourceType(src) == SourceType.Resource:
                    resourceItem = resouceGroupItem.AddElem('ResourceCompile')
                    resourceItem.AddAttr('Include', src.Name)

        if hasSelf:
            itemGroupItem = root.AddElem('ItemGroup')
            for src in self.SelectSources(project):
                if self.DetermineSourceType(src) == SourceType.Self:
                    if self.regeneratorEnabled:
                        customBuildItem = itemGroupItem.AddElem('CustomBuild')
                        customBuildItem.AddAttr('Include', src.Name)
                        customBuildItem.AddElem('Command', '%s --writers=%s --project=%s %s' % (self.GeneratorRelPath(project),
                            self.Name, project.Name, self.SolutionRelPath(project)))
                        customBuildItem.AddElem('Outputs', project.Name + '.vcxproj')
                        customBuildItem.AddElem('SubType', 'Designer')

                    else:
                        resourceItem = itemGroupItem.AddElem('None')
                        resourceItem.AddAttr('Include', src.Name)

        if hasCudaSource:
            resouceGroupItem = root.AddElem('ItemGroup')
            for src in self.SelectSources(project):
                if self.DetermineSourceType(src) == SourceType.CudaSource:
                    if includeCudaSources:
                        resourceItem = resouceGroupItem.AddElem('CudaCompile')
                    else:
                        resourceItem = resouceGroupItem.AddElem('None')
                    resourceItem.AddAttr('Include', src.Name)

        if len(manifestFile):
            manifestGroupItem = root.AddElem('ItemGroup')
            for src in self.SelectSources(project):
                if self.DetermineSourceType(src) == SourceType.Manifest:
                    manifestItem = manifestGroupItem.AddElem('Manifest')
                    manifestItem.AddAttr('Include', src.Name)

        projectReferences = self.SelectProjectReferences(project)
        if len(projectReferences):
            referenceGroupItem = root.AddElem('ItemGroup')
            for projectReference in projectReferences:
                projectReferenceItem = referenceGroupItem.AddElem('ProjectReference')                

                projectDirName = os.path.relpath(os.path.dirname(projectReference.FileName), os.path.dirname(project.FileName))
                projectFileName = os.path.join(projectDirName, self.ProjectName(projectReference) + self.ProjectExt(projectReference))

                projectReferenceItem.AddAttr('Include', projectFileName)
                projectReferenceItem.AddElem('Project', '{%s}' % (self.ProjectId(projectReference)))
                projectReferenceItem.AddElem('Private', 'false')

        importItem = root.AddElem('Import')
        importItem.AddAttr('Project', '$(VCTargetsPath)\\Microsoft.Cpp.targets')

        importGroupItem = root.AddElem('ImportGroup')
        importGroupItem.AddAttr('Label', 'ExtensionTargets')

        if includeCudaSources:
            importItem = importGroupItem.AddElem('Import')
            importItem.AddAttr('Project', '$(VCTargetsPath)\\BuildCustomizations\\CUDA %s.targets' % (self.cudaVersion))

        else:
            importGroupItem.AddElem() # force special formatting        

        root.Write201x(root, fileName, forceUpdate)

    def WriteFilters201x(self, project, fileName, forceUpdate):

        filtersRoot = Xml.Element('Project', sepIsForward=False)
        filtersRoot.AddAttr('ToolsVersion', '4.0')
        filtersRoot.AddAttr('xmlns', 'http://schemas.microsoft.com/developer/msbuild/2003', False)

        sourceGroups = project.SourceGroups
        self.WriteFiltersItem201x(project, sourceGroups, filtersRoot)
        filtersRoot.Write201x(filtersRoot, fileName, forceUpdate)

    def WriteCcppProject201x(self, project, forceUpdate):

        projectFileName = os.path.join(os.path.dirname(project.FileName), self.ProjectName(project) + self.ProjectExt(project))
        filtersFileName = projectFileName + '.filters'

        self.WriteCcppProject201xImpl(project, projectFileName, forceUpdate)
        self.WriteFilters201x(project, filtersFileName, forceUpdate)

    def WriteSolutionFilters(self, solution, output, group):

        for childGroup in group.Groups:
            self.WriteSolutionFilters(solution, output, childGroup)

        output.Format('Project("{2150E333-8FDC-42A3-9474-1A3956D46DE8}") = "%s", "%s", "{%s}"\r\n' % (group.Name, group.Name, self.GroupId(group)))
        output.Format('EndProject\r\n')

    def WriteSolutionNestProjects(self, solution, output, group):

        for childProject in group.Projects:
            output.Format('\t\t{%s} = {%s}\r\n' % (self.ProjectId(childProject), self.GroupId(group)))

        for childGroup in group.Groups:
            self.WriteSolutionNestProjects(solution, output, childGroup)

    def WriteSolutionNestGroups(self, solution, output, group):

        for childGroup in group.Groups:
            self.WriteSolutionNestGroups(solution, output, childGroup)
            output.Format('\t\t{%s} = {%s}\r\n' % (self.GroupId(childGroup), self.GroupId(group)))

    def WriteSolutionNest(self, solution, output, group):
        self.WriteSolutionNestProjects(solution, output, group)
        self.WriteSolutionNestGroups(solution, output, group)

    def WriteSolution(self, solution):

        fileName = os.path.join(os.path.dirname(solution.FileName), self.SolutionName(solution) + self.SolutionExt())

        with Utils.Output(fileName, Utils.EnvironmentStyle.Dollar, False) as output:

            output.WriteBom()
            output.Format('\r\n')
            output.Format('Microsoft Visual Studio Solution File, Format Version ' + self.SolutionFormatVersion() + '\r\n')
            output.Format('# Visual Studio ' + self.SolutionStudioVersion() + '\r\n')

            configurationPlatforms = []
            for project in self.SelectProjects(solution):
                isCSharp = (project.Type == 'C#')

                for config in self.SelectConfigurations(project):
                    name = self.BuildName(config, True, False, isCSharp)
                    try:
                        index = configurationPlatforms.index(name)
                    except (ValueError):
                        # not in list, add
                        configurationPlatforms.append(name)

            for project in self.SelectProjects(solution):

                if project.Type == 'C#':
                    uuidStr = 'FAE04EC0-301F-11D3-BF4B-00C04F79EFBC'
                else:
                    uuidStr = '8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942'

                output.Format('Project("{%s}") = "%s", "%s\\%s%s", "{%s}"\r\n' % (uuidStr, 
                    self.ProjectName(project), 
                    os.path.relpath(os.path.dirname(project.FileName), os.path.dirname(solution.FileName)),
                    self.ProjectName(project), 
                    self.ProjectExt(project),
                    self.ProjectId(project)))

                dependencies = self.SelectDependencies(project)

                if len(dependencies) != 0:
                    output.Format('\tProjectSection(ProjectDependencies) = postProject\r\n')
                    for dep in dependencies:
                        output.Format('\t\t{%s} = {%s}\r\n' % (self.ProjectId(dep), self.ProjectId(dep)))
                    output.Format('\tEndProjectSection\r\n')

                output.Format('EndProject\r\n')

            for group in self.SelectProjectGroups(solution).Groups:
                self.WriteSolutionFilters(solution, output, group)

            output.Format('Global\r\n')

            output.Format('\tGlobalSection(SolutionConfigurationPlatforms) = preSolution\r\n')
            for configPlatform in configurationPlatforms:
                output.Format('\t\t%s = %s\r\n' % (configPlatform, configPlatform))
            output.Format('\tEndGlobalSection\r\n')
            output.Format('\tGlobalSection(ProjectConfigurationPlatforms) = postSolution\r\n')

            for project in self.SelectProjects(solution):
                isCSharp = (project.Type == 'C#')

                for configPlatform in configurationPlatforms:
                    firstConfig = None
                    foundConfig = None
                    for config in self.SelectConfigurations(project):
                        if firstConfig is None:
                            firstConfig = config
                        if configPlatform == self.BuildName(config, True, False, isCSharp):
                            foundConfig = config

                    if foundConfig:
                        output.Format('\t\t{%s}.%s.ActiveCfg = %s\r\n' % (self.ProjectId(project),
                            self.BuildName(foundConfig, True, False, isCSharp),
                            self.BuildName(foundConfig, False, True, isCSharp)))

                        output.Format('\t\t{%s}.%s.Build.0 = %s\r\n' % (self.ProjectId(project),
                            self.BuildName(foundConfig, True, False, isCSharp),
                            self.BuildName(foundConfig, False, True, isCSharp)))

                    elif firstConfig:
                        output.Format('\t\t{%s}.%s.ActiveCfg = %s\r\n' % (self.ProjectId(project),
                            configPlatform,
                            self.BuildName(firstConfig, False, True, isCSharp)))

            output.Format('\tEndGlobalSection\r\n')

            output.Format('\tGlobalSection(SolutionProperties) = preSolution\r\n')
            output.Format('\t\tHideSolutionNode = FALSE\r\n')
            output.Format('\tEndGlobalSection\r\n')

            groups = self.SelectProjectGroups(solution).Groups

            if len(groups) != 0:
                output.Format('\tGlobalSection(NestedProjects) = preSolution\r\n')
                for group in groups:
                    self.WriteSolutionNest(solution, output, group)
                output.Format('\tEndGlobalSection\r\n')

            output.Format('EndGlobal\r\n')
