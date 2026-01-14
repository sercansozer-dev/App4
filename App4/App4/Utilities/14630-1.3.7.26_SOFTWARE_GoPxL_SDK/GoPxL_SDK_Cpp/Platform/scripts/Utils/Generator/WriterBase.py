#!/usr/bin/python
# This file is part of the FireSync Project Generator.
# Copyright (C) 2008-2016 by LMI Technologies Inc.  All rights reserved.
# Distributed under the terms of the MIT License.
# Redistributed files must retain the above copyright notice.

import xml.etree.ElementTree as xml
from . import Project
from . import Solution

class WriterBase:
    def __init__(self, platforms, userPlatforms, projects, userProjects):

        self.__platforms = [ platform for platform in platforms if platform in userPlatforms ]
        self.__projects = [ project for project in projects if project in userProjects ]

    def SelectPlatforms(self, project):
        output = []

        for config in self.SelectConfigurations(project):
            if config.Platform in self.__platforms:
                if config.Platform not in output:
                    output.append(config.Platform)

        return output

    def SelectDependencies(self, project):
        deps = []

        for dep in project.Dependencies:
            if len(self.SelectConfigurations(dep)) != 0:
                deps.append(dep)

        return deps

    def SelectProjectsHelper(self, parent, projects):

        for project in parent.Projects:
            if len(self.SelectConfigurations(project)) != 0 and (project.Type == '' or (project.Type in self.__projects)):
                projects.append(project)

        for group in parent.Groups:
            self.SelectProjectsHelper(group, projects)

    def SelectProjects(self, solution):
        projects = []

        self.SelectProjectsHelper(solution.ProjectGroup, projects)

        return projects

    def SelectProjectGroupsHelper(self, parent, parentGroup):
        hasProjects = False
        hasDeepProjects = False

        for project in parent.Projects:
            if len(self.SelectConfigurations(project)) != 0 and (project.Type == '' or (project.Type in self.__projects)):
                parentGroup.AddProject(project)
                hasProjects = True

        for group in parent.Groups:
            newGroup = Solution.ProjectGroup(group.Name)

            if self.SelectProjectGroupsHelper(group, newGroup):
                parentGroup.AddGroup(newGroup)
                hasDeepProjects = True

        return hasProjects or hasDeepProjects

    def SelectProjectGroups(self, solution):
        group = Solution.ProjectGroup('')

        self.SelectProjectGroupsHelper(solution.ProjectGroup, group)

        return group

    def SelectConfigurations(self, project):
        configurations = []

        for config in project.Configurations:
            if (config.Platform in self.__platforms):
                configurations.append(config)

        return configurations

    def SelectSourcesHelper(self, parent, sources):

        for src in parent.Sources:
            sources.append(src)

        for group in parent.Groups:
            self.SelectSourcesHelper(group, sources)

    def SelectSources(self, project):
        sources = []
        parent = project.SourceGroups
        self.SelectSourcesHelper(parent, sources)
        return sources
