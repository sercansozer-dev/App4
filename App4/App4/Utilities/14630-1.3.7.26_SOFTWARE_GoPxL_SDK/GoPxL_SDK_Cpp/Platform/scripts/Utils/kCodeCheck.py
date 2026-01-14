#!/usr/bin/python

import argparse
import os
import codecs
import io
import re
import collections

def EncodingHandler(fullName, filePath):

    try:
        with codecs.open(fullName, encoding='utf-8') as f:
            f.readlines()
    except UnicodeDecodeError:
        print('%s : warning : file is not encoded in UTF-8 or ANSI' % (filePath))


def AutoFormatHandler(fullName, filePath):

    with io.open(fullName) as f:
        linesToScan = f.readlines()

    kTryIndent = -1
    for lineIndex in range(0, len(linesToScan)):
        line = linesToScan[lineIndex]

        if kTryIndent > 0:
            kCatchResult = re.search(r'([\s]+)kCatch\b', line)
            kCatchIndent = -1 if kCatchResult is None else len(kCatchResult.group(1))

            kCatchExResult = re.search(r'([\s]+)kCatchEx\b', line)
            kCatchExIndent = -1 if kCatchExResult is None else len(kCatchExResult.group(1))

            kFinallyResult = re.search(r'([\s]+)kFinally\b', line)
            kFinallyIndent = -1 if kFinallyResult is None else len(kFinallyResult.group(1))

            kFinallyExResult = re.search(r'([\s]+)kFinallyEx\b', line)
            kFinallyExIndent = -1 if kFinallyExResult is None else len(kFinallyExResult.group(1))

            if kCatchIndent > 0 or kCatchExIndent > 0 or kFinallyIndent > 0 or kFinallyExIndent > 0:
                if ((kCatchIndent > 0 and kCatchIndent != kTryIndent) or 
                    (kCatchExIndent > 0 and kCatchExIndent != kTryIndent) or 
                    (kFinallyIndent > 0 and kFinallyIndent != kTryIndent) or 
                    (kFinallyExIndent > 0 and kFinallyExIndent != kTryIndent)):

                        print('%s(%u) : warning : Found incorrect kTry block indentation. Please disable Visual Studio auto-format.' % (filePath, lineIndex + 1))

                kTryIndent = -1

        else:
            searchResult = re.search(r'^([\s]+)kTry\b', line)
            kTryIndent = -1 if searchResult is None else len(searchResult.group(1))


def LineEndingsHandler(fullName, filePath):

    with open(fullName, 'rb') as f:
        contents = f.read()

    allEndings = (b'\r', b'\r\n', b'\n')

    counts: Dict[bytes, int] = collections.defaultdict(int)

    for line in contents.splitlines(True):
        for ending in allEndings:
            if line.endswith(ending):
                counts[ending] += 1
                break

    mixed = sum(bool(x) for x in counts.values()) > 1
    if (mixed):
        print('%s(%s) : warning : File has inconsistent line endings. Please fix.' % (filePath, '~'))


def TabsHandler(fullName, filePath):

    with io.open(fullName) as f:
        linesToScan = f.readlines()

    for lineIndex in range(0, len(linesToScan)):
        line = linesToScan[lineIndex]
        if len(line.split('\t')) != 1:
            print('%s(%u) : warning : tab character used' % (filePath, lineIndex + 1))


def TryBlocksHandler(fullName, filePath):

    with io.open(fullName) as f:
        linesToScan = f.readlines()

    inkTry = False
    for lineIndex in range(0, len(linesToScan)):
        line = linesToScan[lineIndex]

        inkTry |= (re.search('\\bkTry\\b', line) is not None)

        if inkTry:

            if ((re.search('\\bkCheck\\b', line) is not None) or \
                (re.search('\\bkCheckArgs\\b', line) is not None) or \
                (re.search('\\bkCheckTrue\\b', line) is not None) or \
                (re.search('\\bkCheckState\\b', line) is not None)):

                    print('%s(%u) : warning : kCheck used within a kTry block' % (filePath, lineIndex + 1))

            if (re.search('\\breturn\\b\\s+[a-zA-Z0-9_\\->]+;', line) is not None):

                    print('%s(%u) : warning : return used within a kTry block' % (filePath, lineIndex + 1))

            if (re.search('kThrow\\(kOK\\)', line) is not None):

                    print('%s(%u) : warning : throwing kOK is not portable, please revise' % (filePath, lineIndex + 1))

            if ((re.search('\\bkCatch\\b', line) is not None) or \
                (re.search('\\bkCatchEx\\b', line) is not None) or \
                (re.search('\\bkFinally\\b', line) is not None) or \
                (re.search('\\bkFinallyEx\\b', line) is not None)):

                    inkTry = False


def SepToNative(str):
    if os.path.sep == '/':
        return str.replace('\\', '/')
    else:
        return str.replace('/', '\\')


def CheckFormatImpl(path, handler, directoryExcludes = None, fileExcludes = None):

    excludedDirectories = []
    excludedFiles = []

    if directoryExcludes is not None:
        with io.open(directoryExcludes) as f:
            excludedDirectories = [ os.path.join(path, SepToNative(line.rstrip())) for line in f.readlines() ]

    if fileExcludes is not None:
        with io.open(fileExcludes) as f:
            excludedFiles = [ os.path.join(path, SepToNative(line.rstrip())) for line in f.readlines() ]

    for root, dirNames, fileNames in os.walk(path, topdown=True):

        dirNames[:] = [d for d in dirNames if os.path.join(root, d) not in excludedDirectories] 

        for fileName in fileNames:
            if (fileName.endswith('.c') or
                fileName.endswith('.cpp') or
                fileName.endswith('.cs') or
                fileName.endswith('.cu') or
                fileName.endswith('.h') or
                fileName.endswith('.hpp')):

                if os.path.join(root, fileName) not in excludedFiles:

                    relPath = os.path.relpath(os.path.join(root, fileName), path)

                    handler(os.path.join(root, fileName), relPath)


def CheckFormat(path, handlerType = None, directoryExcludes = None, fileExcludes = None):

    codeCheckHandlers = {
        'encoding':     EncodingHandler,
        'auto-format':  AutoFormatHandler,
        'line-endings': LineEndingsHandler,
        'tabs':         TabsHandler,
        'try-blocks':   TryBlocksHandler,
    }

    if handlerType is not None:
        CheckFormatImpl(path, codeCheckHandlers[handlerType], directoryExcludes, fileExcludes)
    else:
        for handler in codeCheckHandlers:
            CheckFormatImpl(path, codeCheckHandlers[handler], directoryExcludes, fileExcludes)


if __name__ == '__main__':

    parser = argparse.ArgumentParser(description='Perform code formatting check.')

    parser.add_argument('path', help='Directory to check')
    parser.add_argument('--type', help='Select specific code check: \'auto-format\', \'line-endings\', \'tabs\', \'try-blocks\'')
    parser.add_argument('--directory_excludes', help='Exclusion for whole directories')
    parser.add_argument('--file_excludes', help='Exclusion file')

    args = parser.parse_args()
    CheckFormat(args.path, args.type, args.directory_excludes, args.file_excludes)
