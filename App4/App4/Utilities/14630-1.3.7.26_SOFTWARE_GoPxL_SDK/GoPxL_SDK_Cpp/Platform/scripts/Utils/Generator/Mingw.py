#!/usr/bin/python
# This file is part of the FireSync Project Generator.
# Copyright (C) 2008-2016 by LMI Technologies Inc.  All rights reserved.
# Distributed under the terms of the MIT License.
# Redistributed files must retain the above copyright notice.

import re
import os
from . import Project
from . import WriterBase
from . import Utils

def GetTriplet():
    return 'x86_64-w64-mingw32'

def MsgSuffix(plugin):
    return 'MingwW64'

def GnuToolsVersion():
    return '7.5.0'

def GnuToolsPatch():
    return 'p3'

def ToolsPath(isWindows):
    if isWindows:
        return 'C:' + os.path.sep + 'tools'
    else:
        return os.path.sep + 'tools'

def ToolsPrefix(isWindows):
    return os.path.join(ToolsPath(isWindows), 'GccX64_' + GnuToolsVersion() + '-' + GnuToolsPatch())

def CrossToolsPrefix(isWindows):
    return os.path.join(ToolsPrefix(isWindows), GetTriplet(), 'bin', GetTriplet() + '-')

def GeneratorEnvString():
    return 'K_REGENERATOR'

def DebugInfoEnvString():
    return 'K_MINGW_DEBUG_INFO_ENABLED'

# Some legacy/history. Now by default on, unless explicitly disabled via either variable.
def ReGeneratorEnabled():

    envNew = Utils.EnvInt('K_REGENERATOR')
    envOld = Utils.EnvInt('K_GENERATOR_ENABLED')

    if (envNew == 1 or envOld == 1):
        return True

    if (envNew == 0 or envOld == 0):
        return False

    return True

def DebugInfoEnabled():
    try:
        return os.environ[DebugInfoEnvString()] != '0'
    except KeyError:
        return False

class Writer(WriterBase.WriterBase):

    #
    # Supported platforms and project types
    #
    _platforms = [
        'Win64',
    ]

    _projects = [
        'C/CPP',
    ]

    _cFlags = [
        '-std=gnu99',
        '-Wall',
        #'-Wfloat-conversion',    apparently not supported in GCC 4.8.x (used in bash for windows)
        '-Wno-unused-variable',
        '-Wno-unused-parameter',
        '-Wno-unused-value',
        '-Wno-missing-braces', # this is for GCC bug 53119 -- should be removed when GCC >= 5.x is abundant
    ]

    _cxx11Flags = [
        '-std=c++11',
        '-Wall',
        #'-Wfloat-conversion',    apparently not supported in GCC 4.8.x (used in bash for windows)
    ]

    _cxxFlags = [
        '-std=c++17',
        '-Wall',
        '-Wfloat-conversion',
    ]

    _compilerFlags = [
        '-Wall',
        '-Wno-unused-variable',
        '-Wno-unused-parameter',
        '-Wno-unused-value',
        '-Wno-missing-braces', # this is for GCC bug 53119 -- should be removed when GCC >= 5.x is abundant
    ]

    _compilerFlagTemplates = {
        'Symbols': [
            '-g',
        ],

        'Optimize': [
            '-O2',
        ],

        'Optimize3': [
            '-O3', 
        ],

        'OpenMp': [
            '-fopenmp',
        ],

        'NoAutoExport': [
            '-fvisibility=hidden',
        ],

        'LongCalls': [
            '-mlong-calls',
        ],

        'NoBuiltin': [
            '-fno-builtin',
        ],
    }

    _picCompilerFlags = [
        '-fpic',  # can be disabled with 'NoPic' template hint
    ]

    def __init__(self, platforms, projects):
        WriterBase.WriterBase.__init__(self, self._platforms, platforms, self._projects, projects)
        self.name = 'Mingw-Win64'
        self.plugin = None
        self.regeneratorEnabled = ReGeneratorEnabled()
        self.debugInfoEnabled = DebugInfoEnabled()

    @property
    def Name(self):
        return 'Mingw'

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
        return '-Mingw'

    def ForceCxx11(self, config):
        return True if 'c++11' in config.Template.split('|') else False

    def GetPlatformFlags(self):
        return []

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
        #if self.plugin == Plugin.Linux_X86: return [ '-m32' ]
        #else:                               return []
        return []

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

        #if self.plugin == Plugin.Linux_Arm7:
        #    if 'NoNeon' not in components: # TODO: change default to no-NEON (if 'Neon' in components)
        #        compilerFlags.append('-mfpu=neon')
        #    else:
        #        compilerFlags.append('-mfpu=vfpv3')

        if 'NoPic' not in components:
            for flag in self._picCompilerFlags:
                compilerFlags.append(flag)

        return compilerFlags

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
        includeCudaSources = False

        with Utils.Output(fileName, Utils.EnvironmentStyle.Dollar, True, forceUpdate=forceUpdate) as output:

            utilsPath = os.path.normpath(os.path.join(os.path.dirname(os.path.realpath(__file__)), '..'))
            utilsRelPath = os.path.relpath(utilsPath, os.path.dirname(project.FileName))

            configurations = self.SelectConfigurations(project)

            if len(configurations) != 0:

                output.Format('\n')

                if self.regeneratorEnabled:
                    output.Format('export %s := 1\n' % (GeneratorEnvString()))
                    output.Format('export %s := %u\n' % (DebugInfoEnvString(), int(self.debugInfoEnabled)))
                    output.Format('\n')

                output.Format('ifeq ($(OS)$(os), Windows_NT)\n')
                output.Format('\tTOOLS_PATH   := %s\n' % (ToolsPrefix(True)))
                output.Format('\tCROSS_PREFIX := %s\n' % (CrossToolsPrefix(True)))
                output.Format('\tCROSS_SUFFIX := .exe\n')
                output.Format('\tPYTHON       := python\n')
                output.Format('\tWINDRES      := $(TOOLS_PATH)/x86_64-w64-mingw32/bin/windres$(CROSS_SUFFIX)\n')
                output.Format('else\n')
                output.Format('\tTOOLS_PATH   := %s\n' % (ToolsPrefix(False)))
                output.Format('\tCROSS_PREFIX := %s\n' % (CrossToolsPrefix(False)))
                output.Format('\tCROSS_SUFFIX := \n')
                output.Format('\tPYTHON       := python3\n')
                output.Format('\tWINDRES      := $(CROSS_PREFIX)windres$(CROSS_SUFFIX)\n')
                output.Format('endif\n\n')

                output.Format('C_COMPILER := $(CROSS_PREFIX)gcc$(CROSS_SUFFIX)\n')
                output.Format('CXX_COMPILER := $(CROSS_PREFIX)g++$(CROSS_SUFFIX)\n')
                output.Format('LINKER := $(CROSS_PREFIX)g++$(CROSS_SUFFIX)\n') ##??
                output.Format('ARCHIVER := $(CROSS_PREFIX)ar$(CROSS_SUFFIX)\n')
                output.Format('OBJ_NAMES := $(CROSS_PREFIX)nm$(CROSS_SUFFIX)\n')
                output.Format('\n')

                output.Format('MKDIR_P := $(PYTHON) %s mkdir_p\n' % os.path.join(utilsRelPath, 'kUtil.py'))
                output.Format('RM_F := $(PYTHON) %s rm_f\n' % os.path.join(utilsRelPath, 'kUtil.py'))
                output.Format('RM_RF := $(PYTHON) %s rm_rf\n' % os.path.join(utilsRelPath, 'kUtil.py'))
                output.Format('CP := $(PYTHON) %s cp\n' % os.path.join(utilsRelPath, 'kUtil.py'))
                if self.regeneratorEnabled:
                    output.Format('KGENERATOR := $(PYTHON) %s\n' % os.path.join(utilsRelPath, 'kGenerator.py')) 
                output.Format('\n')

                if self.regeneratorEnabled:
                    output.Format('regenerate := 1\n\n')

                output.Format('ifndef verbose\n')
                output.Format('\tSILENT := @\n')
                output.Format('endif\n\n')

                output.Format('ifndef config\n')
                output.Format('\tconfig := %s\n' % (configurations[0].Name))
                output.Format('endif\n\n')

                output.Format('ifeq ($(word 1, $(subst ., ,$(MAKE_VERSION))), 3)\n')
                output.Format('.PHONY: err\n')
                output.Format('err: ; $(error GNU Make version 3.x is not supported)\n')
                output.Format('endif\n\n')

                for config in configurations:

                    target = os.path.join(config.BinDir, config.Target)
                    targetExt = os.path.splitext(target)[1]

                    compilerFlags = self.CompilerFlagsFromTemplate(config.Template)
                    linkerFlags = self.LinkerFlags()
                    configType = config.Type

                    output.Format('ifeq ($(config),%s)\n' % (config.Name))

                    if configType == 'Executable':
                        output.Format('\tTARGET := %s/%s\n' % (
                            config.BinDir,
                            config.Target))

                    elif configType == 'Shared':
                        output.Format('\tTARGET := %s/%s\n' % (
                            config.BinDir,
                            config.Target))
                        output.Format('\tIMPLIB := %s/lib%s.a\n' % (
                            config.LibDir,
                            os.path.splitext(config.Target)[0]))

                    else:
                        output.Format('\tTARGET := %s/%s\n' % (
                            config.LibDir,
                            config.Target))

                    output.Format('\tINTERMEDIATES := \n')
                    output.Format('\tOBJ_DIR := %s/%s-%s-%s\n' % (config.TempDir, project.Name, self.TempName, config.Name))
                    output.Format('\tPREBUILD := %s\n' % (config.Prebuild))
                    output.Format('\tPOSTBUILD := %s\n' % (config.Postbuild))

                    output.Format('\tCOMPILER_FLAGS :=')
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
                    cxxFlags = self._cxx11Flags if self.ForceCxx11(config) else self._cxxFlags
                    for flag in cxxFlags:
                        output.Format(' ')
                        output.Format('%s' % (flag))
                    output.Format('\n')

                    output.Format('\tINCLUDE_DIRS :=')
                    for dir in config.IncludeDirs:
                        output.Format(' ')
                        output.Format('-I%s' % (dir))
                    output.Format('\n')

                    autoDefines = []
                    autoDefines.append('WINVER=0x0601')
                    autoDefines.append('_WIN32_WINNT=0x0601')

                    if 'Unicode' in config.Template.split('|'):
                        autoDefines.append('UNICODE')

                    output.Format('\tDEFINES :=')
                    for define in config.Defines + autoDefines:
                        output.Format(' ')
                        output.Format('-D%s' % (define))
                    output.Format('\n')

                    output.Format('\tLINKER_FLAGS :=')
                    for flag in linkerFlags:
                        output.Format(' ')
                        output.Format('%s' % (flag))

                    if configType == 'Shared':
                        output.Format(' ')
                        output.Format('-shared')
                        output.Format(' ')
                        output.Format('-Wl,--out-implib,$(IMPLIB)')

                    #if configType == 'Executable' or configType == 'Shared':
                    #    output.Format(' ')
                    #    output.Format('-Wl,-no-undefined')
                    #
                    #    # use the $ORIGIN variable known by GNU LD to support portable apps
                    #    # which can run wherever and locate shared libraries with relative paths.
                    #    if configType == 'Executable':
                    #        output.Format(' ')
                    #        output.Format('-Wl,-rpath,\'$$ORIGIN/%s\'' % (os.path.relpath(config.LibDir, config.BinDir)))
                    #    else:
                    #        output.Format(' ')
                    #        output.Format('-Wl,-rpath,\'$$ORIGIN\'')

                    output.Format('\n')

                    output.Format('\tLIB_DIRS :=')
                    for libDir in config.LibraryDirs:
                        output.Format(' ')
                        output.Format('-L%s' % (libDir))
                    output.Format('\n')

                    output.Format('\tLIBS :=')
                    if len(config.Libs) != 0:
                        #output.Format(' -Wl,--start-group')
                        for lib in config.Libs:
                            if (os.path.splitext(lib)[1] == ''):
                                output.Format(' -l%s' % (lib))
                            else:
                                output.Format(' -l%s' % (os.path.splitext(lib)[0]))
                        #output.Format(' -Wl,--end-group')
                    output.Format('\n')

                    output.Format('\tLDFLAGS := $(LINKER_FLAGS) $(LIBS) $(LIB_DIRS)\n')
                    output.Format('\n')

                    output.Format('\tOBJECTS := ')
                    first = True
                    for src in self.FilterSources(project, config):
                        source = src.Name
                        extension = os.path.splitext(source)[1]
                        if (extension == '.c' or extension == '.cpp' or extension == '.cc' or (extension == '.cu' and includeCudaSources) 
                            or extension == '.s' or extension == '.S' or extension == '.asm' or extension == '.rc'):
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
                                target = os.path.join(projectPath, depConfig.BinDir, depConfig.Target)

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

                    output.Format('%s%s: %s.xml %s %s ' % (project.Name, self.ProjectSuffix, project.Name, solFileNameRel, os.path.join(utilsRelPath, 'Generator', 'Mingw.py')))
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
                    #frameworkName = self.SelectFramework(project, config)
                    frameworkName = None
                    linkScript = None

                    if configType == 'Executable':
                        if frameworkName is not None:
                            target = os.path.splitext(os.path.join(config.BinDir, config.Target))[0] + '.kapp'
                            interName = os.path.join(config.BinDir, config.Target)

                            output.Format('%s: $(OBJECTS) $(TARGET_DEPS)\n' % (interName))
                            output.Format('\t$(SILENT) $(info Ld%s %s)\n' % (MsgSuffix(self.plugin), interName))
                            output.Format('\t$(SILENT) $(LINKER) $(OBJECTS) $(LDFLAGS) -o%s\n\n' % (interName))

                            output.Format('$(TARGET): %s\n' % (interName))
                            output.Format('\t$(SILENT) $(info Tar%s $(TARGET))\n' % (MsgSuffix(self.plugin)))
                            output.Format('\t$(SILENT) $(APP_GEN) $(GNU_READELF) %s %s\n' % (frameworkName, target))

                        else:
                            output.Format('$(TARGET): $(OBJECTS) $(TARGET_DEPS)\n')
                            output.Format('\t$(SILENT) $(info Ld%s $(TARGET))\n' % (MsgSuffix(self.plugin)))
                            output.Format('\t$(SILENT) $(LINKER) $(OBJECTS) $(LDFLAGS) -o$(TARGET)\n\n')

                    elif configType == 'Shared':
                        if frameworkName is not None:
                            target = os.path.splitext(os.path.join(config.BinDir, config.Target))[0] + '.kapp'
                            interName = os.path.join(config.LibDir, config.Target)

                            output.Format('%s: $(OBJECTS) $(TARGET_DEPS)\n' % (interName))
                            output.Format('\t$(SILENT) $(info Ld%s %s)\n' % (MsgSuffix(self.plugin), interName))
                            output.Format('\t$(SILENT) $(LINKER) $(OBJECTS) $(LDFLAGS) -o%s\n\n' % (interName))

                            output.Format('$(TARGET): %s\n' % (interName))
                            output.Format('\t$(SILENT) $(info Tar%s $(TARGET))\n' % (MsgSuffix(self.plugin)))
                            output.Format('\t$(SILENT) $(APP_GEN) $(GNU_READELF) %s --application %s %s\n' % (frameworkName, interName, target))

                        else:
                            output.Format('$(TARGET): $(OBJECTS) $(TARGET_DEPS)\n')
                            output.Format('\t$(SILENT) $(info Ld%s $(TARGET))\n' % (MsgSuffix(self.plugin)))
                            output.Format('\t$(SILENT) $(LINKER) $(OBJECTS) $(LDFLAGS) -o$(TARGET)\n\n')

                    elif configType == 'Library':
                        output.Format('$(TARGET): $(OBJECTS) $(TARGET_DEPS)\n')
                        output.Format('\t$(SILENT) $(info Ar%s $(TARGET))\n' % (MsgSuffix(self.plugin)))
                        output.Format('\t$(SILENT) $(RM_F) $(TARGET)\n')
                        output.Format('\t$(SILENT) $(ARCHIVER) rcs $(TARGET) $(OBJECTS)\n\n')

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
                                    output.Format('\t$(SILENT) $(C_COMPILER) $(COMPILER_FLAGS) $(C_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o %s.o -c %s -MMD -MP\n' % \
                                        (os.path.join(tmpDir, os.path.basename(src.Name)), src.Name))

                                else:
                                    output.Format('\t$(SILENT) $(info Gcc%s %s)\n' % (MsgSuffix(self.plugin), src.Name))
                                    output.Format('\t$(SILENT) $(CXX_COMPILER) $(COMPILER_FLAGS) $(CXX_FLAGS) $(DEFINES) $(INCLUDE_DIRS) -o %s.o -c %s -MMD -MP\n' % \
                                        (os.path.join(tmpDir, os.path.basename(src.Name)), src.Name))

                            else:
                                if extension == '.c':
                                    output.Format('\t$(SILENT) $(info Gcc%s %s)\n' % (MsgSuffix(self.plugin), src.Name))
                                    output.Format('\t$(SILENT) $(C_COMPILER) $(C_FLAGS) %s $(DEFINES) $(INCLUDE_DIRS) -o %s.o -c %s -MMD -MP\n' % \
                                        (compilerFlags, os.path.join(tmpDir, os.path.basename(src.Name)), src.Name))

                                else:
                                    output.Format('\t$(SILENT) $(info Gcc%s %s)\n' % (MsgSuffix(self.plugin), src.Name))
                                    output.Format('\t$(SILENT) $(CXX_COMPILER) $(CXX_FLAGS) %s $(DEFINES) $(INCLUDE_DIRS) -o %s.o -c %s -MMD -MP\n' % \
                                        (compilerFlags, os.path.join(tmpDir, os.path.basename(src.Name)), src.Name))

                            output.Format('\n')

                        elif extension == '.s' or extension == '.S' or extension == '.asm':
                            objectFile = '%s/%s.o' % (tmpDir, os.path.basename(src.Name))

                            output.Format('%s: ' % (objectFile))
                            output.Format('%s\n' % (src.Name))

                            output.Format('\t$(SILENT) $(info Gcc%s %s)\n' % (MsgSuffix(self.plugin), src.Name))
                            output.Format('\t$(SILENT) $(C_COMPILER) $(COMPILER_FLAGS) -x assembler-with-cpp $(DEFINES) $(INCLUDE_DIRS) $(ASM_INCLUDE_DIRS) -o %s.o -c %s\n' % (
                                os.path.join(tmpDir, os.path.basename(src.Name)), src.Name))

                            output.Format('\n')

                        elif extension == '.rc':
                            objectFile = '%s/%s.o' % (tmpDir, os.path.basename(src.Name))

                            output.Format('%s: ' % (objectFile))
                            output.Format('%s\n' % (src.Name))

                            output.Format('\t$(SILENT) $(info Windres%s %s)\n' % (MsgSuffix(self.plugin), src.Name))
                            output.Format('\t$(SILENT) $(WINDRES) -o %s.o -i %s\n' % (
                                os.path.join(tmpDir, os.path.basename(src.Name)), src.Name))

                            output.Format('\n')

                        elif extension == '.cu' and includeCudaSources:
                            ppdSource = '%s/%s.d' % (tmpDir, os.path.basename(src.Name))
                            objectFile = '%s/%s.o' % (tmpDir, os.path.basename(src.Name))

                            output.Format('%s %s: ' % (objectFile, ppdSource))
                            output.Format('%s\n' % (src.Name))

                            if float(self.cudaVersion) < 10.2:
                                # Cuda < 10.2 does not support "--generate-dependencies-with-compile" (-MD/-MMD)
                                output.Format('\t$(SILENT) $(info CudaCc%s %s)\n' % (MsgSuffix(self.plugin), src.Name))
                                output.Format('\t$(SILENT) $(CUDA_COMPILER) $(CUDA_FLAGS) --compiler-options="$(COMPILER_FLAGS) $(DEFINES) $(INCLUDE_DIRS)" -o %s.d -M %s --output-directory="%s"\n' % \
                                    (os.path.join(tmpDir, os.path.basename(src.Name)), src.Name, tmpDir))
                                output.Format('\t$(SILENT) $(CUDA_COMPILER) $(CUDA_FLAGS) --compiler-options="$(COMPILER_FLAGS) $(DEFINES) $(INCLUDE_DIRS)" -o %s.o -c %s\n' % \
                                    (os.path.join(tmpDir, os.path.basename(src.Name)), src.Name))

                            else:
                                output.Format('\t$(SILENT) $(info CudaCc%s %s)\n' % (MsgSuffix(self.plugin), src.Name))
                                output.Format('\t$(SILENT) $(CUDA_COMPILER) $(CUDA_FLAGS) --compiler-options="$(COMPILER_FLAGS) $(DEFINES) $(INCLUDE_DIRS)" -o %s.o -c %s -MMD\n' % \
                                    (os.path.join(tmpDir, os.path.basename(src.Name)), src.Name))

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
