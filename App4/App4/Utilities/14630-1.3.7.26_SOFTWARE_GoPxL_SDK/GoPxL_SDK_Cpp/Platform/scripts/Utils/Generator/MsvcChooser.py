#!/usr/bin/python
# This file is part of the FireSync Project Generator.
# Copyright (C) 2008-2016 by LMI Technologies Inc.  All rights reserved.
# Distributed under the terms of the MIT License.
# Redistributed files must retain the above copyright notice.

import os
from . import MsvcBase

class Writer(MsvcBase.MsvcWriter):

    def __init__(self, platforms, projects):

        # choose Microsoft Visual Studio version based on environment
        try:
            envVersion = os.environ['K_MSVC_VERSION']
        except KeyError:
            envVersion = ''

        if envVersion == '2013':
            msvcVersion = MsvcBase.MsvcVersion.Msvc2013
        elif envVersion == '2015':
            msvcVersion = MsvcBase.MsvcVersion.Msvc2015
        elif envVersion == '2017':
            msvcVersion = MsvcBase.MsvcVersion.Msvc2017
        elif envVersion == '2019':
            msvcVersion = MsvcBase.MsvcVersion.Msvc2019
        elif envVersion == '2022':
            msvcVersion = MsvcBase.MsvcVersion.Msvc2022
        else:
            msvcVersion = MsvcBase.MsvcVersion.Msvc2022

        # no suffix for MsvcChooser: looks prettier in Visual Studio and won't collide with other plug-ins
        MsvcBase.MsvcWriter.__init__(self, platforms, projects, msvcVersion, 'MsvcChooser', '')
