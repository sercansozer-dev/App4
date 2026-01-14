#!/usr/bin/python
# This file is part of the FireSync Project Generator.
# Copyright (C) 2008-2016 by LMI Technologies Inc.  All rights reserved.
# Distributed under the terms of the MIT License.
# Redistributed files must retain the above copyright notice.

from . import Utils

class Attribute:

    def __init__(self, name, value, format = True, sepIsForward = True):
        self.__name = name

        if format:
            self.__value = Utils.Format(value, Utils.EnvironmentStyle.Dollar, sepIsForward)
        else:
            self.__value = value

    @property
    def Name(self):
        return self.__name

    @property
    def Value(self):
        return self.__value

class Element:

    def __init__(self, name = None, value = None, format = True, sepIsForward = True):
        self.__name = name

        if value is not None and format:
            self.__value = Utils.Format(value, Utils.EnvironmentStyle.Dollar, sepIsForward)
        else:
            self.__value = value

        self.__sepIsForward = sepIsForward
        self.__attributes = []
        self.__children = []
        
    @property
    def Name(self):
        return self.__name

    @property
    def Value(self):
        return self.__value

    @property
    def SepIsForward(self):
        return self.__sepIsForward

    @property
    def Attributes(self):
        return self.__attributes

    @property
    def Children(self):
        return self.__children

    def AddElem(self, name = None, value = None, format = True):
        child = Element(name, value, format, self.__sepIsForward)
        self.__children.append(child)
        return child

    def AddAttr(self, name, value, format = True):
        attribute = Attribute(name, value, format, self.__sepIsForward)
        self.__attributes.append(attribute)

    def ToStr201x(self, elem, str = '', indent = 0):
        indentation = indent * '  '

        str += indentation + '<' + elem.__name

        for attr in elem.__attributes:
            str += ' ' + attr.Name + '="' + attr.Value + '"'

        if len(elem.__children):
            str += '>'

            for child in elem.__children:
                if child.__name is not None:
                    str = self.ToStr201x(child, str + '\r\n', indent + 1)

            str += '\r\n' + indentation + '</' + elem.__name + '>'

        else:
            if elem.__value is None:
                str += ' />'
            else:
                str += '>' + elem.__value + '</' + elem.__name + '>'

        return str

    def ToStrDotProject(self, elem, str = '', indent = 0):
        indentation = indent * '\t'

        str += indentation + '<' + elem.__name

        for attr in elem.__attributes:
            str += ' ' + attr.Name + '="' + attr.Value + '"'

        if len(elem.__children):
            str += '>'

            for child in elem.__children:
                if child.__name is not None:
                    str = self.ToStrDotProject(child, str + '\r\n', indent + 1)

            str += '\r\n' + indentation + '</' + elem.__name + '>'

        else:
            if elem.__value is None:
                str += ' />'
            else:
                str += '>' + elem.__value + '</' + elem.__name + '>'

        return str

    def ToStrDotCProject(self, elem, str = '', indent = 0):
        indentation = indent * '\t'

        str += indentation + '<' + elem.__name

        for attr in elem.__attributes:
            str += ' ' + attr.Name + '="' + attr.Value + '"'

        if len(elem.__children):
            str += '>'

            for child in elem.__children:
                if child.__name is not None:
                    str = self.ToStrDotCProject(child, str + '\r\n', indent + 1)

            str += '\r\n' + indentation + '</' + elem.__name + '>'

        else:
            if elem.__value is None:
                str += '/>'
            else:
                str += '>' + elem.__value + '</' + elem.__name + '>'

        return str

    def ToStrDotWrProject(self, elem, str = '', indent = 0):
        indentation = indent * '    '

        str += indentation + '<' + elem.__name

        for attr in elem.__attributes:
            str += ' ' + attr.Name + '="' + attr.Value + '"'

        if len(elem.__children):
            str += '>'

            for child in elem.__children:
                if child.__name is not None:
                    str = self.ToStrDotWrProject(child, str + '\r\n', indent + 1)

            str += '\r\n' + indentation + '</' + elem.__name + '>'

        else:
            if elem.__value is None:
                str += '/>'
            else:
                str += '>' + elem.__value + '</' + elem.__name + '>'

        return str

    def Write201x(self, elem, fileName, forceUpdate):
        with Utils.Output(fileName, forceUpdate=forceUpdate) as output:
            output.WriteBom()
            output.Write('<?xml version="1.0" encoding="utf-8"?>\r\n')
            output.Write(self.ToStr201x(elem))

    def WriteDotProject(self, elem, fileName):
        with Utils.Output(fileName) as output:
            output.Write('<?xml version="1.0" encoding="UTF-8"?>\r\n')
            output.Write(self.ToStrDotProject(elem))

    def WriteDotCProject(self, elem, fileName, workBench, eclipseCdtVanilla=False):
        with Utils.Output(fileName) as output:
            output.Write('<?xml version="1.0" encoding="UTF-8" standalone="no"?>\r\n')
            output.Write('<?fileVersion 4.0.0?>')

            if workBench:
                output.Write('\r\n\r\n')

            output.Write(self.ToStrDotCProject(elem))

            if eclipseCdtVanilla:
                output.Write('\r\n')

    def WriteDotWrProject(self, elem, fileName):
        with Utils.Output(fileName) as output:
            output.Write('<?xml version="1.0" encoding="UTF-8" standalone="no"?>\r\n')
            output.Write(self.ToStrDotWrProject(elem))

