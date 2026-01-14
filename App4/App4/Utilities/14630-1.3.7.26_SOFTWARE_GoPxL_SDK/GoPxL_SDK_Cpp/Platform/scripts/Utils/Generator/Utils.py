#!/usr/bin/python
# This file is part of the FireSync Project Generator.
# Copyright (C) 2008-2016 by LMI Technologies Inc.  All rights reserved.
# Distributed under the terms of the MIT License.
# Redistributed files must retain the above copyright notice.

import codecs
import re
import sys
import os
import filecmp
import shutil
import ctypes
import platform
import sys

class EnvironmentStyle:
    Percent     = 0
    Dollar      = 1

def SepToNative(str):
    if os.path.sep == '/':
        return str.replace('\\', '/')
    else:
        return str.replace('/', '\\')

def Format(text, environmentStyle = EnvironmentStyle.Dollar, sepIsForward = True):

    if sepIsForward:
        text = text.replace('\\', '/')
    else:
        text = text.replace('/', '\\')

    if environmentStyle == EnvironmentStyle.Percent:
        text = re.sub(r'\#\[(.*?)\]+', r'%\1%', text)
    elif environmentStyle == EnvironmentStyle.Dollar:
        text = re.sub(r'\#\[(.*?)\]+', r'$(\1)', text)

    return text

def EnvInt(inStr):
    try:
        intVal = int(os.environ[inStr])
    except KeyError:
        return -1
    except ValueError:
        return -2
    else:
        return intVal

class Output:
    def __init__(self, fileName, environmentStyle = EnvironmentStyle.Dollar, sepIsForward = True, forceUpdate = False):
        self.fileName = fileName
        self.stream = codecs.open(fileName + '.tmp', 'w', encoding='utf-8')
        self.environmentStyle = environmentStyle
        self.sepIsForward = sepIsForward
        self.consoleOut = ConsoleOut()
        self.forceUpdate = forceUpdate

    def __enter__(self):
        return self

    def __exit__(self, *args):
        self.stream.close()

        if (os.path.exists(self.fileName) and filecmp.cmp(self.fileName, self.fileName + '.tmp')):
            os.remove(self.fileName + '.tmp')
            if (self.forceUpdate):
                os.utime(self.fileName, None)
        else:
            shutil.move(self.fileName + '.tmp', self.fileName)
            self.consoleOut.Print(self.fileName)

    def Write(self, text):
        self.stream.write(text)

    def WriteBom(self):
        if sys.hexversion >= 0x3000000:
            self.Write(str(codecs.BOM_UTF8, 'utf-8'))
        else:
            self.Write(unicode(codecs.BOM_UTF8, 'utf-8'))

    def Format(self, text, format = True):
        if format:
            self.Write(Format(text, self.environmentStyle, self.sepIsForward))
        else:
            self.Write(text)

class ConsoleOut:

    class Colors:

        Normal  = 0
        Gray    = 1
        Red     = 2
        Green   = 3
        Yellow  = 4
        Blue    = 5
        Purple  = 6
        Cyan    = 7
        White   = 8

    class __ConsoleOut:

        def InitConsole(self):

            if self.__file.isatty():

                if platform.system() == 'Windows':
                    try:
                        kernel32 = ctypes.windll.kernel32
                    except AttributeError:
                        return False

                    if kernel32.SetConsoleMode(kernel32.GetStdHandle(-11), 7) == 1:
                        return True

                else:
                    return True

            return False

        def __init__(self):
            self.__file = sys.stdout
            self.__useColors = self.InitConsole()
            self.__color = ConsoleOut.Colors.Normal
            self.__indent = 0

    instance = None

    def __init__(self):

        if not ConsoleOut.instance:
            ConsoleOut.instance = ConsoleOut.__ConsoleOut()

        self.instance = ConsoleOut.instance

    def Indent(self):
        self.instance.__indent = self.instance.__indent + 2

    def Unindent(self):
        self.instance.__indent = self.instance.__indent - 2

    def SetColor(self, color):
        if self.instance.__useColors:
            self.instance.__color = color

    @property
    def Color(self):
        return self.instance.__color

    def Write(self, text):
        self.instance.__file.write(text + '\n')
        self.instance.__file.flush()

    def Print(self, text):

        # 
        # don't bother enhancing these; only a handful codes appear 
        # to produce the identical results across Windows/Linux.
        #
        if self.instance.__color == ConsoleOut.Colors.Gray:
            printText = '\033[30;1m' + self.instance.__indent * ' ' + text + '\033[0m'
        elif self.instance.__color == ConsoleOut.Colors.Red:
            printText = '\033[31;1m' + self.instance.__indent * ' ' + text + '\033[0m'
        elif self.instance.__color == ConsoleOut.Colors.Green:
            printText = '\033[32;1m' + self.instance.__indent * ' ' + text + '\033[0m'
        elif self.instance.__color == ConsoleOut.Colors.Yellow:
            printText = '\033[33;1m' + self.instance.__indent * ' ' + text + '\033[0m'
        elif self.instance.__color == ConsoleOut.Colors.Blue:
            printText = '\033[34;1m' + self.instance.__indent * ' ' + text + '\033[0m'
        elif self.instance.__color == ConsoleOut.Colors.Purple:
            printText = '\033[35;1m' + self.instance.__indent * ' ' + text + '\033[0m'
        elif self.instance.__color == ConsoleOut.Colors.Cyan:
            printText = '\033[36;1m' + self.instance.__indent * ' ' + text + '\033[0m'
        elif self.instance.__color == ConsoleOut.Colors.White:
            printText = '\033[37;1m' + self.instance.__indent * ' ' + text + '\033[0m'
        else:
            printText = self.instance.__indent * ' ' + text

        self.Write(printText)
