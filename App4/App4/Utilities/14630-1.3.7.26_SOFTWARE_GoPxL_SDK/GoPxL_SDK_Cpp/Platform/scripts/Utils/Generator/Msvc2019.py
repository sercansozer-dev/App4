#!/usr/bin/python
# This file is part of the FireSync Project Generator.
# Copyright (C) 2008-2016 by LMI Technologies Inc.  All rights reserved.
# Distributed under the terms of the MIT License.
# Redistributed files must retain the above copyright notice.

from . import MsvcBase

class Writer(MsvcBase.MsvcWriter):

    def __init__(self, platforms, projects):
        MsvcBase.MsvcWriter.__init__(self, platforms, projects, MsvcBase.MsvcVersion.Msvc2019, 'Msvc2019', '-2019')
