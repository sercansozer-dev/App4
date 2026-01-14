#!/usr/bin/python
# This file is part of the FireSync Project Generator.
# Copyright (C) 2008-2015 by LMI Technologies Inc.  All rights reserved.
# Distributed under the terms of the MIT License.
# Redistributed files must retain the above copyright notice.

import argparse
import sys
import os

try:
    import Generator as generator
except ImportError:
    from . import Generator as generator
    
def WriteProject(solution, project, plugins = None, platforms = None, projects = None, maxThreads = None):

    generator.WriteProject(solution, project, plugins, platforms, projects, maxThreads)

def WriteSolution(input, plugins = None, platforms = None, projects = None, maxThreads = None):

    generator.WriteSolution(input, plugins, platforms, projects, maxThreads)

if __name__ == '__main__':

    parser = argparse.ArgumentParser(description='Generate Project files.')

    parser.add_argument('solution', help='solution XML file')
    parser.add_argument('--project', help='project name')
    parser.add_argument('--writers', help='comma separated list of writers')
    parser.add_argument('--platforms', help='comma separated list of platforms')
    parser.add_argument('--projects', help='comma separated list of project types')
    parser.add_argument('--max_threads', help='maximum number of worker threads')

    args = parser.parse_args()

    writers = None
    if args.writers is not None:
        writers = args.writers.split(',')

    platforms = None
    if args.platforms is not None:
        platforms = args.platforms.split(',')

    projects = None
    if args.projects is not None:
        projects = args.projects.split(',')

    maxThreads = None
    if args.max_threads is not None:
        maxThreads = int(args.max_threads)

    if args.project is not None:
        WriteProject(args.solution, args.project, writers, platforms, projects, maxThreads)
    else:
        WriteSolution(args.solution, writers, platforms, projects, maxThreads)
