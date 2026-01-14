#!/usr/bin/python
# This file is part of the FireSync Project Generator.
# Copyright (C) 2008-2016 by LMI Technologies Inc.  All rights reserved.
# Distributed under the terms of the MIT License.
# Redistributed files must retain the above copyright notice.

import re
import os
import errno
from . import Project
from . import WriterBase
from . import Xml
from . import Utils
from . import GnuMk

def GetName(plugin):
    if plugin == GnuMk.Plugin.Linux_Arm64:  return 'EclipseCdt_Linux_Arm64'
    elif plugin == GnuMk.Plugin.Linux_Arm7: return 'EclipseCdt_Linux_Arm7'
    elif plugin == GnuMk.Plugin.Linux_X64:  return 'EclipseCdt_Linux_X64'
    elif plugin == GnuMk.Plugin.Linux_X86:  return 'EclipseCdt_Linux_X86'
    else:                                   return ''

class Writer(WriterBase.WriterBase):

    def __init__(self, platforms, projects, plugin):
        WriterBase.WriterBase.__init__(self, GnuMk.GetPlatforms(plugin), platforms, GnuMk.GetProjects(plugin), projects)
        self.__name = GetName(plugin)
        self.__suffix = GnuMk.GetSuffix(plugin)
        self.__plugin = plugin

    @property
    def Name(self):
        return self.__name

    @property
    def PluginSuffix(self):
        if self.__plugin == GnuMk.Plugin.Linux_Arm64:   return 'LinuxArm64'
        elif self.__plugin == GnuMk.Plugin.Linux_Arm7:  return 'LinuxArm7'
        elif self.__plugin == GnuMk.Plugin.Linux_X64:   return 'LinuxX64'
        elif self.__plugin == GnuMk.Plugin.Linux_X86:   return 'LinuxX86'
        else:                                           return ''

    @property
    def Ext(self):
        return '.EclipseCdt' + self.PluginSuffix

    def HasSymbols(self, config):

        return True if 'Symbols' in config.Template.split('|') else False

    def IsDebug(self, config):

        # Using the configuration name instead of the template, which helps Eclipse
        # to match up configurations between projects and their dependencies.
        return True if config.Name == 'Debug' else False

    def DebugOrRelease(self, config):

        return 'Debug' if self.IsDebug(config) else 'Release'

    def Id(self, name):

        hash = 0
        for char in name:
            hash = hash * 31 + ord(char)

        return str(hash).zfill(10)

    def UniqueId(self, projectName, configName, id):

        return self.Id(projectName + configName + str(id))

    def GmakePath(self, project, config):
        herePath = os.path.dirname(os.path.realpath(__file__))
        return os.path.normpath(os.path.join(herePath, '..', '..', '..', 'extern', 'Make', 'gmake.exe'))

    def WriteLinkedResourcesFilters(self, project, sourceGroup, filterItem, filterName = ''):

        for childGroup in sourceGroup.Groups:
            linkItem = filterItem.AddElem('link')

            linkItem.AddElem('name', filterName + childGroup.Name)
            linkItem.AddElem('type', '2')
            linkItem.AddElem('locationURI', 'virtual:/virtual')

            self.WriteLinkedResourcesFilters(project, childGroup, filterItem, filterName + childGroup.Name + '/')

    def WriteLinkedResourcesFiles(self, project, sourceGroup, filterItem, filterName = ''):

        for childGroup in sourceGroup.Groups:
            self.WriteLinkedResourcesFiles(project, childGroup, filterItem, filterName + childGroup.Name + '/')

        for src in sourceGroup.Sources:
            linkItem = filterItem.AddElem('link')

            linkItem.AddElem('name', filterName + os.path.basename(src.Name))
            linkItem.AddElem('type', '1')
            linkItem.AddElem('location', os.path.join(os.path.dirname(project.FileName), src.Name))

    def WriteLinkedResources(self, project, sourceGroup, filterItem):
        self.WriteLinkedResourcesFilters(project, sourceGroup, filterItem)
        self.WriteLinkedResourcesFiles(project, sourceGroup, filterItem)

    def WriteDotProject(self, project, fileName):

        root = Xml.Element('projectDescription')
        root.AddElem('name', project.Name)
        root.AddElem('comment', '')

        dependencies = self.SelectDependencies(project)
        projectsItem = root.AddElem('projects')

        if len(dependencies) == 0:
            projectsItem.AddElem()
        else:
            for dependency in dependencies:
                projectsItem.AddElem('project', dependency.Name)

        buildSpecItem = root.AddElem('buildSpec')
        buildCommandItem = buildSpecItem.AddElem('buildCommand')
        buildCommandItem.AddElem('name', 'org.eclipse.cdt.managedbuilder.core.genmakebuilder')
        buildCommandItem.AddElem('triggers', 'clean,full,incremental,')
        argumentsItem = buildCommandItem.AddElem('arguments')
        argumentsItem.AddElem()
        buildCommandItem = buildSpecItem.AddElem('buildCommand')
        buildCommandItem.AddElem('name', 'org.eclipse.cdt.managedbuilder.core.ScannerConfigBuilder')
        buildCommandItem.AddElem('triggers', 'full,incremental,')
        argumentsItem = buildCommandItem.AddElem('arguments')
        argumentsItem.AddElem()

        naturesItem = root.AddElem('natures')
        naturesItem.AddElem('nature', 'org.eclipse.cdt.core.cnature')
        naturesItem.AddElem('nature', 'org.eclipse.cdt.core.ccnature')
        naturesItem.AddElem('nature', 'org.eclipse.cdt.managedbuilder.core.managedBuildNature')
        naturesItem.AddElem('nature', 'org.eclipse.cdt.managedbuilder.core.ScannerConfigNature')

        linkedResourcesItem = root.AddElem('linkedResources')
        self.WriteLinkedResources(project, project.SourceGroups, linkedResourcesItem)

        root.WriteDotProject(root, fileName)

    def WriteDotCProject(self, project, fileName):

        projectName = project.Name
        root = Xml.Element('cproject')
        root.AddAttr('storage_type_id', 'org.eclipse.cdt.core.XmlProjectDescriptionStorage')

        storageModuleItem = root.AddElem('storageModule')
        storageModuleItem.AddAttr('moduleId', 'org.eclipse.cdt.core.settings')

        for config in self.SelectConfigurations(project):
            debugOrRelease = self.DebugOrRelease(config)
            configName = config.Name
            cconfigurationItem = storageModuleItem.AddElem('cconfiguration')
            cconfigurationItem.AddAttr('id', 'cdt.managedbuild.config.gnu.cross.exe.' + debugOrRelease.lower() + '.' + self.UniqueId(projectName, configName, 0))
            subStorageModule = cconfigurationItem.AddElem('storageModule')
            subStorageModule.AddAttr('buildSystemId', 'org.eclipse.cdt.managedbuilder.core.configurationDataProvider')
            subStorageModule.AddAttr('id', 'cdt.managedbuild.config.gnu.cross.exe.%s.%s' % (debugOrRelease.lower(), self.UniqueId(projectName, configName, 0)))
            subStorageModule.AddAttr('moduleId', 'org.eclipse.cdt.core.settings')
            subStorageModule.AddAttr('name', configName)
            subStorageModule.AddElem('externalSettings')

            extensionsItem = subStorageModule.AddElem('extensions')

            extensionItem = extensionsItem.AddElem('extension')
            extensionItem.AddAttr('id', 'org.eclipse.cdt.core.ELF')
            extensionItem.AddAttr('point', 'org.eclipse.cdt.core.BinaryParser')

            extensionItem = extensionsItem.AddElem('extension')
            extensionItem.AddAttr('id', 'org.eclipse.cdt.core.GASErrorParser')
            extensionItem.AddAttr('point', 'org.eclipse.cdt.core.ErrorParser')

            extensionItem = extensionsItem.AddElem('extension')
            extensionItem.AddAttr('id', 'org.eclipse.cdt.core.GmakeErrorParser')
            extensionItem.AddAttr('point', 'org.eclipse.cdt.core.ErrorParser')

            extensionItem = extensionsItem.AddElem('extension')
            extensionItem.AddAttr('id', 'org.eclipse.cdt.core.GLDErrorParser')
            extensionItem.AddAttr('point', 'org.eclipse.cdt.core.ErrorParser')

            extensionItem = extensionsItem.AddElem('extension')
            extensionItem.AddAttr('id', 'org.eclipse.cdt.core.CWDLocator')
            extensionItem.AddAttr('point', 'org.eclipse.cdt.core.ErrorParser')
            
            extensionItem = extensionsItem.AddElem('extension')
            extensionItem.AddAttr('id', 'org.eclipse.cdt.core.GCCErrorParser')
            extensionItem.AddAttr('point', 'org.eclipse.cdt.core.ErrorParser')            

            subStorageModule = cconfigurationItem.AddElem('storageModule')
            subStorageModule.AddAttr('moduleId', 'cdtBuildSystem')
            subStorageModule.AddAttr('version', '4.0.0')

            if config.Type == 'Executable':
                artifactName = os.path.normpath(os.path.join(os.path.dirname(project.FileName), config.BinDir, config.Target))
                artifactExtension = ''
                artifactType = 'exe'
            elif config.Type == 'Library':
                artifactName = os.path.normpath(os.path.join(os.path.dirname(project.FileName), config.LibDir, config.Target))
                artifactExtension = ''
                artifactType = 'staticLibrary'
            else:
                artifactName = os.path.normpath(os.path.join(os.path.dirname(project.FileName), config.LibDir, config.Target))
                artifactExtension = ''
                artifactType = 'sharedLib'

            configurationItem = subStorageModule.AddElem('configuration')
            configurationItem.AddAttr('artifactExtension', artifactExtension)
            configurationItem.AddAttr('artifactName',  artifactName)
            configurationItem.AddAttr('buildArtefactType',  'org.eclipse.cdt.build.core.buildArtefactType.%s' % (artifactType))
            configurationItem.AddAttr('buildProperties',    'org.eclipse.cdt.build.core.buildArtefactType=' + 
                                                            'org.eclipse.cdt.build.core.buildArtefactType.%s,' % (artifactType) +
                                                            'org.eclipse.cdt.build.core.buildType=org.eclipse.cdt.build.core.buildType.%s' % (debugOrRelease.lower()))

            configurationItem.AddAttr('cleanCommand', 'rm -rf')
            configurationItem.AddAttr('description', '')
            configurationItem.AddAttr('id', 'cdt.managedbuild.config.gnu.cross.exe.%s.%s' % (debugOrRelease.lower(), self.UniqueId(projectName, configName, 0)))
            configurationItem.AddAttr('name', configName)
            configurationItem.AddAttr('parent', 'cdt.managedbuild.config.gnu.cross.exe.' + debugOrRelease.lower())

            folderInfoElem = configurationItem.AddElem('folderInfo')
            folderInfoElem.AddAttr('id', 'cdt.managedbuild.config.gnu.cross.exe.%s.%s.' % (debugOrRelease.lower(), self.UniqueId(projectName, configName, 0)))
            folderInfoElem.AddAttr('name', '/')
            folderInfoElem.AddAttr('resourcePath', '')

            toolChainItem = folderInfoElem.AddElem('toolChain')
            toolChainItem.AddAttr('id', 'cdt.managedbuild.toolchain.gnu.cross.exe.%s.%s' % (debugOrRelease.lower(), self.UniqueId(projectName, configName, 60)))
            toolChainItem.AddAttr('name', 'Cross GCC')
            toolChainItem.AddAttr('superClass', 'cdt.managedbuild.toolchain.gnu.cross.exe.%s' % (debugOrRelease.lower()))

            optionItem = toolChainItem.AddElem('option')
            optionItem.AddAttr('id', 'cdt.managedbuild.option.gnu.cross.prefix.%s' % (self.UniqueId(projectName, configName, 70)))
            optionItem.AddAttr('name', 'Prefix')
            optionItem.AddAttr('superClass', 'cdt.managedbuild.option.gnu.cross.prefix')
            optionItem.AddAttr('useByScannerDiscovery', 'false')
            optionItem.AddAttr('value', GnuMk.GetToolsPrefix(self.__plugin))
            optionItem.AddAttr('valueType', 'string')

            optionItem = toolChainItem.AddElem('option')
            optionItem.AddAttr('id', 'cdt.managedbuild.option.gnu.cross.path.%s' % (self.UniqueId(projectName, configName, 80)))
            optionItem.AddAttr('name', 'Path')
            optionItem.AddAttr('superClass', 'cdt.managedbuild.option.gnu.cross.path')
            optionItem.AddAttr('useByScannerDiscovery', 'false')
            optionItem.AddAttr('value', GnuMk.GetToolsBinPath(self.__plugin))
            optionItem.AddAttr('valueType', 'string')

            targetPlatformItem = toolChainItem.AddElem('targetPlatform')
            targetPlatformItem.AddAttr('archList', 'all')
            targetPlatformItem.AddAttr('binaryParser', 'org.eclipse.cdt.core.ELF')
            targetPlatformItem.AddAttr('id', 'cdt.managedbuild.targetPlatform.gnu.cross.%s' % (self.UniqueId(projectName, configName, 90)))
            targetPlatformItem.AddAttr('isAbstract', 'false')
            targetPlatformItem.AddAttr('osList', 'all')
            targetPlatformItem.AddAttr('superClass', 'cdt.managedbuild.targetPlatform.gnu.cross')

            builderItem = toolChainItem.AddElem('builder')
            builderItem.AddAttr('arguments', '-C %s -s -f %s%s.mk config=%s' % (os.path.dirname(project.FileName), project.Name, self.__suffix, configName))
            builderItem.AddAttr('buildPath', '${BuildDirectory}')
            builderItem.AddAttr('command', self.GmakePath(project, config))
            builderItem.AddAttr('id', 'cdt.managedbuild.builder.gnu.cross.%s' % (self.UniqueId(projectName, configName, 110)))
            builderItem.AddAttr('enableAutoBuild', 'false')
            builderItem.AddAttr('autoBuildTarget', 'all')
            builderItem.AddAttr('enableCleanBuild', 'true')
            builderItem.AddAttr('cleanBuildTarget', 'clean')
            builderItem.AddAttr('enabledIncrementalBuild', 'true')
            builderItem.AddAttr('incrementalBuildTarget', 'all')
            builderItem.AddAttr('keepEnvironmentInBuildfile', 'false')
            builderItem.AddAttr('managedBuildOn', 'false')
            builderItem.AddAttr('stopOnErr', 'true')
            builderItem.AddAttr('name', 'Gnu Make Builder')
            builderItem.AddAttr('superClass', 'cdt.managedbuild.builder.gnu.cross')

            toolItem = toolChainItem.AddElem('tool')
            toolItem.AddAttr('id', 'cdt.managedbuild.tool.gnu.cross.c.compiler.%s' % (self.UniqueId(projectName, configName, 120)))
            toolItem.AddAttr('name', 'Cross GCC Compiler')
            toolItem.AddAttr('superClass', 'cdt.managedbuild.tool.gnu.cross.c.compiler')

            optionItem = toolItem.AddElem('option')
            optionItem.AddAttr('id', 'gnu.c.compiler.option.include.paths.%s' % (self.UniqueId(projectName, configName, 130)))
            optionItem.AddAttr('name', 'Include paths (-I)')
            optionItem.AddAttr('superClass', 'gnu.c.compiler.option.include.paths')
            optionItem.AddAttr('useByScannerDiscovery', 'false')
            optionItem.AddAttr('valueType', 'includePath')

            for listOptionValue in [os.path.normpath(os.path.join(os.path.dirname(project.FileName), dir)) for dir in config.IncludeDirs]:
                listOptionValueItem = optionItem.AddElem('listOptionValue')
                listOptionValueItem.AddAttr('builtIn', 'false')
                listOptionValueItem.AddAttr('value', '&quot;' + listOptionValue + '&quot;')

            optionItem = toolItem.AddElem('option')
            optionItem.AddAttr('id', 'gnu.c.compiler.option.preprocessor.def.symbols.%s' % (self.UniqueId(projectName, configName, 140)))
            optionItem.AddAttr('name', 'Defined symbols (-D)')
            optionItem.AddAttr('superClass', 'gnu.c.compiler.option.preprocessor.def.symbols')
            optionItem.AddAttr('useByScannerDiscovery', 'false')
            optionItem.AddAttr('valueType', 'definedSymbols')

            for listOptionValue in config.Defines:
                listOptionValueItem = optionItem.AddElem('listOptionValue')
                listOptionValueItem.AddAttr('builtIn', 'false')
                listOptionValueItem.AddAttr('value', listOptionValue)

            optionItem = toolItem.AddElem('option')
            optionItem.AddAttr('id', 'gnu.c.compiler.option.misc.pic.%s' % (self.UniqueId(projectName, configName, 150)))
            optionItem.AddAttr('name', 'Position Independent Code (-fPIC)')            
            optionItem.AddAttr('superClass', 'gnu.c.compiler.option.misc.pic')
            optionItem.AddAttr('useByScannerDiscovery', 'false')
            optionItem.AddAttr('value', 'true')
            optionItem.AddAttr('valueType', 'boolean')

            inputTypeItem = toolItem.AddElem('inputType')
            inputTypeItem.AddAttr('id', 'cdt.managedbuild.tool.gnu.c.compiler.input.%s' % (self.UniqueId(projectName, configName, 160)))
            inputTypeItem.AddAttr('superClass', 'cdt.managedbuild.tool.gnu.c.compiler.input')

            toolItem = toolChainItem.AddElem('tool')
            toolItem.AddAttr('id', 'cdt.managedbuild.tool.gnu.cross.cpp.compiler.%s' % (self.UniqueId(projectName, configName, 220)))
            toolItem.AddAttr('name', 'Cross G++ Compiler')
            toolItem.AddAttr('superClass', 'cdt.managedbuild.tool.gnu.cross.cpp.compiler')

            optionItem = toolItem.AddElem('option')
            optionItem.AddAttr('id', 'gnu.cpp.compiler.option.include.paths.%s' % (self.UniqueId(projectName, configName, 230)))
            optionItem.AddAttr('name', 'Include paths (-I)')
            optionItem.AddAttr('superClass', 'gnu.cpp.compiler.option.include.paths')
            optionItem.AddAttr('useByScannerDiscovery', 'false')
            optionItem.AddAttr('valueType', 'includePath')

            for listOptionValue in [os.path.normpath(os.path.join(os.path.dirname(project.FileName), dir)) for dir in config.IncludeDirs]:
                listOptionValueItem = optionItem.AddElem('listOptionValue')
                listOptionValueItem.AddAttr('builtIn', 'false')
                listOptionValueItem.AddAttr('value', '&quot;' + listOptionValue + '&quot;')

            optionItem = toolItem.AddElem('option')
            optionItem.AddAttr('id', 'gnu.cpp.compiler.option.preprocessor.def.%s' % (self.UniqueId(projectName, configName, 240)))
            optionItem.AddAttr('name', 'Defined symbols (-D)')
            optionItem.AddAttr('superClass', 'gnu.cpp.compiler.option.preprocessor.def')
            optionItem.AddAttr('useByScannerDiscovery', 'false')
            optionItem.AddAttr('valueType', 'definedSymbols')

            for listOptionValue in config.Defines:
                listOptionValueItem = optionItem.AddElem('listOptionValue')
                listOptionValueItem.AddAttr('builtIn', 'false')
                listOptionValueItem.AddAttr('value', listOptionValue)

            optionItem = toolItem.AddElem('option')
            optionItem.AddAttr('id', 'gnu.cpp.compiler.option.other.pic.%s' % (self.UniqueId(projectName, configName, 250)))
            optionItem.AddAttr('name', 'Position Independent Code (-fPIC)')
            optionItem.AddAttr('superClass', 'gnu.cpp.compiler.option.other.pic')
            optionItem.AddAttr('useByScannerDiscovery', 'false')
            optionItem.AddAttr('value', 'true')
            optionItem.AddAttr('valueType', 'boolean')

            optionItem = toolItem.AddElem('option')
            optionItem.AddAttr('id', 'gnu.cpp.compiler.option.dialect.std.%s' % (self.UniqueId(projectName, configName, 255)))
            optionItem.AddAttr('superClass', 'gnu.cpp.compiler.option.dialect.std')
            optionItem.AddAttr('useByScannerDiscovery', 'true')
            optionItem.AddAttr('value', 'gnu.cpp.compiler.dialect.c++11')
            optionItem.AddAttr('valueType', 'enumerated')            

            inputTypeItem = toolItem.AddElem('inputType')
            inputTypeItem.AddAttr('id', 'cdt.managedbuild.tool.gnu.cpp.compiler.input.%s' % (self.UniqueId(projectName, configName, 260)))
            inputTypeItem.AddAttr('superClass', 'cdt.managedbuild.tool.gnu.cpp.compiler.input')

            toolItem = toolChainItem.AddElem('tool')
            toolItem.AddAttr('id', 'cdt.managedbuild.tool.gnu.cross.c.linker.%s' % (self.UniqueId(projectName, configName, 300)))
            toolItem.AddAttr('name', 'Cross GCC Linker')
            toolItem.AddAttr('superClass', 'cdt.managedbuild.tool.gnu.cross.c.linker')

            if config.Type == 'Shared':
                optionItem = toolItem.AddElem('option')
                optionItem.AddAttr('defaultValue', 'true')
                optionItem.AddAttr('id', 'gnu.c.link.option.shared.%s' % (self.UniqueId(projectName, configName, 310)))
                optionItem.AddAttr('name', 'Shared (-shared)')
                optionItem.AddAttr('superClass', 'gnu.c.link.option.shared')
                optionItem.AddAttr('valueType', 'boolean')

            toolItem = toolChainItem.AddElem('tool')
            toolItem.AddAttr('id', 'cdt.managedbuild.tool.gnu.cross.cpp.linker.%s' % (self.UniqueId(projectName, configName, 400)))
            toolItem.AddAttr('name', 'Cross G++ Linker')
            toolItem.AddAttr('superClass', 'cdt.managedbuild.tool.gnu.cross.cpp.linker')

            optionItem = toolItem.AddElem('option')
            optionItem.AddAttr('id', 'gnu.cpp.link.option.libs.%s' % (self.UniqueId(projectName, configName, 410)))
            optionItem.AddAttr('name', 'Libraries (-l)')
            optionItem.AddAttr('superClass', 'gnu.cpp.link.option.libs')
            optionItem.AddAttr('useByScannerDiscovery', 'false')
            optionItem.AddAttr('valueType', 'libs')

            for listOptionValue in config.Libs:
                listOptionValueItem = optionItem.AddElem('listOptionValue')
                listOptionValueItem.AddAttr('builtIn', 'false')
                listOptionValueItem.AddAttr('value', '&quot;' + listOptionValue + '&quot;')

            optionItem = toolItem.AddElem('option')
            optionItem.AddAttr('id', 'gnu.cpp.link.option.paths.%s' % (self.UniqueId(projectName, configName, 420)))
            optionItem.AddAttr('superClass', 'gnu.cpp.link.option.paths')
            optionItem.AddAttr('useByScannerDiscovery', 'false')
            optionItem.AddAttr('valueType', 'libPaths')

            for listOptionValue in [os.path.normpath(os.path.join(os.path.dirname(project.FileName), dir)) for dir in config.LibraryDirs]:
                listOptionValueItem = optionItem.AddElem('listOptionValue')
                listOptionValueItem.AddAttr('builtIn', 'false')
                listOptionValueItem.AddAttr('value', '&quot;' + listOptionValue + '&quot;')

            if config.Type == 'Shared':
                optionItem = toolItem.AddElem('option')
                optionItem.AddAttr('defaultValue', 'true')
                optionItem.AddAttr('id', 'gnu.cpp.link.option.shared.%s' % (self.UniqueId(projectName, configName, 430)))
                optionItem.AddAttr('name', 'Shared (-shared)')
                optionItem.AddAttr('superClass', 'gnu.cpp.link.option.shared')
                optionItem.AddAttr('valueType', 'boolean')

            inputTypeItem = toolItem.AddElem('inputType')
            inputTypeItem.AddAttr('id', 'cdt.managedbuild.tool.gnu.cpp.linker.input.%s' % (self.UniqueId(projectName, configName, 440)))
            inputTypeItem.AddAttr('superClass', 'cdt.managedbuild.tool.gnu.cpp.linker.input')

            if config.Type == 'Shared':
                outputTypeItem = toolItem.AddElem('outputType')
                outputTypeItem.AddAttr('id', 'cdt.managedbuild.tool.gnu.cpp.linker.output.so.%s' % (self.UniqueId(projectName, configName, 445)))
                outputTypeItem.AddAttr('outputPrefix', '')
                outputTypeItem.AddAttr('superClass', 'cdt.managedbuild.tool.gnu.cpp.linker.output.so')

            additionalInputItem = inputTypeItem.AddElem('additionalInput')
            additionalInputItem.AddAttr('kind', 'additionalinputdependency')
            additionalInputItem.AddAttr('paths', '$(USER_OBJS)')
            additionalInputItem = inputTypeItem.AddElem('additionalInput')
            additionalInputItem.AddAttr('kind', 'additionalinput')
            additionalInputItem.AddAttr('paths', '$(LIBS)')

            toolItem = toolChainItem.AddElem('tool')
            toolItem.AddAttr('id', 'cdt.managedbuild.tool.gnu.cross.archiver.%s' % (self.UniqueId(projectName, configName, 450)))
            toolItem.AddAttr('name', 'Cross GCC Archiver')
            toolItem.AddAttr('superClass', 'cdt.managedbuild.tool.gnu.cross.archiver')

            toolItem = toolChainItem.AddElem('tool')
            toolItem.AddAttr('id', 'cdt.managedbuild.tool.gnu.cross.assembler.%s' % (self.UniqueId(projectName, configName, 460)))
            toolItem.AddAttr('name', 'Cross GCC Assembler')
            toolItem.AddAttr('superClass', 'cdt.managedbuild.tool.gnu.cross.assembler')

            inputTypeItem = toolItem.AddElem('inputType')
            inputTypeItem.AddAttr('id', 'cdt.managedbuild.tool.gnu.assembler.input.%s' % (self.UniqueId(projectName, configName, 470)))
            inputTypeItem.AddAttr('superClass', 'cdt.managedbuild.tool.gnu.assembler.input')

            subStorageModule = cconfigurationItem.AddElem('storageModule')
            subStorageModule.AddAttr('moduleId', 'org.eclipse.cdt.core.externalSettings')

        storageModuleItem = root.AddElem('storageModule')
        storageModuleItem.AddAttr('moduleId', 'cdtBuildSystem')
        storageModuleItem.AddAttr('version', '4.0.0')

        if config.Type == 'Executable':
            builderName = 'Executable'
            builderType = 'exe'
        elif config.Type == 'Shared':
            builderName = 'Shared Library'
            builderType = 'so'
        elif config.Type == 'Library':
            builderName = 'Static Library'
            builderType = 'lib'
        else:
            raise Exception('Unknown configuration type.')

        projectItem = storageModuleItem.AddElem('project')
        projectItem.AddAttr('id', '%s.cdt.managedbuild.target.gnu.cross.%s.%s' % (project.Name, builderType, self.UniqueId(projectName, '', '480')))
        projectItem.AddAttr('name', builderName)
        projectItem.AddAttr('projectType', 'cdt.managedbuild.target.gnu.cross.%s' % (builderType))

        scannerConfigurationItem = root.AddElem('storageModule')
        scannerConfigurationItem.AddAttr('moduleId', 'scannerConfiguration')

        storageModuleItem = root.AddElem('storageModule')
        storageModuleItem.AddAttr('moduleId', 'org.eclipse.cdt.core.LanguageSettingsProviders')

        storageModuleItem = root.AddElem('storageModule')
        storageModuleItem.AddAttr('moduleId', 'refreshScope')
        storageModuleItem.AddAttr('versionNumber', '2')

        for config in self.SelectConfigurations(project):
            configurationItem = storageModuleItem.AddElem('configuration')
            configurationItem.AddAttr('configurationName', config.Name)
            resourceItem = configurationItem.AddElem('resource')
            resourceItem.AddAttr('resourceType', 'PROJECT')
            resourceItem.AddAttr('workspacePath', '/' + project.Name)

        storageModuleItem = root.AddElem('storageModule')
        storageModuleItem.AddAttr('moduleId', 'org.eclipse.cdt.make.core.buildtargets')

        root.WriteDotCProject(root, fileName, False, True)

    def WriteProject(self, project):

        solution = project.Solution

        if solution is not None:
            dirName = os.path.join(os.path.dirname(solution.FileName), '.' + solution.Name + self.Ext, project.Name)
        else:
            dirName = os.path.join(os.path.dirname(project.FileName), '.' + project.Name + self.Ext)

        try:
            os.makedirs(dirName)
        except OSError as exc:
            if exc.errno != errno.EEXIST:
                raise

        self.WriteDotProject(project, os.path.join(dirName, '.project'))
        self.WriteDotCProject(project, os.path.join(dirName, '.cproject'))

    def WriteSolution(self, solution):
        # not implemented
        return
