#!/usr/bin/python
# This file is part of the FireSync Project Generator.
# Copyright (C) 2008-2016 by LMI Technologies Inc.  All rights reserved.
# Distributed under the terms of the MIT License.
# Redistributed files must retain the above copyright notice.

from . import EclipseCdt
from . import GnuMk

class Writer(EclipseCdt.Writer):

    def __init__(self, platforms, projects):
        EclipseCdt.Writer.__init__(self, platforms, projects, GnuMk.Plugin.Linux_Arm7)
