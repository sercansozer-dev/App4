#!/usr/bin/python
"""
GoPxL SDK Build System Generator

This script generates platform-specific build files for the GoPxL C++ SDK and samples.
It reads the SDK configuration from GoPxLSdk.xml and produces:
    - Visual Studio solution/project files (.sln, .vcxproj) for Windows (VS2017, VS2019, VS2022)
    - GNU Makefiles for Linux x64

Includes build configurations for the SDK library and all 18 sample applications.

Usage:
    python generate.py

The script should be run whenever:
    - Setting up the project for the first time
    - Adding or removing source files
    - Modifying build configurations
    - Updating project dependencies
"""

import os
import sys

def Generate():
    """
    Generate platform-specific build files from GoPxLSdk.xml configuration.

    Creates build files for multiple platforms and toolchains by invoking
    the kGenerator utility with appropriate platform and architecture settings.

    Raises:
        ImportError: If Platform.scripts.Utils.kGenerator cannot be imported
        IOError: If GoPxLSdk.xml configuration file is not found
    """
    herePath = os.path.dirname(os.path.realpath(__file__))
    workDir = os.path.normpath(os.path.join(herePath, '..', '..'))
    sys.path.append(workDir)

    print('Generating solution file...')
    import Platform.scripts.Utils.kGenerator as generator

    # Generate Visual Studio projects/solution for Windows.
    generator.WriteSolution(
        os.path.join(herePath, '..', 'GoPxLSdk.xml'),
        ['MsvcChooser', 'Msvc2017', 'Msvc2019', 'Msvc2022'],
        ['Win64']
    )

    # Generate Makefiles for Linux platforms.
    generator.WriteSolution(os.path.join(herePath, '..', 'GoPxLSdk.xml'),
        ['GnuMk_Linux_X64'],
        ['Linux_X64']
    )

if __name__ == '__main__':
    Generate()
