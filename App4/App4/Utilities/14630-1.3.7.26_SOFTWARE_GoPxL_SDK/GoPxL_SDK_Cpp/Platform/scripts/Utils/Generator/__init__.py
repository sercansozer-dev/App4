#!/usr/bin/python
# This file is part of the FireSync Project Generator.
# Copyright (C) 2008-2016 by LMI Technologies Inc.  All rights reserved.
# Distributed under the terms of the MIT License.
# Redistributed files must retain the above copyright notice.

import sys
import os
import importlib
from . import Solution
from . import Project
from . import Utils

_plugins = [ 
    'MsvcChooser',
    'EclipseCdt_Linux_Arm64',
    'EclipseCdt_Linux_Arm7',
    'EclipseCdt_Linux_X64',
    #'EclipseCdt_Linux_X86',    # Disabled: not a prominent platform feature
    'GnuMk_Linux_Arm64',
    'GnuMk_Linux_Arm7',
    'GnuMk_Linux_X64',
    #'GnuMk_Linux_X86',         # Disabled: not a prominent platform feature
]

_platforms = [ 
    'Linux_Arm64',
    'Linux_Arm7',
    'Linux_X64',
    #'Linux_X86',               # Disabled: not a prominent platform feature
    #'Win32',                   # Disabled: not a prominent platform feature (anymore)
    'Win64',
    'Any',
    'Doc',
]

_projects = [ 
    'C/CPP',
    'CPP/CLI',
    'C#',
]

def LoadWriters(plugins = None, platforms = None, projects = None):

    if plugins is None:
        plugins = _plugins

    if platforms is None:
        platforms = _platforms

    if projects is None:
        projects = _projects

    writers = []
    for plugin in plugins:

        mod = importlib.import_module('.' + plugin, __package__)

        class_ = getattr(mod, 'Writer')
        writers.append(class_(platforms, projects))

    return writers

def RunWriter(writer, solution):

    consoleOut = Utils.ConsoleOut()
    projects = writer.SelectProjects(solution)
    if len(projects) != 0:

        consoleOut.SetColor(Utils.ConsoleOut.Colors.White)
        consoleOut.Print('%s' % (writer.Name))

        consoleOut.Indent()
        consoleOut.SetColor(Utils.ConsoleOut.Colors.Gray)
        consoleOut.Print('%s' % (solution.Name))

        consoleOut.Indent()
        for project in projects:

            consoleOut.SetColor(Utils.ConsoleOut.Colors.Gray)
            consoleOut.Print('%s' % (project.Name))

            consoleOut.Indent()
            consoleOut.SetColor(Utils.ConsoleOut.Colors.Purple)

            writer.WriteProject(project)

            consoleOut.Unindent()

        writer.WriteSolution(solution)
        consoleOut.Unindent()
        consoleOut.Unindent()
        consoleOut.Print('')

def RunWritersParalell(futureModule, maxThreads, writers, solution):

    with futureModule.ProcessPoolExecutor(max_workers=maxThreads) as executor:

        resultDict = {executor.submit(RunWriter, writer, solution): writer for writer in writers}

        for future in futureModule.as_completed(resultDict):
            if future.exception() is not None:
                raise future.exception()

def RunWriters(writers, solution):

    for writer in writers:
        RunWriter(writer, solution)

def WriteProject(input, project, plugins = None, platforms = None, projects = None, maxThreads = None):

    writers = LoadWriters(plugins, platforms, projects)
    solution = Solution.Solution(input)

    consoleOut = Utils.ConsoleOut()
    consoleOut.SetColor(Utils.ConsoleOut.Colors.Purple)

    for writer in writers:
        projects = writer.SelectProjects(solution)
        for projectIt in projects:
            if projectIt.Name == project:
                writer.WriteProjectEx(projectIt, forceUpdate=True)

def WriteSolution(input, plugins = None, platforms = None, projects = None, maxThreads = None):

    writers = LoadWriters(plugins, platforms, projects)
    solution = Solution.Solution(input)

    try:
        import concurrent.futures as futureModule
    except ImportError:
        maxThreads = 1
        futureModule = None
        pass

    if ((futureModule is None) or (maxThreads == 1)):
        RunWriters(writers, solution)
    else:
        RunWritersParalell(futureModule, maxThreads, writers, solution)
