#!/usr/bin/python
# This file is part of the FireSync Project Generator.
# Copyright (C) 2008-2016 by LMI Technologies Inc.  All rights reserved.
# Distributed under the terms of the MIT License.
# Redistributed files must retain the above copyright notice.

import xml.etree.ElementTree as xml
import os
from . import Project
from . import Utils

def SolutionName(str):
    return os.path.splitext(os.path.basename(Utils.SepToNative(str)))[0]

class ProjectGroup:
    def __init__(self, name):
        self.__name = name
        self.__projects = []
        self.__groups = []

    @property
    def Name(self):
        return self.__name

    def AddProject(self, project):
        self.__projects.append(project)

    @property
    def Projects(self):
        return self.__projects

    def AddGroup(self, group):
        self.__groups.append(group)

    @property
    def Groups(self):
        return self.__groups

class Solution:
    def __init__(self, name):
        self.__name = SolutionName(name)
        self.__fileName = os.path.realpath(Utils.SepToNative(name))
        self.__projectGroup = ProjectGroup('')

        self.LoadFile(self.__fileName)

    @property
    def Name(self):
        return self.__name

    @property
    def FileName(self):
        return self.__fileName

    @property
    def ProjectGroup(self):
        return self.__projectGroup

    def FindHelper(self, group, name):

        for childGroup in group.Groups:
            result = self.FindHelper(childGroup, name)
            if result is not None:
                return result

        for project in group.Projects:
            if project.Name == name:
                return project

        return None

    def Find(self, name):
        return self.FindHelper(self.__projectGroup, name)

    def LoadV5(self, root):
        projectElements = root.findall('Project')

        if projectElements is not None:
            # Initialize project cache
            for projectElement in projectElements:
                nameElement = projectElement.find('Name')
                if nameElement is None:
                    raise Exception('Name element is missing')

                project = Project.Project(os.path.join(os.path.dirname(self.__fileName), Utils.SepToNative(nameElement.text)), self)
                self.__projectGroup.AddProject(project)

            # Initialize dependencies
            for projectElement in projectElements:
                nameElement = projectElement.find('Name')
                if nameElement is None:
                    raise Exception('Name attribute is missing')

                project = self.Find(Project.ProjectName(nameElement.text))
                if project is None:
                    raise Exception('Unknown dependency')

                depsElement = projectElement.find('Dependencies')
                dependencies = []

                if depsElement is not None:
                    deps = depsElement.findall('Dependency')
                    if deps is not None:
                        for dep in deps:
                            dependencies.append(self.Find(Project.ProjectName(dep.text)))

                project.SetDependencies(dependencies)

    def IncludeImports(self, itemElement, itemOut, currentDirectory):

        if itemElement.tag == 'Import':

            try:
                nameAttrib = itemElement.attrib['name']
            except (KeyError):
                raise Exception('Name attribute is missing')

            importFileName = os.path.normpath(os.path.join(currentDirectory, Utils.SepToNative(nameAttrib)))
            importDoc = xml.parse(importFileName) 
            rootElement = importDoc.getroot()
            self.IncludeImports(rootElement, itemOut, os.path.dirname(importFileName))

        elif itemElement.tag == 'Solution':

            # Absorb all solution items
            solutionElements = itemElement.findall('*')

            for solutionElement in solutionElements:
                self.IncludeImports(solutionElement, itemOut, currentDirectory)

        else:

            # Import all other items
            itemAttributes = itemElement.attrib
            itemChildElements = itemElement.findall('*')

            itemElementOut = xml.Element(itemElement.tag)

            if len(itemChildElements) == 0:
                itemElementOut.text = itemElement.text

            for itemAttribute in itemAttributes.keys():

                # Special treatment for project to have its path converted
                if itemElement.tag == 'Project' and itemAttribute == 'name':
                    itemElementOut.attrib[itemAttribute] = os.path.join(currentDirectory, Utils.SepToNative(itemAttributes[itemAttribute]))
                else:
                    itemElementOut.attrib[itemAttribute] = itemAttributes[itemAttribute]

            for itemChildElement in itemChildElements:
                self.IncludeImports(itemChildElement, itemElementOut, currentDirectory)

            itemOut.append(itemElementOut)

    def LoadGroups(self, group, element, initDeps = False):

        projectGroupElements = element.findall('ProjectGroup')

        if projectGroupElements is not None:
            for projectGroupElement in projectGroupElements:
                try:
                    groupNameAttrib = projectGroupElement.attrib['name']
                except (KeyError):
                    raise Exception('Name attribute is missing')

                newGroup = ProjectGroup(groupNameAttrib)

                if not initDeps:
                    group.AddGroup(newGroup)

                self.LoadGroups(newGroup, projectGroupElement, initDeps)

        projectElements = element.findall('Project')

        if projectElements is not None:
            for projectElement in projectElements:
                try:
                    projectNameAttr = projectElement.attrib['name']
                except (KeyError):
                    raise Exception('Name attribute is missing')

                cachedProject = self.Find(Project.ProjectName(projectNameAttr))

                if not initDeps:

                    # Projects may be listed multiple times, only add new ones to hierarchy
                    if cachedProject is None:
                        project = Project.Project(Utils.SepToNative(projectNameAttr), self)
                        group.AddProject(project)

                else:
                    if cachedProject is None:
                        raise Exception('Unknown project ' + Project.ProjectName(projectNameAttr))

                    depElements = projectElement.findall('Dependency')
                    dependencies = cachedProject.Dependencies

                    for depElement in depElements:

                        try:
                            depNameAttrib = depElement.attrib['name']
                        except (KeyError):
                            raise Exception('Name attribute is missing')

                        depProject = self.Find(Project.ProjectName(depNameAttrib))

                        if depProject is None:
                            raise Exception('Unknown dependency ' + depNameAttrib)

                        dependencies.append(depProject)

                    cachedProject.SetDependencies(dependencies)

    def LoadV6(self, root):
        convertedRoot = xml.Element('Solution')
        self.IncludeImports(root, convertedRoot, os.path.dirname(self.__fileName))
        self.LoadGroups(self.__projectGroup, convertedRoot, False)
        self.LoadGroups(self.__projectGroup, convertedRoot, True)

    def Load(self, root):
        try:
            schemaVersion = root.attrib['version']
        except (KeyError):
            schemaVersion = '5'

        if schemaVersion == '5':
            return self.LoadV5(root)
        else:
            return self.LoadV6(root)

    def LoadFile(self, fileName):
        doc = xml.parse(fileName)
        rootElement = doc.getroot()
        self.Load(rootElement)

    def LoadString(self, string):
        rootElement = xml.XML(string)
        self.Load(rootElement)
