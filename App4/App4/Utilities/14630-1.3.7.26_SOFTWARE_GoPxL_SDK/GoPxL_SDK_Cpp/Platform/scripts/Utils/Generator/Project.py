#!/usr/bin/python
# This file is part of the FireSync Project Generator.
# Copyright (C) 2008-2016 by LMI Technologies Inc.  All rights reserved.
# Distributed under the terms of the MIT License.
# Redistributed files must retain the above copyright notice.

import xml.etree.ElementTree as xml
import re
import os
from . import Utils

def ProjectSelfEnabled():
    try:
        return os.environ['K_PROJECT_SELF_ENABLED'] != '0'
    except KeyError:
        return True

def ProjectName(str):
    return os.path.splitext(os.path.basename(Utils.SepToNative(str)))[0]

class ProjectVariable:
    def __init__(self, name, value):
        self.__name = name
        self.__value = value

    @property
    def Name(self):
        return self.__name

    @property
    def Value(self):
        return self.__value

class ProjectReference:
    def __init__(self, type, value):
        self.__type = type
        self.__value = value

    @property
    def Type(self):
        return self.__type

    @property
    def Value(self):
        return self.__value

class ProjectSource:
    def __init__(self, name, type):
        self.__name = name
        self.__type = type

    @property
    def Name(self):
        return self.__name

    @property
    def Type(self):
        return self.__type

class ProjectSourceGroup:
    def __init__(self, name):
        self.__name = name
        self.__sources = []
        self.__groups = []

    @property
    def Name(self):
        return self.__name

    def AddSource(self, source, type):
        self.__sources.append(ProjectSource(source, type))

    @property
    def Sources(self):
        return self.__sources

    def AddGroup(self, group):
        self.__groups.append(group)

    @property
    def Groups(self):
        return self.__groups

class SourceTemplate:
    def __init__(self, name, add, remove):
        self.__name = name
        self.__add = add
        self.__remove = remove

    @property
    def Name(self):
        return self.__name

    @property
    def Add(self):
        return self.__add

    @property
    def Remove(self):
        return self.__remove

class ProjectConfiguration:
    def __init__(self, name, platform, type, template, binDir, libDir, tempDir):
        self.__name = name
        self.__platform = platform
        self.__type = type
        self.__template = template
        self.__binDir = binDir
        self.__libDir = libDir
        self.__tempDir = tempDir
        self.__target = ''
        self.__postBuild = ''
        self.__preBuild = ''
        self.__buildCmd = ''
        self.__rebuildCmd = ''
        self.__cleanCmd = ''
        self.__includeDirs = []
        self.__defines = []
        self.__libraryDirs = []
        self.__libs = []
        self.__delayLoadLibs = []
        self.__sourceTemplates = []

    @property
    def Name(self):
        return self.__name

    @property
    def Platform(self):
        return self.__platform

    @property
    def Type(self):
        return self.__type

    @property
    def Template(self):
        return self.__template

    @property
    def BinDir(self):
        return self.__binDir

    @property
    def LibDir(self):
        return self.__libDir

    @property
    def TempDir(self):
        return self.__tempDir

    def SetTarget(self, target):
        self.__target = target

    @property
    def Target(self):
        return self.__target

    def SetPostbuild(self, postBuild):
        self.__postBuild = postBuild

    @property
    def Postbuild(self):
        return self.__postBuild
        
    def SetPrebuild(self, preBuild):
        self.__preBuild = preBuild

    @property
    def Prebuild(self):
        return self.__preBuild

    def SetBuildCmd(self, buildCmd):
        self.__buildCmd = buildCmd

    @property
    def BuildCmd(self):
        return self.__buildCmd

    def SetRebuildCmd(self, rebuildCmd):
        self.__rebuildCmd = rebuildCmd

    @property
    def RebuildCmd(self):
        return self.__rebuildCmd

    def SetCleanCmd(self, cleanCmd):
        self.__cleanCmd = cleanCmd

    @property
    def CleanCmd(self):
        return self.__cleanCmd

    def AddIncludeDir(self, dir):
        self.__includeDirs.append(dir)

    @property
    def IncludeDirs(self):
        return self.__includeDirs

    def AddDefine(self, define):
        self.__defines.append(define)

    @property
    def Defines(self):
        return self.__defines

    def AddLibraryDir(self, dir):
        self.__libraryDirs.append(dir)

    @property
    def LibraryDirs(self):
        return self.__libraryDirs

    def AddLib(self, lib):
        self.__libs.append(lib)

    @property
    def Libs(self):
        return self.__libs

    def AddDelayLoadLib(self, lib):
        self.__delayLoadLibs.append(lib)

    @property
    def DelayLoadLibs(self):
        return self.__delayLoadLibs

    def AddSourceTemplate(self, name, add, remove):
        self.__sourceTemplates.append(SourceTemplate(name, add, remove))

    @property
    def SourceTemplates(self):
        return self.__sourceTemplates

class Project:
    def __init__(self, name, solution = None):
        self.__name = ProjectName(name)

        if solution is not None:
            self.__fileName = os.path.join(os.path.dirname(solution.FileName), Utils.SepToNative(name))
        else:
            self.__fileName = os.path.realpath(Utils.SepToNative(name))

        self.__solution = solution
        self.__dependencies = []
        self.__type = ''
        self.__sourceGroups = []
        self.__configurations = []
        self.__variables = []
        self.__references = []

        self.LoadFile(self.__fileName)

    @property
    def Name(self):
        return self.__name

    @property
    def Solution(self):
        return self.__solution

    @property
    def FileName(self):
        return self.__fileName

    def SetDependencies(self, dependencies):
        self.__dependencies = dependencies

    @property
    def Dependencies(self):
        return self.__dependencies

    @property
    def Type(self):
        return self.__type

    @property
    def SourceGroups(self):
        return self.__sourceGroups

    @property
    def Configurations(self):
        return self.__configurations

    @property
    def Variables(self):
        return self.__variables

    @property
    def References(self):
        return self.__references

    def Substitute(self, str):
        result = str
        matches = re.findall(r'\#\[(.*?)\]+', str)
        uniqueMatches = set(matches)

        for match in uniqueMatches:
            for var in self.__variables:
                if var.Name == match:
                    result = result.replace('#[%s]' % (var.Name), var.Value)

        return result

    def Import(self, input):
        if input is None:   return ''
        else:               return Utils.SepToNative(self.Substitute(input))

    def LoadGroups(self, group, element, currentDirectory):

        sourceElements = element.findall('Source')
        if sourceElements is not None:
            for sourceElement in sourceElements:
                if sourceElement.text is not None:

                    sourceName = self.Import(sourceElement.text)

                    # TODO: test the following; needed when imported file is not in the same directory
                    #sourcePath = os.path.join(currentDirectory, self.Import(sourceElement.text))
                    #sourceName = os.path.relpath(sourcePath, os.path.dirname(self.FileName))

                    try:
                        typeName = self.Import(sourceElement.attrib['type'])
                    except (KeyError):
                        typeName = ''

                    # Block the addition of the project source itself
                    if sourceName != self.Name + '.xml':
                        group.AddSource(sourceName, typeName)

        sourceGroups = element.findall('SourceGroup')
        if sourceGroups is not None:
            for sourceGroupElement in sourceGroups:
                try:
                    nameAttrib = self.Import(sourceGroupElement.attrib['name'])
                except (KeyError):
                    nameAttrib =  ''

                newGroup = ProjectSourceGroup(nameAttrib)
                self.LoadGroups(newGroup, sourceGroupElement, currentDirectory)
                group.AddGroup(newGroup)

    def Load(self, root):

        typeElement = root.find('Type')
        if typeElement is not None:
            self.__type = typeElement.text
        else:
            self.__type = 'C/CPP'

        variablesElement = root.find('Variables')
        if variablesElement is not None:
            for varEntry in variablesElement.findall('Variable'):
                nameAttrib = self.Import(varEntry.attrib['name'])
                self.__variables.append(ProjectVariable(nameAttrib, varEntry.text))

        referencesElement = root.find('References')
        if referencesElement is not None:
            for referenceElement in referencesElement.findall('Reference'):
                typeAttrib = self.Import(referenceElement.attrib['type'])
                valueAttrib = self.Import(referenceElement.attrib['value'])
                self.__references.append(ProjectReference(typeAttrib, valueAttrib))

        element = root.find('Sources')
        group = ProjectSourceGroup('')
        self.LoadGroups(group, element, os.path.dirname(self.__fileName))

        if (ProjectSelfEnabled()):
            group.AddSource(self.Name + '.xml', 'self')

        self.__sourceGroups = group

        configurationsElement = root.find('Configurations')
        configurationElements = configurationsElement.findall('Configuration')

        if configurationElements is not None:
            for configurationElement in configurationElements:

                nameElement = configurationElement.find('Name')
                platformElement = configurationElement.find('Platform')
                typeElement = configurationElement.find('Type')
                templateElement = configurationElement.find('Template')
                binDirElement = configurationElement.find('BinDir')
                libDirElement = configurationElement.find('LibDir')
                tempDirElement = configurationElement.find('TempDir')

                if nameElement is None: raise Exception('Name element is absent')
                if platformElement is None: raise Exception('Platform element is absent')
                if typeElement is None: raise Exception('Type element is absent')
                if templateElement is None: raise Exception('Template element is absent')
                if binDirElement is None: raise Exception('BinDir element is absent')
                if libDirElement is None: raise Exception('LibDir element is absent')
                if tempDirElement is None: raise Exception('TempDir element is absent')

                name = self.Import(nameElement.text)
                platform = self.Import(platformElement.text)
                type = self.Import(typeElement.text)

                try:
                    template = self.Import(templateElement.attrib['value'])
                except KeyError:
                    template = ''

                binDir = self.Import(binDirElement.text)
                libDir = self.Import(libDirElement.text)
                tempDir = self.Import(tempDirElement.text)

                config = ProjectConfiguration(name, platform, type, template, binDir, libDir, tempDir)

                targetElement = configurationElement.find('Target')
                if targetElement is not None:
                    config.SetTarget(self.Import(targetElement.text))

                for entry in templateElement.findall('Source'):
                    try:
                        templateName = self.Import(entry.attrib['name'])
                    except KeyError:
                        raise Exception('name attribute is absent')

                    try:
                        templateAdd = self.Import(entry.attrib['add'])
                    except KeyError:
                        templateAdd = ''

                    try:
                        templateRemove = self.Import(entry.attrib['remove'])
                    except KeyError:
                        templateRemove = ''

                    config.AddSourceTemplate(templateName, templateAdd, templateRemove)

                postBuildElement = configurationElement.find('Postbuild')
                if postBuildElement is not None:
                    config.SetPostbuild(self.Import(postBuildElement.text))
                
                preBuildElement = configurationElement.find('Prebuild')
                if preBuildElement is not None:
                    config.SetPrebuild(self.Import(preBuildElement.text))

                buildCmdElement = configurationElement.find('BuildCmd')
                rebuildCmdElement = configurationElement.find('RebuildCmd')
                cleanCmdElement = configurationElement.find('CleanCmd')

                if ((buildCmdElement is None) and (rebuildCmdElement is None) and (cleanCmdElement is None)):
                    # use automatic generation only if all three elements are undefined
                    config.SetBuildCmd(None)
                    config.SetRebuildCmd(None)
                    config.SetCleanCmd(None)

                else:
                    if buildCmdElement is not None:
                        config.SetBuildCmd(self.Import(buildCmdElement.text))

                    if rebuildCmdElement is not None:
                        config.SetRebuildCmd(self.Import(rebuildCmdElement.text))

                    if cleanCmdElement is not None:
                        config.SetCleanCmd(self.Import(cleanCmdElement.text))

                includeDirsElement = configurationElement.find('IncludeDirs')
                if includeDirsElement is not None:
                    for entry in includeDirsElement.findall('IncludeDir'):
                        config.AddIncludeDir(self.Import(entry.text))

                definesElement = configurationElement.find('Defines')
                if definesElement is not None:
                    for entry in definesElement.findall('Define'):
                        config.AddDefine(self.Import(entry.text))

                libraryDirsElement = configurationElement.find('LibraryDirs')
                if libraryDirsElement is not None:
                    for entry in libraryDirsElement.findall('LibraryDir'):
                        config.AddLibraryDir(self.Import(entry.text))

                libsElement = configurationElement.find('Libs')
                if libsElement is not None:
                    for entry in libsElement.findall('Lib'):
                        config.AddLib(self.Import(entry.text))

                        try:
                            isDelayLoadLib = (self.Import(entry.attrib['delayLoad']) == '1')
                        except KeyError:
                            isDelayLoadLib = False

                        if isDelayLoadLib:
                            config.AddDelayLoadLib(self.Import(entry.text))

                # add the configuration
                self.__configurations.append(config)

    def LoadFile(self, fileName):
        doc = xml.parse(fileName)
        rootElement = doc.getroot()
        self.Load(rootElement)

    def LoadString(self, string):
        rootElement = xml.XML(string)
        self.Load(rootElement)
