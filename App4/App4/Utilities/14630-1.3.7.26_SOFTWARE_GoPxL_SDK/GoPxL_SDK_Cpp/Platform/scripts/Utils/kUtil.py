#!/usr/bin/python
# Copyright (C) 2008-2016 by LMI Technologies Inc.  All rights reserved.
# Distributed under the terms of the MIT License.
# Redistributed files must retain the above copyright notice.
# 
# This script emulates a set of GNU coreutils commands.
#
# Invoke this script as follows:
# python kUtil.py rm_f someFileToRemove someOtherFileToRemove
# python kUtil.py rm_rf someDirToRemove someOtherDirToRemove
# python kUtil.py mkdir_p some/dirs/to/create some/other/dirs/to/create
# python kUtil.py cp source destination
#
# Equivalent command for GNU coreutils (respectively):
# rm -f someFileToRemove someOtherFileToRemove
# rm -rf someDirToRemove someOtherDirToRemove
# mkdir -p some/dirs/to/create some/other/dirs/to/create
# cp -T source destination
#

import os
import stat
import errno
import shutil
import sys

def rm_f(args):

    for path in args:
        if (os.path.exists(path)):
            os.remove(path)

def rmtreeOnError(func, path, _):
    os.chmod(path, stat.S_IWRITE)
    func(path)

def rm_rf(args):

    for path in args:
        if (os.path.exists(path)):
            shutil.rmtree(path, onerror=rmtreeOnError)

def mkdir_p(args):

    for path in args:
        try:
            os.makedirs(path)
        except OSError as exc:
            if exc.errno == errno.EEXIST:
                pass
            else:
                raise

def cp(args):

    shutil.copy2(args[0], args[1])

if __name__ == '__main__':

    func = globals()[sys.argv[1]]
    func(sys.argv[2:])
