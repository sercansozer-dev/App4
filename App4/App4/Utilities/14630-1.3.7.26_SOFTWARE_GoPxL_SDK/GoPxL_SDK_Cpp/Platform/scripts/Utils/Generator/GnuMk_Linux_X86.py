#!/usr/bin/python
# This file is part of the FireSync Project Generator.
# Copyright (C) 2008-2016 by LMI Technologies Inc.  All rights reserved.
# Distributed under the terms of the MIT License.
# Redistributed files must retain the above copyright notice.

from . import GnuMk

class Writer(GnuMk.GnuMkWriter):

    def __init__(self, platforms, projects):
        GnuMk.GnuMkWriter.__init__(self, platforms, projects, GnuMk.Plugin.Linux_X86)
